using System.Diagnostics;
using DotsNav.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    [DebuggerTypeProxy(typeof(DequeDebugView<>))]
    readonly unsafe struct Deque<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        readonly DequeControl* _control;

        T* Data => (T*) _control->Data;
        int Capacity => _control->Capacity;
        Allocator Allocator => _control->Allocator;

        public int Count => _control->Count;
        public bool IsCreated => _control != null;

        public T Back
        {
            get
            {
                Assert.IsTrue(_control->Count > 0 , "deque empty");
                return Data[BackIndex];
            }
        }

        public T Front
        {
            get
            {
                Assert.IsTrue(_control->Count > 0 , "deque empty");
                return Data[FrontIndex];
            }
        }

        int BackIndex => NormalizeIndex(_control->Front + _control->Count);
        int FrontIndex => NormalizeIndex(_control->Front + 1);

        // todo versions dealing with under and overflows separately
        int NormalizeIndex(int i) => (i + Capacity) % Capacity;

        public Deque(int capacity, Allocator allocator)
        {
            capacity = math.ceilpow2(math.max(2, capacity));
            _control = (DequeControl*) Mem.Malloc<DequeControl>(allocator);
            *_control = new DequeControl(capacity, allocator, Mem.Malloc<T>(capacity, allocator));
        }

        public void PushBack(T e)
        {
            EnsureCapacity();
            ++_control->Count;
            Data[BackIndex] = e;
        }

        public void PushFront(T e)
        {
            EnsureCapacity();
            _control->Front = NormalizeIndex(_control->Front - 1);
            ++_control->Count;
            Data[FrontIndex] = e;
        }

        public T PopBack()
        {
            Assert.IsTrue(_control->Count > 0 , "deque empty");
            var e = Data[BackIndex];
            --_control->Count;
            return e;
        }

        public T PopFront()
        {
            Assert.IsTrue(_control->Count > 0 , "deque empty");
            var e = Data[FrontIndex];
            _control->Front = NormalizeIndex(_control->Front + 1);
            --_control->Count;
            return e;
        }

        public T FromBack(int index)
        {
            Assert.IsTrue(index < _control->Count, $"index {index} out of range of {_control->Count} elements");
            return Data[NormalizeIndex(_control->Front + _control->Count - index)];
        }

        public T FromFront(int index)
        {
            Assert.IsTrue(index < _control->Count, $"index {index} out of range of {_control->Count} elements");
            return Data[NormalizeIndex(_control->Front + 1 + index)];
        }

        void EnsureCapacity()
        {
            if (_control->Count < _control->Capacity)
                return;
            var newCapacity = _control->Capacity * 2;
            var t = Mem.Malloc<T>(newCapacity, _control->Allocator);
            var front = FrontIndex;
            var size = UnsafeUtility.SizeOf<T>();

            if (front == 0)
                UnsafeUtility.MemCpy(t, _control->Data, Count * size);
            else
            {
                var frontToEnd = _control->Capacity - front;
                UnsafeUtility.MemCpy(t, &((T*) _control->Data)[front], frontToEnd * size);
                UnsafeUtility.MemCpy(&((T*) t)[frontToEnd], _control->Data, front * size);
            }

            _control->Capacity = newCapacity;
            _control->Front = Capacity - 1;
            UnsafeUtility.Free(_control->Data, _control->Allocator);
            _control->Data = t;
        }

        public void Clear()
        {
            _control->Clear();
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Data, Allocator);
            UnsafeUtility.Free(_control, Allocator);
        }
    }

    unsafe struct DequeControl
    {
        public void* Data;
        public int Front;
        public int Count;
        public int Capacity;
        public readonly Allocator Allocator;

        public DequeControl(int intialCapacity, Allocator allocator, void* data)
        {
            Data = data;
            Capacity = intialCapacity;
            Front = Capacity - 1;
            Count = 0;
            Allocator = allocator;
        }

        public void Clear()
        {
            Front = Capacity - 1;
            Count = 0;
        }
    }

    sealed class DequeDebugView<T> where T : unmanaged
    {
        readonly Deque<T> _data;

        public DequeDebugView(Deque<T> data)
        {
            _data = data;
        }

        public T[] Items
        {
            get
            {
                var result = new T[_data.Count];
                for (var i = 0; i < result.Length; ++i)
                    result[i] = _data.FromFront(i);
                return result;
            }
        }
    }
}