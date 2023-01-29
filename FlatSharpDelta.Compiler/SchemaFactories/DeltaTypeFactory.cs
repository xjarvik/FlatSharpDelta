using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FlatSharp;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class DeltaTypeFactory
    {
        public static reflection.Object GetObjectDeltaType(DeltaSchemaFactory.IContext context, reflection.Object baseObj)
        {
            Schema schema = context.Schema;

            reflection.Object deltaObj = new reflection.Object(baseObj);

            RemoveUnwantedAttributes(deltaObj);

            deltaObj.name = baseObj.name + "Delta";
            deltaObj.is_struct = false;

            foreach (Field field in deltaObj.fields.OrderBy(field => field.id).ToList())
            {
                if (field.type.base_type == BaseType.UType || field.type.element == BaseType.UType)
                {
                    continue;
                }

                RemoveUnwantedAttributes(field);

                if (field.type.base_type == BaseType.Array)
                {
                    DecreaseFieldIdsAndOffsets(deltaObj.fields, field.id + 1);
                    deltaObj.fields.Remove(field);
                }
                else if (schema.TypeIsReferenceType(field.type) || schema.TypeIsValueStruct(field.type) || schema.TypeIsUnion(field.type) || field.type.base_type == BaseType.Vector)
                {
                    field.optional = true;
                }

                if (field.type.base_type == BaseType.Vector)
                {
                    field.SetAttribute("fs_vector", "IReadOnlyList");
                }

                field.SetAttribute("fs_setter", "Protected");

                AddDeltaFieldsForField(context, deltaObj, field);
            }

            IncreaseFieldIdsAndOffsets(deltaObj.fields, 0);
            Field byteFieldsField = GetByteFieldsField();
            deltaObj.fields.Add(byteFieldsField);

            IncreaseFieldIdsAndOffsets(deltaObj.fields, 1);
            Field shortFieldsField = GetShortFieldsField();
            deltaObj.fields.Add(shortFieldsField);

            if (schema.GetFieldCountIncludingDeltaFields(baseObj) <= 256)
            {
                shortFieldsField.deprecated = true;
                shortFieldsField.SetAttribute("deprecated");
            }

            if (baseObj.HasAttribute("fs_serializer"))
            {
                deltaObj.SetAttribute("fs_serializer", "Lazy");
            }

            return deltaObj;
        }

        private static void AddDeltaFieldsForField(DeltaSchemaFactory.IContext context, reflection.Object deltaObj, Field field)
        {
            Schema schema = context.Schema;

            if (field.type.base_type == BaseType.Array)
            {
                ushort id = field.id;
                ushort offset = field.offset;

                for (int arrayIndex = 0; arrayIndex < field.type.fixed_length; arrayIndex++)
                {
                    reflection.Type arrayType = new reflection.Type
                    {
                        base_type = field.type.element,
                        index = field.type.index
                    };

                    Field arrayIndexField = new Field
                    {
                        name = $"{field.name}_{arrayIndex}",
                        type = arrayType,
                        id = id,
                        offset = offset,
                        optional = schema.TypeIsReferenceTypeArray(field.type) || schema.TypeIsValueStructArray(field.type)
                    };

                    arrayIndexField.SetAttribute("fs_setter", "Protected");
                    IncreaseFieldIdsAndOffsets(deltaObj.fields, id);
                    deltaObj.fields.Add(arrayIndexField);

                    id++;
                    offset += 2;

                    if (schema.TypeIsReferenceTypeArray(field.type))
                    {
                        reflection.Type arrayDeltaType = new reflection.Type
                        {
                            base_type = BaseType.Obj,
                            index = context.GetDeltaIndexOfType(field.type)
                        };

                        Field arrayIndexDeltaField = new Field
                        {
                            name = $"{field.name}_{arrayIndex}Delta",
                            type = arrayDeltaType,
                            id = id,
                            offset = offset,
                            optional = true
                        };

                        arrayIndexDeltaField.SetAttribute("fs_setter", "Protected");
                        IncreaseFieldIdsAndOffsets(deltaObj.fields, id);
                        deltaObj.fields.Add(arrayIndexDeltaField);

                        id++;
                        offset += 2;
                    }
                }
            }
            else if (schema.TypeIsReferenceType(field.type) || schema.TypeIsUnion(field.type) || field.type.base_type == BaseType.Vector)
            {
                reflection.Type deltaType = new reflection.Type
                {
                    base_type = field.type.base_type,
                    element = field.type.base_type == BaseType.Vector ? BaseType.Obj : BaseType.None,
                    index = context.GetDeltaIndexOfType(field.type)
                };

                Field deltaField = new Field
                {
                    name = $"{field.name}Delta",
                    type = deltaType,
                    id = (ushort)(field.id + 1),
                    offset = (ushort)(field.offset + 2),
                    optional = true
                };

                if (field.deprecated)
                {
                    deltaField.deprecated = true;
                    deltaField.SetAttribute("deprecated");
                }

                if (deltaField.type.base_type == BaseType.Vector)
                {
                    deltaField.SetAttribute("fs_vector", "IReadOnlyList");
                }

                deltaField.SetAttribute("fs_setter", "Protected");
                IncreaseFieldIdsAndOffsets(deltaObj.fields, field.id + 1);
                deltaObj.fields.Add(deltaField);

                if (schema.TypeIsUnion(field.type))
                {
                    Field utypeField = new Field(deltaField);
                    utypeField.name += "_type";
                    utypeField.type.base_type = BaseType.UType;
                    utypeField.optional = false;

                    IncreaseFieldIdsAndOffsets(deltaObj.fields, field.id + 1);
                    deltaObj.fields.Add(utypeField);
                }
            }
        }

        private static Field GetByteFieldsField()
        {
            Field byteFields = new Field
            {
                name = "ByteFields",
                id = 0,
                offset = 4,
                type = new reflection.Type
                {
                    base_type = BaseType.Vector,
                    element = BaseType.UByte
                }
            };

            byteFields.SetAttribute("fs_setter", "Protected");
            byteFields.SetAttribute("fs_vector", "IReadOnlyList");

            return byteFields;
        }

        private static Field GetShortFieldsField()
        {
            Field shortFields = new Field
            {
                name = "ShortFields",
                id = 1,
                offset = 6,
                type = new reflection.Type
                {
                    base_type = BaseType.Vector,
                    element = BaseType.UShort
                }
            };

            shortFields.SetAttribute("fs_setter", "Protected");
            shortFields.SetAttribute("fs_vector", "IReadOnlyList");

            return shortFields;
        }

        private static void IncreaseFieldIdsAndOffsets(IList<Field> fields, int startingId)
        {
            foreach (Field field in fields.Where(field => field.id >= startingId))
            {
                field.id++;
                field.offset += 2;
            }
        }

        private static void DecreaseFieldIdsAndOffsets(IList<Field> fields, int startingId)
        {
            foreach (Field field in fields.Where(field => field.id >= startingId))
            {
                field.id--;
                field.offset -= 2;
            }
        }

        private static void RemoveUnwantedAttributes(reflection.Object obj)
        {
            obj.RemoveAttribute("fs_defaultCtor");
        }

        private static void RemoveUnwantedAttributes(Field field)
        {
            field.required = false;
            field.RemoveAttribute("required");
            field.RemoveAttribute("fs_forceWrite");
            field.RemoveAttribute("fs_writeThrough");
        }

        public static reflection.Enum GetEnumDeltaType(DeltaSchemaFactory.IContext context, reflection.Enum baseEnum)
        {
            if (!baseEnum.IsUnion())
            {
                return null;
            }

            Schema schema = context.Schema;

            reflection.Enum deltaEnum = new reflection.Enum(baseEnum);

            deltaEnum.name = baseEnum.name + "Delta";
            deltaEnum.is_union = true;

            for (int i = 1; i < deltaEnum.values.Count; i++)
            {
                EnumVal enumVal = deltaEnum.values[i];

                if (schema.TypeIsValueStruct(enumVal.union_type) || schema.TypeIsString(enumVal.union_type))
                {
                    deltaEnum.values.RemoveAt(i);
                    i--;
                    continue;
                }

                reflection.Type deltaType = new reflection.Type
                {
                    base_type = BaseType.Obj,
                    index = context.GetDeltaIndexOfType(enumVal.union_type)
                };

                enumVal.name += "Delta";
                enumVal.value = i;
                enumVal.union_type = deltaType;
            }

            return deltaEnum;
        }

        public static reflection.Object GetObjectListDeltaType(DeltaSchemaFactory.IContext context, reflection.Object baseObj)
        {
            Schema schema = context.Schema;

            reflection.Object listDeltaObj = new reflection.Object
            {
                name = baseObj.name + "ListDelta",
                fields = new List<Field>
                {
                    new Field
                    {
                        name = "Operation",
                        type = new reflection.Type { base_type = BaseType.UByte, index = context.ListOperationIndex },
                        id = 0,
                        offset = 4,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "CurrentIndex",
                        type = new reflection.Type { base_type = BaseType.Int },
                        id = 1,
                        offset = 6,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "NewIndex",
                        type = new reflection.Type { base_type = BaseType.Int },
                        id = 2,
                        offset = 8,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "BaseValue",
                        type = new reflection.Type { base_type = BaseType.Obj, index = schema.objects.IndexOf(baseObj) },
                        id = 3,
                        offset = 10,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                        optional = true
                    }
                }
            };

            if (!baseObj.IsValueStruct())
            {
                listDeltaObj.fields.Add(
                    new Field
                    {
                        name = "DeltaValue",
                        type = new reflection.Type
                        {
                            base_type = BaseType.Obj,
                            index = context.GetDeltaIndexOfType(schema.GetTypeFromObject(baseObj))
                        },
                        id = 4,
                        offset = 12,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                        optional = true
                    }
                );
            }

            listDeltaObj.declaration_file = baseObj.declaration_file;

            return listDeltaObj;
        }

        public static reflection.Object GetEnumListDeltaType(DeltaSchemaFactory.IContext context, reflection.Enum baseEnum)
        {
            Schema schema = context.Schema;

            reflection.Object listDeltaObj = new reflection.Object
            {
                name = baseEnum.name + "ListDelta",
                fields = new List<Field>
                {
                    new Field
                    {
                        name = "Operation",
                        type = new reflection.Type { base_type = BaseType.UByte, index = context.ListOperationIndex },
                        id = 0,
                        offset = 4,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "CurrentIndex",
                        type = new reflection.Type { base_type = BaseType.Int },
                        id = 1,
                        offset = 6,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "NewIndex",
                        type = new reflection.Type { base_type = BaseType.Int },
                        id = 2,
                        offset = 8,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                    },
                    new Field
                    {
                        name = "BaseValue",
                        type = new reflection.Type
                        {
                            base_type = baseEnum.IsUnion() ? BaseType.Union : baseEnum.underlying_type.base_type,
                            index = schema.enums.IndexOf(baseEnum)
                        },
                        id = 3,
                        offset = 10,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                        optional = baseEnum.IsUnion(),
                        default_integer = baseEnum.IsUnion() ? 0 : baseEnum.values[0].value
                    }
                }
            };

            if (baseEnum.IsUnion())
            {
                Field baseValueUType = new Field(listDeltaObj.fields[3]);
                baseValueUType.name += "_type";
                baseValueUType.type.base_type = BaseType.UType;
                baseValueUType.optional = false;

                IncreaseFieldIdsAndOffsets(listDeltaObj.fields, 3);
                listDeltaObj.fields.Insert(3, baseValueUType);

                listDeltaObj.fields.Add(
                    new Field
                    {
                        name = "DeltaValue",
                        type = new reflection.Type
                        {
                            base_type = BaseType.Union,
                            index = context.GetDeltaIndexOfType(schema.GetTypeFromEnum(baseEnum))
                        },
                        id = 5,
                        offset = 14,
                        attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                        optional = true
                    }
                );

                Field deltaValueUType = new Field(listDeltaObj.fields[5]);
                deltaValueUType.name += "_type";
                deltaValueUType.type.base_type = BaseType.UType;
                deltaValueUType.optional = false;

                IncreaseFieldIdsAndOffsets(listDeltaObj.fields, 5);
                listDeltaObj.fields.Insert(5, deltaValueUType);
            }

            listDeltaObj.declaration_file = baseEnum.declaration_file;

            return listDeltaObj;
        }
    }
}