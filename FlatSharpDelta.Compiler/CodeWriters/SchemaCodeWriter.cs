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
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class SchemaCodeWriter
    {
        public static string GetAutoGeneratedCommentAndUsages(string compilerVersion, string sourceHash)
        {
            return $@"
                //------------------------------------------------------------------------------
                // <auto-generated>
                //     Code generated by FlatSharpDelta.Compiler, do not modify.
                //
                //     Compiler version: {compilerVersion}
                //     Source hash: {sourceHash}
                // </auto-generated>
                //------------------------------------------------------------------------------

                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;
                using FlatSharp;
                using FlatSharpDelta;
                #nullable enable annotations
            ";
        }

        public static string WriteCode(Schema schema, FileInfo declarationFile, DirectoryInfo declarationFileRelativeTo)
        {
            string code = String.Empty;

            foreach (reflection.Object obj in schema.objects)
            {
                if (obj.declaration_file != IDeclarationFilePropertyExtensions.GetDeclarationFileString(declarationFile.FullName, declarationFileRelativeTo.FullName))
                {
                    continue;
                }

                if (obj.IsReferenceType())
                {
                    code += ReferenceTypeCodeWriter.WriteCode(schema, obj);
                    code += ReferenceTypeListCodeWriter.WriteCode(schema, obj);
                }
                else
                {
                    code += ValueStructListCodeWriter.WriteCode(schema, obj);
                    code += ValueStructExtensionsCodeWriter.WriteCode(schema, obj);
                }
            }

            foreach (reflection.Enum _enum in schema.enums)
            {
                if (_enum.declaration_file != IDeclarationFilePropertyExtensions.GetDeclarationFileString(declarationFile.FullName, declarationFileRelativeTo.FullName))
                {
                    continue;
                }

                if (_enum.IsUnion())
                {
                    code += UnionCodeWriter.WriteCode(schema, _enum);
                    code += UnionListCodeWriter.WriteCode(schema, _enum);
                    code += UnionExtensionsCodeWriter.WriteCode(schema, _enum);
                }
                else
                {
                    code += EnumListCodeWriter.WriteCode(schema, _enum);
                }
            }

            return code;
        }
    }
}