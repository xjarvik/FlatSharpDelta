using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class UnitTest1 : GeneratedCodeTest
    {
        protected override string[] FbsFiles
        {
            get => new string[]
            {
                "UnitTest1.fbs"
            };
        }

        [Fact]
        public void Test1()
        {
            Type type = GeneratedAssembly.GetType("FooBar.Foo");
            object foo = Activator.CreateInstance(type);
            type.GetProperty("Abc").SetValue(foo, 2);
            object delta = type.InvokeMember("GetDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                foo,
                null
            );

            Assert.NotNull(delta);
        }
    }
}