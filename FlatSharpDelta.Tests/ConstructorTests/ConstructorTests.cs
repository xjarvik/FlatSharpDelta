using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using FlatSharp;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class ConstructorTests : IClassFixture<AssemblyFixture<ConstructorTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(ConstructorTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "ConstructorTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public ConstructorTests(AssemblyFixture<ConstructorTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void EmptyConstructor_NewObject_SetPropertiesToDefaultValues()
        {
            // Act
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Assert
            Assert.Equal(100, bar.GetProperty("Prop1"));
            Assert.Null(bar.GetProperty("Prop2"));
            Assert.Null(bar.GetProperty("Prop3"));
            Assert.Null(bar.GetProperty("Prop4"));
            Assert.Null(bar.GetProperty("Prop5"));
            Assert.Null(bar.GetProperty("Prop6"));
            Assert.Null(bar.GetProperty("Prop7"));
            Assert.Null(bar.GetProperty("Prop8"));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val1"), bar.GetProperty("Prop9"));
            Assert.Null(bar.GetProperty("Prop10"));
            Assert.Null(bar.GetProperty("Prop11"));
            Assert.Null(bar.GetProperty("Prop12"));
        }

        [Fact]
        public void CopyConstructor_CopyPopulatedObject_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop1", 200);
            bar1.SetProperty("Prop2", "Hello");
            GeneratedType foo1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            foo1.SetProperty("Abc1", 1);
            bar1.SetProperty("Prop3", foo1);
            GeneratedListType foo1List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            GeneratedType foo1ListItem = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            foo1ListItem.SetProperty("Abc1", 1000);
            foo1List.Add(foo1ListItem);
            bar1.SetProperty("Prop4", foo1List);
            GeneratedType foo2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            foo2.SetProperty("Abc2", 2);
            bar1.SetProperty("Prop5", foo2);
            GeneratedListType foo2List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo2List");
            bar1.SetProperty("Prop6", foo2List);
            GeneratedType foo3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo3");
            foo3.SetField("Abc3", 3);
            bar1.SetProperty("Prop7", foo3);
            GeneratedListType foo3List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo3List");
            foo3List.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo3"));
            bar1.SetProperty("Prop8", foo3List);
            bar1.SetProperty("Prop9", GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val2"));
            GeneratedListType foo4List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo4List");
            foo4List.Add(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val3"));
            bar1.SetProperty("Prop10", foo4List);
            GeneratedType foo1InUnion = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            foo1InUnion.SetProperty("Abc1", 10000);
            bar1.SetProperty("Prop11", new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo1InUnion));
            GeneratedListType foo5List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo5List");
            foo5List.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo5", new GeneratedType(GeneratedAssembly, "FooBar.Foo3")));
            bar1.SetProperty("Prop12", foo5List);

            // Act
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar", bar1);

            // Assert
            Assert.Equal(200, bar2.GetProperty("Prop1"));
            Assert.Equal("Hello", bar2.GetProperty("Prop2"));
            Assert.NotNull(bar2.GetProperty("Prop3"));
            Assert.Equal(1, bar2.GetProperty<GeneratedType>("Prop3").GetProperty("Abc1"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop4"));
            Assert.Equal(1000, bar2.GetProperty<GeneratedListType>("Prop4").GetIndexerProperty<GeneratedType>(0).GetProperty("Abc1"));
            Assert.NotNull(bar2.GetProperty("Prop5"));
            Assert.Equal(2, bar2.GetProperty<GeneratedType>("Prop5").GetProperty("Abc2"));
            Assert.NotNull(bar2.GetProperty("Prop6"));
            Assert.Empty(bar2.GetProperty<GeneratedListType>("Prop6"));
            Assert.NotNull(bar2.GetProperty("Prop7"));
            Assert.Equal(3, bar2.GetProperty<GeneratedType>("Prop7").GetField("Abc3"));
            Assert.NotNull(bar2.GetProperty("Prop8"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop8"));
            Assert.Equal(0, bar2.GetProperty<GeneratedListType>("Prop8").GetIndexerProperty<GeneratedType>(0).GetField("Abc3"));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val2"), bar2.GetProperty("Prop9"));
            Assert.NotNull(bar2.GetProperty("Prop10"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop10"));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val3"), bar2.GetProperty<GeneratedListType>("Prop10").GetIndexerProperty(0));
            Assert.NotNull(bar2.GetProperty("Prop11"));
            Assert.Equal(10000, bar2.GetProperty<GeneratedType>("Prop11").GetProperty<GeneratedType>("MyFoo1").GetProperty("Abc1"));
            Assert.NotNull(bar2.GetProperty("Prop12"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop12"));
            Assert.Equal(0, bar2.GetProperty<GeneratedListType>("Prop12").GetIndexerProperty<GeneratedType>(0).GetProperty<GeneratedType>("MyFoo3").GetField("Abc3"));
        }

        [Fact]
        public void CopyConstructorList_CopyPopulatedList_ObjectReferencesNotEqual()
        {
            // Arrange
            GeneratedListType foo1List1 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List1.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));

            // Act
            GeneratedListType foo1List2 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List", foo1List1);

            // Assert
            Assert.NotEqual(foo1List1[0], foo1List2[0]);
        }

        [Fact]
        public void ShallowCopyList_CopyPopulatedList_ObjectReferencesEqual()
        {
            // Arrange
            GeneratedListType foo1List1 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List1.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));

            // Act
            GeneratedListType foo1List2 = GeneratedListType.ShallowCopy(GeneratedAssembly, "FooBar.Foo1List", foo1List1);

            // Assert
            Assert.Equal(foo1List1[0], foo1List2[0]);
        }

        [Fact]
        public void AsImmutableList_CopyPopulatedList_ObjectReferencesEqual()
        {
            // Arrange
            GeneratedListType foo1List1 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List1.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));

            // Act
            GeneratedListType foo1List2 = GeneratedListType.AsImmutable(GeneratedAssembly, "FooBar.Foo1List", foo1List1);

            // Assert
            Assert.Equal(foo1List1[0], foo1List2[0]);
        }

        [Fact]
        public void AsImmutableList_AccessPropertiesOfCustomListType_DoesNotThrow()
        {
            // Arrange
            GeneratedListType foo1List1 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List1.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));
            foo1List1.SetProperty("Capacity", 5);

            // Act
            GeneratedListType foo1List2 = GeneratedListType.AsImmutable(GeneratedAssembly, "FooBar.Foo1List", foo1List1);

            // Assert
            Assert.Equal(5, foo1List2.GetProperty("Capacity"));
            Assert.NotNull(foo1List1.GetDelta());
        }

        [Fact]
        public void AsImmutableList_ModifyList_Throws()
        {
            // Arrange
            GeneratedListType foo1List1 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List1.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));

            // Act
            GeneratedListType foo1List2 = GeneratedListType.AsImmutable(GeneratedAssembly, "FooBar.Foo1List", foo1List1);

            // Assert
            Assert.Single(foo1List2);
            TargetInvocationException e1 = Assert.Throws<TargetInvocationException>(() => { foo1List2.SetProperty("Capacity", 123); });
            Assert.IsType<NotMutableException>(e1.InnerException);
            TargetInvocationException e2 = Assert.Throws<TargetInvocationException>(() => { foo1List2.SetIndexerProperty(0, new GeneratedType(GeneratedAssembly, "FooBar.Foo1")); });
            Assert.IsType<NotMutableException>(e2.InnerException);
            TargetInvocationException e3 = Assert.Throws<TargetInvocationException>(() => { foo1List2.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1")); });
            Assert.IsType<NotMutableException>(e3.InnerException);
            TargetInvocationException e4 = Assert.Throws<TargetInvocationException>(() => { foo1List2.Clear(); });
            Assert.IsType<NotMutableException>(e4.InnerException);
            TargetInvocationException e5 = Assert.Throws<TargetInvocationException>(() => { foo1List2.Insert(0, new GeneratedType(GeneratedAssembly, "FooBar.Foo1")); });
            Assert.IsType<NotMutableException>(e5.InnerException);
            TargetInvocationException e6 = Assert.Throws<TargetInvocationException>(() => { foo1List2.Move(0, 1); });
            Assert.IsType<NotMutableException>(e6.InnerException);
            TargetInvocationException e7 = Assert.Throws<TargetInvocationException>(() => { foo1List2.Remove(new GeneratedType(GeneratedAssembly, "FooBar.Foo1")); });
            Assert.IsType<NotMutableException>(e7.InnerException);
            TargetInvocationException e8 = Assert.Throws<TargetInvocationException>(() => { foo1List2.RemoveAt(0); });
            Assert.IsType<NotMutableException>(e8.InnerException);
            TargetInvocationException e9 = Assert.Throws<TargetInvocationException>(() => { foo1List2.ApplyDelta(null); });
            Assert.IsType<NotMutableException>(e9.InnerException);
            TargetInvocationException e10 = Assert.Throws<TargetInvocationException>(() => { foo1List2.UpdateReferenceState(); });
            Assert.IsType<NotMutableException>(e10.InnerException);
        }
    }
}