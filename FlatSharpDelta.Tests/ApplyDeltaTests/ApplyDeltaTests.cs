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
    public class ApplyDeltaTests : IClassFixture<AssemblyFixture<ApplyDeltaTests.Configuration>>
    {
        public class Configuration : ITestConfiguration
        {
            public Type TestType
            {
                get => typeof(ApplyDeltaTests);
            }

            public string[] FbsFiles
            {
                get => new string[]
                {
                    "ApplyDeltaTests.fbs"
                };
            }
        }

        private Assembly GeneratedAssembly { get; set; }

        public ApplyDeltaTests(AssemblyFixture<ApplyDeltaTests.Configuration> fixture)
        {
            GeneratedAssembly = fixture.GeneratedAssembly;
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
            Assert.NotNull(bar.GetProperty("Prop3"));
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
        public void ApplyDelta_DeltaContainsTableDelta_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar1.SetProperty("Prop3", new GeneratedType(GeneratedAssembly, "FooBar.Foo1"));
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            bar1.GetProperty<GeneratedType>("Prop3").SetProperty("Abc1", 5);

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(5, bar2.GetProperty<GeneratedType>("Prop3").GetProperty("Abc1"));
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

        [Fact]
        public void ApplyDelta_DeltaContainsEnumListDeltaWithReplacedListOperation_SetsListValueCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedListType foo4List = new GeneratedListType(GeneratedAssembly, "FooBar.Foo4List");
            foo4List.Add(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val1"));
            bar1.SetProperty("Prop10", foo4List);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            foo4List.SetIndexerProperty(0, GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val3"));

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop10"));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo4", "Val3"), bar2.GetProperty<GeneratedListType>("Prop10").GetIndexerProperty(0));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsUnions_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType foo1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            foo1.SetProperty("Abc1", 1);
            GeneratedType prop11 = new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo1);
            bar1.SetProperty("Prop11", prop11);
            GeneratedListType prop12 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo5List");
            bar1.SetProperty("Prop12", prop12);
            GeneratedType foo2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            foo2.SetProperty("Abc2", 2);
            prop12.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo2));

            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty("Prop11"));
            Assert.Equal(1, bar2.GetProperty<GeneratedType>("Prop11").GetProperty<GeneratedType>("MyFoo1").GetProperty("Abc1"));
            Assert.Single(bar2.GetProperty<GeneratedListType>("Prop12"));
            Assert.Equal(2, bar2.GetProperty<GeneratedListType>("Prop12").GetIndexerProperty<GeneratedType>(0).GetProperty<GeneratedType>("MyFoo2").GetProperty("Abc2"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsUnionDelta_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType foo1 = new GeneratedType(GeneratedAssembly, "FooBar.Foo1");
            foo1.SetProperty("Abc1", 1);
            GeneratedType prop11 = new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo1);
            bar1.SetProperty("Prop11", prop11);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            foo1.SetProperty("Abc1", 2);

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(2, bar2.GetProperty<GeneratedType>("Prop11").GetProperty<GeneratedType>("MyFoo1").GetProperty("Abc1"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsUnionListDeltaWithReplacedAndModifiedListOperations_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedListType prop12 = new GeneratedListType(GeneratedAssembly, "FooBar.Foo5List");
            bar1.SetProperty("Prop12", prop12);
            GeneratedType foo2 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            foo2.SetProperty("Abc2", 2);
            prop12.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo2));
            prop12.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo5", new GeneratedType(GeneratedAssembly, "FooBar.Foo3")));
            GeneratedType foo3 = new GeneratedType(GeneratedAssembly, "FooBar.Foo3");
            foo3.SetField("Abc3", 4);
            prop12.Add(new GeneratedType(GeneratedAssembly, "FooBar.Foo5", foo3));
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            prop12.SetIndexerProperty(2, new GeneratedType(GeneratedAssembly, "FooBar.Foo5", new GeneratedType(GeneratedAssembly, "FooBar.Foo3")));
            foo2.SetProperty("Abc2", 5);

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.Equal(3, bar2.GetProperty<GeneratedListType>("Prop12").Count);
            Assert.Equal(5, bar2.GetProperty<GeneratedListType>("Prop12").GetIndexerProperty<GeneratedType>(0).GetProperty<GeneratedType>("MyFoo2").GetProperty("Abc2"));
            Assert.Equal(0, bar2.GetProperty<GeneratedListType>("Prop12").GetIndexerProperty<GeneratedType>(1).GetProperty<GeneratedType>("MyFoo3").GetField("Abc3"));
            Assert.Equal(0, bar2.GetProperty<GeneratedListType>("Prop12").GetIndexerProperty<GeneratedType>(2).GetProperty<GeneratedType>("MyFoo3").GetField("Abc3"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsReferenceStructItemInFixedLengthArrayInReferenceStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            bar1.SetProperty("Prop5", prop5);
            GeneratedListType foo00Array = prop5.GetProperty<GeneratedListType>("Foo00Array");
            GeneratedType foo00 = new GeneratedType(GeneratedAssembly, "FooBar.Foo00");
            foo00.SetProperty("Abc00", 6);
            foo00Array.SetIndexerProperty(0, foo00);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo00Array").GetIndexerProperty(0));
            Assert.Equal(6, bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo00Array").GetIndexerProperty<GeneratedType>(0).GetProperty("Abc00"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsReferenceStructDeltaInFixedLengthArrayInReferenceStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            bar1.SetProperty("Prop5", prop5);
            GeneratedListType foo00Array = prop5.GetProperty<GeneratedListType>("Foo00Array");
            GeneratedType foo00 = new GeneratedType(GeneratedAssembly, "FooBar.Foo00");
            foo00.SetProperty("Abc00", 6);
            foo00Array.SetIndexerProperty(0, foo00);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            bar2.ApplyDelta(bar1.GetDelta());
            bar1.UpdateReferenceState();
            foo00.SetProperty("Abc00", 7);

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo00Array").GetIndexerProperty(0));
            Assert.Equal(7, bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo00Array").GetIndexerProperty<GeneratedType>(0).GetProperty("Abc00"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsValueStructItemInFixedLengthArrayInReferenceStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            bar1.SetProperty("Prop5", prop5);
            GeneratedListType foo01Array = prop5.GetProperty<GeneratedListType>("Foo01Array");
            GeneratedType foo01 = new GeneratedType(GeneratedAssembly, "FooBar.Foo01");
            foo01.SetField("Abc01", 8);
            foo01Array.SetIndexerProperty(0, foo01);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo01Array").GetIndexerProperty(0));
            Assert.Equal(8, bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo01Array").GetIndexerProperty<GeneratedType>(0).GetField("Abc01"));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsEnumItemInFixedLengthArrayInReferenceStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            bar1.SetProperty("Prop5", prop5);
            GeneratedListType foo02Array = prop5.GetProperty<GeneratedListType>("Foo02Array");
            foo02Array.SetIndexerProperty(1, GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo02", "Val3"));
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo02Array").GetIndexerProperty(1));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo02", "Val3"), bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("Foo02Array").GetIndexerProperty(1));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsIntItemInFixedLengthArrayInReferenceStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop5 = new GeneratedType(GeneratedAssembly, "FooBar.Foo2");
            bar1.SetProperty("Prop5", prop5);
            GeneratedListType intArray = prop5.GetProperty<GeneratedListType>("IntArray");
            intArray.SetIndexerProperty(4, 9);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("IntArray").GetIndexerProperty(4));
            Assert.Equal(9, bar2.GetProperty<GeneratedType>("Prop5").GetProperty<GeneratedListType>("IntArray").GetIndexerProperty(4));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsEnumItemInFixedLengthArrayInValueStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop7 = new GeneratedType(GeneratedAssembly, "FooBar.Foo3");
            prop7.SetArrayItem("Foo02Array", 6, GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo02", "Val2"));
            bar1.SetProperty("Prop7", prop7);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop7").GetArrayItem("Foo02Array", 6));
            Assert.Equal(GeneratedType.Enum(GeneratedAssembly, "FooBar.Foo02", "Val2"), bar2.GetProperty<GeneratedType>("Prop7").GetArrayItem("Foo02Array", 6));
        }

        [Fact]
        public void ApplyDelta_DeltaContainsIntItemInFixedLengthArrayInValueStruct_SetsValuesCorrectly()
        {
            // Arrange
            GeneratedType bar1 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");
            GeneratedType prop7 = new GeneratedType(GeneratedAssembly, "FooBar.Foo3");
            prop7.SetArrayItem("IntArray", 4, 10);
            bar1.SetProperty("Prop7", prop7);
            GeneratedType bar2 = new GeneratedType(GeneratedAssembly, "FooBar.Bar");

            // Act
            bar2.ApplyDelta(bar1.GetDelta());

            // Assert
            Assert.NotNull(bar2.GetProperty<GeneratedType>("Prop7").GetArrayItem("IntArray", 4));
            Assert.Equal(10, bar2.GetProperty<GeneratedType>("Prop7").GetArrayItem("IntArray", 4));
        }
    }
}