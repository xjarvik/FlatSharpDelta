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

            // Act
            bar.ApplyDelta(null);

            // Assert
            
        }
    }
}