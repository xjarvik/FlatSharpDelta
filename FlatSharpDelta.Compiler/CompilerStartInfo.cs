using System;

namespace FlatSharpDelta.Compiler
{
    class CompilerStartInfo
    {
        public string[] InputFiles { get; set; }
        public string OutputDirectory { get; set; }
        public string CompilerFile { get; set; }
    }
}