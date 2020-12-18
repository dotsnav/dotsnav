using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav.Drawing
{
    unsafe struct UnsafeArray<T> : IDisposable where T : unmanaged
    {
        readonly T* _pointer;
        internal T* GetUnsafePtr() => _pointer;

        internal UnsafeArray(int length)
        {
            var size = UnsafeUtility.SizeOf<T>() * length;
            var alignment = UnsafeUtility.AlignOf<T>();
            _pointer = (T*)UnsafeUtility.Malloc(size, alignment, Allocator.Persistent);
            Length = length;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(_pointer, Allocator.Persistent);
        }

        internal int Length { get; }

        internal ref T this[int index] => ref UnsafeUtility.AsRef<T>(_pointer + index);

        internal NativeArray<T> ToNativeArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(_pointer, Length, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            return array;
        }
    }

    struct LineBuffer : IDisposable
    {
        internal UnsafeArray<float4> Instance;

        internal LineBuffer(int KMaxLines)
        {
            Instance = new UnsafeArray<float4>(KMaxLines * 2);
        }

        internal void SetLine(Line line, int index)
        {
            Instance[index * 2] = line.Begin;
            Instance[index * 2 + 1] = line.End;
        }

        public  void Dispose()
        {
            Instance.Dispose();
        }

        internal Unit AllocateAll()
        {
            return new Unit(Instance.Length);
        }

        internal unsafe void CopyFrom(void* ptr, int amount, int offset)
        {
            UnsafeUtility.MemCpy(Instance.GetUnsafePtr() + 2 * offset, ptr, amount * UnsafeUtility.SizeOf<Line>());
        }
    }
}
