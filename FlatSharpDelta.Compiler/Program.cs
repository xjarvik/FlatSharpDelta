using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CommandLine;
using FlatSharp;
using reflection;
using ExceptionDispatchInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo;

namespace FlatSharpDelta.Compiler
{
    public class Program
    {
        public static int Main(string[] args)
        {
            int exitCode = -1;

            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed(compilerOptions =>
            {
                try
                {
                    CompilerStartInfo compilerStartInfo = new CompilerStartInfo
                    {
                        InputFiles = GetInputFiles(compilerOptions.Input),
                        OutputDirectory = GetOutputDirectory(compilerOptions.Output),
                        BaseCompilerFile = compilerOptions.BaseCompiler != null ? GetBaseCompilerFile(compilerOptions.BaseCompiler) : GetIncludedBaseCompilerFile()
                    };

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        ChmodFakeFlatc();
                    }

                    RunCompiler(compilerStartInfo);

                    exitCode = 0;
                }
                catch (Exception exception)
                {
                    HandleException(exception, compilerOptions.DebugMode);
                }
            });

            return exitCode;
        }

        static FileInfo[] GetInputFiles(string _input)
        {
            return _input.Split(";").SelectMany(input =>
            {
                string sanitizedInputPath = GetSanitizedPath(input);
                string inputDirectory;
                string inputWildcard;

                if (PathIsDirectory(sanitizedInputPath))
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }

                int lastSlashIndex = sanitizedInputPath.LastIndexOf("/");

                if (lastSlashIndex == -1)
                {
                    inputDirectory = "./";
                    inputWildcard = sanitizedInputPath;
                }
                else if (!PathIsDirectory(sanitizedInputPath.Substring(0, lastSlashIndex)))
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }
                else
                {
                    inputDirectory = sanitizedInputPath.Substring(0, lastSlashIndex);
                    inputWildcard = sanitizedInputPath.Substring(lastSlashIndex + 1);
                }

                string[] files = Directory.GetFiles(inputDirectory, inputWildcard);

                if (files.Length < 1)
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }

