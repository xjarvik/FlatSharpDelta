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
                bool isUnion = field.type.base_type == BaseType.Union;

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
            int offset = 0;

            for(int i = 0; i < obj.fields.Count; i++)
            {
                Field field = obj.fields[i];
                int index = i + offset;
                bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;
                bool isUnion = field.type.base_type == BaseType.Union;

                if(!CodeWriterUtils.PropertyTypeIsDerived(schema, field))
                {
                    deltaComparisons += GetScalarDeltaComparison(schema, field, index);
                }
                else if(field.type.base_type == BaseType.Obj || isArray)
                {
                    deltaComparisons += GetObjectDeltaComparison(schema, field, index);
                    offset++;
                }
                else if(isUnion)
                {
                    deltaComparisons += GetUnionDeltaComparison(schema, field, index);
                    offset++;
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

        private static string GetScalarDeltaComparison(Schema schema, Field field, int fieldIndex, EnumVal enumVal = null)
        {
            reflection.Type type = enumVal != null ? enumVal.union_type : field.type;
            bool isValueStruct = false;

            if(type.base_type == BaseType.Obj)
            {
                reflection.Object obj = schema.objects[type.index];
                isValueStruct = obj.is_struct && obj.HasAttribute("fs_valueStruct");
            }

            string equalityCheck = String.Empty;

            if(!isValueStruct)
            {
                equalityCheck = $"{field.name} != original.{field.name}";
            }
            else if(enumVal != null)
            {
                equalityCheck = $"!{field.name}.Value.{enumVal.name}.IsEqualTo(original.{field.name}.Value.{enumVal.name})";
            }
            else
            {
                equalityCheck = $"!{field.name}.IsEqualTo(original.{field.name})";
            }

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

        private static string GetObjectDeltaComparison(Schema schema, Field field, int fieldIndex, EnumVal enumVal = null)
        {
            bool isArray = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array;
            string deltaType = !isArray ?
                CodeWriterUtils.GetPropertyDeltaType(schema, field) :
                CodeWriterUtils.GetPropertyDeltaListType(schema, field);
            
            string nestedObject = String.Empty;

            if(enumVal != null)
            {
                string baseUnionType = CodeWriterUtils.GetPropertyBaseType(schema, new Field{ type = enumVal.union_type });
                nestedObject = $"{baseUnionType} nestedObject = {field.name}.Value.Base.{enumVal.name};";
            }

            string firstEqualityCheck = String.Empty;

            if(enumVal != null)
            {
                firstEqualityCheck = $"nestedObject != original.{field.name}.Value.{enumVal.name}";
            }
            else
            {
                firstEqualityCheck = $"{field.name} != original.{field.name}";
            }

            string secondEqualityCheck = String.Empty;

            if(enumVal != null)
            {
                secondEqualityCheck = "nestedObject != null";
            }
            else
            {
                secondEqualityCheck = $"{field.name} != null";
            }

            return $@"
            {nestedObject}
            if({firstEqualityCheck})
            {{
                delta.{field.name} = {field.name}{(enumVal != null ? "?.Base" : String.Empty)};
                delta.{field.name}Delta = null;
                {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
            }}
            else if({secondEqualityCheck})
            {{
                {deltaType} nestedDelta = {field.name}{(enumVal != null ? ".Value" : String.Empty)}.GetDelta();
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

        private static string GetUnionDeltaComparison(Schema schema, Field field, int fieldIndex)
        {
            reflection.Enum union = schema.enums[field.type.index];
            string discriminators = String.Empty;

            for(int i = 0; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string comparison = String.Empty;

                if(!CodeWriterUtils.PropertyTypeIsDerived(schema, new Field{ type = enumVal.union_type })
                && enumVal.union_type.base_type != BaseType.None)
                {
                    comparison = $@"{GetScalarDeltaComparison(schema, field, fieldIndex, enumVal)}
                        delta.{field.name}Delta = null;";
                }
                else if(enumVal.union_type.base_type == BaseType.Obj)
                {
                    comparison = GetObjectDeltaComparison(schema, field, fieldIndex, enumVal);
                }

                discriminators += $@"
                    case {i}:
                    {{
                        {comparison}
                        break;
                    }}
                ";
            }

            return $@"
            if({field.name} == null)
            {{
                delta.{field.name} = null;
                delta.{field.name}Delta = null;
                if(original.{field.name} != null)
                {{
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
                }}
            }}
            else if(original.{field.name} == null || {field.name}.Value.Discriminator != original.{field.name}.Value.Discriminator)
            {{
                delta.{field.name} = {field.name}?.Base;
                delta.{field.name}Delta = null;
                {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_Index);
            }}
            else
            {{
                switch({field.name}.Value.Discriminator)
                {{
                    {discriminators}
                }}
            }}
            ";
        }
    }
}