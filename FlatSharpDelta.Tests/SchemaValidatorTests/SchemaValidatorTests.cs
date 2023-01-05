using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class SchemaValidatorTests : IClassFixture<AssemblyFixture<SchemaValidatorTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(SchemaValidatorTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "SchemaValidatorTests.fbs"
                };
            }

            public bool CatchCompilerException
            {
                get => true;
            }
        }

        private Assembly GeneratedAssembly { get; set; }
        private Exception CompilerException { get; set; }

        public SchemaValidatorTests(AssemblyFixture<SchemaValidatorTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
            CompilerException = fixture.CompilerException;
        }

        [Fact]
        public void Compiler_SchemaWithUByteDefaultVectorType_ThrowsException()
        {
            // Assert
            Assert.NotNull(CompilerException);
            Assert.IsType<FlatSharpDeltaException>(CompilerException);
            Assert.Matches(@"Error on field Prop1 in FooBar.Bar: FlatSharpDelta does not support vectors of the Memory type, which is the default for ubyte vectors. Set the fs_vector attribute to IList instead.", CompilerException.Message);
        }

        [Fact]
        public void Compiler_SchemaWithNonIListVectorType_ThrowsException()
        {
            // Assert
            Assert.NotNull(CompilerException);
            Assert.IsType<FlatSharpDeltaException>(CompilerException);
            Assert.Matches(@"Error on field Prop2 in FooBar.Bar: FlatSharpDelta only supports vectors of the IList type.", CompilerException.Message);
        }

        [Fact]
        public void Compiler_SchemaWithUByteIListVectorType_DoesNotThrowException()
        {
            // Assert
            Assert.DoesNotMatch(@"Prop3", CompilerException.Message);
        }

        [Fact]
        public void Compiler_SchemaWithSortedVector_ThrowsException()
        {
            // Assert
            Assert.NotNull(CompilerException);
            Assert.IsType<FlatSharpDeltaException>(CompilerException);
            Assert.Matches(@"Error on field Prop4 in FooBar.Bar: FlatSharpDelta does not support sorted vectors.", CompilerException.Message);
        }
    }
}