using System;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReflectionExtensions
    {
        // reflection.Schema

        public static void ReplaceMatchingDeclarationFiles(this Schema schema, string declarationFileToMatch, string replacementDeclarationFile)
        {
            foreach(reflection.Object obj in schema.objects)
            {
                obj.ReplaceMatchingDeclarationFile(declarationFileToMatch, replacementDeclarationFile);
            }

            foreach(reflection.Enum _enum in schema.enums)
            {
                _enum.ReplaceMatchingDeclarationFile(declarationFileToMatch, replacementDeclarationFile);
            }

            foreach(Service service in schema.services)
            {
                service.ReplaceMatchingDeclarationFile(declarationFileToMatch, replacementDeclarationFile);
            }
        }


        // reflection.Object

        public static bool HasAttribute(this reflection.Object obj, string key) => obj.attributes != null && obj.attributes.Any(kv => kv.key == key);

        public static KeyValue GetAttribute(this reflection.Object obj, string key) => obj.attributes != null ? obj.attributes.First(kv => kv.key == key) : null;

        public static void SetAttribute(this reflection.Object obj, string key, string value = null)
        {
            if(obj.HasAttribute(key))
            {
                obj.GetAttribute(key).value = value;
            }
            else
            {
                if(obj.attributes == null)
                {
                    obj.attributes = new List<KeyValue>();
                }

                obj.attributes.Add(new KeyValue
                {
                    key = key,
                    value = value
                });
            }
        }

        public static bool RemoveAttribute(this reflection.Object obj, string key) => obj.HasAttribute(key) ? obj.attributes.Remove(obj.GetAttribute(key)) : false;

        public static string GetNamespace(this reflection.Object obj) => obj.name.Substring(0, obj.name.LastIndexOf("."));

        public static string GetNameWithoutNamespace(this reflection.Object obj) => obj.name.Substring(obj.name.LastIndexOf(".") + 1);

        public static void ReplaceMatchingDeclarationFile(this reflection.Object obj, string declarationFileToMatch, string replacementDeclarationFile)
        {
            if(obj.declaration_file == declarationFileToMatch)
            {
                obj.declaration_file = replacementDeclarationFile;
            }
        }

        public static void ForEachFieldExceptUType(this reflection.Object obj, Action<Field, int> callback)
        {
            int i = 0;

            obj.fields.OrderBy(f => f.id).ToList().ForEach(field =>
            {
                if(field.type.base_type != BaseType.UType && field.type.element != BaseType.UType)
                {
                    callback(field, i);
                    i++;
                }
            });
        }

        public static void ForEachFieldExceptUType(this reflection.Object obj, Action<Field> callback) => obj.ForEachFieldExceptUType((field, _) => callback(field));
        

        // reflection.Enum

        public static string GetNamespace(this reflection.Enum _enum) => _enum.name.Substring(0, _enum.name.LastIndexOf("."));

        public static string GetNameWithoutNamespace(this reflection.Enum _enum) => _enum.name.Substring(_enum.name.LastIndexOf(".") + 1);

        public static void ReplaceMatchingDeclarationFile(this reflection.Enum _enum, string declarationFileToMatch, string replacementDeclarationFile)
        {
            if(_enum.declaration_file == declarationFileToMatch)
            {
                _enum.declaration_file = replacementDeclarationFile;
            }
        }


        // reflection.Field

        public static bool HasAttribute(this Field field, string key) => field.attributes != null && field.attributes.Any(kv => kv.key == key);

        public static KeyValue GetAttribute(this Field field, string key) => field.attributes != null ? field.attributes.First(kv => kv.key == key) : null;

        public static void SetAttribute(this Field field, string key, string value = null)
        {
            if(field.HasAttribute(key))
            {
                field.GetAttribute(key).value = value;
            }
            else
            {
                if(field.attributes == null)
                {
                    field.attributes = new List<KeyValue>();
                }

                field.attributes.Add(new KeyValue
                {
                    key = key,
                    value = value
                });
            }
        }

        public static bool RemoveAttribute(this Field field, string key) => field.HasAttribute(key) ? field.attributes.Remove(field.GetAttribute(key)) : false;


        // reflection.Service

        public static void ReplaceMatchingDeclarationFile(this Service service, string declarationFileToMatch, string replacementDeclarationFile)
        {
            if(service.declaration_file == declarationFileToMatch)
            {
                service.declaration_file = replacementDeclarationFile;
            }
        }
    }
}