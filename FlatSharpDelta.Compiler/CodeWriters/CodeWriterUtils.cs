using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class CodeWriterUtils
    {
        public static string GetPropertyType(Schema schema, reflection.Type type, bool optional = false)
        {
            return GetPropertyType(schema, type, false, optional);
        }

        private static string GetPropertyType(Schema schema, reflection.Type type, bool isArray, bool optional = false)
        {
            string propertyType;

            switch(!isArray ? type.base_type : type.element)
            {
                case BaseType.Bool:
                    propertyType = "bool";
                    break;

                case BaseType.Byte:
                    propertyType = "sbyte";
                    break;

                case BaseType.UByte:
                    propertyType = "byte";
                    break;

                case BaseType.Short:
                    propertyType = "short";
                    break;

                case BaseType.UShort:
                    propertyType = "ushort";
                    break;

                case BaseType.Int:
                    propertyType = "int";
                    break;

                case BaseType.UInt:
                    propertyType = "uint";
                    break;

                case BaseType.Long:
                    propertyType = "long";
                    break;

                case BaseType.ULong:
                    propertyType = "ulong";
                    break;

                case BaseType.Float:
                    propertyType = "float";
                    break;

                case BaseType.Double:
                    propertyType = "double";
                    break;

                case BaseType.String:
                    propertyType = "string";
                    break;

                case BaseType.Obj:
                    propertyType = schema.objects[type.index].name;
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    propertyType = schema.enums[type.index].name;
                    break;

                default:
                    return null;
            }

            if((!isArray ? PropertyTypeIsBuiltInScalar(type) : PropertyListTypeIsBuiltInScalar(type)) && type.index != -1)
            {
                propertyType = schema.enums[type.index].name;
            }

            if(optional)
            {
                propertyType += "?";
            }

            return propertyType;
        }

        public static string GetPropertyListType(Schema schema, reflection.Type type)
        {
            string propertyListType;

            switch(type.element)
            {
                case BaseType.Bool:
                    propertyListType = "BoolList?";
                    break;

                case BaseType.Byte:
                    propertyListType = "ByteList?";
                    break;

                case BaseType.UByte:
                    propertyListType = "UByteList?";
                    break;

                case BaseType.Short:
                    propertyListType = "ShortList?";
                    break;

                case BaseType.UShort:
                    propertyListType = "UShortList?";
                    break;

                case BaseType.Int:
                    propertyListType = "IntList?";
                    break;

                case BaseType.UInt:
                    propertyListType = "UIntList?";
                    break;

                case BaseType.Long:
                    propertyListType = "LongList?";
                    break;

                case BaseType.ULong:
                    propertyListType = "ULongList?";
                    break;

                case BaseType.Float:
                    propertyListType = "FloatList?";
                    break;

                case BaseType.Double:
                    propertyListType = "DoubleList?";
                    break;

                case BaseType.String:
                    propertyListType = "StringList?";
                    break;

                case BaseType.Obj:
                    propertyListType = schema.objects[type.index].name + "List?";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    propertyListType = schema.enums[type.index].name + "List?";
                    break;

                default:
                    return null;
            }

            if(PropertyListTypeIsBuiltInScalar(type) && type.index != -1)
            {
                propertyListType = schema.enums[type.index].name + "List?";
            }

            return propertyListType;
        }

        public static string GetPropertyBaseType(Schema schema, reflection.Type type, bool optional = false)
        {
            string propertyBaseType = GetPropertyType(schema, type, optional);

            if(!PropertyTypeIsValueStruct(schema, type)
            && (type.base_type == BaseType.Obj
            || type.base_type == BaseType.Union
            || type.base_type == BaseType.UType))
            {
                reflection.Object tempObj = new reflection.Object { name = propertyBaseType };
                propertyBaseType = tempObj.GetNamespace() + ".SupportingTypes.Base" + tempObj.GetNameWithoutNamespace();
            }

            return propertyBaseType;
        }

        public static string GetPropertyBaseListType(Schema schema, reflection.Type type, bool optional = false)
        {
            reflection.Type baseType = new reflection.Type(type);
            baseType.base_type = type.element;
            string propertyBaseType = GetPropertyBaseType(schema, baseType, optional).TrimEnd('?');

            return $"IReadOnlyList<{propertyBaseType}>?";
        }

        public static string GetPropertyBaseArrayType(Schema schema, Field field)
        {
            bool isValueStruct = PropertyListTypeIsValueStruct(schema, field.type);

            if(!PropertyListTypeIsBuiltInScalar(field.type) && !isValueStruct)
            {
                reflection.Object tempObj = new reflection.Object { name = schema.objects[field.type.index].name };
                return tempObj.GetNamespace() + ".SupportingTypes.Base" + tempObj.GetNameWithoutNamespace() + "?";
            }

            return GetPropertyType(schema, field.type, true, isValueStruct);
        }

        public static string GetPropertyDeltaType(Schema schema, reflection.Type type, bool isArray = false)
        {
            string propertyDeltaType;

            switch(!isArray ? type.base_type : type.element)
            {
                case BaseType.Obj:
                    propertyDeltaType = schema.objects[type.index].name + "Delta?";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    propertyDeltaType = schema.enums[type.index].name + "Delta?";
                    break;

                default:
                    return null;
            }

            return propertyDeltaType;
        }

        public static string GetPropertyDeltaListType(Schema schema, reflection.Type type)
        {
            string listDeltaType;

            switch(type.element)
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
                    listDeltaType = schema.objects[type.index].name + "ListDelta";
                    break;

                case BaseType.Union:
                case BaseType.UType:
                    listDeltaType = schema.enums[type.index].name + "ListDelta";
                    break;

                default:
                    return null;
            }

            if(PropertyListTypeIsBuiltInScalar(type) && type.index != -1)
            {
                listDeltaType = schema.enums[type.index].name + "ListDelta";
            }

            return $"IReadOnlyList<{listDeltaType}>?";
        }

        public static string GetArrayDeltaType(Schema schema, reflection.Type type)
        {
            return GetPropertyDeltaType(schema, type, true);
        }

        public static string GetPropertyDefaultValue(Schema schema, Field field, bool isArray = false)
        {
            if(field.optional && !isArray)
            {
                return "null";
            }

            switch(!isArray ? field.type.base_type : field.type.element)
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
                    return $"{(field.type.index != -1 ? $"({schema.enums[field.type.index].name})" : String.Empty)}" + field.default_integer.ToString();

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

        public static string GetArrayDefaultValue(Schema schema, Field field)
        {
            return GetPropertyDefaultValue(schema, field, true);
        }

        public static bool PropertyTypeIsDerived(Schema schema, reflection.Type type)
        {
            switch(type.base_type)
            {
                case BaseType.Obj:
                    reflection.Object obj = schema.objects[type.index];
                    return !(obj.is_struct && obj.HasAttribute("fs_valueStruct"));

                case BaseType.Union:
                case BaseType.UType:
                    reflection.Enum _enum = schema.enums[type.index];
                    return _enum.is_union;

                case BaseType.Vector:
                    return true;

                default:
                    return false;
            }
        }

        public static bool PropertyTypeIsValueStruct(Schema schema, reflection.Type type)
        {
            if(type.base_type == BaseType.Obj)
            {
                reflection.Object obj = schema.objects[type.index];
                return obj.is_struct && obj.HasAttribute("fs_valueStruct");
            }

            return false;
        }

        public static bool PropertyListTypeIsValueStruct(Schema schema, reflection.Type type)
        {
            if(type.element == BaseType.Obj)
            {
                reflection.Object obj = schema.objects[type.index];
                return obj.is_struct && obj.HasAttribute("fs_valueStruct");
            }

            return false;
        }

        public static bool PropertyTypeIsBuiltInScalar(reflection.Type type)
        {
            switch(type.base_type)
            {
                case BaseType.Bool:
                case BaseType.Byte:
                case BaseType.UByte:
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Int:
                case BaseType.UInt:
                case BaseType.Float:
                case BaseType.Long:
                case BaseType.ULong:
                case BaseType.Double:
                     return true;

                default:
                    return false;
            }
        }

        public static bool PropertyListTypeIsBuiltInScalar(reflection.Type type)
        {
            switch(type.element)
            {
                case BaseType.Bool:
                case BaseType.Byte:
                case BaseType.UByte:
                case BaseType.Short:
                case BaseType.UShort:
                case BaseType.Int:
                case BaseType.UInt:
                case BaseType.Float:
                case BaseType.Long:
                case BaseType.ULong:
                case BaseType.Double:
                     return true;

                default:
                    return false;
            }
        }

        public static int GetDeltaFieldsCount(Schema schema, reflection.Object obj)
        {
            int count = 0;

            obj.ForEachFieldExceptUType(field =>
            {
                if(field.type.base_type == BaseType.Array)
                {
                    count += field.type.fixed_length;

                    if(!PropertyListTypeIsBuiltInScalar(field.type) && !PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        count += field.type.fixed_length;
                    }
                }
                else
                {
                    count++;

                    if(PropertyTypeIsDerived(schema, field.type))
                    {
                        count++;
                    }
                }
            });

            return count;
        }

        public static string GetExtensionsType(Schema schema, reflection.Type type, bool isArray = false)
        {
            reflection.Object tempObj = new reflection.Object
            {
                name = !isArray ? CodeWriterUtils.GetPropertyType(schema, type) : schema.objects[type.index].name
            };

            if(tempObj.name.LastIndexOf(".") < 0)
            {
                return null;
            }
           
            return tempObj.GetNamespace() + ".SupportingTypes." + tempObj.GetNameWithoutNamespace() + "Extensions";
        }

        public static string GetArrayExtensionsType(Schema schema, reflection.Type type)
        {
            return GetExtensionsType(schema, type, true);
        }
    }
}