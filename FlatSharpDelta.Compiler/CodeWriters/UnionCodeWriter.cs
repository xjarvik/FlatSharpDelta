using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class UnionCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string _namespace = union.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    public partial struct {name}
                    {{
                        {GetDeepCopy(schema, union)}

                        {GetDelta(schema, union)}

                        {GetApplyDelta(schema, union)}

                        {GetUpdateReferenceState(schema, union)}
                    }}
                }}
            ";
        }

        private static string GetDeepCopy(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                if (schema.TypeIsValueStruct(enumVal.union_type))
                {
                    discriminators += $"case {i}: return new {name}(source.{enumVal.name});";
                }
                else
                {
                    discriminators += $"case {i}: return new {name}(new {schema.GetCSharpType(enumVal.union_type).Trim('?')}(source.{enumVal.name}));";
                }
            }

            return $@"
                public static {name} DeepCopy({name} source)
                {{
                    switch (source.Discriminator)
                    {{
                        {discriminators}
                    }}

                    return new {name}();
                }}
            ";
        }

        private static string GetDelta(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                if (schema.TypeIsValueStruct(enumVal.union_type))
                {
                    continue;
                }

                discriminators += $@"
                    case {i}:
                    {{
                        {schema.GetCSharpDeltaType(enumVal.union_type)} nestedDelta = {enumVal.name}.GetDelta();
                        return nestedDelta != null ? new {name}Delta(nestedDelta) : null;
                    }}
                ";
            }

            return $@"
                public {name}Delta? GetDelta()
                {{
                    switch (Discriminator)
                    {{
                        {discriminators}
                    }}

                    return null;
                }}
            ";
        }

        private static string GetApplyDelta(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;
            int offset = 0;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                if (schema.TypeIsValueStruct(enumVal.union_type))
                {
                    offset--;
                    continue;
                }

                discriminators += $@"
                    case {i + offset}:
                        if (Discriminator == {i})
                        {{
                            {enumVal.name}.ApplyDelta(delta.Value.{enumVal.name}Delta);
                        }}
                        break;
                ";
            }

            return $@"
                public void ApplyDelta({name}Delta? delta)
                {{
                    {(!String.IsNullOrEmpty(discriminators) ? $@"
                    if (delta == null)
                    {{
                        return;
                    }}

                    switch (delta.Value.Discriminator)
                    {{
                        {discriminators}
                    }}
                    " :
                    String.Empty)}
                }}
            ";
        }

        private static string GetUpdateReferenceState(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                if (schema.TypeIsValueStruct(enumVal.union_type))
                {
                    continue;
                }

                discriminators += $@"
                    case {i}:
                        {enumVal.name}.UpdateReferenceState();
                        break;
                ";
            }

            return $@"
                public void UpdateReferenceState()
                {{
                    {(!String.IsNullOrEmpty(discriminators) ? $@"
                    switch (Discriminator)
                    {{
                        {discriminators}
                    }}
                    " :
                    String.Empty)}
                }}
            ";
        }
    }
}