using System;
using CommandLine;

namespace FlatSharpDelta.Compiler
{
    public class CompilerOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input file(s).")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output folder.")]
        public string Output { get; set; }

        [Option('c', "compiler", Required = true, HelpText = "Path to the base FlatSharp compiler.")]
        public string Compiler { get; set; }
    }
}