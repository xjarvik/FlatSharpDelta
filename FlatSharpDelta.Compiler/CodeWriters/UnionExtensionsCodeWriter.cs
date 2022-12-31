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

        private static string GetUsages(Schema schema, reflection.Enum union)
        {
            string _namespace = union.GetNamespace();

            return union.values
                .Skip(1)
                .Select(value => schema.objects[value.union_type.index].GetNamespace())
                .Distinct()
                .Aggregate(String.Empty, (usages, objNamespace) =>
                {
                    if (objNamespace == _namespace)
                    {
                        return usages;
                    }

                    return usages + $"using {objNamespace};";
                });
        }

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