using System;
using CommandLine;

namespace FlatSharpDelta.Compiler
{
    class CompilerOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input file(s).")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory.")]
        public string Output { get; set; }

        [Option('c', "compiler", Required = true, HelpText = "Path to the base FlatSharp compiler.")]
        public string BaseCompiler { get; set; }
    }
}