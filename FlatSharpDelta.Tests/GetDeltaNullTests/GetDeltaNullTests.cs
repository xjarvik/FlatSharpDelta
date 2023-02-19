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
    public class GetDeltaNullTests : IClassFixture<AssemblyFixture<GetDeltaNullTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(GetDeltaNullTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "GetDeltaNullTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public GetDeltaNullTests(AssemblyFixture<GetDeltaNullTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void GetDelta_IntPropertyWasSetToDifferentValue_ReturnsNotNull()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            foo.SetProperty("Abc", 1);

            // Act
            GeneratedDeltaType delta = foo.GetDelta();

            // Assert
            Assert.NotNull(delta);
        }

        [Fact]
        public void GetDelta_IntPropertyWasSetToSameValue_ReturnsNull()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            foo.SetProperty("Abc", 0);

            // Act
            GeneratedDeltaType delta = foo.GetDelta();

            // Assert
            Assert.Null(delta);
        }

        [Fact]
        public void GetDelta_ObjectPropertyWasSetToDifferentValue_ReturnsNotNull()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFoo", foo);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
        }

        [Fact]
        public void GetDelta_PropertyInsideObjectPropertyWasSetToDifferentValue_ReturnsNotNull()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFoo", foo);
            bar.UpdateReferenceState();
            foo.SetProperty("Abc", 1);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
        }

        [Fact]
        public void GetDelta_AfterUpdateReferenceState_ReturnsNull()
        {
            // Arrange
            GeneratedType foo = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFoo", foo);
            bar.UpdateReferenceState();

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Null(delta);
        }

        [Fact]
        public void GetDelta_NothingChanged_ReturnsNull()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Null(delta);
        }
    }
}