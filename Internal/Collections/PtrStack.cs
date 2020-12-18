using System.Diagnostics;
using DotsNav.Assertions;
using Unity.Collections;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(PtrStackDebugView<>))]
    readonly unsafe struct PtrStack<T> where T : unmanaged
    {
        readonly List<IntPtr> _data;
        public int Count => _data.Length;

        public PtrStack(int initialCapacity, Allocator allocator)
        {
            Assert.IsTrue(initialCapacity >= 0 , "invalid capacity");
            _data = new List<IntPtr>(initialCapacity, allocator);
        }

        public void Push(T* element) => _data.Add((IntPtr) element);
        public T* Pop() => (T*) _data.TakeLast();

        public T* this[int i]
        {
            get
            {
                Assert.IsTrue(i >= 0 && i < Count , "index out of range");
                return (T*) _data[i];
            }
        }

        public void Clear() => _data.Clear();
        public void Dispose() => _data.Dispose();
    }

    sealed class PtrStackDebugView<T> where T : unmanaged
    {
        readonly PtrStack<T> _data;

        public PtrStackDebugView(PtrStack<T> data)
        {
            _data = data;
        }

        public unsafe IntPtr[] Items
        {
            get
            {
                var result = new IntPtr[_data.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = (IntPtr) _data[i];
                return result;
            }
        }
    }
}