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
using System.Reflection;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public abstract class GeneratedBaseType
    {
        protected Assembly assembly;
        protected string name;
        protected Type type;
        protected object obj;

        public object NativeObject => obj;

        public GeneratedBaseType()
        {
        }

        public GeneratedBaseType(object obj)
        {
            this.obj = obj;
            type = obj.GetType();
            assembly = type.Assembly;
            name = type.Name;
        }

        public GeneratedBaseType(Assembly assembly, string name, object copy = null)
        {
            this.assembly = assembly;
            this.name = name;

            type = assembly.GetType(name);

            if (copy == null)
            {
                obj = Activator.CreateInstance(type);
            }
            else
            {
                obj = Activator.CreateInstance(type, copy);
            }
        }

        public GeneratedBaseType(Assembly assembly, string name, GeneratedBaseType copy) : this(assembly, name, copy.NativeObject)
        {
        }

        public object GetIndexerProperty(object index)
        {
            return type.GetProperties().First(property => property.Name == "Item").GetValue(obj, new object[] { index });
        }

        public T GetIndexerProperty<T>(object index) where T : GeneratedBaseType, new()
        {
            T gbt = new T();
            gbt.obj = GetIndexerProperty(index);
            gbt.type = gbt.obj.GetType();
            gbt.assembly = gbt.type.Assembly;
            gbt.name = gbt.type.Name;

            return gbt;
        }

        public object GetProperty(string propertyName)
        {
            return type.GetProperties().First(property => property.Name == propertyName).GetValue(obj);
        }

        public T GetProperty<T>(string propertyName) where T : GeneratedBaseType, new()
        {
            T gbt = new T();
            gbt.obj = GetProperty(propertyName);
            gbt.type = gbt.obj.GetType();
            gbt.assembly = gbt.type.Assembly;
            gbt.name = gbt.type.Name;

            return gbt;
        }

        public object GetField(string fieldName)
        {
            return type.GetFields().First(field => field.Name == fieldName).GetValue(obj);
        }

        public T GetField<T>(string fieldName) where T : GeneratedBaseType, new()
        {
            T gbt = new T();
            gbt.obj = GetField(fieldName);
            gbt.type = gbt.obj.GetType();
            gbt.assembly = gbt.type.Assembly;
            gbt.name = gbt.type.Name;

            return gbt;
        }

        public object GetArrayItem(string fieldName, int index)
        {
            return type.GetField($"__flatsharp__{fieldName}_{index}", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        }

        public void SetIndexerProperty(object index, object value)
        {
            type.GetProperties().First(property => property.Name == "Item").SetValue(obj, value, new object[] { index });
        }

        public void SetIndexerProperty(object index, GeneratedBaseType value)
        {
            type.GetProperties().First(property => property.Name == "Item").SetValue(obj, value.NativeObject, new object[] { index });
        }

        public void SetProperty(string propertyName, object value)
        {
            type.GetProperties().First(property => property.Name == propertyName).SetValue(obj, value);
        }

        public void SetProperty(string propertyName, GeneratedBaseType value)
        {
            type.GetProperties().First(property => property.Name == propertyName).SetValue(obj, value.NativeObject);
        }

        public void SetField(string fieldName, object value)
        {
            type.GetFields().First(field => field.Name == fieldName).SetValue(obj, value);
        }

        public void SetField(string fieldName, GeneratedBaseType value)
        {
            type.GetFields().First(field => field.Name == fieldName).SetValue(obj, value.NativeObject);
        }

        public void SetArrayItem(string fieldName, int index, object value)
        {
            type.GetField($"__flatsharp__{fieldName}_{index}", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        }

        public static bool operator ==(GeneratedBaseType obj1, GeneratedBaseType obj2)
        {
            bool obj1IsNull = ReferenceEquals(obj1, null);
            bool obj2IsNull = ReferenceEquals(obj2, null);

            if (obj1IsNull && obj2IsNull)
            {
                return true;
            }
            else if (!obj1IsNull && !obj2IsNull && ReferenceEquals(obj1.obj, obj2.obj))
            {
                return true;
            }

            return false;
        }

        public static bool operator !=(GeneratedBaseType obj1, GeneratedBaseType obj2)
        {
            bool obj1IsNull = ReferenceEquals(obj1, null);
            bool obj2IsNull = ReferenceEquals(obj2, null);

            if (obj1IsNull && obj2IsNull)
            {
                return false;
            }
            else if (!obj1IsNull && !obj2IsNull && ReferenceEquals(obj1.obj, obj2.obj))
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.obj == ((GeneratedBaseType)obj).obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static byte[] Serialize(object obj, bool useBaseType = false)
        {
            Type type = useBaseType ? obj.GetType().BaseType : obj.GetType();
            object serializer = type.GetProperty("Serializer").GetValue(null, null);
            Type serializerType = serializer.GetType();
            Type extensionSerializerType = typeof(FlatSharp.ISerializerExtensions);

            int maxSize = (int)serializerType.InvokeMember("GetMaxSize",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                serializer,
                new object[] { obj }
            );

            byte[] bytes = new byte[maxSize];

            extensionSerializerType.InvokeMember("Write",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                null,
                new object[] { serializer, bytes, obj }
            );

            return bytes;
        }

        public static byte[] Serialize(GeneratedBaseType obj, bool useBaseType = false)
        {
            return Serialize(obj.NativeObject, useBaseType);
        }

        public static object Deserialize(Assembly assembly, string name, byte[] bytes)
        {
            object serializer = new GeneratedType(assembly, name).NativeObject.GetType().GetProperty("Serializer").GetValue(null, null);
            Type serializerType = serializer.GetType();
            Type extensionSerializerType = typeof(FlatSharp.ISerializerExtensions);

            object obj = extensionSerializerType.InvokeMember("Parse",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                null,
                new object[] { serializer, bytes, null }
            );

            return obj;
        }

        public static T Deserialize<T>(Assembly assembly, string name, byte[] bytes) where T : GeneratedBaseType, new()
        {
            object nativeObject = Deserialize(assembly, name, bytes);

            T gbt = new T();
            gbt.obj = nativeObject;
            gbt.type = nativeObject.GetType();
            gbt.assembly = assembly;
            gbt.name = name;

            return gbt;
        }
    }
}