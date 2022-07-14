using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class CodeWriterUtils
    {
        public static string GetPropertyType(Schema schema, Field field)
        {
            string type;

            switch(field.type.base_type)
            {
                case BaseType.Bool:
                    type = "bool";
                    break;

                case BaseType.Byte:
                    type = "sbyte";
                    break;

                case BaseType.UByte:
                    type = "byte";
                    break;

                case BaseType.Short:
                    type = "short";
                    break;

                case BaseType.UShort:
                    type = "ushort";
                    break;

                case BaseType.Int:
                    type = "int";
                    break;

                case BaseType.UInt:
                    type = "uint";
                    break;

                case BaseType.Long:
                    type = "long";
                    break;

                case BaseType.ULong:
                    type = "ulong";
                    break;

                case BaseType.Float:
                    type = "float";
                    break;

                case BaseType.Double:
                    type = "double";
                    break;

                case BaseType.String:
                    type = "string";
                    break;

                case BaseType.Obj:
                    type = schema.objects[field.type.index].name;
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    type = schema.enums[field.type.index].name;
                    break;

                default:
                    return null;
            }

            if(field.optional)
            {
                type += "?";
            }

            return type;
        }

        public static string GetPropertyListType(Schema schema, Field field)
        {
            string listType;

            switch(field.type.element)
            {
                case BaseType.Bool:
                    listType = "BoolList?";
                    break;

                case BaseType.Byte:
                    listType = "ByteList?";
                    break;

                case BaseType.UByte:
                    listType = "UByteList?";
                    break;

                case BaseType.Short:
                    listType = "ShortList?";
                    break;

                case BaseType.UShort:
                    listType = "UShortList?";
                    break;

                case BaseType.Int:
                    listType = "IntList?";
                    break;

                case BaseType.UInt:
                    listType = "UIntList?";
                    break;

                case BaseType.Long:
                    listType = "LongList?";
                    break;

                case BaseType.ULong:
                    listType = "ULongList?";
                    break;

                case BaseType.Float:
                    listType = "FloatList?";
                    break;

                case BaseType.Double:
                    listType = "DoubleList?";
                    break;

                case BaseType.String:
                    listType = "StringList?";
                    break;

                case BaseType.Obj:
                    listType = schema.objects[field.type.index].name + "List?";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    listType = schema.enums[field.type.index].name + "List?";
                    break;

                default:
                    return null;
            }

            return listType;
        }

        public static string GetPropertyBaseType(Schema schema, Field field)
        {
            string baseType = GetPropertyType(schema, field);

            if(field.type.base_type == BaseType.Obj
            || field.type.base_type == BaseType.Union
            || field.type.base_type == BaseType.UType)
            {
                baseType = "Base" + baseType;
            }

            return baseType;
        }

        public static string GetPropertyBaseListType(Schema schema, Field field)
        {
            string baseType = GetPropertyBaseType(schema, field);

            if(field.type.base_type == BaseType.Obj
            || field.type.base_type == BaseType.Union
            || field.type.base_type == BaseType.UType)
            {
                baseType = baseType.TrimEnd('?');
            }

            return $"IReadOnlyList<{baseType}>?";
        }

        public static string GetPropertyDeltaType(Schema schema, Field field)
        {
            string deltaType;

            switch(field.type.base_type)
            {
                case BaseType.Obj:
                    deltaType = schema.objects[field.type.index].name + "Delta?";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    deltaType = schema.enums[field.type.index].name + "Delta?";
                    break;

                default:
                    return null;
            }

            return deltaType;
        }

        public static string GetPropertyDeltaListType(Schema schema, Field field)
        {
            string listDeltaType;

            switch(field.type.element)
            {
                case BaseType.Bool:
                    listDeltaType = "BoolListDelta";
                    break;

                case BaseType.Byte:
                    listDeltaType = "ByteListDelta";
                    break;

                case BaseType.UByte:
                    listDeltaType = "UByteListDelta";
                    break;

                case BaseType.Short:
                    listDeltaType = "ShortListDelta";
                    break;

                case BaseType.UShort:
                    listDeltaType = "UShortListDelta";
                    break;

                case BaseType.Int:
                    listDeltaType = "IntListDelta";
                    break;

                case BaseType.UInt:
                    listDeltaType = "UIntListDelta";
                    break;

                case BaseType.Long:
                    listDeltaType = "LongListDelta";
                    break;

                case BaseType.ULong:
                    listDeltaType = "ULongListDelta";
                    break;

                case BaseType.Float:
                    listDeltaType = "FloatListDelta";
                    break;

                case BaseType.Double:
                    listDeltaType = "DoubleListDelta";
                    break;

                case BaseType.String:
                    listDeltaType = "StringListDelta";
                    break;

                case BaseType.Obj:
                    listDeltaType = schema.objects[field.type.index].name + "ListDelta";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    listDeltaType = schema.enums[field.type.index].name + "ListDelta";
                    break;

                default:
                    return null;
            }

            return $"IReadOnlyList<{listDeltaType}>?";
        }

        public static string GetPropertyDefaultValue(Schema schema, Field field)
        {
            if(field.optional)
            {
                return "null";
            }

            switch(field.type.base_type)
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
                    return field.default_integer.ToString();

                case BaseType.Float:
                    return field.default_real.ToString() + "f";

                case BaseType.Double:
                    return field.default_real.ToString() + "d";

                case BaseType.Union:
                case BaseType.UType:
                    return !schema.enums[field.type.index].is_union ? field.default_integer.ToString() : "null";

                default:
                    return "null";
            }
        }

        public static bool PropertyTypeIsDerived(Schema schema, Field field)
        {
            switch(field.type.base_type)
            {
                case BaseType.Obj:
                    reflection.Object obj = schema.objects[field.type.index];
                    return !(obj.is_struct && obj.HasAttribute("fs_valueStruct"));

                case BaseType.Union:
                case BaseType.UType:
                    reflection.Enum _enum = schema.enums[field.type.index];
                    return _enum.is_union;

                case BaseType.Vector:
                case BaseType.Array:
                    return true;

                default:
                    return false;
            }
        }
    }
}