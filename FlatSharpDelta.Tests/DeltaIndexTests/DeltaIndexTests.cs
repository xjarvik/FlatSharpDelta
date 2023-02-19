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
    public class DeltaIndexTests : IClassFixture<AssemblyFixture<DeltaIndexTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(DeltaIndexTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "DeltaIndexTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public DeltaIndexTests(AssemblyFixture<DeltaIndexTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void GetDelta_MoreThan256Fields_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop1", 1);
            bar1.SetProperty("Prop248", 248);
            bar1.SetProperty("Prop249", 249);
            bar1.SetProperty("Prop257", 257);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            GeneratedDeltaType delta = bar1.GetDelta();
            bar2.ApplyDelta(delta);

            // Assert
            Assert.Equal(1, bar2.GetProperty("Prop1"));
            Assert.Equal(248, bar2.GetProperty("Prop248"));
            Assert.Equal(249, bar2.GetProperty("Prop249"));
            Assert.Equal(257, bar2.GetProperty("Prop257"));
        }
    }
}