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
using System.Collections.Generic;
using System.Linq;
using System.IO;
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

    public interface IDeclarationFileProperty
    {
        string declaration_file { get; set; }
    }

    public partial class Object : INameProperty, IAttributesProperty, IDeclarationFileProperty
    {
    }

    public partial class Enum : INameProperty, IAttributesProperty, IDeclarationFileProperty
    {
    }

    public partial class Service : INameProperty, IAttributesProperty, IDeclarationFileProperty
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

    static class IDeclarationFilePropertyExtensions
    {
        public static string GetDeclarationFileString(string declarationFilePath, string declarationFileRelativeTo) => $"//{Path.GetRelativePath(declarationFileRelativeTo, declarationFilePath).Replace("\\", "/")}";

        public static bool DeclarationFilePathsAreEqual(string path1, string path1Base, string path2, string path2Base)
        {
            path1 = path1.Replace("\\", "/").Replace("/:/", "/").TrimStart('/');
            path1Base = path1Base.Replace("\\", "/").Replace("/:/", "/");
            path2 = path2.Replace("\\", "/").Replace("/:/", "/").TrimStart('/');
            path2Base = path2Base.Replace("\\", "/").Replace("/:/", "/");

            return Path.GetFullPath(path1, path1Base) == Path.GetFullPath(path2, path2Base);
        }
    }

    static class SchemaExtensions
    {
        public static void ForEachDeclarationFileProperty(this Schema schema, Action<IDeclarationFileProperty> callback)
        {
            foreach (reflection.Object obj in schema.objects)
            {
                callback(obj);
            }

            foreach (reflection.Enum _enum in schema.enums)
            {
                callback(_enum);
            }

            foreach (Service service in schema.services)
            {
                callback(service);
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

                if (schema.TypeIsScalarArray(field.type)
                || schema.TypeIsValueStructArray(field.type)
                || schema.TypeIsEnumArray(field.type))
                {
                    return field.type.fixed_length;
                }
                else if (schema.TypeIsReferenceTypeArray(field.type))
                {
                    return field.type.fixed_length * 2;
                }
                else if (schema.TypeHasDeltaType(field.type))
                {
                    return 2;
                }

                return 1;
            });
        }

        public static bool TypeHasDeltaType(this Schema schema, reflection.Type type)
        {
            return schema.TypeIsScalarList(type)
                || schema.TypeIsStringList(type)
                || schema.TypeIsReferenceType(type)
                || schema.TypeIsReferenceTypeList(type)
                || schema.TypeIsValueStructList(type)
                || schema.TypeIsEnumList(type)
                || schema.TypeIsUnion(type)
                || schema.TypeIsUnionList(type);
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

        public static string GetCSharpType(this Schema schema, reflection.Type type, bool optional = false)
        {
            string csharpType = String.Empty;

            if (schema.TypeIsScalar(type))
            {
                csharpType = GetCSharpTypeFromScalarBaseType(type.base_type);
            }
            else if (schema.TypeIsString(type))
            {
                csharpType = "System.String?";
            }
            else if (schema.TypeIsReferenceType(type) || schema.TypeIsValueStruct(type))
            {
                csharpType = schema.GetNameOfObjectWithIndex(type.index) + (schema.TypeIsReferenceType(type) ? "?" : String.Empty);
            }
            else if (schema.TypeIsEnum(type) || schema.TypeIsUnion(type))
            {
                csharpType = schema.GetNameOfEnumWithIndex(type.index);
            }
            else if (schema.TypeIsScalarList(type) || schema.TypeIsStringList(type))
            {
                csharpType = "FlatSharpDelta." + type.element.ToString() + "List?";
            }
            else if (schema.TypeIsReferenceTypeList(type) || schema.TypeIsValueStructList(type))
            {
                csharpType = schema.GetNameOfObjectWithIndex(type.index) + "List?";
            }
            else if (schema.TypeIsEnumList(type) || schema.TypeIsUnionList(type))
            {
                csharpType = schema.GetNameOfEnumWithIndex(type.index) + "List?";
            }

            if (optional && !csharpType.EndsWith('?'))
            {
                csharpType += "?";
            }

            return csharpType;
        }

        private static string GetCSharpTypeFromScalarBaseType(BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Bool: return "System.Boolean";
                case BaseType.Byte: return "System.SByte";
                case BaseType.UByte: return "System.Byte";
                case BaseType.Short: return "System.Int16";
                case BaseType.UShort: return "System.UInt16";
                case BaseType.Int: return "System.Int32";
                case BaseType.UInt: return "System.UInt32";
                case BaseType.Float: return "System.Single";
                case BaseType.Long: return "System.Int64";
                case BaseType.ULong: return "System.UInt64";
                case BaseType.Double: return "System.Double";

                default: return String.Empty;
            }
        }

        public static string GetCSharpDeltaType(this Schema schema, reflection.Type type)
        {
            string csharpDeltaType = String.Empty;

            if (schema.TypeIsReferenceType(type))
            {
                csharpDeltaType = schema.GetNameOfObjectWithIndex(type.index) + "Delta?";
            }
            else if (schema.TypeIsUnion(type))
            {
                csharpDeltaType = schema.GetNameOfEnumWithIndex(type.index) + "Delta?";
            }
            else if (schema.TypeIsScalarList(type) || schema.TypeIsStringList(type))
            {
                csharpDeltaType = $"IReadOnlyList<{type.element.ToString()}ListDelta>?";
            }
            else if (schema.TypeIsReferenceTypeList(type) || schema.TypeIsValueStructList(type))
            {
                csharpDeltaType = $"IReadOnlyList<{schema.GetNameOfObjectWithIndex(type.index)}ListDelta>?";
            }
            else if (schema.TypeIsEnumList(type) || schema.TypeIsUnionList(type))
            {
                csharpDeltaType = $"IReadOnlyList<{schema.GetNameOfEnumWithIndex(type.index)}ListDelta>?";
            }

            return csharpDeltaType;
        }

        public static void NormalizeFieldNames(this Schema schema)
        {
            foreach (reflection.Object obj in schema.objects)
            {
                obj.NormalizeFieldNames();
            }
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

        public static bool IsReferenceType(this reflection.Object obj) => !obj.HasAttribute("fs_valueStruct") || (obj.GetAttribute("fs_valueStruct").value == "false" && obj.is_struct);

        public static bool IsValueStruct(this reflection.Object obj) => obj.HasAttribute("fs_valueStruct") && obj.GetAttribute("fs_valueStruct").value != "false" && obj.is_struct;

        public static void NormalizeFieldNames(this reflection.Object obj)
        {
            bool normalizeObj = !obj.HasAttribute("fs_literalName") || obj.GetAttribute("fs_literalName").value == "false";

            foreach (Field field in obj.fields)
            {
                bool normalizeField = !field.HasAttribute("fs_literalName") ? normalizeObj : field.GetAttribute("fs_literalName").value == "false";

                if (normalizeField)
                {
                    field.NormalizeFieldName();
                }
            }
        }
    }

    static class EnumExtensions
    {
        public static bool IsUnion(this reflection.Enum _enum) => _enum.is_union;
    }

    static class FieldExtensions
    {
        public static void NormalizeFieldName(this Field field)
        {
            string normalized = String.Empty;
            string[] parts = field.name.Split('_', StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                normalized += char.ToUpperInvariant(part[0]);
                if (part.Length > 1)
                {
                    normalized += part.Substring(1);
                }
            }

            field.name = normalized;
        }
    }

    static class TypeExtensions
    {
        public static BaseType GetBaseTypeOrElement(this reflection.Type type) => type.element == BaseType.None ? type.base_type : type.element;

        public static reflection.Type ToElementAsBaseType(this reflection.Type type)
        {
            return new reflection.Type
            {
                base_type = type.element,
                index = type.index
            };
        }
    }
}