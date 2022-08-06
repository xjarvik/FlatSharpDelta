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
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            GeneratedDeltaType delta = bar1.GetDelta();
            bar2.ApplyDelta(delta);

            // Assert
            Assert.Equal(1, bar2.GetProperty("Prop1"));
            Assert.Equal(248, bar2.GetProperty("Prop248"));
            Assert.Equal(249, bar2.GetProperty("Prop249"));
        }
    }
}