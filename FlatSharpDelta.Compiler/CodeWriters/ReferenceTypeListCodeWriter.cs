using System;
using System.IO;
using System.Collections.Generic;
using reflection;

namespace FlatSharpDelta.Compiler
{
    static class ReferenceTypeListCodeWriter
    {
        public static string WriteCode(Schema schema, reflection.Object obj)
        {
            string name = obj.GetNameWithoutNamespace();
            string _namespace = obj.GetNamespace();

            return $@"
                namespace {_namespace}
                {{
                    using T = {name};
                    using TDelta = {name}Delta;
                    using TList = {name}List;
                    using TListDelta = {name}ListDelta;

                    public class {name}List : IReadOnlyList<T>, IList<T>
                    {{
                        private List<ListItem> listItems;
                        private List<MutableTListDelta> deltasToReturn;
                        private ReadOnlyCollection<MutableTListDelta> deltasToReturnReadOnly;
                        private LinkedList<MutableTListDelta> deltas;
                        private Stack<MutableTListDelta> deltaPool;
                        private Stack<LinkedListNode<MutableTListDelta>> deltasNodePool;
                        private Stack<LinkedList<LinkedListNode<MutableTListDelta>>> listItemDeltasPool;
                        private Stack<LinkedListNode<LinkedListNode<MutableTListDelta>>> listItemDeltasNodePool;
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
                                if (value != listItem.Value)
                                {{
                                    listItems[index] = new ListItem(value, listItem.Deltas);
                                    MutableTListDelta delta = GetValueFromDeltaPool();
                                    delta.Operation = ListOperation.Replace;
                                    delta.CurrentIndex = index;
                                    delta.BaseValue = value;
                                    LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                                    deltas.AddLast(node);
                                    listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
                                }}
                            }}
                        }}

                        private void Initialize()
                        {{
                            deltasToReturn = new List<MutableTListDelta>();
                            deltasToReturnReadOnly = new ReadOnlyCollection<MutableTListDelta>(deltasToReturn);
                            deltas = new LinkedList<MutableTListDelta>();
                            deltaPool = new Stack<MutableTListDelta>();
                            deltasNodePool = new Stack<LinkedListNode<MutableTListDelta>>();
                            listItemDeltasPool = new Stack<LinkedList<LinkedListNode<MutableTListDelta>>>();
                            listItemDeltasNodePool = new Stack<LinkedListNode<LinkedListNode<MutableTListDelta>>>();
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
                                T item = list[i];
                                listItems.Add(new ListItem(item != null ? new T(item) : null, new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}
                        }}

                        public {name}List(IList<T> list)
                        {{
                            Initialize();
                            int count = list.Count;
                            listItems = new List<ListItem>(count);
                            for (int i = 0; i < count; i++)
                            {{
                                T item = list[i];
                                listItems.Add(new ListItem(item != null ? new T(item) : null, new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}
                        }}

                        public {name}List(TList list)
                        {{
                            Initialize();
                            int count = list.Count;
                            listItems = new List<ListItem>(count);
                            for (int i = 0; i < count; i++)
                            {{
                                T item = list[i];
                                listItems.Add(new ListItem(item != null ? new T(item) : null, new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}
                        }}
                #pragma warning restore CS8618
                        public static TList ShallowCopy(IReadOnlyList<T> list)
                        {{
                            int count = list.Count;
                            TList _this = new TList(count);
                            for (int i = 0; i < count; i++)
                            {{
                                _this.listItems.Add(new ListItem(list[i], new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}

                            return _this;
                        }}

                        public static TList ShallowCopy(IList<T> list)
                        {{
                            int count = list.Count;
                            TList _this = new TList(count);
                            for (int i = 0; i < count; i++)
                            {{
                                _this.listItems.Add(new ListItem(list[i], new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}

                            return _this;
                        }}

                        public static TList ShallowCopy(TList list)
                        {{
                            int count = list.Count;
                            TList _this = new TList(count);
                            for (int i = 0; i < count; i++)
                            {{
                                _this.listItems.Add(new ListItem(list[i], new LinkedList<LinkedListNode<MutableTListDelta>>()));
                            }}

                            return _this;
                        }}

                        public static TList AsImmutable(IReadOnlyList<T> list) => new ImmutableTList(list);

                        public static TList AsImmutable(IList<T> list) => new ImmutableTList(list);

                        public static TList AsImmutable(TList list) => new ImmutableTList(list);

                        public virtual void Add(T item)
                        {{
                            ListItem listItem = new ListItem(item, GetValueFromListItemDeltasPool());
                            listItems.Add(listItem);
                            MutableTListDelta delta = GetValueFromDeltaPool();
                            delta.Operation = ListOperation.Insert;
                            delta.NewIndex = listItems.Count - 1;
                            delta.BaseValue = item;
                            LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                            deltas.AddLast(node);
                            listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
                        }}

                        public virtual void Clear()
                        {{
                            for (int i = listItems.Count - 1; i >= 0; i--)
                            {{
                                ListItem listItem = listItems[i];
                                ClearListItemDeltas(listItem.Deltas);
                                listItemDeltasPool.Push(listItem.Deltas);
                                listItems.RemoveAt(i);
                            }}
                            ClearDeltas();
                            MutableTListDelta delta = GetValueFromDeltaPool();
                            delta.Operation = ListOperation.Clear;
                            LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                            deltas.AddLast(node);
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
                                if (listItems[i].Value == item)
                                {{
                                    return i;
                                }}
                            }}
                            return -1;
                        }}

                        public virtual void Insert(int index, T item)
                        {{
                            ListItem listItem = new ListItem(item, GetValueFromListItemDeltasPool());
                            listItems.Insert(index, listItem);
                            MutableTListDelta delta = GetValueFromDeltaPool();
                            delta.Operation = ListOperation.Insert;
                            delta.NewIndex = index;
                            delta.BaseValue = item;
                            LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                            deltas.AddLast(node);
                            listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
                        }}

                        public virtual void Move(int currentIndex, int newIndex)
                        {{
                            ListItem listItem = listItems[currentIndex];
                            listItems.RemoveAt(currentIndex);
                            listItems.Insert(newIndex > currentIndex ? newIndex - 1 : newIndex, listItem);
                            MutableTListDelta delta = GetValueFromDeltaPool();
                            delta.Operation = ListOperation.Move;
                            delta.CurrentIndex = currentIndex;
                            delta.NewIndex = newIndex;
                            LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                            deltas.AddLast(node);
                            listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
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
                            MutableTListDelta delta = GetValueFromDeltaPool();
                            delta.Operation = ListOperation.Remove;
                            delta.CurrentIndex = index;
                            LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                            deltas.AddLast(node);
                            listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
                            FinalizeListItemDeltas(listItem, index);
                            ClearListItemDeltas(listItem.Deltas);
                            listItemDeltasPool.Push(listItem.Deltas);
                        }}

                        private enum OptimizationResult {{ _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12 }}

                        public virtual IReadOnlyList<TListDelta>? GetDelta()
                        {{
                            for (int i = 0; i < listItems.Count; i++)
                            {{
                                FinalizeListItemDeltas(listItems[i], i);
                            }}
                            deltasToReturn.Clear();
                            if (deltasToReturn.Capacity < deltas.Count)
                            {{
                                deltasToReturn.Capacity = deltas.Count;
                            }}
                            deltasToReturn.AddRange(deltas);
                            return deltasToReturnReadOnly.Count > 0 ? deltasToReturnReadOnly : null;
                        }}

                        private void FinalizeListItemDeltas(in ListItem listItem, int index)
                        {{
                            LinkedList<LinkedListNode<MutableTListDelta>> listItemDeltas = listItem.Deltas;
                            LinkedListNode<LinkedListNode<MutableTListDelta>>? currentNode = listItemDeltas.First;
                            LinkedListNode<LinkedListNode<MutableTListDelta>>? nextNode = currentNode?.Next;
                            LinkedListNode<LinkedListNode<MutableTListDelta>>? tempNode;
                            bool ignoreModify = false;
                            while (currentNode != listItemDeltas.Last)
                            {{
                                switch (OptimizeDelta(currentNode!.Value, nextNode!.Value))
                                {{
                                    case OptimizationResult._1:
                                        listItemDeltasNodePool.Push(nextNode);
                                        listItemDeltas.Remove(nextNode);
                                        nextNode = currentNode.Next;
                                        break;
                                    case OptimizationResult._2:
                                        tempNode = nextNode.Next;
                                        listItemDeltasNodePool.Push(nextNode);
                                        listItemDeltas.Remove(nextNode);
                                        nextNode = tempNode;
                                        break;
                                    case OptimizationResult._3:
                                        currentNode = null;
                                        ignoreModify = true;
                                        break;
                                    case OptimizationResult._5:
                                    case OptimizationResult._6:
                                    case OptimizationResult._8:
                                    case OptimizationResult._11:
                                        tempNode = currentNode.Next;
                                        listItemDeltasNodePool.Push(currentNode);
                                        listItemDeltas.Remove(currentNode);
                                        currentNode = tempNode;
                                        nextNode = tempNode?.Next;
                                        break;
                                    case OptimizationResult._7:
                                    case OptimizationResult._9:
                                    case OptimizationResult._12:
                                        if (nextNode.Next == null)
                                        {{
                                            currentNode = currentNode.Next;
                                            nextNode = currentNode?.Next;
                                        }}
                                        else
                                        {{
                                            nextNode = nextNode.Next;
                                        }}
                                        break;
                                    case OptimizationResult._4:
                                    case OptimizationResult._10:
                                        break;
                                }}
                                if (ignoreModify)
                                {{
                                    break;
                                }}
                            }}
                            if (!ignoreModify)
                            {{
                                LinkedListNode<LinkedListNode<MutableTListDelta>>? firstNode = listItemDeltas.First;
                                LinkedListNode<LinkedListNode<MutableTListDelta>>? lastNode = listItemDeltas.Last;
                                ListOperation? firstOperation = firstNode?.Value.Value.Operation;
                                ListOperation? lastOperation = lastNode?.Value.Value.Operation;
                                if (listItemDeltas.Count == 0)
                                {{
                                    AddModifyDelta(listItem, index);
                                }}
                                else if (listItemDeltas.Count == 1)
                                {{
                                    if (firstOperation == ListOperation.Modify)
                                    {{
                                        UpdateModifyDelta(listItem, firstNode!);
                                    }}
                                    else if (firstOperation == ListOperation.Move)
                                    {{
                                        AddModifyDelta(listItem, index);
                                    }}
                                }}
                                else if (listItemDeltas.Count == 2)
                                {{
                                    if (firstOperation == ListOperation.Modify)
                                    {{
                                        UpdateModifyDelta(listItem, firstNode!);
                                    }}
                                    else if (lastOperation == ListOperation.Modify)
                                    {{
                                        UpdateModifyDelta(listItem, lastNode!);
                                    }}
                                }}
                            }}
                        }}

                        private void AddModifyDelta(in ListItem listItem, int index)
                        {{
                            TDelta? itemDelta = listItem.Value?.GetDelta();
                            if (itemDelta != null)
                            {{
                                MutableTListDelta delta = GetValueFromDeltaPool();
                                delta.Operation = ListOperation.Modify;
                                delta.CurrentIndex = index;
                                delta.DeltaValue = itemDelta;
                                LinkedListNode<MutableTListDelta> node = GetValueFromDeltasNodePool(delta);
                                deltas.AddLast(node);
                                listItem.Deltas.AddLast(GetValueFromListItemDeltasNodePool(node));
                            }}
                        }}

                        private void UpdateModifyDelta(in ListItem listItem, LinkedListNode<LinkedListNode<MutableTListDelta>> node)
                        {{
                            TDelta? itemDelta = listItem.Value?.GetDelta();
                            if (itemDelta != null)
                            {{
                                node.Value.Value.DeltaValue = itemDelta;
                            }}
                            else
                            {{
                                deltas.Remove(node.Value);
                                listItem.Deltas.Remove(node);
                                ClearDeltasNode(node.Value);
                                listItemDeltasNodePool.Push(node);
                            }}
                        }}

                        private OptimizationResult OptimizeDelta(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            switch (current.Value.Operation)
                            {{
                                case ListOperation.Insert: return OptimizeInsert(current, next);
                                case ListOperation.Move: return OptimizeMove(current, next);
                                case ListOperation.Replace: return OptimizeReplace(current, next);
                                case ListOperation.Modify: return OptimizeModify(current, next);
                                default: return OptimizationResult._10;
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
                                deltas.Remove(current);
                                deltas.AddAfter(next, current);
                                ClearDeltasNode(next);
                                deltas.Remove(next);
                                return OptimizationResult._1;
                            }}
                            else if (next.Value.Operation == ListOperation.Replace)
                            {{
                                current.Value.BaseValue = next.Value.BaseValue;
                                ClearDeltasNode(next);
                                deltas.Remove(next);
                                return OptimizationResult._2;
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
                                ClearDeltasNode(current);
                                ClearDeltasNode(next);
                                deltas.Remove(current);
                                deltas.Remove(next);
                                return OptimizationResult._3;
                            }}
                            return OptimizationResult._4;
                        }}

                        private OptimizationResult OptimizeMove(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            if (current.Value.CurrentIndex == current.Value.NewIndex || current.Value.CurrentIndex == current.Value.NewIndex - 1)
                            {{
                                ClearDeltasNode(current);
                                deltas.Remove(current);
                                return OptimizationResult._5;
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
                                ClearDeltasNode(current);
                                deltas.Remove(current);
                                return OptimizationResult._6;
                            }}
                            return OptimizationResult._7;
                        }}

                        private OptimizationResult OptimizeReplace(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            if (next.Value.Operation == ListOperation.Replace || next.Value.Operation == ListOperation.Remove)
                            {{
                                ClearDeltasNode(current);
                                deltas.Remove(current);
                                return OptimizationResult._8;
                            }}
                            return OptimizationResult._9;
                        }}

                        private OptimizationResult OptimizeModify(LinkedListNode<MutableTListDelta> current, LinkedListNode<MutableTListDelta> next)
                        {{
                            if (next.Value.Operation != ListOperation.Move)
                            {{
                                ClearDeltasNode(current);
                                deltas.Remove(current);
                                return OptimizationResult._11;
                            }}
                            return OptimizationResult._12;
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
                                        Insert(newIndex, baseValue != null ? new T(baseValue) : null!);
                                        break;
                                    }}
                                case ListOperation.Modify:
                                    {{
                                        int currentIndex = delta.CurrentIndex;
                                        if (!CurrentIndexIsValid(currentIndex)) return;
                                        listItems[currentIndex].Value?.ApplyDelta(delta.DeltaValue);
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
                                        this[currentIndex] = baseValue != null ? new T(baseValue) : null!;
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

                        public virtual void UpdateReferenceState()
                        {{
                            for (int i = 0; i < listItems.Count; i++)
                            {{
                                ListItem listItem = listItems[i];
                                listItem.Value?.UpdateReferenceState();
                                ClearListItemDeltas(listItem.Deltas);
                            }}
                            ClearDeltas();
                        }}

                        private void ClearDeltas()
                        {{
                            LinkedListNode<MutableTListDelta>? node = deltas.First;
                            while (node != null)
                            {{
                                ClearDeltasNode(node);
                                deltas.RemoveFirst();
                                node = deltas.First;
                            }}
                        }}

                        private void ClearListItemDeltas(LinkedList<LinkedListNode<MutableTListDelta>> listItemDeltas)
                        {{
                            LinkedListNode<LinkedListNode<MutableTListDelta>>? node = listItemDeltas.First;
                            while (node != null)
                            {{
                                listItemDeltasNodePool.Push(node);
                                listItemDeltas.RemoveFirst();
                                node = listItemDeltas.First;
                            }}
                        }}

                        private void ClearDeltasNode(LinkedListNode<MutableTListDelta> node)
                        {{
                            ResetDelta(node.Value);
                            deltaPool.Push(node.Value);
                            deltasNodePool.Push(node);
                        }}

                        private void ResetDelta(MutableTListDelta delta)
                        {{
                            delta.Operation = 0;
                            delta.CurrentIndex = 0;
                            delta.NewIndex = 0;
                            delta.BaseValue = null;
                            delta.DeltaValue = null;
                        }}

                        private MutableTListDelta GetValueFromDeltaPool()
                        {{
                            return deltaPool.Count > 0 ? deltaPool.Pop() : new MutableTListDelta();
                        }}

                        private LinkedListNode<MutableTListDelta> GetValueFromDeltasNodePool(MutableTListDelta delta)
                        {{
                            LinkedListNode<MutableTListDelta>? node;
                            if (deltasNodePool.TryPop(out node))
                            {{
                                node.Value = delta;
                            }}
                            else
                            {{
                                node = new LinkedListNode<MutableTListDelta>(delta);
                            }}
                            return node;
                        }}

                        private LinkedList<LinkedListNode<MutableTListDelta>> GetValueFromListItemDeltasPool()
                        {{
                            return listItemDeltasPool.Count > 0 ? listItemDeltasPool.Pop() : new LinkedList<LinkedListNode<MutableTListDelta>>();
                        }}

                        private LinkedListNode<LinkedListNode<MutableTListDelta>> GetValueFromListItemDeltasNodePool(LinkedListNode<MutableTListDelta> delta)
                        {{
                            LinkedListNode<LinkedListNode<MutableTListDelta>>? node;
                            if (listItemDeltasNodePool.TryPop(out node))
                            {{
                                node.Value = delta;
                            }}
                            else
                            {{
                                node = new LinkedListNode<LinkedListNode<MutableTListDelta>>(delta);
                            }}
                            return node;
                        }}

                        private struct ListItem
                        {{
                            public T Value;
                            public LinkedList<LinkedListNode<MutableTListDelta>> Deltas;

                            public ListItem(T value, LinkedList<LinkedListNode<MutableTListDelta>> deltas)
                            {{
                                Value = value;
                                Deltas = deltas;
                            }}
                        }}

                        private class MutableTListDelta : TListDelta
                        {{
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

                            public new TDelta? DeltaValue
                            {{
                                get => base.DeltaValue;
                                set => base.DeltaValue = value;
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
                                        if (iReadOnlyList[i] == item)
                                        {{
                                            return i;
                                        }}
                                    }}
                                }}
                                else
                                {{
                                    for (int i = 0; i < Count; i++)
                                    {{
                                        if (iList[i] == item)
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