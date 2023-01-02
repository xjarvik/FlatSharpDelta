using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ValueStructListCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    using T = {name};
                    using TList = {name}List;
                    using TListDelta = {name}ListDelta;

                    public class {name}List : IReadOnlyList<T>, IList<T>
                    {{
                        private List<ListItem> listItems;
                        private List<MutableTListDelta> deltasToReturn;
                        private ReadOnlyCollection<MutableTListDelta> deltasToReturnReadOnly;
                        private LinkedList<MutableTListDelta> deltaNodes;
                        private Stack<MutableTListDelta> deltaPool;
                        private Stack<LinkedListNode<MutableTListDelta>> deltaNodePool;
                        private Stack<object> identifierPool;
                        public virtual int Count {{ get => listItems.Count; }}
                        public virtual bool IsReadOnly {{ get => false; }}

                        public virtual int Capacity
                        {{
                            get => listItems.Capacity;
                            set => listItems.Capacity = value;
                        }}

                        T IReadOnlyList<T>.this[int index]
                        {{
                            get => this[index];
                        }}

                        public virtual T this[int index]
                        {{
                            get => listItems[index].Value;
                            set
                            {{
                                ListItem listItem = listItems[index];
                                if (!value.IsEqualTo(listItem.Value))
                                {{
                                    MutableTListDelta delta = GetValueFromDeltaPool(listItem.Identifier);
                                    delta.Operation = ListOperation.Replace;
                                    delta.CurrentIndex = index;
                                    delta.BaseValue = value;
                                    LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                                    listItems[index] = new ListItem(listItem.Identifier, value, deltaNode);
                                    MutableTListDelta? lastDelta = listItem.LastDeltaNode?.Value;
                                    if (lastDelta != null && lastDelta.Identifier == listItem.Identifier)
                                    {{
                                        lastDelta.NextDeltaNode = deltaNode;
                                    }}
                                    deltaNodes.AddLast(deltaNode);
                                }}
                            }}
                        }}

                        private void Initialize()
                        {{
                            deltasToReturn = new List<MutableTListDelta>();
                            deltasToReturnReadOnly = new ReadOnlyCollection<MutableTListDelta>(deltasToReturn);
                            deltaNodes = new LinkedList<MutableTListDelta>();
                            deltaPool = new Stack<MutableTListDelta>();
                            deltaNodePool = new Stack<LinkedListNode<MutableTListDelta>>();
                            identifierPool = new Stack<object>();
                        }}
                #pragma warning disable CS8618
                        public {name}List()
                        {{
                            Initialize();
                            listItems = new List<ListItem>();
                        }}

                        protected {name}List(byte b)
                        {{
                        }}

                        public {name}List(int capacity)
                        {{
                            Initialize();
                            listItems = new List<ListItem>(capacity);
                        }}

                        public {name}List(IReadOnlyList<T> list)
                        {{
                            Initialize();
                            int count = list.Count;
                            listItems = new List<ListItem>(count);
                            for (int i = 0; i < count; i++)
                            {{
                                listItems.Add(new ListItem(new object(), list[i], null));
                            }}
                        }}

                        public {name}List(IList<T> list)
                        {{
                            Initialize();
                            int count = list.Count;
                            listItems = new List<ListItem>(count);
                            for (int i = 0; i < count; i++)
                            {{
                                listItems.Add(new ListItem(new object(), list[i], null));
                            }}
                        }}

                        public {name}List(TList list)
                        {{
                            Initialize();
                            int count = list.Count;
                            listItems = new List<ListItem>(count);
                            for (int i = 0; i < count; i++)
                            {{
                                listItems.Add(new ListItem(new object(), list[i], null));
                            }}
                        }}
                #pragma warning restore CS8618
                        public static TList AsImmutable(IReadOnlyList<T> list) => new ImmutableTList(list);

                        public static TList AsImmutable(IList<T> list) => new ImmutableTList(list);

                        public static TList AsImmutable(TList list) => new ImmutableTList(list);

                        public virtual void Add(T item)
                        {{
                            object identifier = GetValueFromIdentifierPool();
                            MutableTListDelta delta = GetValueFromDeltaPool(identifier);
                            delta.Operation = ListOperation.Insert;
                            delta.NewIndex = listItems.Count;
                            delta.BaseValue = item;
                            LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                            listItems.Add(new ListItem(identifier, item, deltaNode));
                            deltaNodes.AddLast(deltaNode);
                        }}

                        public virtual void Clear()
                        {{
                            listItems.Clear();
                            ClearDeltaNodes();
                            MutableTListDelta delta = GetValueFromDeltaPool(null!);
                            delta.Operation = ListOperation.Clear;
                            LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                            deltaNodes.AddLast(deltaNode);
                        }}

                        public bool Contains(T item) => IndexOf(item) >= 0;

                        public virtual void CopyTo(T[] array, int arrayIndex)
                        {{
                            if (array == null)
                            {{
                                throw new ArgumentNullException(""The array is null"");
                            }}
                            if (arrayIndex < 0 || arrayIndex >= array.Length)
                            {{
                                throw new ArgumentOutOfRangeException(""The arrayIndex is outside the array"");
                            }}
                            if (listItems.Count > array.Length - arrayIndex)
                            {{
                                throw new ArgumentException(""The array does not have enough space"");
                            }}
                            for (int i = 0; i < listItems.Count; i++)
                            {{
                                array[arrayIndex + i] = listItems[i].Value;
                            }}
                        }}

                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

                        public virtual IEnumerator<T> GetEnumerator()
                        {{
                            for (int i = 0; i < listItems.Count; i++)
                            {{
                                yield return listItems[i].Value;
                            }}
                        }}

                        public virtual int IndexOf(T item)
                        {{
                            for (int i = 0; i < listItems.Count; i++)
                            {{
                                if (listItems[i].Value.IsEqualTo(item))
                                {{
                                    return i;
                                }}
                            }}
                            return -1;
                        }}

                        public virtual void Insert(int index, T item)
                        {{
                            object identifier = GetValueFromIdentifierPool();
                            MutableTListDelta delta = GetValueFromDeltaPool(identifier);
                            delta.Operation = ListOperation.Insert;
                            delta.NewIndex = index;
                            delta.BaseValue = item;
                            LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                            listItems.Insert(index, new ListItem(identifier, item, deltaNode));
                            deltaNodes.AddLast(deltaNode);
                        }}

                        public virtual void Move(int currentIndex, int newIndex)
                        {{
                            ListItem listItem = listItems[currentIndex];
                            MutableTListDelta delta = GetValueFromDeltaPool(listItem.Identifier);
                            delta.Operation = ListOperation.Move;
                            delta.CurrentIndex = currentIndex;
                            delta.NewIndex = newIndex;
                            LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                            MutableTListDelta? lastDelta = listItem.LastDeltaNode?.Value;
                            listItem.LastDeltaNode = deltaNode;
                            listItems.RemoveAt(currentIndex);
                            listItems.Insert(newIndex > currentIndex ? newIndex - 1 : newIndex, listItem);
                            if (lastDelta != null && lastDelta.Identifier == listItem.Identifier)
                            {{
                                lastDelta.NextDeltaNode = deltaNode;
                            }}
                            deltaNodes.AddLast(deltaNode);
                        }}

                        public virtual bool Remove(T item)
                        {{
                            int index = IndexOf(item);
                            if (index >= 0)
                            {{
                                RemoveAt(index);
                                return true;
                            }}
                            return false;
                        }}

                        public virtual void RemoveAt(int index)
                        {{
                            ListItem listItem = listItems[index];
                            listItems.RemoveAt(index);
                            MutableTListDelta delta = GetValueFromDeltaPool(listItem.Identifier);
                            delta.Operation = ListOperation.Remove;
                            delta.CurrentIndex = index;
                            LinkedListNode<MutableTListDelta> deltaNode = GetValueFromDeltaNodePool(delta);
                            MutableTListDelta? lastDelta = listItem.LastDeltaNode?.Value;
                            if (lastDelta != null && lastDelta.Identifier == listItem.Identifier)
                            {{
                                lastDelta.NextDeltaNode = deltaNode;
                            }}
                            deltaNodes.AddLast(deltaNode);
                            identifierPool.Push(listItem.Identifier);
                        }}

                        private enum OptimizationResult
                        {{
                            RemovedNone,
                            RemovedCurrent,
                            RemovedNext,
                            RemovedBoth
                        }}

                        public virtual IReadOnlyList<TListDelta>? GetDelta()
                        {{
                            LinkedListNode<MutableTListDelta>? currentDeltaNode = deltaNodes.First;
                            LinkedListNode<MutableTListDelta>? nextDeltaNode;
                            LinkedListNode<MutableTListDelta>? lastFinalizedDeltaNode = null;
                            while (currentDeltaNode != deltaNodes.Last)
                            {{
                                nextDeltaNode = currentDeltaNode?.Value.NextDeltaNode;
                                if (nextDeltaNode == null)
                                {{
                                    lastFinalizedDeltaNode = currentDeltaNode;
                                    currentDeltaNode = currentDeltaNode?.Next;
                                    continue;
                                }}
                                switch (OptimizeDelta(currentDeltaNode!, nextDeltaNode))
                                {{
                                    case OptimizationResult.RemovedNone:
                                    case OptimizationResult.RemovedNext:
                                        lastFinalizedDeltaNode = currentDeltaNode;
                                        currentDeltaNode = currentDeltaNode?.Next;
                                        break;
                                    case OptimizationResult.RemovedCurrent:
                                    case OptimizationResult.RemovedBoth:
                                        if (lastFinalizedDeltaNode != null)
                                        {{
                                            currentDeltaNode = lastFinalizedDeltaNode?.Next;
                                        }}
                                        else
                                        {{
                                            currentDeltaNode = deltaNodes.First;
                                        }}
                                        break;
                                }}
                            }}
                            deltasToReturn.Clear();
                            if (deltasToReturn.Capacity < deltaNodes.Count)
                            {{
                                deltasToReturn.Capacity = deltaNodes.Count;
                            }}
                            deltasToReturn.AddRange(deltaNodes);
                            return deltasToReturnReadOnly.Count > 0 ? deltasToReturnReadOnly : null;
                        }}

                        private OptimizationResult OptimizeDelta(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            switch (current.Value.Operation)
                            {{
                                case ListOperation.Insert: return OptimizeInsert(current, next);
                                case ListOperation.Move: return OptimizeMove(current, next);
                                case ListOperation.Replace: return OptimizeReplace(current, next);
                                default: return OptimizationResult.RemovedNone;
                            }}
                        }}

                        private OptimizationResult OptimizeInsert(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            int originalIndex = current.Value.NewIndex;
                            int index = originalIndex;
                            if (next.Value.Operation == ListOperation.Move)
                            {{
                                int myIndex = originalIndex;
                                LinkedListNode<MutableTListDelta> after = current.Next!;
                                while (true)
                                {{
                                    int updatedIndex = GetUpdatedIndex(after!.Value, myIndex);
                                    if (after.Value.CurrentIndex > myIndex)
                                    {{
                                        after.Value.CurrentIndex--;
                                    }}
                                    if (after.Value.NewIndex > myIndex)
                                    {{
                                        after.Value.NewIndex--;
                                    }}
                                    myIndex = updatedIndex;
                                    if (after == next)
                                    {{
                                        break;
                                    }}
                                    after = after.Next!;
                                }}
                                current.Value.NewIndex = next.Value.NewIndex;
                                current.Value.NextDeltaNode = next.Value.NextDeltaNode;
                                deltaNodes.Remove(current);
                                deltaNodes.AddAfter(next, current);
                                ClearDeltaNode(next);
                                deltaNodes.Remove(next);
                                return OptimizationResult.RemovedBoth;
                            }}
                            else if (next.Value.Operation == ListOperation.Replace)
                            {{
                                current.Value.BaseValue = next.Value.BaseValue;
                                current.Value.NextDeltaNode = next.Value.NextDeltaNode;
                                ClearDeltaNode(next);
                                deltaNodes.Remove(next);
                                return OptimizationResult.RemovedNext;
                            }}
                            else if (next.Value.Operation == ListOperation.Remove)
                            {{
                                int myIndex = originalIndex;
                                LinkedListNode<MutableTListDelta> after = current.Next!;
                                while (true)
                                {{
                                    int updatedIndex = GetUpdatedIndex(after!.Value, myIndex);
                                    if (after.Value.CurrentIndex > myIndex)
                                    {{
                                        after.Value.CurrentIndex--;
                                    }}
                                    if (after.Value.NewIndex > myIndex)
                                    {{
                                        after.Value.NewIndex--;
                                    }}
                                    myIndex = updatedIndex;
                                    if (after == next)
                                    {{
                                        break;
                                    }}
                                    after = after.Next!;
                                }}
                                ClearDeltaNode(current);
                                ClearDeltaNode(next);
                                deltaNodes.Remove(current);
                                deltaNodes.Remove(next);
                                return OptimizationResult.RemovedBoth;
                            }}
                            return OptimizationResult.RemovedNone;
                        }}

                        private OptimizationResult OptimizeMove(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            if (current.Value.CurrentIndex == current.Value.NewIndex || current.Value.CurrentIndex == current.Value.NewIndex - 1)
                            {{
                                ClearDeltaNode(current);
                                deltaNodes.Remove(current);
                                return OptimizationResult.RemovedCurrent;
                            }}
                            int currentIndex = current.Value.CurrentIndex > current.Value.NewIndex ? current.Value.CurrentIndex + 1 : current.Value.CurrentIndex;
                            int newIndex = current.Value.NewIndex > current.Value.CurrentIndex ? current.Value.NewIndex - 1 : current.Value.NewIndex;
                            if (next.Value.Operation == ListOperation.Move || next.Value.Operation == ListOperation.Remove)
                            {{
                                int myCurrentIndex = currentIndex;
                                int myNewIndex = newIndex;
                                LinkedListNode<MutableTListDelta> after = current.Next!;
                                while (true)
                                {{
                                    int updatedCurrentIndex = GetUpdatedIndex(after!.Value, myCurrentIndex, true);
                                    int updatedNewIndex = GetUpdatedIndex(after.Value, myNewIndex);
                                    if (after.Value.Operation == ListOperation.Replace && after.Value.CurrentIndex == myNewIndex)
                                    {{
                                        after.Value.CurrentIndex = current.Value.CurrentIndex > current.Value.NewIndex ? myCurrentIndex - 1 : myCurrentIndex;
                                    }}
                                    else
                                    {{
                                        if (current.Value.CurrentIndex > current.Value.NewIndex && after.Value.CurrentIndex > myNewIndex && after.Value.CurrentIndex < myCurrentIndex)
                                        {{
                                            after.Value.CurrentIndex--;
                                        }}
                                        else if (current.Value.CurrentIndex < current.Value.NewIndex && after.Value.CurrentIndex <= myNewIndex && after.Value.CurrentIndex >= myCurrentIndex)
                                        {{
                                            after.Value.CurrentIndex++;
                                        }}
                                        if (current.Value.CurrentIndex > current.Value.NewIndex && after.Value.NewIndex > myNewIndex && after.Value.NewIndex < myCurrentIndex)
                                        {{
                                            after.Value.NewIndex--;
                                        }}
                                        else if (current.Value.CurrentIndex < current.Value.NewIndex && after.Value.NewIndex <= myNewIndex && after.Value.NewIndex >= myCurrentIndex)
                                        {{
                                            after.Value.NewIndex++;
                                        }}
                                    }}
                                    if (after != next)
                                    {{
                                        myCurrentIndex = updatedCurrentIndex;
                                        myNewIndex = updatedNewIndex;
                                    }}
                                    else
                                    {{
                                        break;
                                    }}
                                    after = after.Next!;
                                }}
                                next.Value.CurrentIndex = current.Value.CurrentIndex > current.Value.NewIndex ? myCurrentIndex - 1 : myCurrentIndex;
                                ClearDeltaNode(current);
                                deltaNodes.Remove(current);
                                return OptimizationResult.RemovedCurrent;
                            }}
                            return OptimizationResult.RemovedNone;
                        }}

                        private OptimizationResult OptimizeReplace(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            if (next.Value.Operation == ListOperation.Replace || next.Value.Operation == ListOperation.Remove)
                            {{
                                ClearDeltaNode(current);
                                deltaNodes.Remove(current);
                                return OptimizationResult.RemovedCurrent;
                            }}
                            return OptimizationResult.RemovedNone;
                        }}

                        private int GetUpdatedIndex(TListDelta delta, int index, bool ignoreMove = false)
                        {{
                            if (delta.Operation == ListOperation.Insert)
                            {{
                                if (ignoreMove)
                                {{
                                    return delta.NewIndex < index ? index + 1 : index;
                                }}
                                return delta.NewIndex <= index ? index + 1 : index;
                            }}
                            else if (delta.Operation == ListOperation.Move)
                            {{
                                if (delta.CurrentIndex == index)
                                {{
                                    if (ignoreMove)
                                    {{
                                        return delta.NewIndex < delta.CurrentIndex ? index + 1 : index;
                                    }}
                                    return delta.NewIndex > delta.CurrentIndex ? delta.NewIndex - 1 : delta.NewIndex;
                                }}
                                else if (delta.NewIndex == index && ignoreMove)
                                {{
                                    return delta.NewIndex > delta.CurrentIndex ? index - 1 : index;
                                }}
                                if (delta.NewIndex > delta.CurrentIndex && index > delta.CurrentIndex && index < delta.NewIndex)
                                {{
                                    return index - 1;
                                }}
                                if (delta.NewIndex < delta.CurrentIndex && index <= delta.CurrentIndex && index >= delta.NewIndex)
                                {{
                                    return index + 1;
                                }}
                            }}
                            else if (delta.Operation == ListOperation.Remove)
                            {{
                                return delta.CurrentIndex < index ? index - 1 : index;
                            }}
                            return index;
                        }}

                        private void ApplyDelta(TListDelta delta)
                        {{
                            if (delta == null)
                            {{
                                return;
                            }}
                            ListOperation operation = delta.Operation;
                            switch (operation)
                            {{
                                case ListOperation.Insert:
                                    {{
                                        int newIndex = delta.NewIndex;
                                        if (!NewIndexIsValid(newIndex)) return;
                                        T? baseValue = delta.BaseValue;
                                        if (baseValue.HasValue) Insert(newIndex, baseValue.Value);
                                        break;
                                    }}
                                case ListOperation.Move:
                                    {{
                                        int currentIndex = delta.CurrentIndex;
                                        if (!CurrentIndexIsValid(currentIndex)) return;
                                        int newIndex = delta.NewIndex;
                                        if (!NewIndexIsValid(newIndex)) return;
                                        Move(currentIndex, newIndex);
                                        break;
                                    }}
                                case ListOperation.Replace:
                                    {{
                                        int currentIndex = delta.CurrentIndex;
                                        if (!CurrentIndexIsValid(currentIndex)) return;
                                        T? baseValue = delta.BaseValue;
                                        if (baseValue.HasValue) this[currentIndex] = baseValue.Value;
                                        break;
                                    }}
                                case ListOperation.Remove:
                                    {{
                                        int currentIndex = delta.CurrentIndex;
                                        if (!CurrentIndexIsValid(currentIndex)) return;
                                        RemoveAt(currentIndex);
                                        break;
                                    }}
                                case ListOperation.Clear:
                                    {{
                                        Clear();
                                        break;
                                    }}
                            }}
                        }}

                        public virtual void ApplyDelta(IReadOnlyList<TListDelta>? delta)
                        {{
                            if (delta == null)
                            {{
                                return;
                            }}
                            for (int i = 0; i < delta.Count; i++)
                            {{
                                ApplyDelta(delta[i]);
                            }}
                        }}

                        private bool CurrentIndexIsValid(in int currentIndex) => currentIndex >= 0 && currentIndex < listItems.Count;

                        private bool NewIndexIsValid(in int newIndex) => newIndex >= 0 && newIndex <= listItems.Count;

                        public virtual void UpdateReferenceState() => ClearDeltaNodes();

                        private void ClearDeltaNodes()
                        {{
                            LinkedListNode<MutableTListDelta>? node = deltaNodes.First;
                            while (node != null)
                            {{
                                ClearDeltaNode(node);
                                deltaNodes.RemoveFirst();
                                node = deltaNodes.First;
                            }}
                        }}

                        private void ClearDeltaNode(LinkedListNode<MutableTListDelta> deltaNode)
                        {{
                            ResetDelta(deltaNode.Value);
                            deltaPool.Push(deltaNode.Value);
                            deltaNodePool.Push(deltaNode);
                        }}

                        private void ResetDelta(MutableTListDelta delta)
                        {{
                            delta.Identifier = null;
                            delta.NextDeltaNode = null;
                            delta.Operation = 0;
                            delta.CurrentIndex = 0;
                            delta.NewIndex = 0;
                            delta.BaseValue = null;
                        }}

                        private MutableTListDelta GetValueFromDeltaPool(object identifier)
                        {{
                            MutableTListDelta? delta;
                            if (deltaPool.TryPop(out delta))
                            {{
                                delta.Identifier = identifier;
                            }}
                            else
                            {{
                                delta = new MutableTListDelta(identifier);
                            }}
                            return delta;
                        }}

                        private LinkedListNode<MutableTListDelta> GetValueFromDeltaNodePool(MutableTListDelta delta)
                        {{
                            LinkedListNode<MutableTListDelta>? deltaNode;
                            if (deltaNodePool.TryPop(out deltaNode))
                            {{
                                deltaNode.Value = delta;
                            }}
                            else
                            {{
                                deltaNode = new LinkedListNode<MutableTListDelta>(delta);
                            }}
                            return deltaNode;
                        }}

                        private object GetValueFromIdentifierPool() => identifierPool.Count > 0 ? identifierPool.Pop() : new object();

                        private struct ListItem
                        {{
                            public object Identifier;
                            public T Value;
                            public LinkedListNode<MutableTListDelta>? LastDeltaNode;

                            public ListItem(object identifier, T value, LinkedListNode<MutableTListDelta>? lastDeltaNode)
                            {{
                                Identifier = identifier;
                                Value = value;
                                LastDeltaNode = lastDeltaNode;
                            }}
                        }}

                        private class MutableTListDelta : TListDelta
                        {{
                            public object? Identifier {{ get; set; }}
                            public LinkedListNode<MutableTListDelta>? NextDeltaNode {{ get; set; }}

                            public MutableTListDelta(object? identifier)
                            {{
                                Identifier = identifier;
                            }}

                            public new ListOperation Operation
                            {{
                                get => base.Operation;
                                set => base.Operation = value;
                            }}

                            public new int CurrentIndex
                            {{
                                get => base.CurrentIndex;
                                set => base.CurrentIndex = value;
                            }}

                            public new int NewIndex
                            {{
                                get => base.NewIndex;
                                set => base.NewIndex = value;
                            }}

                            public new T? BaseValue
                            {{
                                get => base.BaseValue;
                                set => base.BaseValue = value;
                            }}
                        }}

                        private class ImmutableTList : TList
                        {{
                            private IReadOnlyList<T> iReadOnlyList;
                            private IList<T> iList;
                            private TList tList;
                            public override int Count
                            {{
                                get
                                {{
                                    if (iReadOnlyList != null)
                                    {{
                                        return iReadOnlyList.Count;
                                    }}
                                    else
                                    {{
                                        return iList.Count;
                                    }}
                                }}
                            }}
                            public override bool IsReadOnly {{ get => true; }}

                            public override int Capacity
                            {{
                                get
                                {{
                                    if (tList != null)
                                    {{
                                        return tList.Capacity;
                                    }}
                                    throw new NotSupportedException();
                                }}
                                set => throw new NotMutableException();
                            }}

                            public override T this[int index]
                            {{
                                get
                                {{
                                    if (iReadOnlyList != null)
                                    {{
                                        return iReadOnlyList[index];
                                    }}
                                    else
                                    {{
                                        return iList[index];
                                    }}
                                }}
                                set => throw new NotMutableException();
                            }}

                            public ImmutableTList(IReadOnlyList<T> list) : base(byte.MinValue)
                            {{
                                this.iReadOnlyList = list;
                            }}

                            public ImmutableTList(IList<T> list) : base(byte.MinValue)
                            {{
                                this.iList = list;
                            }}

                            public ImmutableTList(TList list) : base(byte.MinValue)
                            {{
                                this.iReadOnlyList = list;
                                this.iList = list;
                                this.tList = list;
                            }}

                            public override void Add(T item) => throw new NotMutableException();

                            public override void Clear() => throw new NotMutableException();

                            public override void CopyTo(T[] array, int arrayIndex)
                            {{
                                if (array == null)
                                {{
                                    throw new ArgumentNullException(""The array is null"");
                                }}
                                if (arrayIndex < 0 || arrayIndex >= array.Length)
                                {{
                                    throw new ArgumentOutOfRangeException(""The arrayIndex is outside the array"");
                                }}
                                if (Count > array.Length - arrayIndex)
                                {{
                                    throw new ArgumentException(""The array does not have enough space"");
                                }}
                                if (iReadOnlyList != null)
                                {{
                                    for (int i = 0; i < Count; i++)
                                    {{
                                        array[arrayIndex + i] = iReadOnlyList[i];
                                    }}
                                }}
                                else
                                {{
                                    for (int i = 0; i < Count; i++)
                                    {{
                                        array[arrayIndex + i] = iList[i];
                                    }}
                                }}

                            }}

                            public override IEnumerator<T> GetEnumerator()
                            {{
                                if (iReadOnlyList != null)
                                {{
                                    return iReadOnlyList.GetEnumerator();
                                }}
                                else
                                {{
                                    return iList.GetEnumerator();
                                }}
                            }}

                            public override int IndexOf(T item)
                            {{
                                if (iReadOnlyList != null)
                                {{
                                    for (int i = 0; i < Count; i++)
                                    {{
                                        if (iReadOnlyList[i].IsEqualTo(item))
                                        {{
                                            return i;
                                        }}
                                    }}
                                }}
                                else
                                {{
                                    for (int i = 0; i < Count; i++)
                                    {{
                                        if (iList[i].IsEqualTo(item))
                                        {{
                                            return i;
                                        }}
                                    }}
                                }}

                                return -1;
                            }}

                            public override void Insert(int index, T item) => throw new NotMutableException();

                            public override void Move(int currentIndex, int newIndex) => throw new NotMutableException();

                            public override bool Remove(T item) => throw new NotMutableException();

                            public override void RemoveAt(int index) => throw new NotMutableException();

                            public override IReadOnlyList<TListDelta>? GetDelta()
                            {{
                                if (tList != null)
                                {{
                                    return tList.GetDelta();
                                }}
                                throw new NotSupportedException();
                            }}

                            public override void ApplyDelta(IReadOnlyList<TListDelta>? delta) => throw new NotMutableException();

                            public override void UpdateReferenceState() => throw new NotMutableException();
                        }}
                    }}
                }}
            ";
        }
    }
}