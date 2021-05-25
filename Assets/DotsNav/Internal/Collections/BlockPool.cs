using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Collections
{
    unsafe struct BlockPool<T> where T : unmanaged
    {
        readonly int _blockSize;
        readonly Allocator _allocator;

        List<IntPtr> _blocks;
        readonly PtrStack<T> _available;

        // todo add initial capacity argument
        public BlockPool(int blockSize, Allocator allocator)
        {
            _blockSize = blockSize;
            _allocator = allocator;
            _blocks = new List<IntPtr>(allocator);
            _available = new PtrStack<T>(blockSize, allocator);
            AddBlock();
        }

        public T* Set(T value)
        {
            if (_available.Count == 0)
                AddBlock();
            var ptr = _available.Pop();
            *ptr = value;
            return ptr;
        }

        void AddBlock()
        {
            var ptr = (T*) Util.Malloc<T>(_blockSize, _allocator);
            for (int i = 0; i < _blockSize; i++)
                _available.Push(ptr + i);
            _blocks.Add((IntPtr) ptr);
        }

        public void Recycle(T* t)
        {
            _available.Push(t);
        }

        int Capacity => _blocks.Length * _blockSize;

        public void Dispose()
        {
            for (int i = 0; i < _blocks.Length; i++)
                UnsafeUtility.Free((void*) _blocks[i], _allocator);
            _blocks.Dispose();
            _available.Dispose();
        }

        public int Count => Capacity - _available.Count;

        public void Clear()
        {
            _available.Clear();
            for (int i = 0; i < _blocks.Length; i++)
            {
                var block = (T*) _blocks[i];
                for (int j = 0; j < _blockSize; j++)
                    _available.Push(block + j);
            }
        }
    }
}
