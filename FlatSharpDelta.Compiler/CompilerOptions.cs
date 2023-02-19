/*
 * Copyright 2023 William Söder
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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

        [Option('c', "compiler", HelpText = "Path to the base FlatSharp compiler.")]
        public string BaseCompiler { get; set; }

        [Option('I', "includes", HelpText = "Search path(s) for the \"include\" statement.")]
        public string Includes { get; set; }

        [Option("normalize-field-names", Default = true, HelpText = "Transform snake_case and camelCase field names to PascalCase.")]
        public bool? NormalizeFieldNames { get; set; }

        [Option("nullable-warnings", Default = false, HelpText = "Emit full nullable annotations and enable warnings.")] // fix
        public bool? NullableWarnings { get; set; }

        [Option("gen-poolable", Hidden = false, Default = false, HelpText = "EXPERIMENTAL: Generate extra code to enable object pooling for allocation reductions.")] // fix
        public bool? GeneratePoolableObjects { get; set; }

        [Option("unity-assembly-path", HelpText = "Path to assembly (e.g. UnityEngine.dll) which enables Unity support.")] // fix
        public string UnityAssemblyPath { get; set; }

        [Option("debug", Default = false, Hidden = true)]
        public bool DebugMode { get; set; }
    }
}