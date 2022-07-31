using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class ApplyDeltaTests : GeneratedCodeTests
    {
        protected override string[] FbsFiles
        {
            get => new string[]
            {
                "ApplyDeltaTests.fbs"
            };
        }

        [Fact]
        public void ApplyDelta_DeltaIsNull_DoesNothing()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("Prop1", 1);
            bar.SetProperty("Prop2", "Hello");
            bar.SetProperty("Prop3", new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));

            // Act
            bar.ApplyDelta(null);

            // Assert
            Assert.Equal(1, bar.GetProperty("Prop1"));
            Assert.Equal("Hello", bar.GetProperty("Prop2"));
            Assert.NotNull(bar.GetProperty<GeneratedType>("Prop3").NativeObject);
        }
    }
}