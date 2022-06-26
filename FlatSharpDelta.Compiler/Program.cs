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
            }
            catch(FlatSharpDeltaException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return -1;
            }

            return 0;
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
            Directory.CreateDirectory("temp");

            foreach(FileInfo inputFile in startInfo.InputFiles)
            {
                RunFlatc(new string[]
                {
                    "-b",
                    "--schema",
                    "--bfbs-comments",
                    "--bfbs-builtins",
                    "--bfbs-filenames",
                    inputFile.DirectoryName,
                    "--no-warnings",
                    "-o",
                    "temp",
                    inputFile.FullName
                });

                FileInfo bfbsFile = new FileInfo("temp/" + Path.GetFileNameWithoutExtension(inputFile.Name) + ".bfbs");
                
                Schema originalSchema = Schema.Serializer.Parse(File.ReadAllBytes(bfbsFile.FullName));
                Schema baseSchema = BaseSchemaFactory.GetBaseSchema(originalSchema);

                File.Delete(bfbsFile.FullName);
            }
        }

        static void RunFlatc(string[] args)
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
        }

        static void RunBaseCompiler(string path, string[] args)
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
        }
    }
}
