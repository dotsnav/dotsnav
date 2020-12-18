using System;
using DotsNav.Assertions;
using Unity.Collections.LowLevel.Unsafe;

static class UnsafeListExtensions
{
    public static unsafe T Read<T>(this UnsafeList l, int index)
    {
        Assert.IsTrue(index >= 0 && index < l.Length);
        return UnsafeUtility.ReadArrayElement<T>(l.Ptr, index);
    }

    public static unsafe void Write<T>(this UnsafeList l, int index, T value)
    {
        Assert.IsTrue(index >= 0 && index < l.Length);
        UnsafeUtility.WriteArrayElement(l.Ptr, index, value);
    }

    /// <summary>
    /// Checks two sorted lists for equality
    /// </summary>
    public static bool SequenceEqual<T>(this UnsafeList l, UnsafeList other) where T : unmanaged, IEquatable<T>
    {
        if (l.Length != other.Length)
            return false;
        for (int i = 0; i < l.Length; i++)
            if (!l.Read<T>(i).Equals(other.Read<T>(i)))
                return false;
        return true;
    }

    public static bool Remove<T>(this ref UnsafeList l, T t) where T : unmanaged, IEquatable<T>
    {
        for (int i = 0; i < l.Length; i++)
        {
            if (l.Read<T>(i).Equals(t))
            {
                l.RemoveAtSwapBack<T>(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Inserting in sorted list results in a sorted list with with item added. If the same value is already present in the list no change is made
    /// </summary>
    public static void InsertSorted<T>(this ref UnsafeList list, T item) where T : unmanaged, IComparable<T>
    {
        if (list.Length == 0 || item.CompareTo(list.Read<T>(list.Length - 1)) > 0)
        {
            list.Add(item);
            return;
        }

        for (int i = list.Length - 1; i >= 0; i--)
        {
            var r = item.CompareTo(list.Read<T>(i));

            if (r == 0)
                return;

            if (r < 0)
            {
                list.Add(list.Read<T>(list.Length - 1));
                for (int j = list.Length - 2; j > i; j--)
                    list.Write(j, list.Read<T>(j - 1));
                list.Write(i, item);
                return;
            }
        }
    }
}