                return files.Select(f => new FileInfo(f));
            }).ToArray();
        }

        static DirectoryInfo GetOutputDirectory(string output)
        {
            string sanitizedOutputPath = GetSanitizedPath(output);

            if (!PathIsDirectory(sanitizedOutputPath))
            {
                throw new FlatSharpDeltaException($"{output} is not a valid directory.");
            }

            return new DirectoryInfo(sanitizedOutputPath);
        }

        static FileInfo GetBaseCompilerFile(string baseCompiler)
        {
            string sanitizedBaseCompilerPath = GetSanitizedPath(baseCompiler);

            if (!File.Exists(sanitizedBaseCompilerPath))
            {
                throw new FlatSharpDeltaException($"{baseCompiler} is not a valid FlatSharp compiler.");
            }

            return new FileInfo(sanitizedBaseCompilerPath);
        }

        static FileInfo GetIncludedBaseCompilerFile()
        {
            return new FileInfo
            (
                Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "FlatSharp.Compiler", "FlatSharp.Compiler.dll")
            );
        }

        static string GetSanitizedPath(string path)
        {
            path = Regex.Replace(path, @"\\+", "/");

            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        static bool PathIsDirectory(string path)
        {
            try
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static void ChmodFakeFlatc()
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod"
                }
            };

            process.StartInfo.ArgumentList.Add("a+x");
            process.StartInfo.ArgumentList.Add(GetFakeFlatcPath());

            process.Start();
            process.WaitForExit();
        }

        static void HandleException(Exception exception, bool debugMode)
        {
            if (debugMode)
            {
                throw exception;
            }
            else if (exception is FlatSharpDeltaException)
            {
                Console.WriteLine(exception.Message);
            }
            else
            {
                Console.WriteLine(exception);
            }
        }

        static void RunCompiler(CompilerStartInfo startInfo)
        {
            string tempDirPath = Path.Combine(Path.GetTempPath(), $"flatsharpdeltacompiler_temp_{Guid.NewGuid():n}");
            Directory.CreateDirectory(tempDirPath);
            DirectoryInfo tempDir = new DirectoryInfo(tempDirPath);

            try
            {
                Parallel.ForEach(startInfo.InputFiles, inputFile =>
                {
                    byte[] originalSchemaBfbs = GetBfbs(inputFile, tempDir, out FileInfo bfbsFile);
                    Schema originalSchema = Schema.Serializer.Parse(originalSchemaBfbs);
                    Schema schemaWithDeltaTypes = DeltaSchemaFactory.GetSchemaWithDeltaTypes(originalSchema);

                    // FlatSharp only generates code for an object if the declaration_file value equals the name of the
                    // input file in which the object is declared.
                    schemaWithDeltaTypes.ReplaceMatchingDeclarationFiles
                    (
                        "//" + inputFile.Name,
                        "//" + Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs"
                    );

                    byte[] schemaWithDeltaTypesBfbs = new byte[Schema.Serializer.GetMaxSize(schemaWithDeltaTypes)];
                    Schema.Serializer.Write(schemaWithDeltaTypesBfbs, schemaWithDeltaTypes);
                    File.WriteAllBytes(bfbsFile.FullName, schemaWithDeltaTypesBfbs);

                    GenerateFlatSharpCode(startInfo.BaseCompilerFile, bfbsFile, startInfo.OutputDirectory); // these two
                    GenerateFlatSharpDeltaCode(originalSchema, inputFile, startInfo.OutputDirectory); // can run in parallel (await?)
                });

                /*Parallel.ForEach(namespaces, _namespace =>
                {
                    string fileName = _namespace.Replace('.', '_') + "_PredefinedTypes.bfbs";
                    FileInfo bfbsFile = new FileInfo(Path.Combine(tempDir.FullName, fileName));
                    Schema predefinedTypesSchema = PredefinedTypeFactory.GetPredefinedTypesSchema(_namespace, "//" + fileName);

                    byte[] predefinedTypesSchemaBfbs = new byte[Schema.Serializer.GetMaxSize(predefinedTypesSchema)];
                    Schema.Serializer.Write(predefinedTypesSchemaBfbs, predefinedTypesSchema);
                    File.WriteAllBytes(bfbsFile.FullName, predefinedTypesSchemaBfbs);

                    GenerateFlatSharpCode(startInfo.BaseCompilerFile, bfbsFile, startInfo.OutputDirectory);

                    string primitiveListTypesCode = CSharpSyntaxTree.ParseText(PrimitiveListTypesCodeWriter.WriteCode(_namespace))
                        .GetRoot().NormalizeWhitespace().ToFullString();

                    string codeFilePath = Path.Combine
                    (
                        startInfo.OutputDirectory.FullName,
                        _namespace.Replace('.', '_') + "_PredefinedTypes.flatsharpdelta.generated.cs"
                    );

                    File.WriteAllText(codeFilePath, primitiveListTypesCode);
                });*/
            }
            finally
            {
                Directory.Delete(tempDirPath, true);
            }
        }

        static byte[] GetBfbs(FileInfo inputFile, DirectoryInfo outputDirectory, out FileInfo bfbsFile)
        {
            int exitCode = RunFlatc(new string[]
            {
                "-b",
                "--schema",
                "--bfbs-comments",
                "--bfbs-builtins",
                "--bfbs-filenames", inputFile.DirectoryName,
                "--no-warnings",
                "-o", outputDirectory.FullName,
                inputFile.FullName
            },
            out string consoleOutput);

            if (exitCode != 0)
            {
                throw new FlatSharpDeltaException
                (
                    "FlatSharpDelta compiler was interrupted because flatc returned an error. Output displayed below.\n\n" +
                    consoleOutput
                );
            }

            string bfbsFilePath = Path.Combine
            (
                outputDirectory.FullName,
                Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs"
            );

            bfbsFile = new FileInfo(bfbsFilePath);

            byte[] bfbs = File.ReadAllBytes(bfbsFilePath);

            return bfbs;
        }

        static void GenerateFlatSharpCode(FileInfo baseCompilerFile, FileInfo bfbsFile, DirectoryInfo outputDirectory)
        {
            int exitCode = RunBaseCompiler(baseCompilerFile.FullName, new string[]
            {
                "-i", bfbsFile.FullName,
                "-o", outputDirectory.FullName,
                // We pass in "fake-flatc", which is just a shell script that copies the bfbs file (because we already have it).
                // It's quite a hack, but it works.
                "--flatc-path", "./" + GetFakeFlatcPath()
            },
            out string consoleOutput);

            if (exitCode != 0)
            {
                throw new FlatSharpDeltaException
                (
                    "FlatSharpDelta compiler was interrupted because the base FlatSharp compiler returned an error. Output displayed below.\n\n" +
                    consoleOutput
                );
            }
        }

        static void GenerateFlatSharpDeltaCode(Schema originalSchema, FileInfo declarationFile, DirectoryInfo outputDirectory)
        {
            string code = CSharpSyntaxTree.ParseText(SchemaCodeWriter.WriteCode(originalSchema, declarationFile))
                .GetRoot().NormalizeWhitespace().ToFullString();

            string codeFilePath = Path.Combine
            (
                outputDirectory.FullName,
                Path.GetFileName(declarationFile.Name) + ".flatsharpdelta.generated.cs"
            );

            File.WriteAllText(codeFilePath, code);
        }

        static int RunFlatc(string[] args, out string consoleOutput)
        {
            string os;
            string name;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "windows";
                name = "flatc.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                os = "macos";
                name = "flatc";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = "linux";
                name = "flatc";
            }
            else
            {
                throw new FlatSharpDeltaException("FlatSharpDelta compiler is not supported on this operating system.");
            }

            string currentProcess = typeof(Program).Assembly.Location;
            string currentDirectory = Path.GetDirectoryName(currentProcess);
            string flatcPath = Path.Combine(currentDirectory, "FlatSharp.Compiler", "flatc", os, name);

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = flatcPath,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            foreach (string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            var tempOutput = new List<string>();
            process.OutputDataReceived += (_, args) => { tempOutput.Add(args.Data); };
            process.ErrorDataReceived += (_, args) => { tempOutput.Add(args.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            consoleOutput = String.Join("\n", tempOutput);

            return process.ExitCode;
        }

        static int RunBaseCompiler(string path, string[] args, out string consoleOutput)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            process.StartInfo.ArgumentList.Add(path);
            foreach (string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            var tempOutput = new List<string>();
            process.OutputDataReceived += (_, args) => { tempOutput.Add(args.Data); };
            process.ErrorDataReceived += (_, args) => { tempOutput.Add(args.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            consoleOutput = String.Join("\n", tempOutput);

            return process.ExitCode;
        }

        static string GetFakeFlatcPath()
        {
            string shell;
            string name;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd";
                name = "fake-flatc.cmd";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                shell = "bash";
                name = "fake-flatc.sh";
            }
            else
            {
                throw new FlatSharpDeltaException("FlatSharpDelta compiler is not supported on this operating system.");
            }

            string currentProcess = typeof(Program).Assembly.Location;
            string currentDirectory = Path.GetDirectoryName(currentProcess);

            // We need to return the relative path because running a script with "./" does not work if the path starts with
            // a Windows drive letter.
            return Path.GetRelativePath(currentDirectory, Path.Combine(currentDirectory, "fake-flatc", shell, name));
        }
    }
}
