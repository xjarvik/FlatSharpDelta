using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class IncludeTests : IClassFixture<AssemblyFixture<IncludeTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(IncludeTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "IncludeTests1.fbs",
                    "IncludeTests2.fbs",
                    "IncludeTests3.fbs",
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public IncludeTests(AssemblyFixture<IncludeTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void Compiler_SchemaWithIncludedTypesFromExternalFiles_GeneratesValidCSharpCode()
        {
        }
    }
}