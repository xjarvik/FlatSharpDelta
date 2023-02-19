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
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class UnionExtensionsCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string _namespace = union.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    {GetUsages(schema, union)}

                    public static class {name}Extensions
                    {{
                        {GetIsEqualTo(schema, union)}

                        {GetIsEqualToNullable(union)}
                    }}
                }}
            ";
        }

        private static string GetUsages(Schema schema, reflection.Enum union) => union.values
            .Where(value => schema.TypeIsValueStruct(value.union_type))
            .Select(value => schema.objects[value.union_type.index].GetNamespace())
            .Where(value => value != union.GetNamespace())
            .Distinct()
            .Aggregate(String.Empty, (usages, _namespace) => usages + $"using {_namespace};");

        private static string GetIsEqualTo(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = @"
                case 0: return true;
            ";

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                if (!schema.TypeIsValueStruct(enumVal.union_type))
                {
                    discriminators += $"case {i}: return self.{enumVal.name} == other.{enumVal.name};";
                }
                else
                {
                    discriminators += $"case {i}: return self.{enumVal.name}.IsEqualTo(other.{enumVal.name});";
                }
            }

            return $@"
                public static bool IsEqualTo(this {name} self, {name} other)
                {{
                    if(self.Discriminator == other.Discriminator)
                    {{
                        switch(self.Discriminator)
                        {{
                            {discriminators}
                        }}
                    }}
                    return false;
                }}
            ";
        }

        private static string GetIsEqualToNullable(reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();

            return $@"
                public static bool IsEqualTo(this {name}? self, {name}? other)
                {{
                    return self.HasValue ? other.HasValue && self.Value.IsEqualTo(other.Value) : !other.HasValue;
                }}
            ";
        }
    }
}