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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FlatSharpDelta.Tests
{
    public class GeneratedListType : GeneratedBaseType, IReadOnlyList<GeneratedType>, IList<GeneratedType>
    {
        public int Count { get => (int)GetProperty("Count"); }
        public bool IsReadOnly { get => (bool)GetProperty("IsReadOnly"); }
        GeneratedType IReadOnlyList<GeneratedType>.this[int index]
        {
            get => this[index];
        }

        public GeneratedType this[int index]
        {
            get => GetIndexerProperty<GeneratedType>(index);
            set => SetIndexerProperty(index, value);
        }

        public GeneratedListType() : base()
        {
        }

        public GeneratedListType(object obj) : base(obj)
        {
        }

        public GeneratedListType(Assembly assembly, string name, object copy = null) : base(assembly, name, copy)
        {
        }

        public GeneratedListType(Assembly assembly, string name, GeneratedBaseType copy) : base(assembly, name, copy.NativeObject)
        {
        }

        public static GeneratedListType ShallowCopy(Assembly assembly, string name, GeneratedListType list)
        {
            Type listType = list.NativeObject.GetType();

            object shallowCopy = listType.InvokeMember("ShallowCopy",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                null,
                new object[] { list.NativeObject }
            );

            return new GeneratedListType(shallowCopy);
        }

        public static GeneratedListType AsImmutable(Assembly assembly, string name, GeneratedListType list)
        {
            Type listType = list.NativeObject.GetType();

            object immutableList = listType.InvokeMember("AsImmutable",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                null,
                new object[] { list.NativeObject }
            );

            return new GeneratedListType(immutableList);
        }

        public void Add(GeneratedType item)
        {
            type.InvokeMember("Add",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public void Add(object item)
        {
            type.InvokeMember("Add",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void Clear()
        {
            type.InvokeMember("Clear",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );
        }

        public bool Contains(GeneratedType item)
        {
            return (bool)type.InvokeMember("Contains",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public bool Contains(object item)
        {
            return (bool)type.InvokeMember("Contains",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void CopyTo(GeneratedType[] array, int arrayIndex)
        {
            type.InvokeMember("CopyTo",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { array.Select(t => t.NativeObject).ToArray(), arrayIndex }
            );
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            type.InvokeMember("CopyTo",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { array, arrayIndex }
            );
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<GeneratedType> IEnumerable<GeneratedType>.GetEnumerator() => GetEnumerator();

        public IEnumerator<GeneratedType> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return GetIndexerProperty<GeneratedType>(i);
            }
        }

        public int IndexOf(GeneratedType item)
        {
            return (int)type.InvokeMember("IndexOf",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public int IndexOf(object item)
        {
            return (int)type.InvokeMember("IndexOf",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void Insert(int index, GeneratedType item)
        {
            type.InvokeMember("Insert",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { index, item.NativeObject }
            );
        }

        public void Insert(int index, object item)
        {
            type.InvokeMember("Insert",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { index, item }
            );
        }

        public void Move(int currentIndex, int newIndex)
        {
            type.InvokeMember("Move",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { currentIndex, newIndex }
            );
        }

        public bool Remove(GeneratedType item)
        {
            return (bool)type.InvokeMember("Remove",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public bool Remove(object item)
        {
            return (bool)type.InvokeMember("Remove",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void RemoveAt(int index)
        {
            type.InvokeMember("RemoveAt",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { index }
            );
        }

        public IReadOnlyList<GeneratedListDeltaType> GetDelta()
        {
            object delta = type.InvokeMember("GetDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                null
            );

            return ((IReadOnlyList<object>)delta).Select(d => new GeneratedListDeltaType(d)).ToList();
        }

        public void ApplyDelta(IReadOnlyList<GeneratedListDeltaType> delta)
        {
            type.InvokeMember("ApplyDelta",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { delta != null ? delta.Select(d => d.NativeObject).ToList() : null }
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
    }
}