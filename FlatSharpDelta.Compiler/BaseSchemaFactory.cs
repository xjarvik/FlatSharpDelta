using System;
using System.Linq;
using reflection;

namespace FlatSharpDelta.Compiler
{
    class BaseSchemaFactory
    {
        public static Schema GetBaseSchema(Schema originalSchema)
        {
            Schema baseSchema = new Schema(originalSchema);

            foreach(reflection.Object obj in baseSchema.objects)
            {
                if(!(obj.is_struct && obj.HasAttribute("fs_valueStruct")))
                {
                    obj.name = obj.GetNamespace() + ".SupportingTypes.Base" + obj.GetNameWithoutNamespace();

                    if(obj.HasAttribute("fs_serializer"))
                    {
                        obj.SetAttribute("fs_serializer", "Lazy");
                    }

                    foreach(Field field in obj.fields)
                    {
                        field.SetAttribute("fs_setter", "Protected");

                        if(field.type.base_type == BaseType.Vector || field.type.base_type == BaseType.Array)
                        {
                            field.SetAttribute("fs_vector", "IReadOnlyList");
                        }
                    }
                }
            }

            foreach(reflection.Enum _enum in baseSchema.enums)
            {
                if(_enum.is_union)
                {
                    _enum.name = _enum.GetNamespace() + ".SupportingTypes.Base" + _enum.GetNameWithoutNamespace();
                }
            }

            return baseSchema;
        }
    }
}