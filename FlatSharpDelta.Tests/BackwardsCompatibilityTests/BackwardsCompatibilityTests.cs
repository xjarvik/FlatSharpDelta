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
    public class BackwardsCompatibilityTests : IClassFixture<AssemblyFixture<BackwardsCompatibilityTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(BackwardsCompatibilityTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "Version1.fbs",
                    "Version2.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public BackwardsCompatibilityTests(AssemblyFixture<BackwardsCompatibilityTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void ApplyDelta_ApplyVersion1OnVersion2_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType version1 = new GeneratedType(GeneratedAssembly, "FooBar.Version1");
            version1.SetProperty("Prop1", (byte)1);
            version1.SetProperty("Prop2", new GeneratedListType(GeneratedAssembly, "FlatSharpDelta.IntList"));
            version1.UpdateReferenceState();
            GeneratedType version2 = new GeneratedType(GeneratedAssembly, "FooBar.Version2");
            version2.SetProperty("Prop1", (byte)1);
            version2.SetProperty("Prop2", new GeneratedListType(GeneratedAssembly, "FlatSharpDelta.IntList"));
            version2.UpdateReferenceState();
            version1.SetProperty("Prop1", (byte)123);
            version1.GetProperty<GeneratedListType>("Prop2").Add(456);

            // Act
            byte[] bytes = GeneratedType.Serialize(version1.GetDelta(), true);
            version2.ApplyDelta(GeneratedType.Deserialize<GeneratedDeltaType>(GeneratedAssembly, "FooBar.Version2Delta", bytes));

            // Assert
            Assert.Equal((byte)123, version2.GetProperty("Prop1"));
            Assert.Single(version2.GetProperty<GeneratedListType>("Prop2"));
            Assert.Equal(456, version2.GetProperty<GeneratedListType>("Prop2").GetIndexerProperty(0));
        }

        [Fact]
        public void ApplyDelta_ApplyVersion2OnVersion1_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType version1 = new GeneratedType(GeneratedAssembly, "FooBar.Version1");
            version1.SetProperty("Prop1", (byte)1);
            version1.SetProperty("Prop2", new GeneratedListType(GeneratedAssembly, "FlatSharpDelta.IntList"));
            version1.UpdateReferenceState();
            GeneratedType version2 = new GeneratedType(GeneratedAssembly, "FooBar.Version2");
            version2.SetProperty("Prop1", (byte)1);
            version2.SetProperty("Prop2", new GeneratedListType(GeneratedAssembly, "FlatSharpDelta.IntList"));
            version2.SetProperty("Prop3", new GeneratedListType(GeneratedAssembly, "FlatSharpDelta.UByteList"));
            version2.SetProperty("Prop4", new GeneratedListType(GeneratedAssembly, "FooBar.FooList"));
            version2.UpdateReferenceState();
            version2.SetProperty("Prop1", (byte)123);
            version2.GetProperty<GeneratedListType>("Prop2").Add(456);

            // Act
            byte[] bytes = GeneratedType.Serialize(version2.GetDelta(), true);
            version1.ApplyDelta(GeneratedType.Deserialize<GeneratedDeltaType>(GeneratedAssembly, "FooBar.Version1Delta", bytes));

            // Assert
            Assert.Equal((byte)123, version1.GetProperty("Prop1"));
            Assert.Single(version1.GetProperty<GeneratedListType>("Prop2"));
            Assert.Equal(456, version1.GetProperty<GeneratedListType>("Prop2").GetIndexerProperty(0));
        }
    }
}