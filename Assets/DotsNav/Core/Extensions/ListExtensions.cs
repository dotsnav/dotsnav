using System;
using DotsNav.Collections;

namespace DotsNav
{
    static class NativeListExtensions
    {
        public static void Reverse<T>(this List<T> l) where T : unmanaged
        {
            var i0 = 0;
            var i1 = l.Length - 1;
            while (i0 < i1)
            {
                var e = l[i0];
                l[i0] = l[i1];
                l[i1] = e;
                ++i0;
                --i1;
            }
        }

        public static bool Contains<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < list.Length; i++)
                if (list[i].Equals(value))
                    return true;
            return false;
        }

        public static T Last<T>(this List<T> l) where T : unmanaged => l[^1];

        public static void Insert<T>(this List<T> list, int index, T t) where T : unmanaged
        {
            Assert.IsTrue(index <= list.Length);
            if (index == list.Length)
                list.Add(t);
            else
            {
                list.Add(list.Last());
                for (int i = list.Length - 2; i > index; i--)
                    list[i] = list[i - 1];
                list[index] = t;
            }
        }
    }
}