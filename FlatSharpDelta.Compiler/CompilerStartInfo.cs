using System;
using System.IO;

namespace FlatSharpDelta.Compiler
{
    class CompilerStartInfo
    {
        public FileInfo[] InputFiles { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public FileInfo BaseCompilerFile { get; set; }
    }
}