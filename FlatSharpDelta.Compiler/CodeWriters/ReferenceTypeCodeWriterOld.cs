using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReferenceTypeCodeWriterOld
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();
            bool hasSerializer = obj.HasAttribute("fs_serializer");

            string code = $@"
                namespace {_namespace}
                {{
                    {GetUsages(obj)}

                    public partial class {name} : Base{name} {(hasSerializer ? $", IFlatBufferSerializable<{name}>" : String.Empty)}
                    {{
                        {GetMembers(schema, obj)}

                        {GetIndexes(schema, obj)}

                        {GetProperties(schema, obj)}

                        {GetInitialize(schema, obj)}

                #pragma warning disable CS8618
                        {GetDefaultConstructor(obj)}

                        {GetCopyConstructor(schema, obj)}
                #pragma warning restore CS8618

                        {GetDelta(schema, obj)}

                        {GetApplyDelta(schema, obj)}

                        {GetUpdateInternalReferenceState(obj)}

                        {GetUpdateReferenceState(schema, obj)}

                        {GetArrayClasses(schema, obj)}

                        {GetMutableBaseClass(schema, obj)}

                        {GetMutableDeltaClass(schema, obj)}

                        {(hasSerializer ? GetSerializerClass(obj) : String.Empty)}
                    }}
                }}
            ";

            return code;
        }

        private static string GetUsages(reflection.Object obj)
        {
            string _namespace = obj.GetNamespace();

            return $@"
                using {_namespace}.SupportingTypes;
            ";
        }

        private static string GetMembers(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            bool hasSerializer = obj.HasAttribute("fs_serializer");

            return $@"
                private MutableBase{name} original;
                private Mutable{name}Delta delta;
                private List<byte> byteFields;
                {(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? "private List<ushort> shortFields;" : String.Empty)}
                {(hasSerializer ?
                $@"
                private static {name}Serializer _Serializer = new {name}Serializer();
                public static new ISerializer<{name}> Serializer => _Serializer;
                ISerializer IFlatBufferSerializable.Serializer => _Serializer;
                ISerializer<{name}> IFlatBufferSerializable<{name}>.Serializer => _Serializer;
                " :
                String.Empty)}
            ";
        }

        private static string GetIndexes(Schema schema, reflection.Object obj)
        {
            string indexes = String.Empty;
            int index = 0;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        indexes += $@"
                            private const {(index <= 255 ? "byte" : "ushort")} {field.name}_{arrayIndex}_Index = {index++};
                        ";

                        if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                        && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                        {
                            indexes += $@"
                                private const {(index <= 255 ? "byte" : "ushort")} {field.name}_{arrayIndex}Delta_Index = {index++};
                            ";
                        }
                    }
                }
                else
                {
                    indexes += $@"
                        private const {(index <= 255 ? "byte" : "ushort")} {field.name}_Index = {index++};
                    ";

                    if (CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                    {
                        indexes += $@"
                            private const {(index <= 255 ? "byte" : "ushort")} {field.name}Delta_Index = {index++};
                        ";
                    }
                }
            });

            return indexes;
        }

        private static string GetProperties(Schema schema, reflection.Object obj)
        {
            string properties = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        properties += $@"
                            private __{field.name}_Vector? _{field.name};
                            public new __{field.name}_Vector {field.name} => (_{field.name} ??= new __{field.name}_Vector(this));
                        ";

                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            string type = schema.objects[field.type.index].name;

                            properties += $@"
                                private {type} __flatsharpdelta__{field.name}_{arrayIndex};
                                protected new {type} __flatsharp__{field.name}_{arrayIndex}
                                {{
                                    get => __flatsharpdelta__{field.name}_{arrayIndex};
                                    set
                                    {{
                                        __flatsharpdelta__{field.name}_{arrayIndex} = value;
                                        base.__flatsharp__{field.name}_{arrayIndex} = value;
                                    }}
                                }}
                            ";
                        }
                    }
                }
                else
                {
                    string type = field.type.base_type != BaseType.Vector ?
                        CodeWriterUtils.GetPropertyType(schema, field.type, field.optional) :
                        CodeWriterUtils.GetPropertyListType(schema, field.type);
                    bool privateMember = CodeWriterUtils.PropertyTypeIsDerived(schema, field.type);
                    bool isUnion = field.type.base_type == BaseType.Union;

                    properties += $@"
                        {(privateMember ? $"private {type} _{field.name};" : String.Empty)}
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
            });

            return properties;
        }

        private static string GetInitialize(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
                private void Initialize()
                {{
                    original = new MutableBase{name}();
                    delta = new Mutable{name}Delta();
                    byteFields = new List<byte>();
                    {(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? "shortFields = new List<ushort>();" : String.Empty)}
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

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        string baseType = CodeWriterUtils.GetPropertyBaseArrayType(schema, field).TrimEnd('?');
                        string type = schema.objects[field.type.index].name;

                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            fieldCopies += $@"
                                {baseType} b_{field.name}_{arrayIndex} = b.{field.name}[{arrayIndex}];
                                __flatsharp__{field.name}_{arrayIndex} = b_{field.name}_{arrayIndex} != null ? new {type}(b_{field.name}_{arrayIndex}) : null!;
                            ";
                        }
                    }
                    else
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            fieldCopies += $@"
                                __flatsharp__{field.name}_{arrayIndex} = b.{field.name}[{arrayIndex}];
                            ";
                        }
                    }
                }
                else if (!CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                {
                    fieldCopies += $@"
                        {field.name} = b.{field.name};
                    ";
                }
                else
                {
                    bool isUnion = field.type.base_type == BaseType.Union;
                    string baseType = field.type.base_type != BaseType.Vector ?
                        CodeWriterUtils.GetPropertyBaseType(schema, field.type, field.optional) :
                        CodeWriterUtils.GetPropertyBaseListType(schema, field.type, field.optional);
                    string type = field.type.base_type != BaseType.Vector ?
                        CodeWriterUtils.GetPropertyType(schema, field.type, field.optional) :
                        CodeWriterUtils.GetPropertyListType(schema, field.type);

                    fieldCopies += $@"
                        {baseType} b_{field.name} = b.{field.name};
                        {field.name} = b_{field.name} != null ? new {type.TrimEnd('?')}(b_{field.name}{(isUnion && field.optional ? ".Value" : String.Empty)}) : null!;
                    ";
                }
            });

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
            int index = 0;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            deltaComparisons += GetArrayObjectDeltaComparison(schema, field, index, arrayIndex);
                            index += 2;
                        }
                    }
                    else
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            deltaComparisons += GetArrayScalarDeltaComparison(schema, field, index, arrayIndex);
                            index++;
                        }
                    }
                }
                else if (!CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                {
                    deltaComparisons += GetScalarDeltaComparison(schema, field, index);
                    index++;
                }
                else if (field.type.base_type == BaseType.Obj || field.type.base_type == BaseType.Vector)
                {
                    deltaComparisons += GetObjectDeltaComparison(schema, field, index);
                    index += 2;
                }
                else if (field.type.base_type == BaseType.Union)
                {
                    deltaComparisons += GetUnionDeltaComparison(schema, field, index);
                    index += 2;
                }
            });

            return $@"
                public {name}Delta? GetDelta()
                {{
                    byteFields.Clear();
                    {(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? "shortFields.Clear();" : String.Empty)}

                    {deltaComparisons}

                    delta.ByteFields = byteFields.Count > 0 ? byteFields : null;
                    {(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? "delta.ShortFields = shortFields.Count > 0 ? shortFields : null;" : String.Empty)}

                    return byteFields.Count > 0{(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? " || shortFields.Count > 0" : String.Empty)} ? delta : null;
                }}
            ";
        }

        private static string GetArrayObjectDeltaComparison(Schema schema, Field field, int fieldIndex, int arrayIndex)
        {
            string deltaType = CodeWriterUtils.GetArrayDeltaType(schema, field.type);

            return $@"
                if(__flatsharp__{field.name}_{arrayIndex} != original.__flatsharp__{field.name}_{arrayIndex})
                {{
                    delta.{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                    delta.{field.name}_{arrayIndex}Delta = null;
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}_Index);
                }}
                else if(__flatsharp__{field.name}_{arrayIndex} != null)
                {{
                    {deltaType} nestedDelta = __flatsharp__{field.name}_{arrayIndex}.GetDelta();
                    if(nestedDelta != null)
                    {{
                        delta.{field.name}_{arrayIndex}Delta = nestedDelta;
                        {(fieldIndex + 1 <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}Delta_Index);
                    }}
                    else
                    {{
                        delta.{field.name}_{arrayIndex}Delta = null;
                    }}
                    delta.{field.name}_{arrayIndex} = null;
                }}
                else
                {{
                    delta.{field.name}_{arrayIndex} = null;
                    delta.{field.name}_{arrayIndex}Delta = null;
                }}
            ";
        }

        private static string GetArrayScalarDeltaComparison(Schema schema, Field field, int fieldIndex, int arrayIndex)
        {
            bool isValueStruct = CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type);
            string equalityCheck = String.Empty;

            if (!isValueStruct)
            {
                equalityCheck = $"__flatsharp__{field.name}_{arrayIndex} != original.__flatsharp__{field.name}_{arrayIndex}";
            }
            else
            {
                string extensionsType = CodeWriterUtils.GetArrayExtensionsType(schema, field.type);
                equalityCheck = $"!{extensionsType}.IsEqualTo(__flatsharp__{field.name}_{arrayIndex}, original.__flatsharp__{field.name}_{arrayIndex})";
            }

            return $@"
                if({equalityCheck})
                {{
                    delta.{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                    {(fieldIndex <= 255 ? "byteFields" : "shortFields")}.Add({field.name}_{arrayIndex}_Index);
                }}
                else
                {{
                    delta.{field.name}_{arrayIndex} = {CodeWriterUtils.GetArrayDefaultValue(schema, field)};
                }}
            ";
        }

        private static string GetScalarDeltaComparison(Schema schema, Field field, int fieldIndex, EnumVal enumVal = null)
        {
            reflection.Type type = enumVal != null ? enumVal.union_type : field.type;
            bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, type);
            string extensionsType = CodeWriterUtils.GetExtensionsType(schema, type);
            string equalityCheck = String.Empty;

            if (!isValueStruct)
            {
                equalityCheck = $"{field.name} != original.{field.name}";
            }
            else if (enumVal != null)
            {
                equalityCheck = $"!{extensionsType}.IsEqualTo({field.name}.Value.{enumVal.name}, original.{field.name}.Value.{enumVal.name})";
            }
            else
            {
                equalityCheck = $"!{extensionsType}.IsEqualTo({field.name}, original.{field.name})";
            }

            return $@"
                if({equalityCheck})
                {{
                    delta.{field.name} = {field.name}{(enumVal != null ? "?.Base" : String.Empty)};
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
            string deltaType = field.type.base_type != BaseType.Vector ?
                CodeWriterUtils.GetPropertyDeltaType(schema, field.type) :
                CodeWriterUtils.GetPropertyDeltaListType(schema, field.type);

            string nestedObject = String.Empty;

            if (enumVal != null)
            {
                string baseUnionType = CodeWriterUtils.GetPropertyBaseType(schema, enumVal.union_type);
                nestedObject = $"{baseUnionType} nestedObject = {field.name}.Value.Base.{enumVal.name};";
            }

            string firstEqualityCheck = String.Empty;

            if (enumVal != null)
            {
                firstEqualityCheck = $"nestedObject != original.{field.name}.Value.{enumVal.name}";
            }
            else
            {
                firstEqualityCheck = $"{field.name} != original.{field.name}";
            }

            string secondEqualityCheck = String.Empty;

            if (enumVal != null)
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

            for (int i = 0; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string comparison = String.Empty;

                if (!CodeWriterUtils.PropertyTypeIsDerived(schema, enumVal.union_type)
                && enumVal.union_type.base_type != BaseType.None)
                {
                    comparison = $@"
                        {GetScalarDeltaComparison(schema, field, fieldIndex, enumVal)}
                        delta.{field.name}Delta = null;
                    ";
                }
                else if (enumVal.union_type.base_type == BaseType.Obj)
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

        private static string GetApplyDelta(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            List<string> cases = new List<string>();

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            (string _case, string deltaCase) = GetApplyArrayObjectDelta(schema, field, arrayIndex);
                            cases.Add(_case);
                            cases.Add(deltaCase);
                        }
                    }
                    else
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            cases.Add(GetApplyArrayScalarDelta(schema, obj, field, arrayIndex));
                        }
                    }
                }
                else if (!CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                {
                    cases.Add(GetApplyScalarDelta(schema, obj, field));
                }
                else
                {
                    (string _case, string deltaCase) = GetApplyObjectDelta(schema, field);
                    cases.Add(_case);
                    cases.Add(deltaCase);
                }
            });

            return $@"
                public void ApplyDelta({name}Delta? delta)
                {{
                    if(delta == null)
                    {{
                        return;
                    }}

                    {(cases.Count > 0 ? $@"
                    IReadOnlyList<byte>? byteFields = delta.ByteFields;

                    if(byteFields != null)
                    {{
                        int count = byteFields.Count;

                        for(int i = 0; i < count; i++)
                        {{
                            byte field = byteFields[i];
                            switch(field)
                            {{
                                {String.Concat(cases.GetRange(0, Math.Min(cases.Count, 256)))}
                            }}
                        }}
                    }}
                    " :
                    String.Empty)}

                    {(cases.Count > 256 ? $@"
                    IReadOnlyList<ushort>? shortFields = delta.ShortFields;

                    if(shortFields != null)
                    {{
                        int count = shortFields.Count;

                        for(int i = 0; i < count; i++)
                        {{
                            ushort field = shortFields[i];
                            switch(field)
                            {{
                                {String.Concat(cases.GetRange(256, cases.Count - 256))}
                            }}
                        }}
                    }}
                    " :
                    String.Empty)}
                }}
            ";
        }

        private static (string, string) GetApplyArrayObjectDelta(Schema schema, Field field, int arrayIndex)
        {
            string baseType = CodeWriterUtils.GetPropertyBaseArrayType(schema, field);
            string type = schema.objects[field.type.index].name;

            string _case = $@"
                case {field.name}_{arrayIndex}_Index:
                {{
                    {baseType} nestedObject = delta.{field.name}_{arrayIndex};
                    __flatsharp__{field.name}_{arrayIndex} = nestedObject != null ? new {type}(nestedObject) : null!;
                    break;
                }}
            ";

            string deltaCase = $@"
                case {field.name}_{arrayIndex}Delta_Index:
                {{
                    __flatsharp__{field.name}_{arrayIndex}?.ApplyDelta(delta.{field.name}_{arrayIndex}Delta);
                    break;
                }}
            ";

            return (_case, deltaCase);
        }

        private static string GetApplyArrayScalarDelta(Schema schema, reflection.Object obj, Field field, int arrayIndex)
        {
            bool nullCoalescing = CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type);

            return $@"
                case {field.name}_{arrayIndex}_Index:
                {{
                    __flatsharp__{field.name}_{arrayIndex} = delta.{field.name}_{arrayIndex}{(nullCoalescing ? $" ?? __flatsharp__{field.name}_{arrayIndex}" : String.Empty)};
                    break;
                }}
            ";
        }

        private static string GetApplyScalarDelta(Schema schema, reflection.Object obj, Field field)
        {
            bool nullCoalescing = obj.is_struct && CodeWriterUtils.PropertyTypeIsValueStruct(schema, field.type);

            return $@"
                case {field.name}_Index:
                {{
                    {field.name} = delta.{field.name}{(nullCoalescing ? $" ?? {field.name}" : String.Empty)};
                    break;
                }}
            ";
        }

        private static (string, string) GetApplyObjectDelta(Schema schema, Field field)
        {
            bool isUnion = field.type.base_type == BaseType.Union;
            string baseType = field.type.base_type != BaseType.Vector ?
                CodeWriterUtils.GetPropertyBaseType(schema, field.type, field.optional) :
                CodeWriterUtils.GetPropertyBaseListType(schema, field.type, field.optional);
            string type = field.type.base_type != BaseType.Vector ?
                CodeWriterUtils.GetPropertyType(schema, field.type, field.optional) :
                CodeWriterUtils.GetPropertyListType(schema, field.type);

            string _case = $@"
                case {field.name}_Index:
                {{
                    {baseType} nestedObject = delta.{field.name};
                    {field.name} = nestedObject != null ? new {type.TrimEnd('?')}(nestedObject{(isUnion && field.optional ? ".Value" : String.Empty)}) : null;
                    break;
                }}
            ";

            string deltaCase = $@"
                case {field.name}Delta_Index:
                {{
                    {field.name}?.ApplyDelta(delta.{field.name}Delta);
                    break;
                }}
            ";

            return (_case, deltaCase);
        }

        private static string GetUpdateInternalReferenceState(reflection.Object obj)
        {
            string assignments = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        assignments += $@"
                            original.__flatsharp__{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};
                        ";
                    }
                }
                else
                {
                    bool isUnion = field.type.base_type == BaseType.Union;

                    assignments += $@"
                        original.{field.name} = {field.name}{(isUnion ? "?.Base" : String.Empty)};
                    ";
                }
            });

            return $@"
                private void UpdateInternalReferenceState()
                {{
                    {assignments}
                }}
            ";
        }

        private static string GetUpdateReferenceState(Schema schema, reflection.Object obj)
        {
            string calls = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            calls += $@"
                                __flatsharp__{field.name}_{arrayIndex}?.UpdateReferenceState();
                            ";
                        }
                    }
                }
                else if (CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                {
                    calls += $@"
                        {field.name}?.UpdateReferenceState();
                    ";
                }
            });

            return $@"
                public void UpdateReferenceState()
                {{
                    UpdateInternalReferenceState();
                    {calls}
                }}
            ";
        }

        private static string GetArrayClasses(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string classes = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array
                && !CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                {
                    string baseType = CodeWriterUtils.GetPropertyBaseArrayType(schema, field);
                    string type = schema.objects[field.type.index].name;

                    string indexerGetCases = String.Empty;
                    string indexerSetCases = String.Empty;
                    string enumeratorYields = String.Empty;
                    string copyFromAssignments = String.Empty;

                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        indexerGetCases += $"case {arrayIndex}: return item.__flatsharp__{field.name}_{arrayIndex};";
                        indexerSetCases += $"case {arrayIndex}: item.__flatsharp__{field.name}_{arrayIndex} = value; break;";
                        enumeratorYields += $"yield return item.__flatsharp__{field.name}_{arrayIndex};";
                        copyFromAssignments += $@"
                            {baseType.TrimEnd('?')} source{arrayIndex} = source[{arrayIndex}];
                            item.__flatsharp__{field.name}_{arrayIndex} = source{arrayIndex} != null ? new {type}(source{arrayIndex}) : null!;
                        ";
                    }

                    classes += $@"
                        public new partial class __{field.name}_Vector : Base{name}.__{field.name}_Vector, IEnumerable<{type}>
                        {{
                            private readonly {name} item;

                            public __{field.name}_Vector({name} item) : base(item)
                            {{
                                this.item = item;
                            }}

                            public new {type} this[int index]
                            {{
                                get
                                {{
                                    switch(index)
                                    {{
                                        {indexerGetCases}
                                        default: throw new IndexOutOfRangeException();
                                    }}
                                }}
                                set
                                {{
                                    switch(index)
                                    {{
                                        {indexerSetCases}
                                        default: throw new IndexOutOfRangeException();
                                    }}
                                }}
                            }}

                            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

                            public new IEnumerator<{type}> GetEnumerator()
                            {{
                                {enumeratorYields}
                            }}

                            public new void CopyFrom(ReadOnlySpan<{baseType.TrimEnd('?')}> source)
                            {{
                                {copyFromAssignments}
                            }}

                            public void CopyFrom(ReadOnlySpan<{type}> source)
                            {{
                                {copyFromAssignments}
                            }}

                            public new void CopyFrom(IReadOnlyList<{baseType.TrimEnd('?')}> source)
                            {{
                                {copyFromAssignments}
                            }}
                        }}
                    ";
                }
            });

            return classes;
        }

        private static string GetMutableBaseClass(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string properties = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    string baseType = CodeWriterUtils.GetPropertyBaseArrayType(schema, field);

                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        properties += $@"
                            public new {baseType.TrimEnd('?')} __flatsharp__{field.name}_{arrayIndex}
                            {{
                                get => base.__flatsharp__{field.name}_{arrayIndex};
                                set => base.__flatsharp__{field.name}_{arrayIndex} = value;
                            }}
                        ";
                    }
                }
                else
                {
                    string baseType = field.type.base_type != BaseType.Vector ?
                        CodeWriterUtils.GetPropertyBaseType(schema, field.type, field.optional) :
                        CodeWriterUtils.GetPropertyBaseListType(schema, field.type, field.optional);

                    properties += $@"
                        public new {baseType} {field.name}
                        {{
                            get => base.{field.name};
                            set => base.{field.name} = value;
                        }}
                    ";
                }
            });

            return $@"
                private class MutableBase{name} : Base{name}
                {{
                    {properties}
                }}
            ";
        }

        private static string GetMutableDeltaClass(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string properties = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    string baseType = CodeWriterUtils.GetPropertyBaseArrayType(schema, field);

                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        properties += $@"
                            public new {baseType} {field.name}_{arrayIndex}
                            {{
                                get => base.{field.name}_{arrayIndex};
                                set => base.{field.name}_{arrayIndex} = value;
                            }}
                        ";
                    }

                    if (!CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type)
                    && !CodeWriterUtils.PropertyListTypeIsValueStruct(schema, field.type))
                    {
                        string deltaType = CodeWriterUtils.GetArrayDeltaType(schema, field.type);

                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            properties += $@"
                                public new {deltaType} {field.name}_{arrayIndex}Delta
                                {{
                                    get => base.{field.name}_{arrayIndex}Delta;
                                    set => base.{field.name}_{arrayIndex}Delta = value;
                                }}
                            ";
                        }
                    }
                }
                else
                {
                    string baseType = field.type.base_type != BaseType.Vector ?
                        CodeWriterUtils.GetPropertyBaseType(schema, field.type, field.optional) :
                        CodeWriterUtils.GetPropertyBaseListType(schema, field.type, field.optional);

                    properties += $@"
                        public new {baseType} {field.name}
                        {{
                            get => base.{field.name};
                            set => base.{field.name} = value;
                        }}
                    ";

                    if (CodeWriterUtils.PropertyTypeIsDerived(schema, field.type))
                    {
                        string deltaType = field.type.base_type != BaseType.Vector ?
                            CodeWriterUtils.GetPropertyDeltaType(schema, field.type) :
                            CodeWriterUtils.GetPropertyDeltaListType(schema, field.type);

                        properties += $@"
                            public new {deltaType} {field.name}Delta
                            {{
                                get => base.{field.name}Delta;
                                set => base.{field.name}Delta = value;
                            }}
                        ";
                    }
                }
            });

            return $@"
                private class Mutable{name}Delta : {name}Delta
                {{
                    private List<byte>? _ByteFields {{ get; set; }}
                    public new List<byte>? ByteFields
                    {{
                        get => _ByteFields;
                        set
                        {{
                            _ByteFields = value;
                            base.ByteFields = value;
                        }}
                    }}

                    {(CodeWriterUtils.GetDeltaFieldsCount(schema, obj) > 256 ? $@"
                    private List<ushort>? _ShortFields {{ get; set; }}
                    public new List<ushort>? ShortFields
                    {{
                        get => _ShortFields;
                        set
                        {{
                            _ShortFields = value;
                            base.ShortFields = value;
                        }}
                    }}
                    " :
                    String.Empty)}

                    {properties}
                }}
            ";
        }

        private static string GetSerializerClass(reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();

            return $@"
                private class {name}Serializer : ISerializer<{name}>
                {{
                    private ISerializer<Base{name}> baseSerializer;
                    private Type _RootType = typeof({name});
                    public Type RootType {{ get => _RootType; }}
                    public string? CSharp {{ get => null; }}
                    public Assembly? Assembly {{ get => null; }}
                    public byte[]? AssemblyBytes {{ get => null; }}
                    public FlatBufferDeserializationOption DeserializationOption {{ get => FlatBufferDeserializationOption.GreedyMutable; }}

                    public {name}Serializer()
                    {{
                        baseSerializer = Base{name}.Serializer;
                    }}

                    public {name}Serializer(ISerializer<Base{name}> baseSerializer)
                    {{
                        this.baseSerializer = baseSerializer;
                    }}

                    public int GetMaxSize({name} item) => baseSerializer.GetMaxSize(item);

                    public int GetMaxSize(object item) => baseSerializer.GetMaxSize(item);

                    public int Write<TSpanWriter>(TSpanWriter writer, Span<byte> destination, {name} item) where TSpanWriter : ISpanWriter => baseSerializer.Write(writer, destination, item);

                    public int Write<TSpanWriter>(TSpanWriter writer, Span<byte> destination, object item) where TSpanWriter : ISpanWriter => baseSerializer.Write(writer, destination, item);

                    public {name} Parse<TInputBuffer>(TInputBuffer buffer) where TInputBuffer : IInputBuffer => new {name}(baseSerializer.Parse<TInputBuffer>(buffer));

                    object ISerializer.Parse<TInputBuffer>(TInputBuffer buffer) => new {name}(baseSerializer.Parse<TInputBuffer>(buffer));

                    public ISerializer<{name}> WithSettings(SerializerSettings settings) => new {name}Serializer(baseSerializer.WithSettings(settings));
                }}
            ";
        }
    }
}