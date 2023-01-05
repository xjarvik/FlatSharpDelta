using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ApplyDeltaCaseCodeWriter
    {
        public static string GetApplyReferenceTypeArray(Schema schema, Field field, int arrayIndex)
        {
            return $@"
                case {field.name}_{arrayIndex}_Index:
                    {{
                        {schema.GetNameOfObjectWithIndex(field.type.index)}? nestedObject = delta.{field.name}_{arrayIndex};
                        __flatsharp__{field.name}_{arrayIndex} = nestedObject != null ? new {schema.GetNameOfObjectWithIndex(field.type.index)}(nestedObject) : null!;
                        break;
                    }}
            ";
        }

        public static string GetApplyReferenceTypeArrayDelta(Schema schema, Field field, int arrayIndex)
        {
            return $@"
                case {field.name}_{arrayIndex}Delta_Index:
                    {{
                        __flatsharp__{field.name}_{arrayIndex}?.ApplyDelta(delta.{field.name}_{arrayIndex}Delta);
                        break;
                    }}
            ";
        }

        public static string GetApplyValueStructArray(Schema schema, Field field, int arrayIndex)
        {
            return $@"
                case {field.name}_{arrayIndex}_Index:
                    {{
                        __flatsharp__{field.name}_{arrayIndex} = delta.{field.name}_{arrayIndex} ?? __flatsharp__{field.name}_{arrayIndex};
                        break;
                    }}
            ";
        }

        public static string GetApplyScalarArray(Schema schema, Field field, int arrayIndex)
        {
            return $@"
                case {field.name}_{arrayIndex}_Index:
                    {{
                        __flatsharp__{field.name}_{arrayIndex} = delta.{field.name}_{arrayIndex};
                        break;
                    }}
            ";
        }

        public static string GetApplyReferenceType(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        {schema.GetNameOfObjectWithIndex(field.type.index)}? nestedObject = delta.{field.name};
                        {field.name} = nestedObject != null ? new {schema.GetNameOfObjectWithIndex(field.type.index)}(nestedObject) : null!;
                        break;
                    }}
            ";
        }

        public static string GetApplyValueStruct(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        {field.name} = delta.{field.name}{(!field.optional ? $" ?? {field.name}" : String.Empty)};
                        break;
                    }}
            ";
        }

        public static string GetApplyReferenceTypeDelta(Schema schema, Field field)
        {
            return $@"
                case {field.name}Delta_Index:
                    {{
                        {field.name}?.ApplyDelta(delta.{field.name}Delta);
                        break;
                    }}
            ";
        }

        public static string GetApplyScalar(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        {field.name} = delta.{field.name};
                        break;
                    }}
            ";
        }

        public static string GetApplyUnion(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        {schema.GetNameOfEnumWithIndex(field.type.index)}? nestedObject = delta.{field.name};
                        {field.name} = nestedObject != null ? {schema.GetNameOfEnumWithIndex(field.type.index)}.DeepCopy(nestedObject.Value) : {(field.optional ? "null" : $"{field.name}")};
                        break;
                    }}
            ";
        }

        public static string GetApplyUnionDelta(Schema schema, Field field)
        {
            return $@"
                case {field.name}Delta_Index:
                    {{
                        {field.name}{(field.optional ? "?" : String.Empty)}.ApplyDelta(delta.{field.name}Delta);
                        break;
                    }}
            ";
        }

        public static string GetApplyReferenceTypeList(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        IReadOnlyList<{schema.GetNameOfObjectWithIndex(field.type.index)}>? nestedObject = delta.{field.name};
                        {field.name} = nestedObject != null ? new {schema.GetNameOfObjectWithIndex(field.type.index)}List(nestedObject) : null!;
                        break;
                    }}
            ";
        }

        public static string GetApplyReferenceTypeListDelta(Schema schema, Field field)
        {
            return $@"
                case {field.name}Delta_Index:
                    {{
                        {field.name}?.ApplyDelta(delta.{field.name}Delta);
                        break;
                    }}
            ";
        }

        public static string GetApplyEnumList(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        IReadOnlyList<{schema.GetNameOfEnumWithIndex(field.type.index)}>? nestedObject = delta.{field.name};
                        {field.name} = nestedObject != null ? new {schema.GetNameOfEnumWithIndex(field.type.index)}List(nestedObject) : null!;
                        break;
                    }}
            ";
        }

        public static string GetApplyEnumListDelta(Schema schema, Field field)
        {
            return $@"
                case {field.name}Delta_Index:
                    {{
                        {field.name}?.ApplyDelta(delta.{field.name}Delta);
                        break;
                    }}
            ";
        }

        public static string GetApplyScalarList(Schema schema, Field field)
        {
            return $@"
                case {field.name}_Index:
                    {{
                        IReadOnlyList<{schema.GetCSharpType(field.type.ToElementAsBaseType())}>? nestedObject = delta.{field.name};
                        {field.name} = nestedObject != null ? new {field.type.element.ToString()}List(nestedObject) : null!;
                        break;
                    }}
            ";
        }

        public static string GetApplyScalarListDelta(Schema schema, Field field)
        {
            return $@"
                case {field.name}Delta_Index:
                    {{
                        {field.name}?.ApplyDelta(delta.{field.name}Delta);
                        break;
                    }}
            ";
        }
    }
}