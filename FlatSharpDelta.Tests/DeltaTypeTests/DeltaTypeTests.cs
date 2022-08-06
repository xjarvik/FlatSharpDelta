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
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop1").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop2").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop3").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop3Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop4").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop4Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop5").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop5Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop6").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop6Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop7").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop8").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop8Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop9").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop10").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop10Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop11").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop11Delta").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop12").GetSetMethod());
            Assert.Null(baseDeltaType.GetProperties().First(property => property.Name == "Prop12Delta").GetSetMethod());
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
    }
}