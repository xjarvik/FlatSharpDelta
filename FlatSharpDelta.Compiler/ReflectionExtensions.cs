using System;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace reflection
{
    public interface INameProperty
    {
        string name { get; set; }
    }

    public interface IAttributesProperty
    {
        IList<KeyValue> attributes { get; set; }
    }

    public partial class Object : INameProperty, IAttributesProperty
    {
    }

    public partial class Enum : INameProperty, IAttributesProperty
    {
    }

    public partial class Service : INameProperty, IAttributesProperty
    {
    }

    public partial class Field : INameProperty, IAttributesProperty
    {
    }
}

namespace FlatSharpDelta.Compiler
{
    static class INamePropertyExtensions
    {
        public static string GetNamespace(this INameProperty self) => self.name.Substring(0, self.name.LastIndexOf("."));

        public static string GetNameWithoutNamespace(this INameProperty self) => self.name.Substring(self.name.LastIndexOf(".") + 1);
    }

    static class IAttributesPropertyExtensions
    {
        public static bool HasAttribute(this IAttributesProperty self, string key) => self.attributes != null && self.attributes.Any(kv => kv.key == key);

        public static KeyValue GetAttribute(this IAttributesProperty self, string key) => self.attributes != null ? self.attributes.First(kv => kv.key == key) : null;

        public static void SetAttribute(this IAttributesProperty self, string key, string value = null)
        {
            if (self.HasAttribute(key))
            {
                self.GetAttribute(key).value = value;
            }
            else
            {
                if (self.attributes == null)
                {
                    self.attributes = new List<KeyValue>();
                }

                self.attributes.Add
                (
                    new KeyValue
                    {
                        key = key,
                        value = value
                    }
                );
            }
        }

        public static bool RemoveAttribute(this IAttributesProperty self, string key) => self.HasAttribute(key) ? self.attributes.Remove(self.GetAttribute(key)) : false;
    }

    static class SchemaExtensions
    {
        public static void ReplaceMatchingDeclarationFiles(this Schema schema, string declarationFileToMatch, string replacementDeclarationFile)
        {
            foreach (reflection.Object obj in schema.objects)
            {
                if (obj.declaration_file == declarationFileToMatch)
                {
                    obj.declaration_file = replacementDeclarationFile;
                }
            }

            foreach (reflection.Enum _enum in schema.enums)
            {
                if (_enum.declaration_file == declarationFileToMatch)
                {
                    _enum.declaration_file = replacementDeclarationFile;
                }
            }

            foreach (Service service in schema.services)
            {
                if (service.declaration_file == declarationFileToMatch)
                {
                    service.declaration_file = replacementDeclarationFile;
                }
            }
        }

