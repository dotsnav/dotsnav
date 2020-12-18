using System;
using System.Diagnostics;
using DotsNav.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(PriorityQueueDebugView<>))]
    struct PriorityQueue<T> where T : unmanaged, PriorityQueue<T>.IElement
    {
        // todo can we nest debugview and make private?
        internal List<T> _data;
        UnsafeHashMap<int, int> _index;

        public PriorityQueue(int intialCapacity, Allocator allocator = Allocator.Persistent)
        {
            Assert.IsTrue(intialCapacity > 1, "capacity must be larger than 1");
            _data = new List<T>(intialCapacity, allocator);
            _index = new UnsafeHashMap<int, int>(intialCapacity, allocator);
            Clear();
        }

        public T Top => _data[0];
        public int Count => _data.Length;
        public bool Contains(int id) => _index.ContainsKey(id);

        public void InsertOrLowerKey(T n)
        {
            if (!_index.TryGetValue(n.Id, out var i))
                Insert(n);
            else if (n.CompareTo(_data[i]) < 0)
                Percolate(i, n);
        }

        public void Insert(T n)
        {
            Assert.IsTrue(!_index.ContainsKey(n.Id), "duplicate element inserted");
            _data.Resize(_data.Length + 1);
            Percolate(_data.Length - 1, n);
        }

        public T Extract()
        {
            Assert.IsTrue(Count > 0, "heap contains no elements");
            var top = _data[0];
            if (Count > 1)
                Trickle(0, _data.TakeLast());
            else
                _data.Clear();
            return top;
        }

        public void Remove(int id)
        {
            Assert.IsTrue(_index.ContainsKey(id), "unknown id");
            Trickle(_index[id], _data.TakeLast());
            _index.Remove(id);
        }

        public void Clear()
        {
            _data.Clear();
            _index.Clear();
        }

        public void Dispose()
        {
            _data.Dispose();
            _index.Dispose();
        }

        void Trickle(int i, T n)
        {
            var size = Count;
            var child = Left(i);

            while (child < size)
            {
                if (child + 1 < size && _data[child].CompareTo(_data[child + 1]) > 0)
                    ++child;

                var e = _data[child];
                _data[i] = e;
                _index[e.Id] = i;
                i = child;
                child = Left(i);
            }

            Percolate(i, n);
        }

        void Percolate(int i, T n)
        {
            var parent = Parent(i);

            while (i > 0 && _data[parent].CompareTo(n) > 0)
            {
                var e = _data[parent];
                _data[i] = e;
                _index[e.Id] = i;
                i = parent;
                parent = Parent(i);
            }

            _data[i] = n;
            _index[n.Id] = i;
        }

        static int Parent(int i) => (i - 1) >> 1;
        static int Left(int i) => (i << 1) + 1;

        public interface IElement : IComparable<T>
        {
            int Id { get; }
        }
    }

    sealed class PriorityQueueDebugView<T> where T : unmanaged, PriorityQueue<T>.IElement
    {
        readonly PriorityQueue<T> _data;

        public PriorityQueueDebugView(PriorityQueue<T> data)
        {
            _data = data;
        }

        public System.Collections.Generic.List<T> Items
        {
            get
            {
                var l = new System.Collections.Generic.List<T>();
                for (int i = 0; i < _data.Count; i++)
                    l.Add(_data._data[i]);
                l.Sort();
                return l;
            }
        }
    }
}