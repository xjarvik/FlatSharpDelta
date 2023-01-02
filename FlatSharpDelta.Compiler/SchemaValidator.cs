using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class SchemaValidator
    {
        public static IList<string> ValidateSchema(Schema schema)
        {
            IList<string> validationErrors = new List<string>();

            foreach (reflection.Object obj in schema.objects)
            {
                obj.ForEachFieldExceptUType(field =>
                {
                    if (field.type.base_type == BaseType.Vector)
                    {
                        if (field.HasAttribute("fs_vector"))
                        {
                            string listType = field.GetAttribute("fs_vector").value;

                            if (listType != "IList")
                            {
                                validationErrors.Add($"Error on field {field.name} in {obj.name}: FlatSharpDelta only supports vectors of the IList type.");
                            }
                        }
                        else if (field.type.element == BaseType.UByte && field.type.index == -1)
                        {
                            validationErrors.Add($"Error on field {field.name} in {obj.name}: FlatSharpDelta does not support vectors of the Memory type, which is the default for ubyte vectors. Set the fs_vector attribute to IList instead.");
                        }
                    }
                });
            }

            return validationErrors;
        }
    }
}