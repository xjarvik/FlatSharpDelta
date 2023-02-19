/*
 * Copyright 2023 William Söder
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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

                        if (field.HasAttribute("fs_sortedVector"))
                        {
                            validationErrors.Add($"Error on field {field.name} in {obj.name}: FlatSharpDelta does not support sorted vectors.");
                        }
                    }
                });
            }

            return validationErrors;
        }
    }
}