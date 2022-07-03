using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

            CompilerOptions compilerOptions = null;
            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed<CompilerOptions>(c => compilerOptions = c);

            try
            {
                RunCompiler(new CompilerStartInfo
                {
                    InputFiles = GetInputFiles(compilerOptions.Input),
                    OutputDirectory = GetOutputDirectory(compilerOptions.Output),
                    BaseCompilerFile = GetBaseCompilerFile(compilerOptions.BaseCompiler)
                });
                exitCode = 0;
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            return exitCode;
        }

        static FileInfo[] GetInputFiles(string input)
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
            return files.Select(f => new FileInfo(f)).ToArray();
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
                foreach(FileInfo inputFile in startInfo.InputFiles)
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
                        throw new FlatSharpDeltaException(
                            "FlatSharpDelta compiler was interrupted because flatc returned an error."
                        );
                    }

                    string bfbsFilePath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs");

                    Schema originalSchema = Schema.Serializer.Parse(File.ReadAllBytes(bfbsFilePath));
                    Schema baseSchema = BaseSchemaFactory.GetBaseSchema(originalSchema);
                    
                    baseSchema.ReplaceMatchingDeclarationFiles(
                        "//" + inputFile.Name,
                        "//" + Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs"
                    );

                    byte[] baseBfbs = new byte[Schema.Serializer.GetMaxSize(baseSchema)];
                    Schema.Serializer.Write(baseBfbs, baseSchema);
                    File.WriteAllBytes(bfbsFilePath, baseBfbs);

                    int baseCompilerExitCode = RunBaseCompiler(startInfo.BaseCompilerFile.FullName, new string[]
                    {
                        "-i", bfbsFilePath,
                        "-o", startInfo.OutputDirectory.FullName,
                        "--flatc-path", "./" + GetFakeFlatcPath()
                    });

                    if(baseCompilerExitCode != 0)
                    {
                        throw new FlatSharpDeltaException(
                            "FlatSharpDelta compiler was interrupted because the base FlatSharp compiler returned an error."
                        );
                    }
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        static int RunFlatc(string[] args)
        {
            string path;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "flatc/flatc-windows.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = "flatc/flatc-macos";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                path = "flatc/flatc-linux";
            }
            else
            {
                throw new FlatSharpDeltaException("FlatSharpDelta compiler is not supported on this operating system.");
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "flatc/fake-flatc-windows.cmd";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "flatc/fake-flatc-unix.sh";
            }
            else
            {
                throw new FlatSharpDeltaException("FlatSharpDelta compiler is not supported on this operating system.");
            }
        }
    }
}
