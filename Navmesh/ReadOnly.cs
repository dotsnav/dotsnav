using System.Collections;
using System.Collections.Generic;
using DotsNav.Assertions;

namespace DotsNav
{
    public unsafe struct ReadOnly<T> : IEnumerable<T> where T : unmanaged
    {
        readonly T* _ptr;
        public readonly int Length;

        public ReadOnly(T* ptr, int length)
        {
            _ptr = ptr;
            Length = length;
        }

        public T this[int index]
        {
            get
            {
                Assert.IsTrue(index >= 0 && index < Length);
                return _ptr[index];
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(_ptr, Length);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_ptr, Length);

        struct Enumerator : IEnumerator<T>
        {
            readonly T* _ptr;
            readonly int _length;
            int _current;

            public Enumerator(T* ptr, int length)
            {
                _ptr = ptr;
                _length = length;
                _current = -1;
            }

            public bool MoveNext() => ++_current < _length;

            public void Reset() => _current = -1;

            public T Current => _current < 0 || _current >= _length ? default : _ptr[_current];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}