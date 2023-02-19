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
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class DuplicateInputFilesTests : IClassFixture<AssemblyFixture<DuplicateInputFilesTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(DuplicateInputFilesTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "DuplicateInputFilesTests.fbs",
                    "DuplicateInputFilesTests.fbs",
                    "DuplicateInputFilesTests.fbs",
                    "DuplicateInputFilesTests.fbs",
                    "DuplicateInputFilesTests.fbs",
                    Path.Combine("NestedFolder", "DuplicateInputFilesTests.fbs")
                };
            }

            public bool CatchCompilerException
            {
                get => true;
            }
        }

        private Assembly GeneratedAssembly { get; set; }
        private Exception CompilerException { get; set; }

        public DuplicateInputFilesTests(AssemblyFixture<DuplicateInputFilesTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
            CompilerException = fixture.CompilerException;
        }

        [Fact]
        public void Compiler_DuplicateInputFiles_GeneratesValidCSharpCode()
        {
            // Assert
            Assert.Null(CompilerException);
        }

        [Fact]
        public void Compiler_SameFileNames_GeneratesCodeForBothFiles()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar1");
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar2");
        }
    }
}