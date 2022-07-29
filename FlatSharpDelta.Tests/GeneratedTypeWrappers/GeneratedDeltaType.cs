using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedDeltaType : GeneratedBaseType
    {
        public GeneratedDeltaType() : base()
        {
        }

        public GeneratedDeltaType(object obj) : base(obj)
        {
        }

        public GeneratedDeltaType(Assembly assembly, string name, object copy = null) : base(assembly, name, copy)
        {
        }

        public IReadOnlyList<GeneratedListDeltaType> GetListDeltaProperty(string propertyName)
        {
            return ((IReadOnlyList<object>)GetProperty(propertyName)).Select(d => new GeneratedListDeltaType(d)).ToList();
        }
    }
}