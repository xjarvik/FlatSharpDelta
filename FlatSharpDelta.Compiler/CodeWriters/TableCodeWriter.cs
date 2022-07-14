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

        {GetIndexes(schema, obj)}

        {GetProperties(schema, obj)}

        {GetInitialize(obj)}

#pragma warning disable CS8618
        {GetDefaultConstructor(obj)}

        {GetCopyConstructor(schema, obj)}
#pragma warning restore CS8618

        {GetDelta(schema, obj)}
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

        private static string GetIndexes(Schema schema, reflection.Object obj)
        {
            string indexes = String.Empty;
            int offset = 0;

            for(int i = 0; i < obj.fields.Count; i++)
            {
                Field field = obj.fields[i];

                int index = i + offset;
                indexes += $@"
        private const {(index <= 255 ? "byte" : "ushort")} {field.name}_Index = {index};";

                if(CodeWriterUtils.PropertyTypeIsDerived(schema, field))
                {
                    offset++;
                    index = i + offset;
                    indexes += $@"
        private const {(index <= 255 ? "byte" : "ushort")} {field.name}Delta_Index = {index};";
                }
            }

            return indexes;
        }

        private static string GetProperties(Schema schema, reflection.Object obj)
        {
            string properties = String.Empty;

            foreach(Field field in obj.fields)
            {
                bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;
                string type = !isArray ?
                    CodeWriterUtils.GetPropertyType(schema, field) :
                    CodeWriterUtils.GetPropertyListType(schema, field);
                bool privateMember = CodeWriterUtils.PropertyTypeIsDerived(schema, field);
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
        private void Initialize()
        {{
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
        public {name}()
        {{
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
                if(!CodeWriterUtils.PropertyTypeIsDerived(schema, field))
                {
                    fieldCopies += $@"
            {field.name} = b.{field.name};";
                }
                else
                {
                    bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;
                    string baseType = !isArray ?
                        CodeWriterUtils.GetPropertyBaseType(schema, field) :
                        CodeWriterUtils.GetPropertyBaseListType(schema, field);
                    string type = !isArray ?
                        CodeWriterUtils.GetPropertyType(schema, field) :
                        CodeWriterUtils.GetPropertyListType(schema, field);
                    
                    fieldCopies += $@"
            {baseType} b_{field.name} = b.{field.name};
            {field.name} = b_{field.name} != null ? new {type}(b_{field.name}) : null;";
                }
            }

            return $@"
        public {name}(Base{name} b)
        {{
            Initialize();
            {fieldCopies}
            UpdateInternalReferenceState();
        }}
            ";
        }

        private static string GetDelta(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string deltaComparisons = String.Empty;

            for(int i = 0; i < obj.fields.Count; i++)
            {
                Field field = obj.fields[i];
                bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;

                if(!CodeWriterUtils.PropertyTypeIsDerived(schema, field))
                {
                    deltaComparisons += GetScalarDeltaComparison(schema, field, i);
                }
                else if(field.type.base_type == BaseType.Obj || isArray)
                {
                    deltaComparisons += GetObjectDeltaComparison(schema, field, i);
                }
            }

            return $@"
        public {name}Delta? GetDelta()
        {{
            byteFields.Clear();
            {(obj.fields.Count > 255 ? "shortFields.Clear();" : String.Empty)}

            {deltaComparisons}

            delta.ByteFields = byteFields.Count > 0 ? byteFields : null;
            {(obj.fields.Count > 255 ? "delta.ShortFields = shortFields.Count > 0 ? shortFields : null;" : String.Empty)}

            return byteFields.Count > 0{(obj.fields.Count > 255 ? " || shortFields.Count > 0" : String.Empty)} ? delta : null;
        }}
            ";
        }

        private static string GetScalarDeltaComparison(Schema schema, Field field, int fieldIndex)
        {
            bool isValueStruct = false;

            if(field.type.base_type == BaseType.Obj)
            {
                reflection.Object obj = schema.objects[field.type.index];
                isValueStruct = obj.is_struct && obj.HasAttribute("fs_valueStruct");
            }
            
            string equalityCheck = !isValueStruct ?
                $"{field.name} != original.{field.name}" :
                $"!{field.name}.IsEqualTo(original.{field.name})";

            return $@"
            if({equalityCheck})
            {{
                delta.{field.name} = {field.name};
                {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
            }}
            else
            {{
                delta.{field.name} = {CodeWriterUtils.GetPropertyDefaultValue(schema, field)};
            }}
            ";
        }

        private static string GetObjectDeltaComparison(Schema schema, Field field, int fieldIndex)
        {
            bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;
            string deltaType = !isArray ?
                CodeWriterUtils.GetPropertyDeltaType(schema, field) :
                CodeWriterUtils.GetPropertyDeltaListType(schema, field);

            return $@"
            if({field.name} != original.{field.name})
            {{
                delta.{field.name} = {field.name};
                delta.{field.name}Delta = null;
                {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
            }}
            else if({field.name} != null)
            {{
                {deltaType} nestedDelta = {field.name}.GetDelta();
                if(nestedDelta != null)
                {{
                    delta.{field.name}Delta = nestedDelta;
                    {(fieldIndex + 1 <= 255 ? "byteFields" : "shortFields")}.Add({field.name}Delta_Index);
                }}
                else
                {{
                    delta.{field.name}Delta = null;
                }}
                delta.{field.name} = null;
            }}
            else
            {{
                delta.{field.name} = null;
                delta.{field.name}Delta = null;
            }}
            ";
        }
    }
}