using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReferenceTypeCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();

            string code = $@"
                namespace {_namespace}
                {{
                    public partial class {name}
                    {{
                        {GetMembers(schema, obj)}

                        {GetFieldIndexes(schema, obj)}

                        {GetOnInitialized(schema, obj)}

                        {GetDelta(schema, obj)}

                        {GetApplyDelta(schema, obj)}

                        {GetUpdateInternalReferenceState(schema, obj)}

                        {GetUpdateReferenceState(schema, obj)}

                        {GetMutableDeltaClass(schema, obj)}
                    }}
                }}
            ";

            return code;
        }

        private static string GetMembers(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            int fieldCount = schema.GetFieldCountIncludingDeltaFields(obj);

            return $@"
                private {name} original = default!;
                private Mutable{name}Delta delta = default!;
                private List<byte> byteFields = default!;
                {(fieldCount > 256 ? "private List<ushort> shortFields = default!;" : String.Empty)}
            ";
        }

        private static string GetFieldIndexes(Schema schema, reflection.Object obj)
        {
            string fieldIndexes = String.Empty;
            int fieldIndex = 0;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        fieldIndexes += $"private const {(fieldIndex <= 255 ? "byte" : "ushort")} {field.name}_{arrayIndex}_Index = {fieldIndex++};";

                        if (schema.TypeIsReferenceTypeArray(field.type))
                        {
                            fieldIndexes += $"private const {(fieldIndex <= 255 ? "byte" : "ushort")} {field.name}_{arrayIndex}Delta_Index = {fieldIndex++};";
                        }
                    }
                }
                else
                {
                    fieldIndexes += $"private const {(fieldIndex <= 255 ? "byte" : "ushort")} {field.name}_Index = {fieldIndex++};";

                    if (schema.TypeHasDeltaType(field.type))
                    {
                        fieldIndexes += $"private const {(fieldIndex <= 255 ? "byte" : "ushort")} {field.name}Delta_Index = {fieldIndex++};";
                    }
                }
            });

            return fieldIndexes;
        }

        private static string GetOnInitialized(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            int fieldCount = schema.GetFieldCountIncludingDeltaFields(obj);
            string requiredFieldSetters = String.Join(',', obj.fields.Where(f => f.required).Select(f => $"{f.name} = default!"));

            return $@"
                partial void OnInitialized(FlatBufferDeserializationContext? context)
                {{
                    original = new {name}(new FlatBufferDeserializationContext(FlatBufferDeserializationOption.Default))
                    {{
                        {requiredFieldSetters}
                    }};
                    delta = new Mutable{name}Delta();
                    byteFields = new List<byte>();
                    {(fieldCount > 256 ? "shortFields = new List<ushort>();" : String.Empty)}
                    UpdateInternalReferenceState();
                }}
            ";
        }

        private static string GetDelta(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            int fieldCount = schema.GetFieldCountIncludingDeltaFields(obj);
            string deltaComparisons = String.Empty;
            int fieldIndex = 0;

            obj.ForEachFieldExceptUType(field =>
            {
                if (schema.TypeIsReferenceTypeArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        deltaComparisons += DeltaComparisonCodeWriter.GetReferenceTypeArrayComparison(schema, field, fieldIndex, arrayIndex);
                        fieldIndex += 2;
                    }
                }
                else if (schema.TypeIsValueStructArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        deltaComparisons += DeltaComparisonCodeWriter.GetValueStructArrayComparison(schema, field, fieldIndex, arrayIndex);
                        fieldIndex++;
                    }
                }
                else if (schema.TypeIsScalarArray(field.type) || schema.TypeIsEnumArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        deltaComparisons += DeltaComparisonCodeWriter.GetScalarArrayComparison(schema, field, fieldIndex, arrayIndex);
                        fieldIndex++;
                    }
                }
                else if (schema.TypeIsReferenceType(field.type) || field.type.base_type == BaseType.Vector)
                {
                    deltaComparisons += DeltaComparisonCodeWriter.GetReferenceTypeComparison(schema, field, fieldIndex);
                    fieldIndex += 2;
                }
                else if (schema.TypeIsValueStruct(field.type))
                {
                    deltaComparisons += DeltaComparisonCodeWriter.GetValueStructComparison(schema, field, fieldIndex);
                    fieldIndex++;
                }
                else if (schema.TypeIsScalar(field.type) || schema.TypeIsString(field.type) || schema.TypeIsEnum(field.type))
                {
                    deltaComparisons += DeltaComparisonCodeWriter.GetScalarComparison(schema, field, fieldIndex);
                    fieldIndex++;
                }
                else if (schema.TypeIsUnion(field.type))
                {
                    deltaComparisons += DeltaComparisonCodeWriter.GetUnionComparison(schema, field, fieldIndex);
                    fieldIndex += 2;
                }
            });

            return $@"
                public {name}Delta? GetDelta()
                {{
                    byteFields.Clear();
                    {(fieldCount > 256 ? "shortFields.Clear();" : String.Empty)}

                    {deltaComparisons}

                    delta.ByteFields = byteFields.Count > 0 ? byteFields : null;
                    {(fieldCount > 256 ? "delta.ShortFields = shortFields.Count > 0 ? shortFields : null;" : String.Empty)}

                    return byteFields.Count > 0{(fieldCount > 256 ? " || shortFields.Count > 0" : String.Empty)} ? delta : null;
                }}
            ";
        }

        private static string GetApplyDelta(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            List<string> cases = new List<string>();

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.deprecated)
                {
                    return;
                }

                if (schema.TypeIsReferenceTypeArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceTypeArray(schema, field, arrayIndex));
                        cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceTypeArrayDelta(schema, field, arrayIndex));
                    }
                }
                else if (schema.TypeIsValueStructArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        cases.Add(ApplyDeltaCaseCodeWriter.GetApplyValueStructArray(schema, field, arrayIndex));
                    }
                }
                else if (schema.TypeIsScalarArray(field.type) || schema.TypeIsEnumArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        cases.Add(ApplyDeltaCaseCodeWriter.GetApplyScalarArray(schema, field, arrayIndex));
                    }
                }
                else if (schema.TypeIsReferenceType(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceType(schema, field));
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceTypeDelta(schema, field));
                }
                else if (schema.TypeIsValueStruct(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyValueStruct(schema, field));
                }
                else if (schema.TypeIsScalar(field.type) || schema.TypeIsString(field.type) || schema.TypeIsEnum(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyScalar(schema, field));
                }
                else if (schema.TypeIsUnion(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyUnion(schema, field));
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyUnionDelta(schema, field));
                }
                else if (schema.TypeIsReferenceTypeList(field.type) || schema.TypeIsValueStructList(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceTypeList(schema, field));
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyReferenceTypeListDelta(schema, field));
                }
                else if (schema.TypeIsEnumList(field.type) || schema.TypeIsUnionList(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyEnumList(schema, field));
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyEnumListDelta(schema, field));
                }
                else if (schema.TypeIsScalarList(field.type) || schema.TypeIsStringList(field.type))
                {
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyScalarList(schema, field));
                    cases.Add(ApplyDeltaCaseCodeWriter.GetApplyScalarListDelta(schema, field));
                }
            });

            return $@"
                public void ApplyDelta({name}Delta? delta)
                {{
                    if (delta == null)
                    {{
                        return;
                    }}

                    {(cases.Count > 0 ? $@"
                    IReadOnlyList<byte>? byteFields = delta.ByteFields;

                    if (byteFields != null)
                    {{
                        int count = byteFields.Count;

                        for (int i = 0; i < count; i++)
                        {{
                            byte field = byteFields[i];
                            switch (field)
                            {{
                                {String.Concat(cases.GetRange(0, Math.Min(cases.Count, 256)))}
                            }}
                        }}
                    }}
                    " :
                    String.Empty)}

                    {(cases.Count > 256 ? $@"
                    IReadOnlyList<ushort>? shortFields = delta.ShortFields;

                    if (shortFields != null)
                    {{
                        int count = shortFields.Count;

                        for (int i = 0; i < count; i++)
                        {{
                            ushort field = shortFields[i];
                            switch (field)
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

        private static string GetUpdateInternalReferenceState(Schema schema, reflection.Object obj)
        {
            string assignments = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        assignments += $"original.__flatsharp__{field.name}_{arrayIndex} = __flatsharp__{field.name}_{arrayIndex};";
                    }
                }
                else
                {
                    assignments += $"original.{field.name} = {field.name};";
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
                if (schema.TypeIsReferenceTypeArray(field.type))
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        calls += $"__flatsharp__{field.name}_{arrayIndex}?.UpdateReferenceState();";
                    }
                }
                else if (schema.TypeHasDeltaType(field.type))
                {
                    calls += $"{field.name}{(schema.TypeIsUnion(field.type) && !field.optional ? String.Empty : "?")}.UpdateReferenceState();";
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

        private static string GetMutableDeltaClass(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string properties = String.Empty;

            obj.ForEachFieldExceptUType(field =>
            {
                if (field.type.base_type == BaseType.Array)
                {
                    for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        properties += $@"
                            public new {schema.GetCSharpType(field.type.ToElementAsBaseType(), schema.TypeIsValueStructArray(field.type))} {field.name}_{arrayIndex}
                            {{
                                get => base.{field.name}_{arrayIndex};
                                set => base.{field.name}_{arrayIndex} = value;
                            }}
                        ";
                    }

                    if (schema.TypeHasDeltaType(field.type.ToElementAsBaseType()))
                    {
                        for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                        {
                            properties += $@"
                                public new {schema.GetCSharpDeltaType(field.type.ToElementAsBaseType())} {field.name}_{arrayIndex}Delta
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
                    string csharpType = String.Empty;

                    if (field.type.base_type == BaseType.Vector)
                    {
                        csharpType = $"IReadOnlyList<{schema.GetCSharpType(field.type.ToElementAsBaseType()).TrimEnd('?')}>";
                    }
                    else
                    {
                        csharpType = schema.GetCSharpType(field.type, field.optional || schema.TypeIsReferenceType(field.type) || schema.TypeIsValueStruct(field.type) || schema.TypeIsUnion(field.type));
                    }

                    properties += $@"
                        public new {csharpType} {field.name}
                        {{
                            get => base.{field.name};
                            set => base.{field.name} = value;
                        }}
                    ";

                    if (schema.TypeHasDeltaType(field.type))
                    {
                        properties += $@"
                            public new {schema.GetCSharpDeltaType(field.type)} {field.name}Delta
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

                    {(schema.GetFieldCountIncludingDeltaFields(obj) > 256 ? $@"
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
    }
}