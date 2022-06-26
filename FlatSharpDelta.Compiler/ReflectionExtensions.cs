using System;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReflectionExtensions
    {
        // reflection.Object

        public static bool HasAttribute(this reflection.Object obj, string key) => obj.attributes != null && obj.attributes.Any(kv => kv.key == key);

        public static KeyValue GetAttribute(this reflection.Object obj, string key) => obj.attributes != null ? obj.attributes.First(kv => kv.key == key) : null;

        public static void SetAttribute(this reflection.Object obj, string key, string value)
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

        public static string GetNamespace(this reflection.Object obj) => obj.name.Substring(0, obj.name.LastIndexOf("."));

        public static string GetNameWithoutNamespace(this reflection.Object obj) => obj.name.Substring(obj.name.LastIndexOf(".") + 1);
        

        // reflection.Enum

        public static string GetNamespace(this reflection.Enum _enum) => _enum.name.Substring(0, _enum.name.LastIndexOf("."));

        public static string GetNameWithoutNamespace(this reflection.Enum _enum) => _enum.name.Substring(_enum.name.LastIndexOf(".") + 1);


        // reflection.Field

        public static bool HasAttribute(this Field field, string key) => field.attributes != null && field.attributes.Any(kv => kv.key == key);

        public static KeyValue GetAttribute(this Field field, string key) => field.attributes != null ? field.attributes.First(kv => kv.key == key) : null;

        public static void SetAttribute(this Field field, string key, string value)
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
    }
}