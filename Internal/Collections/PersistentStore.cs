using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Collections
{
    unsafe struct PersistentStore<T> where T : unmanaged
    {
        readonly int _chunkSize;
        readonly Allocator _allocator;

        List<IntPtr> _chunks;
        readonly PtrStack<T> _available;

        // todo add initial capacity argument
        public PersistentStore(int chunkSize, Allocator allocator)
        {
            _chunkSize = chunkSize;
            _allocator = allocator;
            _chunks = new List<IntPtr>(allocator);
            _available = new PtrStack<T>(chunkSize, allocator);
            AddChunk();
        }

        public T* Set(T value)
        {
            if (_available.Count == 0)
                AddChunk();
            var ptr = _available.Pop();
            *ptr = value;
            return ptr;
        }

        void AddChunk()
        {
            var ptr = (T*) Util.Malloc<T>(_chunkSize, _allocator);
            for (int i = 0; i < _chunkSize; i++)
                _available.Push(ptr + i);
            _chunks.Add((IntPtr) ptr);
        }

        public void Recycle(T* t)
        {
            _available.Push(t);
        }

        int Capacity => _chunks.Length * _chunkSize;

        public void Dispose()
        {
            for (int i = 0; i < _chunks.Length; i++)
                UnsafeUtility.Free((void*) _chunks[i], _allocator);
            _chunks.Dispose();
            _available.Dispose();
        }

        public int Count => Capacity - _available.Count;

        public void Clear()
        {
            _available.Clear();
            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = (T*) _chunks[i];
                for (int j = 0; j < _chunkSize; j++)
                    _available.Push(chunk + j);
            }
        }
    }
}
