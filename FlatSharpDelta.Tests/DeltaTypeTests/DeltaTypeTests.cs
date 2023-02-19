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
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class DeltaTypeTests : IClassFixture<AssemblyFixture<DeltaTypeTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(DeltaTypeTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "DeltaTypeTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public DeltaTypeTests(AssemblyFixture<DeltaTypeTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void TableDeltaType_ReturnedFromGetDelta_AllFieldSettersProtected()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop1", 1);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();
            Type baseDeltaType = delta.NativeObject.GetType().BaseType;

            // Assert
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop1").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop2").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop3").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop3Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop4").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop4Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop5").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop5Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop6").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop6Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop7").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop8").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop8Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop9").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop10").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop10Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop11").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop11Delta").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop12").SetMethod.IsPublic);
            Assert.False(baseDeltaType.GetProperties().First(property => property.Name == "Prop12Delta").SetMethod.IsPublic);
        }

        [Fact]
        public void ReferenceStructDeltaType_ReturnedFromGetDelta_AllFieldSettersProtected()
        {
            // Arrange
            GeneratedType foo2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            foo2.SetProperty("Abc2", 2);

            // Act
            GeneratedDeltaType delta = foo2.GetDelta();
            Type baseDeltaType = delta.NativeObject.GetType().BaseType;

            // Assert
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Abc2").GetSetMethod());
        }

        [Fact]
        public void ReferenceStructDeltaType_ReturnedFromGetDelta_ListTypesAreIReadOnlyList()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop1", 1);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();
            Type baseDeltaType = delta.NativeObject.GetType().BaseType;

            // Assert
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop4").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop4Delta").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop6").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop6Delta").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop8").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop8Delta").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop10").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop10Delta").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop12").PropertyType.Name);
            Assert.Equal("IReadOnlyList`1", baseDeltaType.GetProperties().First(property => property.Name == "Prop12Delta").PropertyType.Name);
        }
    }
}