using System;
using System.IO;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class SchemaCodeWriter
    {
        public static string WriteCode(Schema schema, FileInfo declarationFile)
        {
            string code = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using FlatSharp;
#nullable enable annotations
            ";

            foreach(reflection.Object obj in schema.objects)
            {
                if(obj.declaration_file != "//" + declarationFile.Name)
                {
                    continue;
                }

                if(!obj.is_struct)
                {
                    code += TableCodeWriter.WriteCode(schema, obj);
                    // code += TableListCodeWriter.WriteCode(schema, obj);
                }
                else
                {

                }
            }

            return code;
        }
    }
}