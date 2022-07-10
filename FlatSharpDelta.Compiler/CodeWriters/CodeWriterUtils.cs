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

        public static bool PropertyRequiresPrivateMember(Schema schema, Field field)
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