﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Reflection;
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
        private static string CompilerVersion => typeof(Program).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
        private static string FlatSharpGeneratedFileName => "FlatSharp.generated.cs";
        private static string ModifiedFlatSharpGeneratedFileName => "ModifiedFlatSharp.generated.cs";
        private static string FlatSharpDeltaGeneratedFileName => "FlatSharpDelta.generated.cs";

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

                    if (compilerOptions.DebugMode || InputFilesChanged(compilerStartInfo.InputFiles, compilerStartInfo.OutputDirectory))
                    {
                        RunCompiler(compilerStartInfo);
                    }

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
            })
            .GroupBy(f => f.FullName)
            .Select(f => f.First())
            .ToArray();
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
            else if (exception is AggregateException)
            {
                foreach (Exception innerException in (exception as AggregateException).InnerExceptions)
                {
                    HandleException(innerException, debugMode);
                }
            }
            else
            {
                Console.WriteLine(exception);
            }
        }

        static bool InputFilesChanged(IEnumerable<FileInfo> inputFiles, DirectoryInfo outputDirectory)
        {
            string existingGeneratedFilePath = Path.Combine(outputDirectory.FullName, FlatSharpDeltaGeneratedFileName);
            string existingGeneratedFileContent = File.Exists(existingGeneratedFilePath) ? File.ReadAllText(existingGeneratedFilePath) : String.Empty;

            if (existingGeneratedFileContent.Contains($"Compiler version: {CompilerVersion}")
            && existingGeneratedFileContent.Contains($"Source hash: {GetSourceHash(inputFiles)}"))
            {
                return false;
            }

            return true;
        }

        static void RunCompiler(CompilerStartInfo startInfo)
        {
            string tempDirPath = Path.Combine(Path.GetTempPath(), $"flatsharpdeltacompiler_temp_{Guid.NewGuid():n}");
            Directory.CreateDirectory(tempDirPath);
            DirectoryInfo tempDir = new DirectoryInfo(tempDirPath);

            try
            {
                ConcurrentBag<FileInfo> bfbsFiles = new ConcurrentBag<FileInfo>();
                ConcurrentDictionary<FileInfo, Schema> originalSchemas = new ConcurrentDictionary<FileInfo, Schema>();
                ConcurrentBag<string> validationErrors = new ConcurrentBag<string>();

                Parallel.ForEach(startInfo.InputFiles, inputFile =>
                {
                    byte[] originalSchemaBfbs = GetBfbs(inputFile, tempDir, out FileInfo bfbsFile);
                    Schema originalSchema = Schema.Serializer.Parse(originalSchemaBfbs);
                    SchemaValidator.ValidateSchema(originalSchema).ToList().ForEach(e => validationErrors.Add(e));
                    Schema schemaWithDeltaTypes = DeltaSchemaFactory.GetSchemaWithDeltaTypes(originalSchema);

                    // FlatSharp only generates code for an object if the declaration_file value equals the name of the
                    // input file in which the object is declared.
                    schemaWithDeltaTypes.ReplaceMatchingDeclarationFiles
                    (
                        $"//{inputFile.Name}",
                        $"//{Path.GetFileNameWithoutExtension(inputFile.Name)}.bfbs"
                    );

                    byte[] schemaWithDeltaTypesBfbs = new byte[Schema.Serializer.GetMaxSize(schemaWithDeltaTypes)];
                    Schema.Serializer.Write(schemaWithDeltaTypesBfbs, schemaWithDeltaTypes);
                    File.WriteAllBytes(bfbsFile.FullName, schemaWithDeltaTypesBfbs);

                    bfbsFiles.Add(bfbsFile);
                    originalSchemas[inputFile] = originalSchema;
                });

                if (validationErrors.Count > 0)
                {
                    throw new FlatSharpDeltaException
                    (
                        "FlatSharpDelta validation failed. Errors outlined below.\n\n" +
                        String.Join("\n", validationErrors)
                    );
                }

                Schema builtInTypesSchema = BuiltInTypesSchemaFactory.GetBuiltInTypesSchema();
                byte[] builtInTypesSchemaBfbs = new byte[Schema.Serializer.GetMaxSize(builtInTypesSchema)];
                Schema.Serializer.Write(builtInTypesSchemaBfbs, builtInTypesSchema);
                string builtInTypesSchemaFilePath = Path.Combine(tempDirPath, BuiltInTypeFactory.BuiltInTypesBfbsFileName);
                File.WriteAllBytes(builtInTypesSchemaFilePath, builtInTypesSchemaBfbs);

                bfbsFiles.Add(new FileInfo(builtInTypesSchemaFilePath));

                GenerateFlatSharpCode(startInfo.BaseCompilerFile, bfbsFiles, tempDir);
                string generatedFlatSharpCode = File.ReadAllText(Path.Combine(tempDirPath, FlatSharpGeneratedFileName));
                string modifiedFlatSharpCode = FlatSharpCodeModifier.ModifyGeneratedCode(generatedFlatSharpCode, originalSchemas);
                File.WriteAllText(Path.Combine(startInfo.OutputDirectory.FullName, ModifiedFlatSharpGeneratedFileName), modifiedFlatSharpCode);

                GenerateFlatSharpDeltaCode(GetSourceHash(startInfo.InputFiles), originalSchemas, startInfo.OutputDirectory);
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

            string bfbsFilePath = Path.Combine(outputDirectory.FullName, Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs");

            bfbsFile = new FileInfo(bfbsFilePath);

            byte[] bfbs = File.ReadAllBytes(bfbsFilePath);

            return bfbs;
        }

        static string GetSourceHash(IEnumerable<FileInfo> files)
        {
            string sourceHash = String.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] sourceHashBytes = sha256.ComputeHash(File.ReadAllBytes(typeof(Program).Assembly.Location));

                foreach (FileInfo file in files.OrderBy(f => f.FullName))
                {
                    byte[] tempHash = sha256.ComputeHash(File.ReadAllBytes(file.FullName));
                    for (int i = 0; i < 32; i++)
                    {
                        sourceHashBytes[i] ^= tempHash[i];
                    }
                }

                sourceHash = Convert.ToBase64String(sourceHashBytes);
            }

            return sourceHash;
        }

        static void GenerateFlatSharpCode(FileInfo baseCompilerFile, IEnumerable<FileInfo> bfbsFiles, DirectoryInfo outputDirectory)
        {
            int exitCode = RunBaseCompiler(baseCompilerFile.FullName, new string[]
            {
                "-i", String.Join(';', bfbsFiles.Select(f => f.FullName)),
                "-o", outputDirectory.FullName,
                // We pass in "fake-flatc", which is just a shell script that copies the bfbs file (because we already have it).
                // It's quite a hack, but it works.
                "--flatc-path", "./" + GetFakeFlatcPath(),
                "--normalize-field-names", "false" // fix
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

        static void GenerateFlatSharpDeltaCode(string sourceHash, IDictionary<FileInfo, Schema> schemas, DirectoryInfo outputDirectory)
        {
            string code = String.Empty;

            code += SchemaCodeWriter.GetAutoGeneratedCommentAndUsages(CompilerVersion, sourceHash);
            code += BuiltInTypesSchemaCodeWriter.WriteCode();

            foreach (KeyValuePair<FileInfo, Schema> kvp in schemas)
            {
                FileInfo declarationFile = kvp.Key;
                Schema schema = kvp.Value;
                code += SchemaCodeWriter.WriteCode(schema, declarationFile);
            }

            string prettyCode = CSharpSyntaxTree.ParseText(code).GetRoot().NormalizeWhitespace().ToFullString();
            string codeFilePath = Path.Combine(outputDirectory.FullName, FlatSharpDeltaGeneratedFileName);

            File.WriteAllText(codeFilePath, prettyCode);
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
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    os = "macos_arm";
                    name = "flatc";
                }
                else
                {
                    os = "macos_intel";
                    name = "flatc";
                }
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
