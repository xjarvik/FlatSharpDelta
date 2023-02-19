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
    public class GetDeltaValueTests : IClassFixture<AssemblyFixture<GetDeltaValueTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(GetDeltaValueTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "GetDeltaValueTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public GetDeltaValueTests(AssemblyFixture<GetDeltaValueTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void GetDelta_Prop1WasSetToDifferentValue_DeltaContainsProperty()
        {
            // Arrange
            int propertyValue = 1;
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop1", propertyValue);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetProperty("Prop1"));
        }

        [Fact]
        public void GetDelta_Prop2WasSetToDifferentValue_DeltaContainsProperty()
        {
            // Arrange
            string propertyValue = "Hello";
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop2", propertyValue);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetProperty("Prop2"));
        }

        [Fact]
        public void GetDelta_Prop3WasSetToDifferentValue_DeltaContainsProperty()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType propertyValue = foo;
            bar.SetProperty("Prop3", propertyValue);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetProperty<GeneratedType>("Prop3"));
        }

        [Fact]
        public void GetDelta_PropertyInsideProp3WasSetToDifferentValue_DeltaContainsDeltaProperty()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop3", foo);
            bar.UpdateReferenceState();
            foo.SetProperty("Abc", 1);
            GeneratedDeltaType propertyValue = foo.GetDelta();

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetProperty<GeneratedDeltaType>("Prop3Delta"));
        }

        [Fact]
        public void GetDelta_Prop4WasSetToDifferentValue_DeltaContainsProperty()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedListType propertyValue = fooList;
            bar.SetProperty("Prop4", fooList);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetProperty<GeneratedListType>("Prop4"));
        }

        [Fact]
        public void GetDelta_ItemWasAddedToProp4_DeltaContainsListDeltaProperty()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop4", fooList);
            bar.UpdateReferenceState();
            fooList.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo"));
            IReadOnlyList<GeneratedListDeltaType> propertyValue = fooList.GetDelta();

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Equal(propertyValue, delta.GetListDeltaProperty("Prop4Delta"));
        }
    }
}