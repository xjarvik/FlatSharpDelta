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
    static class ValueStructExtensionsCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    {GetUsages(schema, obj)}

                    public static class {name}Extensions
                    {{
                        {GetIsEqualTo(schema, obj)}

                        {GetIsEqualToNullable(obj)}
                    }}
                }}
            ";
        }

        private static string GetUsages(Schema schema, reflection.Object obj)
        {
            string _namespace = obj.GetNamespace();

            return obj.fields
                .Select(field =>
                {
                    if (schema.TypeIsValueStruct(field.type) || schema.TypeIsValueStructArray(field.type))
                    {
                        return schema.objects[field.type.index].GetNamespace();
                    }

                    return null;
                })
                .Distinct()
                .Where(objNamespace => !String.IsNullOrEmpty(objNamespace))
                .Aggregate(String.Empty, (usages, objNamespace) =>
                {
                    if (objNamespace == _namespace)
                    {
                        return usages;
                    }

                    return usages + $@"
                        using {objNamespace};
                    ";
                });
        }

        private static string GetIsEqualTo(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            List<string> comparisons = new List<string>();

            obj.ForEachFieldExceptUType(field =>
            {
                if (schema.TypeIsValueStruct(field.type))
                {
                    comparisons.Add($"self.{field.name}.IsEqualTo(other.{field.name})");
                }
                else if (field.type.base_type == BaseType.Array)
                {
                    bool isValueStructArray = schema.TypeIsValueStructArray(field.type);

                    for (int i = 0; i < field.type.fixed_length; i++)
                    {
                        if (!isValueStructArray)
                        {
                            comparisons.Add($"self.{field.name}({i}) == other.{field.name}({i})");
                        }
                        else
                        {
                            comparisons.Add($"self.{field.name}({i}).IsEqualTo(other.{field.name}({i}))");
                        }
                    }
                }
                else
                {
                    comparisons.Add($"self.{field.name} == other.{field.name}");
                }
            });

            return $@"
                public static bool IsEqualTo(this {name} self, {name} other)
                {{
                    return {(comparisons.Count > 0 ? String.Join(" && ", comparisons) : "true")};
                }}
            ";
        }

        private static string GetIsEqualToNullable(reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
                public static bool IsEqualTo(this {name}? self, {name}? other)
                {{
                    return self.HasValue ? other.HasValue && self.Value.IsEqualTo(other.Value) : !other.HasValue;
                }}
            ";
        }
    }
}