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