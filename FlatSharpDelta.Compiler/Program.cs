using System;
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
        private static string CompilerVersion => FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
        private static string FlatSharpGeneratedFileName => "FlatSharp.generated.cs";
        private static string ModifiedFlatSharpGeneratedFileName => "ModifiedFlatSharp.generated.cs";
        private static string FlatSharpDeltaGeneratedFileName => "FlatSharpDelta.generated.cs";
        public static DirectoryInfo ExecutingDirectory => new DirectoryInfo(Path.GetDirectoryName(typeof(Program).Assembly.Location));

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
                        BaseCompilerFile = !String.IsNullOrWhiteSpace(compilerOptions.BaseCompiler) ? GetBaseCompilerFile(compilerOptions.BaseCompiler) : GetIncludedBaseCompilerFile(),
                        IncludesDirectories = !String.IsNullOrWhiteSpace(compilerOptions.Includes) ? GetIncludesDirectories(compilerOptions.Includes) : new DirectoryInfo[0],
                        NormalizeFieldNames = compilerOptions.NormalizeFieldNames.Value
                    };

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

        static DirectoryInfo[] GetIncludesDirectories(string includes)
        {
            return includes.Split(";").Select(include =>
            {
                string sanitizedIncludesPath = GetSanitizedPath(include);

                if (!PathIsDirectory(sanitizedIncludesPath))
                {
                    throw new FlatSharpDeltaException($"{includes} is not a valid directory.");
                }

                return new DirectoryInfo(sanitizedIncludesPath);
            })
            .GroupBy(d => d.FullName)
            .Select(d => d.First())
            .ToArray();
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
                Path.Combine(ExecutingDirectory.FullName, "FlatSharp.Compiler", "FlatSharp.Compiler.dll")
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

        static void RunCompiler(CompilerStartInfo startInfo)
        {
            string tempDirPath = Path.Combine(Path.GetTempPath(), $"flatsharpdeltacompiler_temp_{Guid.NewGuid():N}");
            DirectoryInfo tempDir = Directory.CreateDirectory(tempDirPath);

            try
            {
                ConcurrentDictionary<FileInfo, Guid> inputFileGuids = new ConcurrentDictionary<FileInfo, Guid>();
                ConcurrentDictionary<FileInfo, Schema> originalSchemas = new ConcurrentDictionary<FileInfo, Schema>();
                ConcurrentBag<FileInfo> schemaWithDeltaTypesBfbsFiles = new ConcurrentBag<FileInfo>();
                ConcurrentBag<string> validationErrors = new ConcurrentBag<string>();

                foreach (FileInfo inputFile in startInfo.InputFiles)
                {
                    inputFileGuids[inputFile] = Guid.NewGuid();
                }

                Parallel.ForEach(GroupFilesByUniqueNames(startInfo.InputFiles), fileGroup =>
                {
                    DirectoryInfo groupDir = Directory.CreateDirectory(Path.Combine(tempDirPath, Guid.NewGuid().ToString("N")));
                    GenerateBfbsFiles(fileGroup, groupDir, startInfo.IncludesDirectories);

                    Parallel.ForEach(groupDir.GetFiles("*.bfbs"), originalSchemaBfbsFile =>
                    {
                        FileInfo inputFile = fileGroup.Where(inputFile => Path.GetFileNameWithoutExtension(inputFile.FullName) == Path.GetFileNameWithoutExtension(originalSchemaBfbsFile.FullName)).First();

                        byte[] originalSchemaBfbs = File.ReadAllBytes(originalSchemaBfbsFile.FullName);
                        Schema originalSchema = Schema.Serializer.Parse(originalSchemaBfbs);

                        if (startInfo.NormalizeFieldNames)
                        {
                            originalSchema.NormalizeFieldNames();
                        }

                        originalSchemas[inputFile] = originalSchema;
                        SchemaValidator.ValidateSchema(originalSchema).ToList().ForEach(e => validationErrors.Add(e));

                        Schema schemaWithDeltaTypes = DeltaSchemaFactory.GetSchemaWithDeltaTypes(originalSchema);
                        string schemaWithDeltaTypesBfbsFilePath = Path.Combine(tempDirPath, $"{Guid.NewGuid():N}.bfbs");

                        // FlatSharp only generates code for an object if the declaration_file value equals the name of the file in which the object is declared.
                        schemaWithDeltaTypes.ForEachDeclarationFileProperty(property =>
                        {
                            FileInfo declarationFile = startInfo.InputFiles.Where(f => IDeclarationFilePropertyExtensions.GetDeclarationFileString(f.FullName, ExecutingDirectory.FullName) == property.declaration_file).FirstOrDefault();

                            if (declarationFile != null)
                            {
                                property.declaration_file = IDeclarationFilePropertyExtensions.GetDeclarationFileString(Path.Combine(tempDirPath, $"{inputFileGuids[declarationFile]:N}.bfbs"), startInfo.BaseCompilerFile.DirectoryName);
                            }
                        });

                        byte[] schemaWithDeltaTypesBfbs = new byte[Schema.Serializer.GetMaxSize(schemaWithDeltaTypes)];
                        Schema.Serializer.Write(schemaWithDeltaTypesBfbs, schemaWithDeltaTypes);
                        File.WriteAllBytes(schemaWithDeltaTypesBfbsFilePath, schemaWithDeltaTypesBfbs);
                        schemaWithDeltaTypesBfbsFiles.Add(new FileInfo(schemaWithDeltaTypesBfbsFilePath));
                    });
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
                string builtInTypesSchemaBfbsFilePath = Path.Combine(tempDirPath, BuiltInTypeFactory.BuiltInTypesBfbsFileName);
                File.WriteAllBytes(builtInTypesSchemaBfbsFilePath, builtInTypesSchemaBfbs);

                schemaWithDeltaTypesBfbsFiles.Add(new FileInfo(builtInTypesSchemaBfbsFilePath));

                GenerateFlatSharpFile(startInfo.BaseCompilerFile, CopyFakeFlatc(tempDir), schemaWithDeltaTypesBfbsFiles, tempDir);
                string generatedFlatSharpCode = File.ReadAllText(Path.Combine(tempDirPath, FlatSharpGeneratedFileName));
                string modifiedFlatSharpCode = FlatSharpCodeModifier.ModifyGeneratedCode(generatedFlatSharpCode, originalSchemas);
                File.WriteAllText(Path.Combine(startInfo.OutputDirectory.FullName, ModifiedFlatSharpGeneratedFileName), modifiedFlatSharpCode);

                GenerateFlatSharpDeltaFile(originalSchemas, startInfo.OutputDirectory);
            }
            finally
            {
                Directory.Delete(tempDirPath, true);
            }
        }

        // Splits files with the same name into separate lists. Needed because flatc outputs all bfbs files to a single folder, overwriting files with the same name.
        static List<List<FileInfo>> GroupFilesByUniqueNames(IEnumerable<FileInfo> files)
        {
            List<List<FileInfo>> groups = new List<List<FileInfo>>();

            foreach (FileInfo file in files)
            {
                List<FileInfo> group = groups.Where(g => !g.Any(f => f.Name == file.Name)).FirstOrDefault();
                if (group == null)
                {
                    group = new List<FileInfo>();
                    groups.Add(group);
                }
                group.Add(file);
            }

            return groups;
        }

        static void GenerateBfbsFiles(IEnumerable<FileInfo> inputFiles, DirectoryInfo outputDirectory, IEnumerable<DirectoryInfo> includesDirectories)
        {
            List<string> args = new List<string>
            {
                "-b",
                "--schema",
                "--bfbs-comments",
                "--bfbs-builtins",
                "--bfbs-filenames", ExecutingDirectory.FullName,
                "--no-warnings",
                "-o", outputDirectory.FullName
            };

            args.AddRange(includesDirectories.SelectMany(d => new string[] { "-I", d.FullName }));
            args.AddRange(inputFiles.Select(f => f.FullName));

            int exitCode = RunFlatc(args.ToArray(), out string consoleOutput);

            if (exitCode != 0)
            {
                throw new FlatSharpDeltaException
                (
                    "FlatSharpDelta compiler was interrupted because flatc returned an error. Output displayed below.\n\n" +
                    consoleOutput
                );
            }
        }

        static void GenerateFlatSharpFile(FileInfo baseCompilerFile, FileInfo fakeFlatcFile, IEnumerable<FileInfo> bfbsFiles, DirectoryInfo outputDirectory)
        {
            int exitCode = RunBaseCompiler(baseCompilerFile.FullName, new string[]
            {
                "-i", String.Join(';', bfbsFiles.Select(f => f.FullName)),
                "-o", outputDirectory.FullName,
                "--normalize-field-names", "false",

                // We pass in "fake-flatc", which is just a shell script that copies the bfbs files (because we already have them).
                // It's quite a hack, but it works.
                "--flatc-path", fakeFlatcFile.FullName
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

        static void GenerateFlatSharpDeltaFile(IDictionary<FileInfo, Schema> schemas, DirectoryInfo outputDirectory)
        {
            string code = String.Empty;

            code += SchemaCodeWriter.GetAutoGeneratedCommentAndUsages(CompilerVersion, GetSourceHash(schemas.Keys));
            code += BuiltInTypesSchemaCodeWriter.WriteCode();

            foreach (KeyValuePair<FileInfo, Schema> kvp in schemas)
            {
                FileInfo declarationFile = kvp.Key;
                Schema schema = kvp.Value;
                code += SchemaCodeWriter.WriteCode(schema, declarationFile, ExecutingDirectory);
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

            string flatcPath = Path.Combine(ExecutingDirectory.FullName, "FlatSharp.Compiler", "flatc", os, name);

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

        static FileInfo CopyFakeFlatc(DirectoryInfo targetDirectory)
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

            string currentFakeFlatcPath = Path.Combine(ExecutingDirectory.FullName, "fake-flatc", shell, name);
            string targetFakeFlatcPath = Path.Combine(targetDirectory.FullName, name);

            File.Copy(currentFakeFlatcPath, targetFakeFlatcPath);
            FileInfo targetFakeFlatcFile = new FileInfo(targetFakeFlatcPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ChmodAddExecute(targetFakeFlatcFile);
            }

            return targetFakeFlatcFile;
        }

        static void ChmodAddExecute(FileInfo file)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod"
                }
            };

            process.StartInfo.ArgumentList.Add("a+x");
            process.StartInfo.ArgumentList.Add(file.FullName);

            process.Start();
            process.WaitForExit();
        }
    }
}
