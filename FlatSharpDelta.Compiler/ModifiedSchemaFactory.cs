using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FlatSharp;
using reflection;

// poop code, please refactor
namespace FlatSharpDelta.Compiler
{
    static class ModifiedSchemaFactory
    {
        private class DeltaIndexes
        {
            public Dictionary<reflection.Object, (int, int)> DeltaTableIndexes { get; set; }
            public Dictionary<reflection.Enum, (int, int)> DeltaUnionIndexes { get; set; }
            public Dictionary<string, int[]> PrimitiveListDeltaIndexes { get; set; }
            public Dictionary<string, int> ListOperationIndexes { get; set; }
            private int CurrentDeltaTableIndex;
            private int CurrentDeltaUnionIndex;

            public DeltaIndexes(Schema baseSchema)
            {
                DeltaTableIndexes = new Dictionary<reflection.Object, (int, int)>();
                DeltaUnionIndexes = new Dictionary<reflection.Enum, (int, int)>();
                PrimitiveListDeltaIndexes = new Dictionary<string, int[]>();
                ListOperationIndexes = new Dictionary<string, int>();
                CurrentDeltaTableIndex = baseSchema.objects.Count - 1;
                CurrentDeltaUnionIndex = baseSchema.enums.Count - 1;
            }

            public void AddDeltaTableIndex(reflection.Object baseObj)
            {
                int deltaIndex = -1;

                if(!(baseObj.is_struct && baseObj.HasAttribute("fs_valueStruct")))
                {
                    deltaIndex = ++CurrentDeltaTableIndex;
                }

                int listDeltaIndex = ++CurrentDeltaTableIndex;

                DeltaTableIndexes[baseObj] = (deltaIndex, listDeltaIndex);
            }

            public void AddDeltaUnionIndex(reflection.Enum baseUnion)
            {
                int deltaIndex = -1;

                if(baseUnion.is_union)
                {
                    deltaIndex = ++CurrentDeltaUnionIndex;
                }

                int listDeltaIndex = ++CurrentDeltaTableIndex;

                DeltaUnionIndexes[baseUnion] = (deltaIndex, listDeltaIndex);
            }

            public void AddPrimitiveDeltaListIndexIfNotExistsInNamespace(string _namespace)
            {
                if(!PrimitiveListDeltaIndexes.ContainsKey(_namespace))
                {
                    ListOperationIndexes[_namespace] = ++CurrentDeltaUnionIndex;

                    int[] indexes = new int[12];

                    for(int i = 0; i < indexes.Length; i++)
                    {
                        indexes[i] = ++CurrentDeltaTableIndex;
                    }

                    PrimitiveListDeltaIndexes[_namespace] = indexes;
                }
            }

            public reflection.Type GetArrayTypeForType(Schema baseSchema, reflection.Type type)
            {
                reflection.Type arrayType = new reflection.Type();

                arrayType.base_type = type.element;
                arrayType.index = type.index;

                return arrayType;
            }

            public reflection.Type GetArrayDeltaTypeForType(Schema baseSchema, reflection.Type type)
            {
                reflection.Type arrayDeltaType = new reflection.Type();

                arrayDeltaType.base_type = BaseType.Obj;
                arrayDeltaType.index = DeltaTableIndexes[baseSchema.objects[type.index]].Item1;

                return arrayDeltaType;
            }

            public reflection.Type GetDeltaTypeForType(Schema baseSchema, string _namespace, reflection.Type type)
            {
                reflection.Type deltaType = new reflection.Type();

                if(type.base_type != BaseType.Vector)
                {
                    if(type.base_type != BaseType.Union)
                    {
                        deltaType.base_type = BaseType.Obj;
                        deltaType.index = DeltaTableIndexes[baseSchema.objects[type.index]].Item1;
                    }
                    else
                    {
                        deltaType.base_type = BaseType.Union;
                        deltaType.index = DeltaUnionIndexes[baseSchema.enums[type.index]].Item1;
                    }
                }
                else
                {
                    switch(type.element)
                    {
                        case BaseType.Byte:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][0];
                            break;
                        
                        case BaseType.UByte:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][1];
                            break;

                        case BaseType.Bool:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][2];
                            break;

