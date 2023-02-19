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