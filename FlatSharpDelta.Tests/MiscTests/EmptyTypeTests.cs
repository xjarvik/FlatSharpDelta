using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class EmptyTypeTests : IClassFixture<AssemblyFixture<EmptyTypeTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(EmptyTypeTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "EmptyTypeTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public EmptyTypeTests(AssemblyFixture<EmptyTypeTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void Compiler_SchemaWithTypesContainingNoFields_GeneratesValidCSharpCode()
        {
        }
    }
}