                        case BaseType.Short:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][3];
                            break;

                        case BaseType.UShort:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][4];
                            break;

                        case BaseType.Int:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][5];
                            break;

                        case BaseType.UInt:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][6];
                            break;
                        
                        case BaseType.Float:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][7];
                            break;

                        case BaseType.Long:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][8];
                            break;

                        case BaseType.ULong:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][9];
                            break;

                        case BaseType.Double:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][10];
                            break;

                        case BaseType.String:
                            deltaType.index = PrimitiveListDeltaIndexes[_namespace][11];
                            break;

                        case BaseType.Obj:
                            deltaType.index = DeltaTableIndexes[baseSchema.objects[type.index]].Item2;
                            break;

                        case BaseType.Union:
                            deltaType.index = DeltaUnionIndexes[baseSchema.enums[type.index]].Item2;
                            break;
                    }

                    if(CodeWriterUtils.PropertyListTypeIsBuiltInScalar(type) && type.index != -1)
                    {
                        deltaType.index = DeltaUnionIndexes[baseSchema.enums[type.index]].Item2;
                    }

                    deltaType.base_type = BaseType.Vector;
                    deltaType.element = BaseType.Obj;
                }

                return deltaType;
            }
        }

        // Method that returns a schema that contains both the "base" objects (see BaseSchemaFactory) and their corresponding delta objects.
        public static Schema GetModifiedSchema(Schema originalSchema)
        {
            Schema baseSchema = BaseSchemaFactory.GetBaseSchema(originalSchema);
            DeltaIndexes deltaIndexes = new DeltaIndexes(baseSchema);

            foreach(reflection.Object baseObj in baseSchema.objects)
            {
                deltaIndexes.AddDeltaTableIndex(baseObj);
                deltaIndexes.AddPrimitiveDeltaListIndexIfNotExistsInNamespace(GetOriginalNamespace(baseObj));
            }

            foreach(reflection.Enum baseEnum in baseSchema.enums)
            {
                deltaIndexes.AddDeltaUnionIndex(baseEnum);
                deltaIndexes.AddPrimitiveDeltaListIndexIfNotExistsInNamespace(GetOriginalNamespace(baseEnum));
            }

            foreach(KeyValuePair<reflection.Object, (int, int)> kvp in deltaIndexes.DeltaTableIndexes)
            {
                (int deltaIndex, int listDeltaIndex) = kvp.Value;

                if(deltaIndex != -1)
                {
                    baseSchema.objects.Add(null);
                }

                baseSchema.objects.Add(null);
            }

            foreach(KeyValuePair<reflection.Enum, (int, int)> kvp in deltaIndexes.DeltaUnionIndexes)
            {
                (int deltaIndex, int listDeltaIndex) = kvp.Value;

                if(deltaIndex != -1)
                {
                    baseSchema.enums.Add(null);
                }

                baseSchema.objects.Add(null);
            }

            foreach(KeyValuePair<string, int[]> kvp in deltaIndexes.PrimitiveListDeltaIndexes)
            {
                kvp.Value.ToList().ForEach(_ => baseSchema.objects.Add(null));
                baseSchema.enums.Add(null);
            }

            foreach(reflection.Object key in new List<reflection.Object>(deltaIndexes.DeltaTableIndexes.Keys))
            {
                (int deltaIndex, int listDeltaIndex) = deltaIndexes.DeltaTableIndexes[key];

                if(deltaIndex != -1)
                {
                    baseSchema.objects[deltaIndex] = GetDeltaTable(baseSchema, key, deltaIndexes);
                }
                
                baseSchema.objects[listDeltaIndex] = GetListDeltaTable(baseSchema, key, deltaIndexes);
            }

            foreach(reflection.Enum key in new List<reflection.Enum>(deltaIndexes.DeltaUnionIndexes.Keys))
            {
                (int deltaIndex, int listDeltaIndex) = deltaIndexes.DeltaUnionIndexes[key];

                if(deltaIndex != -1)
                {
                    baseSchema.enums[deltaIndex] = GetDeltaUnion(baseSchema, key, deltaIndexes);
                }

                baseSchema.objects[listDeltaIndex] = GetListDeltaUnion(baseSchema, key, deltaIndexes);
            }

            foreach(string _namespace in deltaIndexes.PrimitiveListDeltaIndexes.Keys)
            {
                int[] indexes = deltaIndexes.PrimitiveListDeltaIndexes[_namespace];
                int listOperationIndex = deltaIndexes.ListOperationIndexes[_namespace];
                baseSchema.enums[listOperationIndex] = PredefinedTypeFactory.GetListOperation(_namespace + ".SupportingTypes", "//_.fbs");
                reflection.Object[] primitiveListDeltaTypes = PredefinedTypeFactory.GetPrimitiveListDeltaTypes
                (
                    _namespace,
                    "//_.fbs",
                    listOperationIndex
                );

                for(int i = 0; i < indexes.Length; i++)
                {
                    baseSchema.objects[indexes[i]] = primitiveListDeltaTypes[i];
                }
            }

            return GetSortedSchema(baseSchema);
        }

        private static Schema GetSortedSchema(Schema baseSchema)
        {
            byte[] bytes = new byte[Schema.Serializer.GetMaxSize(baseSchema)];
            Schema.Serializer.Write(bytes, baseSchema);
            Schema sortedSchema = Schema.Serializer.Parse(bytes);

            foreach(reflection.Object obj in sortedSchema.objects)
            {
                foreach(Field field in obj.fields)
                {
                    if(field.type.index == -1)
                    {
                        continue;
                    }

                    BaseType typeToCheck = field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array ? field.type.element : field.type.base_type;

                    if(typeToCheck == BaseType.Obj)
                    {
                        field.type.index = sortedSchema.objects.IndexOf(sortedSchema.objects.First(o => o.name == baseSchema.objects[field.type.index].name));
                    }
                    else if(typeToCheck == BaseType.Union || typeToCheck == BaseType.UType || field.type.index != -1)
                    {
                        field.type.index = sortedSchema.enums.IndexOf(sortedSchema.enums.First(e => e.name == baseSchema.enums[field.type.index].name));
                    }
                }
            }

            foreach(reflection.Enum _enum in sortedSchema.enums)
            {
                if(!_enum.is_union)
                {
                    continue;
                }

                foreach(EnumVal enumVal in _enum.values)
                {
                    if(enumVal.union_type.index == -1)
                    {
                        continue;
                    }

                    BaseType typeToCheck = enumVal.union_type.base_type == BaseType.Vector || enumVal.union_type.base_type == BaseType.Array ? enumVal.union_type.element : enumVal.union_type.base_type;

                    if(typeToCheck == BaseType.Obj)
                    {
                        enumVal.union_type.index = sortedSchema.objects.IndexOf(sortedSchema.objects.First(o => o.name == baseSchema.objects[enumVal.union_type.index].name));
                    }
                    else if(typeToCheck == BaseType.Union || typeToCheck == BaseType.UType || enumVal.union_type.index != -1)
                    {
                        enumVal.union_type.index = sortedSchema.enums.IndexOf(sortedSchema.enums.First(e => e.name == baseSchema.enums[enumVal.union_type.index].name));
                    }
                }
            }

            return sortedSchema;
        }

        private static reflection.Object GetDeltaTable(Schema baseSchema, reflection.Object baseObj, DeltaIndexes deltaIndexes)
        {
            reflection.Object deltaObj = new reflection.Object(baseObj);

            deltaObj.name = GetOriginalName(baseObj) + "Delta";
            deltaObj.is_struct = false;

            IncreaseIdAndOffset(deltaObj, 2, 4, 0);
            deltaObj.fields.Insert(0, GetByteFields());
            deltaObj.fields.Insert(1, GetShortFields(baseSchema, baseObj));

            List<Field> deltaObjFields = deltaObj.fields.OrderBy(f => f.id).ToList();
            for(int i = deltaObjFields.Count - 1; i >= 2; i--)
            {
                Field field = deltaObjFields[i];

                if(field.type.base_type == BaseType.UType || field.type.element == BaseType.UType)
                {
                    continue;
                }

                field.required = false;
                field.RemoveAttribute("required");

                if(field.type.base_type == BaseType.Array)
                {
                    int removalIndex = deltaObj.fields.IndexOf(field);
                    DecreaseIdAndOffset(deltaObj, 1, 2, field.id + 1);
                    deltaObj.fields.RemoveAt(removalIndex);

                    bool isBuiltInScalar = CodeWriterUtils.PropertyListTypeIsBuiltInScalar(field.type);
                    bool isValueStruct = CodeWriterUtils.PropertyListTypeIsValueStruct(baseSchema, field.type);
                    ushort id = field.id;
                    ushort offset = field.offset;
                    int index = i;

                    for(int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                    {
                        Field arrayIndexField = new Field
                        {
                            name = $"{field.name}_{arrayIndex}",
                            type = deltaIndexes.GetArrayTypeForType(baseSchema, field.type),
                            id = id,
                            offset = offset,
                            optional = !isBuiltInScalar,
                        };

                        arrayIndexField.SetAttribute("fs_setter", "Protected");
                        IncreaseIdAndOffset(deltaObj, 1, 2, id);
                        deltaObj.fields.Insert(index, arrayIndexField);

                        id++;
                        offset += 2;
                        index++;

                        if(!isBuiltInScalar && !isValueStruct)
                        {
                            Field arrayIndexDeltaField = new Field
                            {
                                name = $"{field.name}_{arrayIndex}Delta",
                                type = deltaIndexes.GetArrayDeltaTypeForType(baseSchema, field.type),
                                id = id,
                                offset = offset,
                                optional = true,
                            };

                            arrayIndexDeltaField.SetAttribute("fs_setter", "Protected");
                            IncreaseIdAndOffset(deltaObj, 1, 2, id);
                            deltaObj.fields.Insert(index, arrayIndexDeltaField);

                            id++;
                            offset += 2;
                            index++;
                        }
                    }
                }
                else if(CodeWriterUtils.PropertyTypeIsDerived(baseSchema, field.type))
                {
                    field.optional = true;

                    Field deltaField = new Field
                    {
                        name = field.name + "Delta",
                        type = deltaIndexes.GetDeltaTypeForType(baseSchema, deltaObj.GetNamespace(), field.type),
                        id = (ushort)(field.id + 1),
                        offset = (ushort)(field.offset + 2),
                        optional = true,
                    };

                    if(field.deprecated)
                    {
                        deltaField.deprecated = true;
                        deltaField.SetAttribute("deprecated");
                    }

                    deltaField.SetAttribute("fs_setter", "Protected");

                    if(deltaField.type.base_type == BaseType.Vector || deltaField.type.base_type == BaseType.Array)
                    {
                        deltaField.SetAttribute("fs_vector", "IReadOnlyList");
                    }

                    IncreaseIdAndOffset(deltaObj, 1, 2, i + 1);
                    deltaObj.fields.Insert(i + 1, deltaField);

                    if(field.type.base_type == BaseType.Union)
                    {
                        Field utypeField = new Field(deltaField);
                        utypeField.name += "_type";
                        utypeField.type.base_type = BaseType.UType;
                        utypeField.optional = false;

                        IncreaseIdAndOffset(deltaObj, 1, 2, i + 1);
                        deltaObj.fields.Insert(i + 1, utypeField);
                    }
                }
            }

            if(baseObj.HasAttribute("fs_serializer"))
            {
                deltaObj.SetAttribute("fs_serializer", "Lazy");
            }

            return deltaObj;
        }

        private static reflection.Object GetListDeltaTable(Schema baseSchema, reflection.Object baseObj, DeltaIndexes deltaIndexes)
        {
            reflection.Object listDeltaObj = new reflection.Object();

            listDeltaObj.name = GetOriginalName(baseObj) + "ListDelta";
            listDeltaObj.fields = new List<Field>
            {
                new Field
                {
                    name = "Operation",
                    type = new reflection.Type { base_type = BaseType.UByte, index = deltaIndexes.ListOperationIndexes[GetOriginalNamespace(baseObj)] },
                    id = 0,
                    offset = 4,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "CurrentIndex",
                    type = new reflection.Type { base_type = BaseType.Int },
                    id = 1,
                    offset = 6,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "NewIndex",
                    type = new reflection.Type { base_type = BaseType.Int },
                    id = 2,
                    offset = 8,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "BaseValue",
                    type = new reflection.Type { base_type = BaseType.Obj, index = baseSchema.objects.IndexOf(baseObj) },
                    id = 3,
                    offset = 10,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                    optional = true,
                },
            };

            if(!(baseObj.is_struct && baseObj.HasAttribute("fs_valueStruct")))
            {
                listDeltaObj.fields.Add(new Field
                {
                    name = "DeltaValue",
                    type = new reflection.Type { base_type = BaseType.Obj, index = deltaIndexes.DeltaTableIndexes[baseObj].Item1 },
                    id = 4,
                    offset = 12,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                    optional = true,
                });
            }

            listDeltaObj.declaration_file = baseObj.declaration_file;

            return listDeltaObj;
        }

        private static reflection.Enum GetDeltaUnion(Schema baseSchema, reflection.Enum baseUnion, DeltaIndexes deltaIndexes)
        {
            reflection.Enum deltaUnion = new reflection.Enum(baseUnion);
            
            deltaUnion.name = GetOriginalName(baseUnion) + "Delta";
            deltaUnion.is_union = true;

            for(int i = 1; i < deltaUnion.values.Count; i++)
            {
                EnumVal enumVal = deltaUnion.values[i];

                if(CodeWriterUtils.PropertyTypeIsValueStruct(baseSchema, enumVal.union_type))
                {
                    deltaUnion.values.RemoveAt(i);
                    i--;
                    continue;
                }

                enumVal.name += "Delta";
                enumVal.value = i;
                enumVal.union_type = deltaIndexes.GetDeltaTypeForType(baseSchema, deltaUnion.GetNamespace(), enumVal.union_type);
            }

            return deltaUnion;
        }

        private static reflection.Object GetListDeltaUnion(Schema baseSchema, reflection.Enum baseUnion, DeltaIndexes deltaIndexes)
        {
            reflection.Object listDeltaObj = new reflection.Object();

            listDeltaObj.name = GetOriginalName(baseUnion) + "ListDelta";
            listDeltaObj.fields = new List<Field>
            {
                new Field
                {
                    name = "Operation",
                    type = new reflection.Type { base_type = BaseType.UByte, index = deltaIndexes.ListOperationIndexes[GetOriginalNamespace(baseUnion)] },
                    id = 0,
                    offset = 4,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "CurrentIndex",
                    type = new reflection.Type { base_type = BaseType.Int },
                    id = 1,
                    offset = 6,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "NewIndex",
                    type = new reflection.Type { base_type = BaseType.Int },
                    id = 2,
                    offset = 8,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
                new Field
                {
                    name = "BaseValue",
                    type = new reflection.Type { base_type = baseUnion.is_union ? BaseType.Union : baseUnion.underlying_type.base_type, index = baseSchema.enums.IndexOf(baseUnion) },
                    id = 3,
                    offset = 10,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                    optional = baseUnion.is_union,
                    default_integer = baseUnion.is_union ? 0 : baseUnion.values[0].value
                },
            };

            if(baseUnion.is_union)
            {
                Field baseValueUType = new Field(listDeltaObj.fields[3]);
                baseValueUType.name += "_type";
                baseValueUType.type.base_type = BaseType.UType;
                baseValueUType.optional = false;

                IncreaseIdAndOffset(listDeltaObj, 1, 2, 3);
                listDeltaObj.fields.Insert(3, baseValueUType);

                listDeltaObj.fields.Add(new Field
                {
                    name = "DeltaValue",
                    type = new reflection.Type { base_type = BaseType.Union, index = deltaIndexes.DeltaUnionIndexes[baseUnion].Item1 },
                    id = 5,
                    offset = 14,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                    optional = true,
                });

                Field deltaValueUType = new Field(listDeltaObj.fields[5]);
                deltaValueUType.name += "_type";
                deltaValueUType.type.base_type = BaseType.UType;
                deltaValueUType.optional = false;

                IncreaseIdAndOffset(listDeltaObj, 1, 2, 5);
                listDeltaObj.fields.Insert(5, deltaValueUType);
            }

            listDeltaObj.declaration_file = baseUnion.declaration_file;

            return listDeltaObj;
        }

        private static Field GetByteFields()
        {
            Field byteFields = new Field();

            byteFields.name = "ByteFields";
            byteFields.id = 0;
            byteFields.type = new reflection.Type
            {
                base_type = BaseType.Vector,
                element = BaseType.UByte
            };
            byteFields.offset = 4;
            byteFields.SetAttribute("fs_setter", "Protected");
            byteFields.SetAttribute("fs_vector", "IReadOnlyList");

            return byteFields;
        }

        private static Field GetShortFields(Schema baseSchema, reflection.Object baseObj)
        {
            Field shortFields = new Field();

            shortFields.name = "ShortFields";
            shortFields.id = 1;
            shortFields.type = new reflection.Type
            {
                base_type = BaseType.Vector,
                element = BaseType.UShort
            };
            shortFields.offset = 6;

            if(GetIndexCount(baseSchema, baseObj) <= 256)
            {
                shortFields.deprecated = true;
                shortFields.SetAttribute("deprecated");
            }

            shortFields.SetAttribute("fs_setter", "Protected");
            shortFields.SetAttribute("fs_vector", "IReadOnlyList");

            return shortFields;
        }

        private static int GetIndexCount(Schema baseSchema, reflection.Object baseObj)
        {
            int count = 0;

            foreach(Field field in baseObj.fields)
            {
                count++;

                if(CodeWriterUtils.PropertyTypeIsDerived(baseSchema, field.type))
                {
                    count++;
                }
            }

            return count;
        }

        private static void IncreaseIdAndOffset(reflection.Object obj, ushort idBy, ushort offsetBy, int startingId)
        {
            obj.fields.Where(f => f.id >= startingId).OrderBy(f => f.id).ToList().ForEach(field =>
            {
                field.id += idBy;
                field.offset += offsetBy;
            });
        }

        private static void DecreaseIdAndOffset(reflection.Object obj, ushort idBy, ushort offsetBy, int startingId)
        {
            obj.fields.Where(f => f.id >= startingId).OrderBy(f => f.id).ToList().ForEach(field =>
            {
                field.id -= idBy;
                field.offset -= offsetBy;
            });
        }

        private static string GetOriginalName(reflection.Object baseObj)
        {
            string _namespace = baseObj.GetNamespace();
            string name = baseObj.GetNameWithoutNamespace();

            string originalNamespace;
            string originalName;

            if(!(baseObj.is_struct && baseObj.HasAttribute("fs_valueStruct")))
            {
                originalNamespace = _namespace.Substring(0, _namespace.Length - ".SupportingTypes".Length);
                originalName = name.Substring("Base".Length);
            }
            else
            {
                originalNamespace = _namespace;
                originalName = name;
            }

            return originalNamespace + "." + originalName;
        }

        private static string GetOriginalName(reflection.Enum baseUnion)
        {
            string _namespace = baseUnion.GetNamespace();
            string name = baseUnion.GetNameWithoutNamespace();

            string originalNamespace;
            string originalName;

            if(baseUnion.is_union)
            {
                originalNamespace = _namespace.Substring(0, _namespace.Length - ".SupportingTypes".Length);
                originalName = name.Substring("Base".Length);
            }
            else
            {
                originalNamespace = _namespace;
                originalName = name;
            }

            return originalNamespace + "." + originalName;
        }

        private static string GetOriginalNamespace(reflection.Object baseObj)
        {
            string _namespace = baseObj.GetNamespace();
            string originalNamespace;

            if(!(baseObj.is_struct && baseObj.HasAttribute("fs_valueStruct")))
            {
                originalNamespace = _namespace.Substring(0, _namespace.Length - ".SupportingTypes".Length);
            }
            else
            {
                originalNamespace = _namespace;
            }

            return originalNamespace;
        }

        private static string GetOriginalNamespace(reflection.Enum baseUnion)
        {
            string _namespace = baseUnion.GetNamespace();
            string originalNamespace;

            if(baseUnion.is_union)
            {
                originalNamespace = _namespace.Substring(0, _namespace.Length - ".SupportingTypes".Length);
            }
            else
            {
                originalNamespace = _namespace;
            }

            return originalNamespace;
        }
    }
}