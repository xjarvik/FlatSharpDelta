using System;
using System.Reflection;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedBaseType
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

            if(copy == null)
            {
                obj = Activator.CreateInstance(type);
            }
            else
            {
                obj = Activator.CreateInstance(type, copy);
            }
        }

        public GeneratedBaseType(Assembly assembly, string name, GeneratedBaseType copy = null)
        {
            this.assembly = assembly;
            this.name = name;

            type = assembly.GetType(name);

            if(copy == null)
            {
                obj = Activator.CreateInstance(type);
            }
            else
            {
                obj = Activator.CreateInstance(type, copy.NativeObject);
            }
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

        public static bool operator==(GeneratedBaseType obj1, GeneratedBaseType obj2) => obj1.obj == obj2.obj;

        public static bool operator!=(GeneratedBaseType obj1, GeneratedBaseType obj2) => obj1.obj != obj2.obj;

        public override bool Equals(object obj)
        {
            if(obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            return this.obj == ((GeneratedBaseType)obj).obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}