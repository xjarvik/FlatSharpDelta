using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using FlatSharp;
using CommandLine;
using reflection;

namespace FlatSharpDelta.Compiler
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitCode = -1;

            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed<CompilerOptions>(compilerOptions =>
            {
                string[] inputFiles;
                string outputDirectory;

                try
                {
                    ProcessInput(compilerOptions.Input, out inputFiles);
                    ProcessOutput(compilerOptions.Output, out outputDirectory);
                }
                catch(FlatSharpDeltaException exception)
                {
                    Console.Error.WriteLine(exception.Message);
                    exitCode = -1;
                    return;
                }

                exitCode = 0;
            });

            return exitCode;
        }

        static void ProcessInput(string input, out string[] inputFiles)
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
            inputFiles = files.Select(f => GetSanitizedPath(Path.GetFullPath(f))).ToArray();
        }

        static void ProcessOutput(string output, out string outputDirectory)
        {
            string sanitizedOutputPath = GetSanitizedPath(output);

            if(!PathIsDirectory(sanitizedOutputPath))
            {
                throw new FlatSharpDeltaException($"{output} is not a valid directory.");
            }

            outputDirectory = GetSanitizedPath(Path.GetFullPath(sanitizedOutputPath));
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
                FileAttributes attributes = File.GetAttributes(path);
                if(attributes.HasFlag(FileAttributes.Directory))
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
    }
}
