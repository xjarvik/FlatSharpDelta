using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class TableCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();

            string code = $@"
namespace {_namespace}
{{
    {GetUsings(obj)}

    public partial class {name} : Base{name}, IFlatBufferSerializable<{name}>
    {{
        {GetMembers(obj)}

        {GetIndexes(obj)}

        {GetProperties(schema, obj)}

        {GetInitialize(obj)}

#pragma warning disable CS8618
        {GetDefaultConstructor(obj)}

        {GetCopyConstructor(schema, obj)}
#pragma warning restore CS8618
    }}
            ";

            return code;
        }

        private static string GetUsings(reflection.Object obj)
        {
            string _namespace = obj.GetNamespace();

            return $@"
    using {_namespace}.SupportingTypes;
            ";
        }

        private static string GetMembers(reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
        private MutableBase{name} original;
        private Mutable{name}Delta delta;
        private List<byte> byteFields;
        {(obj.fields.Count > 255 ? "private List<ushort> shortFields;" : String.Empty)}
        private static {name}Serializer _Serializer = new {name}Serializer();
        public static new ISerializer<{name}> Serializer => _Serializer;
        ISerializer IFlatBufferSerializable.Serializer => _Serializer;
        ISerializer<{name}> IFlatBufferSerializable<{name}>.Serializer => _Serializer;
            ";
        }

        private static string GetIndexes(reflection.Object obj)
        {
            string indexes = String.Empty;

            for(int i = 0; i < obj.fields.Count; i++)
            {
                Field field = obj.fields[i];
                string type = i <= 255 ? "byte" : "ushort";

                indexes += $@"
        private const {type} {field.name}_Index = {i};";
            }

            return indexes;
        }

        private static string GetProperties(Schema schema, reflection.Object obj)
        {
            string properties = String.Empty;

            foreach(Field field in obj.fields)
            {
                string type = !(field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array) ?
                    CodeWriterUtils.GetPropertyType(schema, field) :
                    CodeWriterUtils.GetPropertyListType(schema, field);
                bool privateMember = CodeWriterUtils.PropertyRequiresPrivateMember(schema, field);
                bool isUnion = field.type.base_type == BaseType.Union || field.type.base_type == BaseType.UType;

                properties += $@"
        {(privateMember ? $"private new {type} _{field.name};" : String.Empty)}
        public new {type} {field.name}
        {{
            get => {(privateMember ? $"_{field.name}" : $"base.{field.name}")};
            {(privateMember ?
            $@"set
            {{
                _{field.name} = value;
                base.{field.name} = value{(isUnion ? "?.Base" : String.Empty)};
            }}" :
            $"set => base.{field.name} = value;"
            )}
        }}
                ";
            }

            return properties;
        }

        private static string GetInitialize(reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
        private void Initialize(){{
            original = new MutableBase{name}();
            delta = new Mutable{name}Delta();
            byteFields = new List<byte>();
            {(obj.fields.Count > 255 ? "shortFields = new List<ushort>();" : String.Empty)}
        }}
            ";
        }

        private static string GetDefaultConstructor(reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
        public {name}(){{
            Initialize();
        }}
            ";
        }

        private static string GetCopyConstructor(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string fieldCopies = String.Empty;

            foreach(Field field in obj.fields)
            {
                if(!CodeWriterUtils.PropertyRequiresPrivateMember(schema, field))
                {
                    fieldCopies += $@"
            {field.name} = b.{field.name}";
                }
                else
                {

                }
            }

            return $@"
        public {name}(Base{name} b){{
            Initialize();
            {fieldCopies}
            UpdateInternalReferenceState();
        }}
            ";
        }
    }
}