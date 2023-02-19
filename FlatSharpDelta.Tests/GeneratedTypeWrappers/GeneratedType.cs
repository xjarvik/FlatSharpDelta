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
using System.Reflection;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedType : GeneratedBaseType
    {
        public GeneratedType() : base()
        {
        }

        public GeneratedType(object obj) : base(obj)
        {
        }

        public GeneratedType(Assembly assembly, string name, object copy = null) : base(assembly, name, copy)
        {
        }

        public GeneratedType(Assembly assembly, string name, GeneratedBaseType copy) : base(assembly, name, copy.NativeObject)
        {
        }

        public GeneratedDeltaType GetDelta()
        {
            object delta = type.InvokeMember("GetDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );

            return delta != null ? new GeneratedDeltaType(delta) : null;
        }

        public void ApplyDelta(GeneratedDeltaType delta)
        {
            type.InvokeMember("ApplyDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { delta != null ? delta.NativeObject : null }
            );
        }

        public void UpdateReferenceState()
        {
            type.InvokeMember("UpdateReferenceState",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );
        }

        public static object Enum(Assembly assembly, string name, string value)
        {
            return System.Enum.Parse(assembly.GetType(name), value);
        }

        public static object Enum(Assembly assembly, string name, object value)
        {
            return System.Enum.ToObject(assembly.GetType(name), value);
        }
    }
}