using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedListType : GeneratedBaseType
    {
        public GeneratedListType() : base()
        {
        }

        public GeneratedListType(object obj) : base(obj)
        {
        }

        public GeneratedListType(Assembly assembly, string name, object copy = null) : base(assembly, name, copy)
        {
        }

        public void Add(GeneratedType item)
        {
            type.InvokeMember("Add",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public IReadOnlyList<GeneratedListDeltaType> GetDelta()
        {
            object delta = type.InvokeMember("GetDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );

            return ((IReadOnlyList<object>)delta).Select(d => new GeneratedListDeltaType(d)).ToList();
        }

        public void ApplyDelta(IReadOnlyList<GeneratedListDeltaType> delta)
        {
            type.InvokeMember("ApplyDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { delta.Select(d => d.NativeObject).ToList() }
            );
        }

        public void UpdateReferenceState()
        {
            type.InvokeMember("UpdateReferenceState",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );
        }
    }
}