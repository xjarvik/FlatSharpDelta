using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReferenceStructCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            // Identical to TableCodeWriter.
            return TableCodeWriter.WriteCode(schema, obj);
        }
    }
}