using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class GetDeltaOptimizationTests : IClassFixture<AssemblyFixture<GetDeltaOptimizationTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(GetDeltaOptimizationTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "GetDeltaOptimizationTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public GetDeltaOptimizationTests(AssemblyFixture<GetDeltaOptimizationTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
        }

        [Fact]
        public void GetDelta_MyFooItemWasAddedThenRemoved_ReturnsNull()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            bar.UpdateReferenceState();
            GeneratedType listItem = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem);
            fooList.Remove(listItem);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Null(delta);
        }

        [Fact]
        public void GetDelta_MyFooItemWasAddedThenMoved_ReturnsOneListOperation()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            GeneratedType listItem1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            bar.UpdateReferenceState();
            GeneratedType listItem4 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem4);
            fooList.Move(3, 0);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
            Assert.Single(delta.GetProperty<GeneratedListType>("MyFooListDelta"));
        }

        [Fact]
        public void GetDelta_MyFooItemWasMovedTwice_ReturnsOneListOperation()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            GeneratedType listItem1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            bar.UpdateReferenceState();
            fooList.Move(2, 0);
            fooList.Move(0, 1);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
            Assert.Single(delta.GetProperty<GeneratedListType>("MyFooListDelta"));
        }

        [Fact]
        public void GetDelta_MyFooItemWasModifiedThenMoved_ReturnsTwoListOperations()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            GeneratedType listItem1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            bar.UpdateReferenceState();
            listItem3.SetProperty("Abc", 123);
            fooList.Move(2, 0);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
            Assert.Equal(2, delta.GetProperty<GeneratedListType>("MyFooListDelta").Count);
        }

        [Fact]
        public void GetDelta_MyFooItemWasModifiedThenRemoved_ReturnsOneListOperation()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            GeneratedType listItem1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            bar.UpdateReferenceState();
            listItem3.SetProperty("Abc", 123);
            fooList.Remove(listItem3);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
            Assert.Single(delta.GetProperty<GeneratedListType>("MyFooListDelta"));
        }

        [Fact]
        public void GetDelta_MyFooListWasCleared_ReturnsOneListOperation()
        {
            // Arrange
            GeneratedListType fooList = new GeneratedListType(GeneratedAssembly, "FooBar.FooList");
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar.SetProperty("MyFooList", fooList);
            GeneratedType listItem1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            GeneratedType listItem3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo");
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            bar.UpdateReferenceState();
            fooList.Add(listItem1);
            fooList.Add(listItem2);
            fooList.Add(listItem3);
            fooList.Clear();

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.NotNull(delta);
            Assert.Single(delta.GetProperty<GeneratedListType>("MyFooListDelta"));
        }

        [Fact]
        public void GetDelta_MyValueStructIsEqualToReferenceState_DoesNotContainMyValueStruct()
        {
            // Arrange
            GeneratedType bar = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType valueStruct = new GeneratedType(GeneratedAssembly, "FooBar.ValueStruct");
            valueStruct.SetField("Prop1", 1);
            valueStruct.SetField("Prop2", 2);
            valueStruct.SetField("Prop3", true);
            bar.SetProperty("MyValueStruct", valueStruct);
            bar.UpdateReferenceState();
            GeneratedType newValueStruct = new GeneratedType(GeneratedAssembly, "FooBar.ValueStruct");
            newValueStruct.SetField("Prop1", 1);
            newValueStruct.SetField("Prop2", 2);
            newValueStruct.SetField("Prop3", true);
            bar.SetProperty("MyValueStruct", newValueStruct);

            // Act
            GeneratedDeltaType delta = bar.GetDelta();

            // Assert
            Assert.Null(delta);
        }
    }
}