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
                namespace {_namespace}.SupportingTypes
                {{
                    {GetUsings(schema, obj)}

                    public static class {name}Extensions
                    {{
                        {GetIsEqualTo(schema, obj)}

                        {GetIsEqualToNullable(obj)}
                    }}
                }}
            ";
        }

        private static string GetUsings(Schema schema, reflection.Object obj)
        {
            string _namespace = obj.GetNamespace();

            return obj.fields
                .Select(field =>
                {
                    if(CodeWriterUtils.PropertyTypeIsValueStruct(schema, field.type))
                    {
                        return schema.objects[field.type.index].GetNamespace();
                    }

                    return null;
                })
                .Distinct()
                .Where(objNamespace => !String.IsNullOrEmpty(objNamespace))
                .Aggregate(String.Empty, (usings, objNamespace) =>
                {
                    if(objNamespace == _namespace)
                    {
                        return usings;
                    }

                    return usings + $@"
                        using {objNamespace}.SupportingTypes;
                    ";
                });
        }

        private static string GetIsEqualTo(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            List<string> comparisons = new List<string>();

            obj.ForEachFieldExceptUType(field =>
            {
                if(CodeWriterUtils.PropertyTypeIsValueStruct(schema, field.type))
                {
                    comparisons.Add($"self.{field.name}.IsEqualTo(other.{field.name})");
                }
                else if(field.type.base_type == BaseType.Array)
                {
                    bool isValueStructArray = CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type);

                    for(int i = 0; i < field.type.fixed_length; i++)
                    {
                        if(!isValueStructArray)
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