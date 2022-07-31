using System;
using System.Reflection;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedType : GeneratedBaseType
    {
        public GeneratedType() : base()
        {
        }

        public GeneratedType(object obj) : base(obj)
        {
        }

        public GeneratedType(Assembly assembly, string name, object copy = null) : base(assembly, name, copy)
        {
        }

        public GeneratedDeltaType GetDelta()
        {
            object delta = type.InvokeMember("GetDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );

            return delta != null ? new GeneratedDeltaType(delta) : null;
        }

        public void ApplyDelta(GeneratedDeltaType delta)
        {
            type.InvokeMember("ApplyDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { delta != null ? delta.NativeObject : null }
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