using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Collections
{
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(ListHandleDebugView<>))]
    unsafe struct List<T> where T : unmanaged
    {
        readonly Allocator _allocator;
        [NativeDisableUnsafePtrRestriction]
        UnsafeList<T>* _list;

        public List(int initialCapacity, Allocator allocator)
        {
            _list = UnsafeList<T>.Create(initialCapacity, allocator);
            _allocator = allocator;
        }

        public List(Allocator allocator) : this(1, allocator)
        {
        }

        public int Length => _list->Length;
        public int Capacity => _list->Capacity;

        public bool IsCreated => _list != null;
        public void* Ptr => _list->Ptr;

        public void Clear()
        {
            _list->Clear();
        }

        public T this[int i]
        {
            get
            {
                Assert.IsTrue(i >= 0 && i < Length);
                return UnsafeUtility.ReadArrayElement<T>(Ptr, i);
            }
            set
            {
                Assert.IsTrue(i >= 0 && i < Length);
                UnsafeUtility.WriteArrayElement(_list->Ptr, i, value);
            }
        }

        public void Add(T t)
        {
            _list->Add(t);
        }

        public void RemoveAt(int i)
        {
            _list->RemoveAt(i);
        }

        public void Dispose()
        {
            _list->Dispose();
            UnsafeUtility.Free(_list, _allocator);
            _list = null;
        }

        public void Sort<T>() where T : unmanaged, IComparable<T>
        {
            NativeSortExtension.Sort((T*) _list->Ptr, Length, new NativeSortExtension.DefaultComparer<T>());
        }

        public void Resize(int newSize, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            _list->Resize(newSize, options);
        }

        public T TakeLast()
        {
            Assert.IsTrue(_list->Length > 0);
            return UnsafeUtility.ReadArrayElement<T>(Ptr, --_list->Length);
        }

        public void AddRange(NativeArray<T> r)
        {
            _list->AddRange(r.GetUnsafeReadOnlyPtr(), r.Length);
        }

        public NativeArray<T> AsArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(_list->Ptr, _list->Length, Allocator.None);
            return array;
        }

        public void RemoveAtSwapBack(int index) => _list->RemoveAtSwapBack(index);
    }

    sealed class ListHandleDebugView<T> where T : unmanaged
    {
        List<T> _list;

        public ListHandleDebugView(List<T> list)
        {
            _list = list;
        }

        public T[] Items
        {
            get
            {
                var a = new T[_list.Length];
                for (int i = 0; i < _list.Length; i++)
                    a[i] = _list[i];
                return a;
            }
        }
    }
}