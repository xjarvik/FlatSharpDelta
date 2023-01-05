using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class DeltaComparisonCodeWriter
    {
        public static string GetReferenceTypeArrayComparison(Schema schema, Field field, int fieldIndex, int arrayIndex)
        {
            return $@"
                if (__flatsharp__{field.name}_{arrayIndex} != original.__flatsharp__{field.name}_{arrayIndex})
                {{
                    delta.{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                    delta.{field.name}_{arrayIndex}Delta = null;
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}_Index);
                }}
                else if (__flatsharp__{field.name}_{arrayIndex} != null)
                {{
                    {schema.GetCSharpDeltaType(field.type.ToElementAsBaseType())} nestedDelta = __flatsharp__{field.name}_{arrayIndex}.GetDelta();
                    if (nestedDelta != null)
                    {{
                        delta.{field.name}_{arrayIndex}Delta = nestedDelta;
                        {(fieldIndex + 1 <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}Delta_Index);
                    }}
                    else
                    {{
                        delta.{field.name}_{arrayIndex}Delta = null;
                    }}
                    delta.{field.name}_{arrayIndex} = null;
                }}
                else
                {{
                    delta.{field.name}_{arrayIndex} = null;
                    delta.{field.name}_{arrayIndex}Delta = null;
                }}
            ";
        }

        public static string GetValueStructArrayComparison(Schema schema, Field field, int fieldIndex, int arrayIndex)
        {
            return $@"
                if (!{schema.GetNameOfObjectWithIndex(field.type.index)}Extensions.IsEqualTo(__flatsharp__{field.name}_{arrayIndex}, original.__flatsharp__{field.name}_{arrayIndex}))
                {{
                    delta.{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}_Index);
                }}
                else
                {{
                    delta.{field.name}_{arrayIndex} = null;
                }}
            ";
        }

        public static string GetScalarArrayComparison(Schema schema, Field field, int fieldIndex, int arrayIndex)
        {
            return $@"
                if (__flatsharp__{field.name}_{arrayIndex} != original.__flatsharp__{field.name}_{arrayIndex})
                {{
                    delta.{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}_Index);
                }}
                else
                {{
                    delta.{field.name}_{arrayIndex} = {GetCSharpDefaultValue(schema, field, true)};
                }}
            ";
        }

        public static string GetReferenceTypeComparison(Schema schema, Field field, int fieldIndex)
        {
            return GetReferenceTypeComparison(schema, field, fieldIndex, null);
        }

        public static string GetValueStructComparison(Schema schema, Field field, int fieldIndex)
        {
            return $@"
                if (!{schema.GetNameOfObjectWithIndex(field.type.index)}Extensions.IsEqualTo({field.name}, original.{field.name}))
                {{
                    delta.{field.name} = {field.name};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
                else
                {{
                    delta.{field.name} = {GetCSharpDefaultValue(schema, field)};
                }}
            ";
        }

        public static string GetScalarComparison(Schema schema, Field field, int fieldIndex)
        {
            return $@"
                if ({field.name} != original.{field.name})
                {{
                    delta.{field.name} = {field.name};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
                else
                {{
                    delta.{field.name} = {GetCSharpDefaultValue(schema, field)};
                }}
            ";
        }

        public static string GetUnionComparison(Schema schema, Field field, int fieldIndex)
        {
            reflection.Enum union = schema.enums[field.type.index];
            string discriminators = String.Empty;

            for (int i = 0; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string comparison = String.Empty;

                if (schema.TypeIsReferenceType(enumVal.union_type))
                {
                    comparison = GetReferenceTypeComparison(schema, field, fieldIndex, enumVal);
                }
                else if (enumVal.union_type.base_type != BaseType.None)
                {
                    comparison = GetNonReferenceTypeUnionValueComparison(schema, field, fieldIndex, enumVal);
                }

                discriminators += $@"
                    case {i}:
                        {{
                            {comparison}
                            break;
                        }}
                ";
            }

            return $@"
                {(field.optional ? $@"
                if ({field.name} == null)
                {{
                    delta.{field.name} = null;
                    delta.{field.name}Delta = null;
                    if(original.{field.name} != null)
                    {{
                        {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                    }}
                }}
                else if (original.{field.name} == null || {field.name}.Value.Discriminator != original.{field.name}.Value.Discriminator)
                " :
                $"if ({field.name}.Discriminator != original.{field.name}.Discriminator)")}
                {{
                    delta.{field.name} = {field.name};
                    delta.{field.name}Delta = null;
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
                else
                {{
                    switch({field.name}{(field.optional ? ".Value" : String.Empty)}.Discriminator)
                    {{
                        {discriminators}
                    }}
                }}
            ";
        }

        private static string GetCSharpDefaultValue(Schema schema, Field field, bool useElement = false)
        {
            BaseType typeToCheck = useElement ? field.type.element : field.type.base_type;

            if (field.optional && (!useElement || field.type.base_type != BaseType.Array))
            {
                return "null";
            }

            switch (typeToCheck)
            {
                case BaseType.Bool:
                    return field.default_integer > 0 ? "true" : "false";

                case BaseType.Byte:
                case BaseType.UByte:
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Int:
                case BaseType.UInt:
                case BaseType.Long:
                case BaseType.ULong:
                    {
                        if (field.type.index != -1)
                        {
                            return $"({schema.GetNameOfEnumWithIndex(field.type.index)}){field.default_integer}";
                        }

                        return field.default_integer.ToString();
                    }

                case BaseType.Float:
                    return field.default_real.ToString() + "f";

                case BaseType.Double:
                    return field.default_real.ToString() + "d";

                default:
                    return "null";
            }
        }

        private static string GetReferenceTypeComparison(Schema schema, Field field, int fieldIndex, EnumVal enumVal)
        {
            string nestedObject = String.Empty;

            if (enumVal != null)
            {
                nestedObject = $"{schema.GetNameOfObjectWithIndex(enumVal.union_type.index)} nestedObject = {field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name};";
            }

            string firstEqualityCheck = String.Empty;

            if (enumVal != null)
            {
                firstEqualityCheck = $"nestedObject != original.{field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name}";
            }
            else
            {
                firstEqualityCheck = $"{field.name} != original.{field.name}";
            }

            string secondEqualityCheck = String.Empty;

            if (enumVal != null)
            {
                secondEqualityCheck = "nestedObject != null";
            }
            else
            {
                secondEqualityCheck = $"{field.name} != null";
            }

            return $@"
                {nestedObject}
                if ({firstEqualityCheck})
                {{
                    delta.{field.name} = {field.name};
                    delta.{field.name}Delta = null;
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
                else if ({secondEqualityCheck})
                {{
                    {schema.GetCSharpDeltaType(field.type)} nestedDelta = {field.name}{(enumVal != null && field.optional ? ".Value" : String.Empty)}.GetDelta();
                    if (nestedDelta != null)
                    {{
                        delta.{field.name}Delta = nestedDelta;
                        {(fieldIndex + 1 <= 255 ? "byteFields" : "shortFields")}.Add({field.name}Delta_Index);
                    }}
                    else
                    {{
                        delta.{field.name}Delta = null;
                    }}
                    delta.{field.name} = null;
                }}
                else
                {{
                    delta.{field.name} = null;
                    delta.{field.name}Delta = null;
                }}
            ";
        }

        private static string GetNonReferenceTypeUnionValueComparison(Schema schema, Field field, int fieldIndex, EnumVal enumVal)
        {
            string equalityCheck = String.Empty;

            if (schema.TypeIsString(enumVal.union_type))
            {
                equalityCheck = $"{field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name} != original.{field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name}";
            }
            else
            {
                equalityCheck = $"!{schema.GetNameOfObjectWithIndex(enumVal.union_type.index)}Extensions.IsEqualTo({field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name}, original.{field.name}{(field.optional ? ".Value" : String.Empty)}.{enumVal.name})";
            }

            return $@"
                if ({equalityCheck})
                {{
                    delta.{field.name} = {field.name};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
                else
                {{
                    delta.{field.name} = null;
                }}
            ";
        }
    }
}