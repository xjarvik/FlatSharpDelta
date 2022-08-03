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

        [Fact]
        public void ApplyDelta_DeltaContainsProp1_SetsProp1ToCorrectValue()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop1", 1);

            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(1, bar2.GetProperty("Prop1"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsTablesAndReferenceStructs_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop1", 1);
            bar1.SetProperty("Prop3", new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));
            GeneratedListType prop4 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            prop4.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));
            bar1.SetProperty("Prop4", prop4);
            bar1.SetProperty("Prop5", new GeneratedType(GeneratedAssembly, "FooBar.Foo2"));
            GeneratedListType prop6 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo2List");
            prop6.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo2"));
            GeneratedType prop6Value = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            prop6Value.SetProperty("Abc2", 2);
            prop6.Add(prop6Value);
            bar1.SetProperty("Prop6", prop6);

            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(1, bar2.GetProperty("Prop1"));
            Assert.NotNull(bar2.GetProperty("Prop3"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop4"));
            Assert.NotNull(bar2.GetProperty("Prop5"));
            Assert.Equal(2, bar2.GetProperty<GeneratedListType>("Prop6").Count);
            Assert.Equal(2, bar2.GetProperty<GeneratedListType>("Prop6").GetIndexerProperty<GeneratedType>(1).GetProperty("Abc2"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsValueStructs_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop7 = new GeneratedType(GeneratedAssembly, "FooBar.Foo3");
            prop7.SetField("Abc3", 3);
            bar1.SetProperty("Prop7", prop7);
            GeneratedListType prop8 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo3List");
            prop8.Add(prop7);
            bar1.SetProperty("Prop8", prop8);

            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(3, bar2.GetProperty<GeneratedType>("Prop7").GetField("Abc3"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop8"));
            Assert.Equal(3, bar2.GetProperty<GeneratedListType>("Prop8").GetIndexerProperty<GeneratedType>(0).GetField("Abc3"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsTableListDeltaWithModifiedListOperation_SetsListValueCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType foo1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            GeneratedListType foo1List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo1List");
            foo1List.Add(foo1);
            bar1.SetProperty("Prop4", foo1List);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            foo1.SetProperty("Abc1", 1);

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop4"));
            Assert.Equal(1, bar2.GetProperty<GeneratedListType>("Prop4").GetIndexerProperty<GeneratedType>(0).GetProperty("Abc1"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsEnums_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop9", GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val2"));
            GeneratedListType prop10 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo4List");
            bar1.SetProperty("Prop10", prop10);
            prop10.Add(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val3"));

            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", 2), bar2.GetProperty("Prop9"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop10"));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", 3), bar2.GetProperty<GeneratedListType>("Prop10").GetIndexerProperty(0));
        }
    }
}