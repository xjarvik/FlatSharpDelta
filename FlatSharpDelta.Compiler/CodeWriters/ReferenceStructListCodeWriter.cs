using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReferenceStructListCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            // Identical to TableListCodeWriter.
            return TableListCodeWriter.WriteCode(schema, obj);
        }
    }
}