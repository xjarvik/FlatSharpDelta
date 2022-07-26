using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class PredefinedTypeFactory
    {
        public static Schema GetPredefinedTypesSchema(string _namespace, string declarationFile)
        {
            return new Schema
            {
                objects = new List<reflection.Object>(GetPrimitiveListDeltaTypes(_namespace, declarationFile, 0)),
                enums = new List<reflection.Enum> { GetListOperation(_namespace, declarationFile) },
            };
        }

        public static reflection.Enum GetListOperation(string _namespace, string declarationFile)
        {
            return new reflection.Enum
            {
                name = _namespace + ".ListOperation",
                values = new List<EnumVal>
                {
                    new EnumVal { name = "Insert",  value = 0 },
                    new EnumVal { name = "Modify",  value = 1 },
                    new EnumVal { name = "Move",    value = 2 },
                    new EnumVal { name = "Replace", value = 3 },
                    new EnumVal { name = "Remove",  value = 4 },
                    new EnumVal { name = "Clear",   value = 5 },
                },
                underlying_type = new reflection.Type { base_type = BaseType.UByte },
                declaration_file = declarationFile,
            };
        }

        public static reflection.Object[] GetPrimitiveListDeltaTypes
        (
            string _namespace,
            string declarationFile,
            int listOperationIndex
        ){
            return new reflection.Object[]
            {
                new reflection.Object
                {
                    name = _namespace + ".ByteListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Byte),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".UByteListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.UByte),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".BoolListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Bool),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".ShortListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Short),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".UShortListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.UShort),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".IntListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Int),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".UIntListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.UInt),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".FloatListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Float),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".LongListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Long),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".ULongListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.ULong),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".DoubleListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.Double),
                    declaration_file = declarationFile,
                },
                new reflection.Object
                {
                    name = _namespace + ".StringListDelta",
                    fields = GetPrimitiveListDeltaTypeFields(listOperationIndex, BaseType.String),
                    declaration_file = declarationFile,
                },
            };
        }

        private static List<Field> GetPrimitiveListDeltaTypeFields(int listOperationIndex, BaseType baseType)
        {
            return new List<Field>
            {
                new Field
                {
                    name = "Operation",
                    type = new reflection.Type { base_type = BaseType.UByte, index = listOperationIndex },
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
                    type = new reflection.Type { base_type = baseType },
                    id = 3,
                    offset = 10,
                    attributes = new List<KeyValue> { new KeyValue { key = "fs_setter", value = "Protected" } },
                },
            };
        }
    }
}