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