using System;
using CommandLine;

namespace FlatSharpDelta.Compiler
{
    public class CompilerOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input file(s).")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory.")]
        public string Output { get; set; }

        [Option('c', "compiler", Required = false, HelpText = "Path to the base FlatSharp compiler.")]
        public string BaseCompiler { get; set; }

        [Option("debug", Default = false, Required = false, Hidden = true)]
        public bool DebugMode { get; set; }
    }
}