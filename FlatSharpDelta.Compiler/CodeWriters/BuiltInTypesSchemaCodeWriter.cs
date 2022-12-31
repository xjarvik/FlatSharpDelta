using System;
using System.IO;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class BuiltInTypesSchemaCodeWriter
    {
        public static string WriteCode()
        {
            string code = String.Empty;

            code += BuiltInListTypesCodeWriter.WriteCode();

            return code;
        }
    }
}