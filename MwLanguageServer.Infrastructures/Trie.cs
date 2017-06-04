using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MwLanguageServer.Infrastructures
{
    public class Trie<TKeyElement, TValue> : IDictionary<IEnumerable<TKeyElement>, TValue>
    {

        private Node root;
        private int _Count;
        private KeyCollection _Keys;
        private ValueCollection _Value;

        public Trie() : this(Comparer<TKeyElement>.Default)
        {

        }

        public Trie(IComparer<TKeyElement> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            Comparer = comparer;
        }

        public IComparer<TKeyElement> Comparer { get; }

        /// <inheritdoc />
        public KeyCollection Keys
        {
            get
            {
                if (_Keys == null) _Keys = new KeyCollection(this);
                return _Keys;
            }
        }

        /// <inheritdoc />
        public ValueCollection Values
        {
            get
            {
                if (_Value == null) _Value = new ValueCollection(this);
                return _Value;
            }
        }

        public IEnumerable<KeyValuePair<IEnumerable<TKeyElement>, TValue>> WithPrefix(IEnumerable<TKeyElement> prefix)
        {
            var node = GetNode(prefix, false, true);
            if (node == null) yield break;
            if (node.HasValue)
                yield return new KeyValuePair<IEnumerable<TKeyElement>, TValue>(node.FullKey, node.Value);
            using (var enu = new Enumerator(this, node.Mid))
            {
                while (enu.MoveNext()) yield return enu.Current;
            }
        }

        private IEnumerable<TKeyElement> MakeSafeKey(IEnumerable<TKeyElement> key, out int length)
        {
            length = -1;
            if (key == null) return null;
            var ks = key as string;
            if (ks != null)
            {
                length = ks.Length;
                return key;
            }
            else
            {
                var copy = new ReadOnlyCollection<TKeyElement>(key.ToArray());
                length = copy.Count;
                return copy;
            }
        }

        #region Trie Implementation

        private Node GetNode(IEnumerable<TKeyElement> prefix, bool allowInsertion, bool prefixMode)
        {
            Debug.Assert(prefix != null);
            Debug.Assert(!(allowInsertion && prefixMode));
            using (var enu = prefix.GetEnumerator())
            {
                var hasMore = enu.MoveNext();
                if (!hasMore)       // Empty sequence
                {
                    return prefixMode ? root : null;
                }
                var curKey = enu.Current;
                var current = root;
                while (true)
                {
                    var cmp = current == null ? 0 : Comparer.Compare(curKey, current.Key);
                    if (cmp > 0)
                    {
                        if (current.Right == null)
                        {
                            if (!allowInsertion) return null;
                            current.Right = new Node(curKey);
                        }
                        current = current.Right;
                    }
                    else if (cmp < 0)
                    {
                        if (current.Left == null)
                        {
                            if (!allowInsertion) return null;
                            current.Left = new Node(curKey);
                        }
                        current = current.Left;
                    }
                    else
                    {
                        hasMore = enu.MoveNext();
                        if (current == null)
                        {
                            Debug.Assert(root == null);
                            if (!allowInsertion) return null;
                            current = root = new Node(curKey);
                        }
                        if (!hasMore) return current;
                        curKey = enu.Current;
                        if (current.Mid == null)
                        {
                            if (!allowInsertion) return null;
                            current.Mid = new Node(curKey);
                        }
                        current = current.Mid;
                    }
                }
            }
        }

        private bool RemoveNode(IEnumerable<TKeyElement> key)
        {
            //TODO Delete the node to release the memory.
            Debug.Assert(key != null);
            using (var enu = key.GetEnumerator())
            {
                var hasMore = enu.MoveNext();
                if (!hasMore) return false;     // Empty sequence.
                var curKey = enu.Current;
                if (root == null) return false;
                var current = root;
                while (true)
                {
                    var cmp = Comparer.Compare(curKey, current.Key);
                    if (cmp > 0)
                    {
                        if (current.Right == null) return false;
                        current = current.Right;
                    }
                    else if (cmp < 0)
                    {
                        if (current.Left == null) return false;
                        current = current.Left;
                    }
                    else
                    {
                        hasMore = enu.MoveNext();
                        if (hasMore)
                        {
                            if (current.Mid == null) return false;
                            current = current.Mid;
                            curKey = enu.Current;
                        }
                        else
                        {
                            if (!current.HasValue) return false;
                            current.FullKey = null;
                            current.Value = default(TValue);
                            return true;
                        }
                    }
                }
            }
        }

        [DebuggerDisplay("({Key}, {Value})")]
        internal class Node
        {
            public Node(TKeyElement key)
            {
                Key = key;
            }

            public TKeyElement Key { get; }

            public Node Left { get; set; }

            public Node Right { get; set; }

            public Node Mid { get; set; }

            public IEnumerable<TKeyElement> FullKey { get; set; }

            public TValue Value { get; set; }

            public bool HasValue => FullKey != null;

            private static void Dump(StringBuilder builder, Node node, char leading, int indension)
            {
                builder.Append('.', indension);
                builder.Append(leading);
                builder.Append(' ');
                if (node == null)
                {
                    builder.AppendLine("[null]");
                    return;
                }
                builder.Append(node.Key);
                if (node.HasValue)
                {
                    builder.Append(" = ");
                    builder.Append(node.Value);
                }
                builder.AppendLine();
                Dump(builder, node.Left, 'L', indension + 1);
                Dump(builder, node.Mid, 'M', indension + 1);
                Dump(builder, node.Right, 'R', indension + 1);
            }

            public string Dump()
            {
                var sb = new StringBuilder();
                Dump(sb, this, '-', 0);
                return sb.ToString();
            }

        }

        #endregion

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<IEnumerable<TKeyElement>, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<KeyValuePair<IEnumerable<TKeyElement>,TValue>>

        /// <inheritdoc />
        void ICollection<KeyValuePair<IEnumerable<TKeyElement>, TValue>>.Add(
            KeyValuePair<IEnumerable<TKeyElement>, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            root = null;
            _Count = 0;
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<IEnumerable<TKeyElement>, TValue> item)
        {
            return GetNode(item.Key, false, false)?.HasValue ?? false;
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<IEnumerable<TKeyElement>, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            foreach (var p in this)
            {
                array[arrayIndex++] = new KeyValuePair<IEnumerable<TKeyElement>, TValue>(p.Key.ToArray(), p.Value);
            }
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<IEnumerable<TKeyElement>, TValue>>.Remove(
            KeyValuePair<IEnumerable<TKeyElement>, TValue> item)
        {
            return Remove(item.Key);
        }

        /// <inheritdoc />
        public int Count => _Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        #endregion

        #region Implementation of IDictionary<IEnumerable<TKeyElement>,TValue>

        /// <inheritdoc />
        public void Add(IEnumerable<TKeyElement> key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            key = MakeSafeKey(key, out var length);
            if (length == 0) throw new ArgumentException("Key is empty.", nameof(key));
            var node = GetNode(key, true, false);
            if (node.HasValue) throw new InvalidOperationException("An element with the same key already exists.");
            node.FullKey = key;
            node.Value = value;
            _Count++;
        }

        /// <inheritdoc />
        public bool ContainsKey(IEnumerable<TKeyElement> key)
        {
            return GetNode(key, false, false)?.HasValue ?? false;
        }

        /// <inheritdoc />
        public bool Remove(IEnumerable<TKeyElement> key)
        {
            if (RemoveNode(key))
            {
                _Count--;
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public bool TryGetValue(IEnumerable<TKeyElement> key, out TValue value)
        {
            value = default(TValue);
            var node = GetNode(key, false, false);
            if (node == null || !node.HasValue) return false;
            value = node.Value;
            return true;
        }

        /// <inheritdoc />
        public TValue this[IEnumerable<TKeyElement> key]
        {
            get
            {
                var node = GetNode(key, false, false);
                if (node == null || !node.HasValue) throw new KeyNotFoundException();
                return node.Value;
            }
            set
            {
                key = MakeSafeKey(key, out var length);
                if (length == 0) throw new ArgumentException("Key is empty.", nameof(key));
                var node = GetNode(key, true, false);
                if (!node.HasValue)
                {
                    node.FullKey = key;
                    _Count++;
                }
                node.Value = value;
            }
        }

        /// <inheritdoc />
        ICollection<IEnumerable<TKeyElement>> IDictionary<IEnumerable<TKeyElement>, TValue>.Keys => Keys;

        /// <inheritdoc />
        ICollection<TValue> IDictionary<IEnumerable<TKeyElement>, TValue>.Values => Values;

        [DebuggerDisplay("({Node}, {State})")]
        private class EnumeratorStackState
        {
            public EnumeratorStackState(Node node)
            {
                Node = node;
            }

            public Node Node { get; }

            public byte State { get; set; }

        }

        public struct Enumerator : IEnumerator<KeyValuePair<IEnumerable<TKeyElement>, TValue>>
        {

            private readonly Trie<TKeyElement, TValue> owner;
            private readonly Node root;
            private readonly Stack<EnumeratorStackState> stack;

            internal Enumerator(Trie<TKeyElement, TValue> owner) : this(owner, owner.root)
            {
            }

            internal Enumerator(Trie<TKeyElement, TValue> owner, Node root)
            {
                this.owner = owner;
                this.root = root;
                stack = new Stack<EnumeratorStackState>();
                Reset();
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                if (owner == null) throw new InvalidOperationException();
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    switch (top.State++)
                    {
                        case 0:
                            if (top.Node.Left != null) stack.Push(new EnumeratorStackState(top.Node.Left));
                            break;
                        case 1:
                            //key?.Add(top.Node.Key);
                            if (top.Node.HasValue)
                            {
                                Current = new KeyValuePair<IEnumerable<TKeyElement>, TValue>(top.Node.FullKey,
                                    top.Node.Value);
                                return true;
                            }
                            break;
                        case 2:
                            if (top.Node.Mid != null) stack.Push(new EnumeratorStackState(top.Node.Mid));
                            break;
                        case 3:
                            //key?.RemoveAt(key.Count - 1);
                            if (top.Node.Right != null) stack.Push(new EnumeratorStackState(top.Node.Right));
                            break;
                        case 4:
                            stack.Pop();
                            break;
                    }
                }
                return false;
            }

            /// <inheritdoc />
            public void Reset()
            {
                if (owner == null) throw new InvalidOperationException();
                stack.Clear();
                if (root != null)
                    stack.Push(new EnumeratorStackState(root));
            }

            /// <inheritdoc />
            public KeyValuePair<IEnumerable<TKeyElement>, TValue> Current { get; private set; }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public void Dispose()
            {
                stack.Clear();
            }
        }

        public class KeyCollection : ICollection<IEnumerable<TKeyElement>>
        {
            private readonly Trie<TKeyElement, TValue> owner;

            internal KeyCollection(Trie<TKeyElement, TValue> owner)
            {
                this.owner = owner;
            }

            /// <inheritdoc />
            public IEnumerator<IEnumerable<TKeyElement>> GetEnumerator()
            {
                using (var enu = new Enumerator(owner))
                {
                    while (enu.MoveNext()) yield return enu.Current.Key;
                }
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <inheritdoc />
            void ICollection<IEnumerable<TKeyElement>>.Add(IEnumerable<TKeyElement> item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            void ICollection<IEnumerable<TKeyElement>>.Clear()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public bool Contains(IEnumerable<TKeyElement> item)
            {
                return owner.ContainsKey(item);
            }

            /// <inheritdoc />
            public void CopyTo(IEnumerable<TKeyElement>[] array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                foreach (var i in this)
                {
                    array[arrayIndex++] = i;
                }
            }

            /// <inheritdoc />
            public bool Remove(IEnumerable<TKeyElement> item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public int Count => owner.Count;

            /// <inheritdoc />
            public bool IsReadOnly => true;
        }

        public class ValueCollection : ICollection<TValue>
        {
            private readonly Trie<TKeyElement, TValue> owner;

            internal ValueCollection(Trie<TKeyElement, TValue> owner)
            {
                this.owner = owner;
            }

            /// <inheritdoc />
            public IEnumerator<TValue> GetEnumerator()
            {
                using (var enu = new Enumerator(owner))
                {
                    while (enu.MoveNext()) yield return enu.Current.Value;
                }
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <inheritdoc />
            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public bool Contains(TValue item)
            {
                return this.AsEnumerable().Contains(item);
            }

            /// <inheritdoc />
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                foreach (var i in this)
                {
                    array[arrayIndex++] = i;
                }
            }

            /// <inheritdoc />
            public bool Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public int Count => owner.Count;

            /// <inheritdoc />
            public bool IsReadOnly => true;
        }

        #endregion
    }
}
