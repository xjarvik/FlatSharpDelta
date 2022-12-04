using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class PredefinedTypeFactory
    {
        public static Schema GetPredefinedTypesSchema()
        {
            return new Schema
            {
                objects = GetBuiltInListDeltaTypes(0).Values.ToList(),
                enums = new List<reflection.Enum> { GetListOperation() },
            };
        }

        public static reflection.Enum GetListOperation()
        {
            return new reflection.Enum
            {
                name = "FlatSharpDelta.ListOperation",
                values = new List<EnumVal>
                {
                    new EnumVal { name = "Insert",  value = 0 },
                    new EnumVal { name = "Modify",  value = 1 },
                    new EnumVal { name = "Move",    value = 2 },
                    new EnumVal { name = "Replace", value = 3 },
                    new EnumVal { name = "Remove",  value = 4 },
                    new EnumVal { name = "Clear",   value = 5 }
                },
                underlying_type = new reflection.Type { base_type = BaseType.UByte },
                declaration_file = "//FlatSharpDelta.PredefinedTypes.bfbs"
            };
        }

        public static Dictionary<BaseType, reflection.Object> GetBuiltInListDeltaTypes(int listOperationIndex)
        {
            return new Dictionary<BaseType, reflection.Object>
            {
                {
                    BaseType.Bool,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.BoolListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Bool),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Byte,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.ByteListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Byte),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.UByte,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.UByteListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.UByte),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Short,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.ShortListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Short),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.UShort,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.UShortListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.UShort),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Int,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.IntListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Int),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.UInt,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.UIntListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.UInt),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Float,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.FloatListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Float),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Long,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.LongListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Long),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.ULong,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.ULongListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.ULong),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.Double,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.DoubleListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.Double),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                },
                {
                    BaseType.String,
                    new reflection.Object
                    {
                        name = "FlatSharpDelta.StringListDelta",
                        fields = GetBuiltInListDeltaTypeFields(listOperationIndex, BaseType.String),
                        declaration_file =  "//FlatSharpDelta.PredefinedTypes.bfbs"
                    }
                }
            };
        }

        private static List<Field> GetBuiltInListDeltaTypeFields(int listOperationIndex, BaseType baseType)
        {
            return new List<Field>
            {
                new Field
                {
                    name = "Operation",
                    type = new reflection.Type { base_type = BaseType.UByte, index = listOperationIndex },
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
                    type = new reflection.Type { base_type = baseType },
                    id = 3,
                    offset = 10,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } }
                }
            };
        }
    }
}