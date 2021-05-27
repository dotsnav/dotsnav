using System.Diagnostics;
using DotsNav.Assertions;
using Unity.Collections;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(StackDebugView<>))]
    readonly struct Stack<T> where T : struct
    {
        readonly List<T> _data;
        public int Count => _data.Length;

        public Stack(int initialCapacity, Allocator allocator)
        {
            Assert.IsTrue(initialCapacity >= 0 , "invalid capacity");
            _data = new List<T>(initialCapacity, allocator);
        }

        public void Push(T element) => _data.Add(element);
        public T Pop() => _data.TakeLast();

        public T this[int i]
        {
            get
            {
                Assert.IsTrue(i >= 0 && i < Count , "index out of range");
                return _data[i];
            }
        }

        public void Clear() => _data.Clear();
        public void Dispose() => _data.Dispose();
    }

    sealed class StackDebugView<T> where T : struct
    {
        readonly Stack<T> _data;

        public StackDebugView(Stack<T> data)
        {
            _data = data;
        }

        public T[] Items
        {
            get
            {
                var result = new T[_data.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = _data[i];
                return result;
            }
        }
    }
}