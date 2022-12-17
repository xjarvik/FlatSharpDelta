using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FlatSharp;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class DeltaSchemaFactory
    {
        public static Schema GetSchemaWithDeltaTypes(Schema originalSchema)
        {
            Context context = new Context();
            Schema schema = new Schema(originalSchema);
            context.Schema = schema;

            int currentObjectsIndex = schema.objects.Count - 1;
            int currentEnumsIndex = schema.enums.Count - 1;

            context.ObjectDeltaTypeIndexes = GetObjectDeltaTypeIndexes(schema, ref currentObjectsIndex);
            context.EnumDeltaTypeIndexes = GetEnumDeltaTypeIndexes(schema, ref currentEnumsIndex);
            context.ListOperationIndex = ++currentEnumsIndex;

            context.ObjectListDeltaTypeIndexes = GetObjectListDeltaTypeIndexes(schema, ref currentObjectsIndex);
            context.EnumListDeltaTypeIndexes = GetEnumListDeltaTypeIndexes(schema, ref currentObjectsIndex);
            context.BuiltInListDeltaTypeIndexes = GetBuiltInListDeltaTypeIndexes(ref currentObjectsIndex);

            IncreaseListCountTo(schema.objects, currentObjectsIndex + 1);
            IncreaseListCountTo(schema.enums, currentEnumsIndex + 1);

            AddObjectDeltaTypes(context);
            AddEnumDeltaTypes(context);
            AddListOperation(context);

            AddObjectListDeltaTypes(context);
            AddEnumListDeltaTypes(context);
            AddBuiltInListDeltaTypes(context);

            Schema sortedSchema = GetSortedSchema(schema);
            FixIndexesInSortedSchema(sortedSchema, schema);

            return sortedSchema;
        }

        private static Dictionary<int, int> GetObjectDeltaTypeIndexes(Schema schema, ref int currentObjectsIndex)
        {
            Dictionary<int, int> objectDeltaTypeIndexes = new Dictionary<int, int>();

            for (int i = 0; i < schema.objects.Count; i++)
            {
                if (!schema.objects[i].IsValueStruct())
                {
                    objectDeltaTypeIndexes.Add(i, ++currentObjectsIndex);
                }
            }

            return objectDeltaTypeIndexes;
        }

        private static Dictionary<int, int> GetEnumDeltaTypeIndexes(Schema schema, ref int currentEnumsIndex)
        {
            Dictionary<int, int> enumDeltaTypeIndexes = new Dictionary<int, int>();

            for (int i = 0; i < schema.enums.Count; i++)
            {
                if (schema.enums[i].IsUnion())
                {
                    enumDeltaTypeIndexes.Add(i, ++currentEnumsIndex);
                }
            }

            return enumDeltaTypeIndexes;
        }

        private static Dictionary<int, int> GetObjectListDeltaTypeIndexes(Schema schema, ref int currentObjectsIndex)
        {
            Dictionary<int, int> objectListDeltaTypeIndexes = new Dictionary<int, int>();

            for (int i = 0; i < schema.objects.Count; i++)
            {
                objectListDeltaTypeIndexes.Add(i, ++currentObjectsIndex);
            }

            return objectListDeltaTypeIndexes;
        }

        private static Dictionary<int, int> GetEnumListDeltaTypeIndexes(Schema schema, ref int currentObjectsIndex)
        {
            Dictionary<int, int> enumListDeltaTypeIndexes = new Dictionary<int, int>();

            for (int i = 0; i < schema.enums.Count; i++)
            {
                enumListDeltaTypeIndexes.Add(i, ++currentObjectsIndex);
            }

            return enumListDeltaTypeIndexes;
        }

        private static Dictionary<BaseType, int> GetBuiltInListDeltaTypeIndexes(ref int currentObjectDeltaTypeIndex)
        {
            return new Dictionary<BaseType, int>
            {
                { BaseType.Bool, ++currentObjectDeltaTypeIndex },
                { BaseType.Byte, ++currentObjectDeltaTypeIndex },
                { BaseType.UByte, ++currentObjectDeltaTypeIndex },
                { BaseType.Short, ++currentObjectDeltaTypeIndex },
                { BaseType.UShort, ++currentObjectDeltaTypeIndex },
                { BaseType.Int, ++currentObjectDeltaTypeIndex },
                { BaseType.UInt, ++currentObjectDeltaTypeIndex },
                { BaseType.Float, ++currentObjectDeltaTypeIndex },
                { BaseType.Long, ++currentObjectDeltaTypeIndex },
                { BaseType.ULong, ++currentObjectDeltaTypeIndex },
                { BaseType.Double, ++currentObjectDeltaTypeIndex },
                { BaseType.String, ++currentObjectDeltaTypeIndex }
            };
        }

        private static void AddObjectDeltaTypes(Context context)
        {
            foreach (KeyValuePair<int, int> kvp in context.ObjectDeltaTypeIndexes)
            {
                reflection.Object baseObj = context.Schema.objects[kvp.Key];
                reflection.Object deltaObj = DeltaTypeFactory.GetObjectDeltaType(context, baseObj);
                context.Schema.objects[kvp.Value] = deltaObj;
            }
        }

        private static void AddEnumDeltaTypes(Context context)
        {
            foreach (KeyValuePair<int, int> kvp in context.EnumDeltaTypeIndexes)
            {
                reflection.Enum baseEnum = context.Schema.enums[kvp.Key];
                reflection.Enum deltaEnum = DeltaTypeFactory.GetEnumDeltaType(context, baseEnum);
                context.Schema.enums[kvp.Value] = deltaEnum;
            }
        }

        private static void AddListOperation(Context context)
        {
            context.Schema.enums[context.ListOperationIndex] = BuiltInTypeFactory.GetListOperation();
        }

        private static void AddObjectListDeltaTypes(Context context)
        {
            foreach (KeyValuePair<int, int> kvp in context.ObjectListDeltaTypeIndexes)
            {
                reflection.Object baseObj = context.Schema.objects[kvp.Key];
                reflection.Object listDeltaObj = DeltaTypeFactory.GetObjectListDeltaType(context, baseObj);
                context.Schema.objects[kvp.Value] = listDeltaObj;
            }
        }

        private static void AddEnumListDeltaTypes(Context context)
        {
            foreach (KeyValuePair<int, int> kvp in context.EnumListDeltaTypeIndexes)
            {
                reflection.Enum baseEnum = context.Schema.enums[kvp.Key];
                reflection.Object listDeltaObj = DeltaTypeFactory.GetEnumListDeltaType(context, baseEnum);
                context.Schema.objects[kvp.Value] = listDeltaObj;
            }
        }

        private static void AddBuiltInListDeltaTypes(Context context)
        {
            Dictionary<BaseType, reflection.Object> builtInListDeltaTypes = BuiltInTypeFactory.GetBuiltInListDeltaTypes(context.ListOperationIndex);
            foreach (KeyValuePair<BaseType, int> kvp in context.BuiltInListDeltaTypeIndexes)
            {
                context.Schema.objects[kvp.Value] = builtInListDeltaTypes[kvp.Key];
            }
        }

        private static void IncreaseListCountTo<T>(IList<T> list, int count) where T : class
        {
            while (list.Count < count)
            {
                list.Add(null);
            }
        }

        private static Schema GetSortedSchema(Schema schema)
        {
            byte[] bytes = new byte[Schema.Serializer.GetMaxSize(schema)];
            Schema.Serializer.Write(bytes, schema);
            Schema sortedSchema = Schema.Serializer.Parse(bytes);

            return sortedSchema;
        }

        private static void FixIndexesInSortedSchema(Schema sortedSchema, Schema unsortedSchema)
        {
            foreach (reflection.Object obj in sortedSchema.objects)
            {
                foreach (Field field in obj.fields)
                {
                    if (field.type.index == -1)
                    {
                        continue;
                    }

                    BaseType baseType = field.type.GetBaseTypeOrElement();

                    if (baseType == BaseType.Obj)
                    {
                        field.type.index = sortedSchema.GetIndexOfObjectWithName(unsortedSchema.GetNameOfObjectWithIndex(field.type.index));
                    }
                    else if (baseType == BaseType.Union || baseType == BaseType.UType || field.type.index != -1)
                    {
                        field.type.index = sortedSchema.GetIndexOfEnumWithName(unsortedSchema.GetNameOfEnumWithIndex(field.type.index));
                    }
                }
            }

            foreach (reflection.Enum _enum in sortedSchema.enums)
            {
                if (!_enum.is_union)
                {
                    continue;
                }

                foreach (EnumVal enumVal in _enum.values)
                {
                    if (enumVal.union_type.index == -1)
                    {
                        continue;
                    }

                    BaseType baseType = enumVal.union_type.GetBaseTypeOrElement();

                    if (baseType == BaseType.Obj)
                    {
                        enumVal.union_type.index = sortedSchema.GetIndexOfObjectWithName(unsortedSchema.GetNameOfObjectWithIndex(enumVal.union_type.index));
                    }
                    else if (baseType == BaseType.Union || baseType == BaseType.UType || enumVal.union_type.index != -1)
                    {
                        enumVal.union_type.index = sortedSchema.GetIndexOfEnumWithName(unsortedSchema.GetNameOfEnumWithIndex(enumVal.union_type.index));
                    }
                }
            }
        }

        public interface IContext
        {
            Schema Schema { get; }
            int ListOperationIndex { get; }

            int GetDeltaIndexOfType(reflection.Type type);
        }

        private class Context : IContext
        {
            public Schema Schema { get; set; }

            public Dictionary<int, int> ObjectDeltaTypeIndexes { get; set; }
            public Dictionary<int, int> EnumDeltaTypeIndexes { get; set; }
            public int ListOperationIndex { get; set; }

            public Dictionary<int, int> ObjectListDeltaTypeIndexes { get; set; }
            public Dictionary<int, int> EnumListDeltaTypeIndexes { get; set; }
            public Dictionary<BaseType, int> BuiltInListDeltaTypeIndexes { get; set; }

            public int GetDeltaIndexOfType(reflection.Type type)
            {
                int deltaIndex = -1;

                if (Schema.TypeIsReferenceType(type) || Schema.TypeIsReferenceTypeArray(type))
                {
                    ObjectDeltaTypeIndexes.TryGetValue(type.index, out deltaIndex);
                }
                else if (Schema.TypeIsUnion(type))
                {
                    EnumDeltaTypeIndexes.TryGetValue(type.index, out deltaIndex);
                }
                else if (Schema.TypeIsReferenceTypeList(type) || Schema.TypeIsValueStructList(type))
                {
                    ObjectListDeltaTypeIndexes.TryGetValue(type.index, out deltaIndex);
                }
                else if (Schema.TypeIsEnumList(type) || Schema.TypeIsUnionList(type))
                {
                    EnumListDeltaTypeIndexes.TryGetValue(type.index, out deltaIndex);
                }
                else if (Schema.TypeIsScalarList(type) || Schema.TypeIsStringList(type))
                {
                    BuiltInListDeltaTypeIndexes.TryGetValue(type.element, out deltaIndex);
                }

                return deltaIndex;
            }
        }
    }
}