        private static bool BaseTypeIsScalar(BaseType baseType)
        {
            switch (baseType)
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

        public static bool TypeIsScalar(this Schema schema, reflection.Type type) => BaseTypeIsScalar(type.base_type) && type.index == -1;

        public static bool TypeIsScalarList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && BaseTypeIsScalar(type.element) && type.index == -1;

        public static bool TypeIsScalarArray(this Schema schema, reflection.Type type) => type.base_type == BaseType.Array && BaseTypeIsScalar(type.element) && type.index == -1;

        public static bool TypeIsString(this Schema schema, reflection.Type type) => type.base_type == BaseType.String;

        public static bool TypeIsStringList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && type.element == BaseType.String;

        public static bool TypeIsReferenceType(this Schema schema, reflection.Type type) => type.base_type == BaseType.Obj && schema.objects[type.index].IsReferenceType();

        public static bool TypeIsReferenceTypeList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && type.element == BaseType.Obj && schema.objects[type.index].IsReferenceType();

        public static bool TypeIsReferenceTypeArray(this Schema schema, reflection.Type type) => type.base_type == BaseType.Array && type.element == BaseType.Obj && schema.objects[type.index].IsReferenceType();

        public static bool TypeIsValueStruct(this Schema schema, reflection.Type type) => type.base_type == BaseType.Obj && schema.objects[type.index].IsValueStruct();

        public static bool TypeIsValueStructList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && type.element == BaseType.Obj && schema.objects[type.index].IsValueStruct();

        public static bool TypeIsValueStructArray(this Schema schema, reflection.Type type) => type.base_type == BaseType.Array && type.element == BaseType.Obj && schema.objects[type.index].IsValueStruct();

        public static bool TypeIsEnum(this Schema schema, reflection.Type type) => BaseTypeIsScalar(type.base_type) && type.index != -1;

        public static bool TypeIsEnumList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && BaseTypeIsScalar(type.element) && type.index != -1;

        public static bool TypeIsEnumArray(this Schema schema, reflection.Type type) => type.base_type == BaseType.Array && BaseTypeIsScalar(type.element) && type.index != -1;

        public static bool TypeIsUnion(this Schema schema, reflection.Type type) => type.base_type == BaseType.Union && schema.enums[type.index].IsUnion();

        public static bool TypeIsUnionList(this Schema schema, reflection.Type type) => type.base_type == BaseType.Vector && type.element == BaseType.Union && schema.enums[type.index].IsUnion();

        public static int GetFieldCountIncludingDeltaFields(this Schema schema, reflection.Object obj)
        {
            return obj.fields.Sum(field =>
            {
                if (field.type.base_type == BaseType.UType || field.type.element == BaseType.UType)
                {
                    return 0;
                }

                if (schema.TypeIsScalarList(field.type)
                || schema.TypeIsStringList(field.type)
                || schema.TypeIsReferenceType(field.type)
                || schema.TypeIsReferenceTypeList(field.type)
                || schema.TypeIsValueStructList(field.type)
                || schema.TypeIsEnumList(field.type)
                || schema.TypeIsUnion(field.type)
                || schema.TypeIsUnionList(field.type))
                {
                    return 2;
                }
                else if (schema.TypeIsScalarArray(field.type)
                || schema.TypeIsValueStructArray(field.type)
                || schema.TypeIsEnumArray(field.type))
                {
                    return field.type.fixed_length;
                }
                else if (schema.TypeIsReferenceTypeArray(field.type))
                {
                    return field.type.fixed_length * 2;
                }

                return 1;
            });
        }

        public static string GetNameOfObjectWithIndex(this Schema schema, int index) => schema.objects[index].name;

        public static int GetIndexOfObjectWithName(this Schema schema, string name) => schema.objects.IndexOf(schema.objects.First(obj => obj.name == name));

        public static string GetNameOfEnumWithIndex(this Schema schema, int index) => schema.enums[index].name;

        public static int GetIndexOfEnumWithName(this Schema schema, string name) => schema.enums.IndexOf(schema.enums.First(_enum => _enum.name == name));

        public static reflection.Type GetTypeFromObject(this Schema schema, reflection.Object obj)
        {
            return new reflection.Type
            {
                base_type = BaseType.Obj,
                index = schema.objects.IndexOf(obj)
            };
        }

        public static reflection.Type GetTypeFromEnum(this Schema schema, reflection.Enum _enum)
        {
            return new reflection.Type
            {
                base_type = _enum.IsUnion() ? BaseType.Union : _enum.underlying_type.base_type,
                index = schema.enums.IndexOf(_enum)
            };
        }
    }

    static class ObjectExtensions
    {
        public static void ForEachFieldExceptUType(this reflection.Object obj, Action<Field> callback)
        {
            foreach (Field field in obj.fields.OrderBy(f => f.id))
            {
                if (field.type.base_type != BaseType.UType && field.type.element != BaseType.UType)
                {
                    callback(field);
                }
            }
        }

        public static bool IsReferenceType(this reflection.Object obj)
        {
            KeyValue attribute = obj.GetAttribute("fs_valueStruct");
            return attribute == null || (attribute.value == "false" && obj.is_struct);
        }

        public static bool IsValueStruct(this reflection.Object obj)
        {
            KeyValue attribute = obj.GetAttribute("fs_valueStruct");
            return attribute != null && attribute.value != "false" && obj.is_struct;
        }
    }

    static class EnumExtensions
    {
        public static bool IsUnion(this reflection.Enum _enum) => _enum.is_union;
    }

    static class TypeExtensions
    {
        public static BaseType GetBaseTypeOrElement(this reflection.Type type) => type.element == BaseType.None ? type.base_type : type.element;
    }
}