using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class UnionCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string _namespace = union.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    {GetUsages(union)}

                    public partial struct {name} : IFlatBufferUnion<{GetIFlatBufferUnionTypes(schema, union)}>
                    {{
                        {GetProperties(schema, union)}

                        {GetItemKind(union)}

                        {GetConstructors(schema, union)}

                        {GetCopyConstructor(schema, union)}

                        {GetDelta(schema, union)}

                        {GetApplyDelta(schema, union)}

                        {GetUpdateReferenceState(schema, union)}

                        {GetTryGets(schema, union)}

                        {GetSwitches(schema, union)}
                    }}
                }}
            ";
        }

        private static string GetUsages(reflection.Enum union)
        {
            string _namespace = union.GetNamespace();

            return $@"
                using {_namespace}.SupportingTypes;
            ";
        }

        private static string GetIFlatBufferUnionTypes(Schema schema, reflection.Enum union)
        {
            List<string> types = new List<string>();

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                types.Add(CodeWriterUtils.GetPropertyType(schema, enumVal.union_type));
            }

            return String.Join(", ", types);
        }

        private static string GetProperties(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string properties = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string type = CodeWriterUtils.GetPropertyType(schema, enumVal.union_type, false);
                bool cast = !CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);

                properties += $@"
                    public {type} {enumVal.name} => {(cast ? $"({type})" : String.Empty)}Base.Item{i};
                    public {type} Item{i} => {(cast ? $"({type})" : String.Empty)}Base.Item{i};
                ";
            }

            return $@"
                public Base{name} Base {{ get; }}

                public ItemKind Kind => (ItemKind)Base.Discriminator;
                public byte Discriminator => Base.Discriminator;

                {properties}
            ";
        }

        private static string GetItemKind(reflection.Enum union)
        {
            string kinds = String.Empty;

            for (int i = 0; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];

                kinds += $@"
                    {enumVal.name} = {i},
                ";
            }

            return $@"
                public enum ItemKind : byte
                {{
                    {kinds}
                }}
            ";
        }

        private static string GetConstructors(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string constructors = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string type = CodeWriterUtils.GetPropertyType(schema, enumVal.union_type, false);

                constructors += $@"
                    public {name}({type} value)
                    {{
                        Base = new Base{name}(value);
                    }}
                ";
            }

            return constructors;
        }

        private static string GetCopyConstructor(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string type = CodeWriterUtils.GetPropertyType(schema, enumVal.union_type, false);
                bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);

                discriminators += $@"
                    case {i}:
                        Base = {(isValueStruct ? "b" : $"new Base{name}(new {type}(b.{enumVal.name}))")};
                        return;
                ";
            }

            return $@"
                public {name}(Base{name} b)
                {{
                    switch(b.Discriminator)
                    {{
                        {discriminators}
                    }}

                    Base = new Base{name}();
                }}
            ";
        }

        private static string GetDelta(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string deltaType = CodeWriterUtils.GetPropertyDeltaType(schema, enumVal.union_type);
                bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);

                if (isValueStruct)
                {
                    continue;
                }

                discriminators += $@"
                    case {i}:
                    {{
                        {deltaType} nestedDelta = {enumVal.name}.GetDelta();
                        return nestedDelta != null ? new {name}Delta(nestedDelta) : null;
                    }}
                ";
            }

            return $@"
                public {name}Delta? GetDelta()
                {{
                    switch(Discriminator)
                    {{
                        {discriminators}
                    }}

                    return null;
                }}
            ";
        }

        private static string GetApplyDelta(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;
            int offset = 0;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);
                int index = i + offset;

                if (isValueStruct)
                {
                    offset--;
                    continue;
                }

                discriminators += $@"
                    case {index}:
                        if(Discriminator == {index})
                        {{
                            {enumVal.name}.ApplyDelta(delta.Value.{enumVal.name}Delta);
                        }}
                        break;
                ";
            }

            return $@"
                public void ApplyDelta({name}Delta? delta)
                {{
                    {(!String.IsNullOrEmpty(discriminators) ?
                    $@"
                    if(delta == null)
                    {{
                        return;
                    }}

                    switch(delta.Value.Discriminator)
                    {{
                        {discriminators}
                    }}
                    " :
                    String.Empty)}
                }}
            ";
        }

        private static string GetUpdateReferenceState(Schema schema, reflection.Enum union)
        {
            string name = union.GetNameWithoutNamespace();
            string discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);

                if (isValueStruct)
                {
                    continue;
                }

                discriminators += $@"
                    case {i}:
                        {enumVal.name}.UpdateReferenceState();
                        break;
                ";
            }

            return $@"
                public void UpdateReferenceState()
                {{
                    {(!String.IsNullOrEmpty(discriminators) ?
                    $@"
                    switch(Discriminator)
                    {{
                        {discriminators}
                    }}
                    " :
                    String.Empty)}
                }}
            ";
        }

        private static string GetTryGets(Schema schema, reflection.Enum union)
        {
            string tryGets = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                bool isValueStruct = CodeWriterUtils.PropertyTypeIsValueStruct(schema, enumVal.union_type);
                string type = CodeWriterUtils.GetPropertyType(schema, enumVal.union_type, !isValueStruct);

                if (isValueStruct)
                {
                    tryGets += $@"
                        public bool TryGet(out {type} value) => Base.TryGet(out value);
                    ";
                }
                else
                {
                    string baseType = CodeWriterUtils.GetPropertyBaseType(schema, enumVal.union_type, true);

                    tryGets += $@"
                        public bool TryGet(out {type} value)
                        {{
                            bool result = Base.TryGet(out {baseType} baseValue);
                            value = ({type})baseValue;
                            return result;
                        }}
                    ";
                }
            }

            return tryGets;
        }

        private static string GetSwitches(Schema schema, reflection.Enum union)
        {
            string switch1Arguments = String.Empty;
            string switch2Arguments = String.Empty;
            string switch3Arguments = String.Empty;
            string switch4Arguments = String.Empty;

            string switch1Discriminators = String.Empty;
            string switch2Discriminators = String.Empty;
            string switch3Discriminators = String.Empty;
            string switch4Discriminators = String.Empty;

            for (int i = 1; i < union.values.Count; i++)
            {
                EnumVal enumVal = union.values[i];
                string type = CodeWriterUtils.GetPropertyType(schema, enumVal.union_type, false);

                switch1Arguments += $", Func<TState, {type}, TReturn> case{enumVal.name}";
                switch2Arguments += $", Func<{type}, TReturn> case{enumVal.name}";
                switch3Arguments += $", Action<TState, {type}> case{enumVal.name}";
                switch4Arguments += $", Action<{type}> case{enumVal.name}";

                switch1Discriminators += $"case {i}: return case{enumVal.name}(state, {enumVal.name});";
                switch2Discriminators += $"case {i}: return case{enumVal.name}({enumVal.name});";
                switch3Discriminators += $"case {i}: case{enumVal.name}(state, {enumVal.name}); break;";
                switch4Discriminators += $"case {i}: case{enumVal.name}({enumVal.name}); break;";
            }

            return $@"
                public TReturn Switch<TState, TReturn>
                (
                    TState state,
                    Func<TState, TReturn> caseDefault
                    {switch1Arguments}
                ){{
                    switch(Discriminator)
                    {{
                        {switch1Discriminators}
                        default: return caseDefault(state);
                    }}
                }}

                public TReturn Switch<TReturn>
                (
                    Func<TReturn> caseDefault
                    {switch2Arguments}
                ){{
                    switch(Discriminator)
                    {{
                        {switch2Discriminators}
                        default: return caseDefault();
                    }}
                }}

                public void Switch<TState>
                (
                    TState state,
                    Action<TState> caseDefault
                    {switch3Arguments}
                ){{
                    switch(Discriminator)
                    {{
                        {switch3Discriminators}
                        default: caseDefault(state); break;
                    }}
                }}

                public void Switch
                (
                    Action caseDefault
                    {switch4Arguments}
                ){{
                    switch(Discriminator)
                    {{
                        {switch4Discriminators}
                        default: caseDefault(); break;
                    }}
                }}
            ";
        }
    }
}