using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class BaseTypeTests : IClassFixture<AssemblyFixture<BaseTypeTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(BaseTypeTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "BaseTypeTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public BaseTypeTests(AssemblyFixture<BaseTypeTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void BaseTableType_GeneratedTypeDerivesFrom_AllFieldSettersProtected()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            Type baseType = bar.NativeObject.GetType().BaseType;

            // Assert
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop1").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop2").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop3").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop4").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop5").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop6").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop7").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop8").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop9").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop10").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop11").GetSetMethod());
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Prop12").GetSetMethod());
        }

        [Fact]
        public void BaseReferenceStructType_GeneratedTypeDerivesFrom_AllFieldSettersProtected()
        {
            // Arrange
            GeneratedType foo2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            Type baseType = foo2.NativeObject.GetType().BaseType;

            // Assert
            Assert.Null(baseType.GetProperties().First(property => property.Name == "Abc2").GetSetMethod());
        }

        [Fact]
        public void SetBase_GeneratedUnionType_IsProtected()
        {
            // Arrange
            GeneratedType foo5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo5");
            Type type = foo5.NativeObject.GetType();

            // Assert
            Assert.Null(type.GetProperties().First(property => property.Name == "Base").GetSetMethod());
        }
    }
}