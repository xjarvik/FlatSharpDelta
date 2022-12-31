using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class BuiltInTypesSchemaFactory
    {
        public static Schema GetBuiltInTypesSchema()
        {
            return new Schema
            {
                objects = BuiltInTypeFactory.GetBuiltInListDeltaTypes(0).Values.ToList(),
                enums = new List<reflection.Enum> { BuiltInTypeFactory.GetListOperation() },
            };
        }
    }
}