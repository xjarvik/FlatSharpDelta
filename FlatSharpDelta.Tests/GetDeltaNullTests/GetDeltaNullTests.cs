using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class GetDeltaNullTests : GeneratedCodeTests
    {
        protected override string[] FbsFiles
        {
            get => new string[]
            {
                "GetDeltaNullTests.fbs"
            };
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