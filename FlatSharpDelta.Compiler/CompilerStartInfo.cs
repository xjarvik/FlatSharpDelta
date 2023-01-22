using System;
using System.IO;

namespace FlatSharpDelta.Compiler
{
    public class CompilerStartInfo
    {
        public FileInfo[] InputFiles { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public FileInfo BaseCompilerFile { get; set; }
        public DirectoryInfo[] IncludesDirectories { get; set; }
        public bool NormalizeFieldNames { get; set; }
    }
}