using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Collections
{
    struct HashSetControl
    {
        public Allocator Allocator;
        public int Count;
        public int LastIndex;
        public int FreeList;
    }

    [DebuggerDisplay("Count = {Length}")]
    [DebuggerTypeProxy(typeof (NativeHashSetDebugView<>))]
    unsafe struct HashSet<T> where T : unmanaged, IEquatable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        readonly HashSetControl* _control;
        List<int> _buckets;
        List<Slot> _slots;

        struct Slot
        {
            internal int HashCode;
            internal int Next;
            internal T Value;
        }

        public HashSet(int capacity, Allocator allocator)
        {
            _control = (HashSetControl*) Mem.Malloc<HashSetControl>(allocator);
            var prime = HashHelpers.GetPrime(capacity);
            _buckets = new List<int>(prime, allocator);
            _slots = new List<Slot>(prime, allocator);
            _slots.Resize(prime, NativeArrayOptions.ClearMemory);
            _buckets.Resize(prime, NativeArrayOptions.ClearMemory);
            _control->LastIndex = 0;
            _control->Count = 0;
            _control->FreeList = -1;
            _control->Allocator = allocator;
            SetCapacity(prime);
        }

        public void Clear()
        {
            if (_control->LastIndex > 0)
            {
                // todo clear more efficiently
                for (var i = 0; i < _slots.Length; i++)
                    _slots[i] = default;
                for (var i = 0; i < _buckets.Length; i++)
                    _buckets[i] = default;

                _control->LastIndex = 0;
                _control->Count = 0;
                _control->FreeList = -1;
            }
        }

        public bool Contains(T item)
        {
            var num = 0;
            var hashCode = item.GetHashCode() & int.MaxValue;
            var slots = _slots;
            for (var index = _buckets[hashCode % _buckets.Length] - 1; index >= 0; index = slots[index].Next)
            {
                if (slots[index].HashCode == hashCode && slots[index].Value.Equals(item))
                    return true;
                if (num >= slots.Length)
                    throw new InvalidOperationException();
                ++num;
            }

            return false;
        }

        public bool Remove(T item)
        {
            var hashCode = item.GetHashCode() & int.MaxValue;
            var index1 = hashCode % _buckets.Length;
            var index2 = -1;
            var num = 0;
            var slots = _slots;
            for (var index3 = _buckets[index1] - 1; index3 >= 0; index3 = slots[index3].Next)
            {
                if (slots[index3].HashCode == hashCode && slots[index3].Value.Equals(item))
                {
                    if (index2 < 0)
                        _buckets[index1] = slots[index3].Next + 1;
                    else
                    {
                        var slot = slots[index2];
                        slot.Next = slots[index3].Next;
                        slots[index2] = slot;
                    }

                    var slot1 = slots[index3];
                    slot1.HashCode = -1;
                    slot1.Next = _control->FreeList;
                    slots[index3] = slot1;

                    --_control->Count;
                    if (_control->Count == 0)
                    {
                        _control->LastIndex = 0;
                        _control->FreeList = -1;
                    }
                    else
                        _control->FreeList = index3;

                    return true;
                }

                if (num >= slots.Length)
                    throw new InvalidOperationException();
                ++num;
                index2 = index3;
            }

            return false;
        }

        public int Length => _control->Count;
        public bool IsCreated => _slots.IsCreated;

        public bool TryAdd(T value)
        {
            var hashCode = value.GetHashCode() & int.MaxValue;
            var index1 = hashCode % _buckets.Length;
            var num = 0;
            var slots = _slots;
            for (var index2 = _buckets[index1] - 1; index2 >= 0; index2 = slots[index2].Next)
            {
                if (slots[index2].HashCode == hashCode && slots[index2].Value.Equals(value))
                    return false;
                if (num >= slots.Length)
                    throw new InvalidOperationException();
                ++num;
            }

            int index3;
            if (_control->FreeList >= 0)
            {
                index3 = _control->FreeList;
                _control->FreeList = slots[index3].Next;
            }
            else
            {
                if (_control->LastIndex == slots.Length)
                {
                    IncreaseCapacity();
                    slots = _slots;
                    index1 = hashCode % _buckets.Length;
                }

                index3 = _control->LastIndex;
                ++_control->LastIndex;
            }

            var slot = slots[index3];
            slot.HashCode = hashCode;
            slot.Value = value;
            slot.Next = _buckets[index1] - 1;
            slots[index3] = slot;
            _buckets[index1] = index3 + 1;
            ++_control->Count;
            return true;
        }

        void IncreaseCapacity()
        {
            var newSize = HashHelpers.ExpandPrime(_control->Count);
            if (newSize <= _control->Count)
                throw new ArgumentException();
            SetCapacity(newSize);
        }

        void SetCapacity(int newSize)
        {
            _slots.Resize(newSize, NativeArrayOptions.ClearMemory);
            _buckets.Clear();
            _buckets.Resize(newSize, NativeArrayOptions.ClearMemory);

            for (var index1 = 0; index1 < _control->LastIndex; ++index1)
            {
                var index2 = _slots[index1].HashCode % newSize;
                var slot = _slots[index1];
                slot.Next = _buckets[index2] - 1;
                _slots[index1] = slot;
                _buckets[index2] = index1 + 1;
            }
        }

        public NativeArray<T> ToNativeArray()
        {
            return ToNativeArray(new NativeArray<T>(Length, _control->Allocator));
        }

        public NativeArray<T> ToNativeArray(NativeArray<T> array)
        {
            var num = 0;
            for (var i = 0; i < _control->LastIndex && num < Length; ++i)
            {
                if (_slots[i].HashCode >= 0)
                {
                    array[num] = _slots[i].Value;
                    ++num;
                }
            }

            return array;
        }


        static class HashHelpers
        {
            public static readonly int[] primes = new int[72]
            {
                3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
            };

            public static bool IsPrime(int candidate)
            {
                if ((candidate & 1) == 0)
                    return candidate == 2;
                var num = (int) System.Math.Sqrt((double) candidate);
                for (var index = 3; index <= num; index += 2)
                {
                    if (candidate % index == 0)
                        return false;
                }

                return true;
            }

            public static int GetPrime(int min)
            {
                if (min < 0)
                    throw new ArgumentException();
                for (var index = 0; index < primes.Length; ++index)
                {
                    var prime = primes[index];
                    if (prime >= min)
                        return prime;
                }

                for (var candidate = min | 1; candidate < int.MaxValue; candidate += 2)
                {
                    if (IsPrime(candidate) && (candidate - 1) % 101 != 0)
                        return candidate;
                }

                return min;
            }

            public static int ExpandPrime(int oldSize)
            {
                var min = 2 * oldSize;
                return (uint) min > 2146435069U && 2146435069 > oldSize ? 2146435069 : GetPrime(min);
            }
        }

        public T[] ToArray()
        {
            var array = new T[_control->Count];
            int num = 0;
            for (int index = 0; index < _control->LastIndex && num < _control->Count; ++index)
            {
                if (_slots[index].HashCode >= 0)
                {
                    array[num] = _slots[index].Value;
                    ++num;
                }
            }

            return array;
        }

        public void Dispose()
        {
            _slots.Dispose();
            _buckets.Dispose();
            UnsafeUtility.Free(_control, _control->Allocator);
        }

        public Enumerator GetEnumerator() => new(this);

        public struct Enumerator
        {
            public T Current { get; private set; }

            readonly HashSet<T> _set;
            int _num;
            int _i;

            internal Enumerator(HashSet<T> set) : this()
            {
                _set = set;
            }

            public bool MoveNext()
            {
                while (_i < _set._control->LastIndex && _num < _set.Length)
                {
                    if (_set._slots[_i].HashCode >= 0)
                    {
                        Current = _set._slots[_i].Value;
                        ++_num;
                        ++_i;
                        return true;
                    }

                    ++_i;
                }

                return false;
            }
        }
    }

    class NativeHashSetDebugView<T> where T : unmanaged, IEquatable<T>
    {
        HashSet<T> _set;

        public NativeHashSetDebugView(HashSet<T> set)
        {
            _set = set;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _set.ToArray();
    }
}