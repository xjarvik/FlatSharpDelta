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
                    "DuplicateInputFilesTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public DuplicateInputFilesTests(AssemblyFixture<DuplicateInputFilesTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void Compiler_DuplicateInputFiles_GeneratesValidCSharpCode()
        {
        }
    }
}