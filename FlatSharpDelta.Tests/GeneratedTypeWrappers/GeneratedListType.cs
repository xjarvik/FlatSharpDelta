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
        GeneratedType IReadOnlyList<GeneratedType>.this[int index] {
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

        public void CopyTo(GeneratedType[] array, int arrayIndex){
            type.InvokeMember("CopyTo",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { array.Select(t => t.NativeObject).ToArray(), arrayIndex }
            );
        }

        public void CopyTo(object[] array, int arrayIndex){
            type.InvokeMember("CopyTo",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { array, arrayIndex }
            );
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<GeneratedType> IEnumerable<GeneratedType>.GetEnumerator() => GetEnumerator();

        public IEnumerator<GeneratedType> GetEnumerator(){
            for(int i = 0; i < Count; i++){
                yield return GetIndexerProperty<GeneratedType>(i);
            }
        }

        public int IndexOf(GeneratedType item){
            return (int)type.InvokeMember("IndexOf",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public int IndexOf(object item){
            return (int)type.InvokeMember("IndexOf",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void Insert(int index, GeneratedType item){
            type.InvokeMember("Insert",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { index, item.NativeObject }
            );
        }

        public void Insert(int index, object item){
            type.InvokeMember("Insert",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { index, item }
            );
        }

        public void Move(int currentIndex, int newIndex){
            type.InvokeMember("Move",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { currentIndex, newIndex }
            );
        }

        public bool Remove(GeneratedType item){
            return (bool)type.InvokeMember("Remove",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item.NativeObject }
            );
        }

        public bool Remove(object item){
            return (bool)type.InvokeMember("Remove",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { item }
            );
        }

        public void RemoveAt(int index){
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
                new object[] { delta.Select(d => d.NativeObject).ToList() }
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