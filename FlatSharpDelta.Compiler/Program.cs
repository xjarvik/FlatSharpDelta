using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CommandLine;
using FlatSharp;
using reflection;

namespace FlatSharpDelta.Compiler
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitCode = -1;

            try
            {
                Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed<CompilerOptions>(compilerOptions =>
                {
                    FileInfo baseCompilerFile = compilerOptions.BaseCompiler != null ?
                        GetBaseCompilerFile(compilerOptions.BaseCompiler) :
                        new FileInfo(Path.Combine
                        (
                            Path.GetDirectoryName(typeof(Program).Assembly.Location),
                            "FlatSharp.Compiler",
                            "FlatSharp.Compiler.dll"
                        ));

                    CompilerStartInfo compilerStartInfo = new CompilerStartInfo
                    {
                        InputFiles = GetInputFiles(compilerOptions.Input),
                        OutputDirectory = GetOutputDirectory(compilerOptions.Output),
                        BaseCompilerFile = baseCompilerFile
                    };

                    RunCompiler(compilerStartInfo);
                });
                
                exitCode = 0;
            }
            catch(FlatSharpDeltaException exception)
            {
                Console.WriteLine(exception.Message);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            return exitCode;
        }

        static FileInfo[] GetInputFiles(string _input)
        {
            return _input.Split(";").SelectMany(input =>
            {
                string sanitizedInputPath = GetSanitizedPath(input);
                string inputDirectory;
                string inputWildcard;

                if(PathIsDirectory(sanitizedInputPath))
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }
                
                int lastSlashIndex = sanitizedInputPath.LastIndexOf("/");
                if(lastSlashIndex == -1)
                {
                    inputDirectory = "./";
                    inputWildcard = sanitizedInputPath;
                }
                else if(!PathIsDirectory(sanitizedInputPath.Substring(0, lastSlashIndex)))
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }
                else
                {
                    inputDirectory = sanitizedInputPath.Substring(0, lastSlashIndex);
                    inputWildcard = sanitizedInputPath.Substring(lastSlashIndex + 1);
                }

                string[] files = Directory.GetFiles(inputDirectory, inputWildcard);
                if(files.Length < 1)
                {
                    throw new FlatSharpDeltaException($"{input} is not a valid path.");
                }
                return files.Select(f => new FileInfo(f));
            }).ToArray();
        }

        static DirectoryInfo GetOutputDirectory(string output)
        {
            string sanitizedOutputPath = GetSanitizedPath(output);

            if(!PathIsDirectory(sanitizedOutputPath))
            {
                throw new FlatSharpDeltaException($"{output} is not a valid directory.");
            }

            return new DirectoryInfo(sanitizedOutputPath);
        }

        static FileInfo GetBaseCompilerFile(string baseCompiler)
        {
            string sanitizedBaseCompilerPath = GetSanitizedPath(baseCompiler);

            if(!File.Exists(sanitizedBaseCompilerPath))
            {
                throw new FlatSharpDeltaException($"{baseCompiler} is not a valid FlatSharp compiler.");
            }

            return new FileInfo(sanitizedBaseCompilerPath);
        }

        static string GetSanitizedPath(string path)
        {
            path = Regex.Replace(path, @"\\+", "/");
            if(path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path;
        }

        static bool PathIsDirectory(string path)
        {
            try
            {
                if(File.GetAttributes(path).HasFlag(FileAttributes.Directory))
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

        static void RunCompiler(CompilerStartInfo startInfo)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"flatsharpdeltacompiler_temp_{Guid.NewGuid():n}");
            Directory.CreateDirectory(tempDir);

            try
            {
                Parallel.ForEach(startInfo.InputFiles, inputFile =>
                {
                    int flatcExitCode = RunFlatc(new string[]
                    {
                        "-b",
                        "--schema",
                        "--bfbs-comments",
                        "--bfbs-builtins",
                        "--bfbs-filenames", inputFile.DirectoryName,
                        "--no-warnings",
                        "-o", tempDir,
                        inputFile.FullName
                    });

                    if(flatcExitCode != 0)
                    {
                        throw new FlatSharpDeltaException
                        (
                            "FlatSharpDelta compiler was interrupted because flatc returned an error."
                        );
                    }

                    string bfbsFilePath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs");

                    Schema originalSchema = Schema.Serializer.Parse(File.ReadAllBytes(bfbsFilePath));
                    Schema deltaSchema = DeltaSchemaFactory.GetDeltaSchema(originalSchema);
                    
                    // FlatSharp only generates code for an object if the declaration_file value equals the name of the
                    // input file where the object is defined.
                    deltaSchema.ReplaceMatchingDeclarationFiles
                    (
                        "//" + inputFile.Name,
                        "//" + Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs"
                    );

                    byte[] deltaSchemaBfbs = new byte[Schema.Serializer.GetMaxSize(deltaSchema)];
                    Schema.Serializer.Write(deltaSchemaBfbs, deltaSchema);
                    File.WriteAllBytes(bfbsFilePath, deltaSchemaBfbs);

                    int baseCompilerExitCode = RunBaseCompiler(startInfo.BaseCompilerFile.FullName, new string[]
                    {
                        "-i", bfbsFilePath,
                        "-o", startInfo.OutputDirectory.FullName,
                        // We use a "fake" flatc that just copies the bfbs file (because we already have it).
                        // It's quite a hack, but it works.
                        "--flatc-path", "./" + GetFakeFlatcPath()
                    });

                    if(baseCompilerExitCode != 0)
                    {
                        throw new FlatSharpDeltaException
                        (
                            "FlatSharpDelta compiler was interrupted because the base FlatSharp compiler returned an error."
                        );
                    }

                    string schemaCode = CSharpSyntaxTree.ParseText
                    (
                        SchemaCodeWriter.WriteCode(originalSchema, inputFile)
                    )
                    .GetRoot().NormalizeWhitespace().ToFullString();

                    string schemaCodeFilePath = Path.Combine
                    (
                        startInfo.OutputDirectory.FullName,
                        Path.GetFileName(inputFile.Name) + ".generated.delta.cs"
                    );
                    File.WriteAllText(schemaCodeFilePath, schemaCode);
                });
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        static int RunFlatc(string[] args)
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
                    FileName = flatcPath
                }
            };

            foreach(string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }

        static int RunBaseCompiler(string path, string[] args)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet"
                }
            };

            process.StartInfo.ArgumentList.Add(path);
            foreach(string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();
            process.WaitForExit();

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
