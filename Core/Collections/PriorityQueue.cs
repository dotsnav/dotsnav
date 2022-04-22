using System;
using System.Diagnostics;
using Unity.Collections;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(PriorityQueueDebugView<>))]
    struct PriorityQueue<T> where T : unmanaged, PriorityQueue<T>.IElement
    {
        internal List<T> _data;

        public PriorityQueue(int intialCapacity, Allocator allocator = Allocator.Persistent)
        {
            Assert.IsTrue(intialCapacity > 1, "capacity must be larger than 1");
            _data = new List<T>(intialCapacity, allocator);
            Clear();
        }

        public T Top => _data[0];
        public int Count => _data.Length;

        public void Insert(T n)
        {
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

        public void Clear()
        {
            _data.Clear();
        }

        public void Dispose()
        {
            _data.Dispose();
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
                i = parent;
                parent = Parent(i);
            }

            _data[i] = n;
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