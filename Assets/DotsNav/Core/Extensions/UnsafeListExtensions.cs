using System;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav
{
    static class UnsafeListExtensions
    {
        /// <summary>
        /// Checks two sorted lists for equality
        /// </summary>
        public static bool SequenceEqual<T>(this UnsafeList<T> l, UnsafeList<T> other) where T : unmanaged, IEquatable<T>
        {
            if (l.Length != other.Length)
                return false;
            for (int i = 0; i < l.Length; i++)
                if (!l[i].Equals(other[i]))
                    return false;
            return true;
        }

        public static bool Remove<T>(this ref UnsafeList<T> l, T t) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < l.Length; i++)
            {
                if (l[i].Equals(t))
                {
                    l.RemoveAtSwapBack(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Inserting in sorted list results in a sorted list with with item added. If the same value is already present in the list no change is made
        /// </summary>
        public static void InsertSorted<T>(this ref UnsafeList<T> list, T item) where T : unmanaged, IComparable<T>
        {
            if (list.Length == 0 || item.CompareTo(list[list.Length - 1]) > 0)
            {
                list.Add(item);
                return;
            }

            for (int i = list.Length - 1; i >= 0; i--)
            {
                var r = item.CompareTo(list[i]);

                if (r == 0)
                    return;

                if (r < 0)
                {
                    list.Add(list[list.Length - 1]);
                    for (int j = list.Length - 2; j > i; j--)
                        list[j] = list[j - 1];
                    list[i] = item;
                    return;
                }
            }
        }

        public static bool Contains<T>(this UnsafeList<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < list.Length; i++)
                if (list[i].Equals(value))
                    return true;
            return false;
        }
    }
}