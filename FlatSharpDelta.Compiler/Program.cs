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
            CompilerOptions compilerOptions = null;
            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed<CompilerOptions>(c => compilerOptions = c);

            try
            {
                RunCompiler(new CompilerStartInfo
                {
                    InputFiles = GetInputFiles(compilerOptions.Input),
                    OutputDirectory = GetOutputDirectory(compilerOptions.Output),
                    CompilerFile = GetCompilerFile(compilerOptions.Compiler)
                });
            }
            catch(FlatSharpDeltaException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return -1;
            }

            return 0;
        }

        static string[] GetInputFiles(string input)
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
            return files.Select(f => GetSanitizedPath(Path.GetFullPath(f))).ToArray();
        }

        static string GetOutputDirectory(string output)
        {
            string sanitizedOutputPath = GetSanitizedPath(output);

            if(!PathIsDirectory(sanitizedOutputPath))
            {
                throw new FlatSharpDeltaException($"{output} is not a valid directory.");
            }

            return GetSanitizedPath(Path.GetFullPath(sanitizedOutputPath));
        }

        static string GetCompilerFile(string compiler)
        {
            string sanitizedCompilerPath = GetSanitizedPath(compiler);

            if(!File.Exists(sanitizedCompilerPath))
            {
                throw new FlatSharpDeltaException($"{compiler} is not a valid compiler.");
            }

            return GetSanitizedPath(Path.GetFullPath(sanitizedCompilerPath));
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
            
        }
    }
}
