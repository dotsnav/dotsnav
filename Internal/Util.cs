using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav
{
    static unsafe class Util
    {
        public static void* Malloc<T>(Allocator allocator) where T : struct
            => Malloc<T>(1, allocator);

        public static void* Malloc<T>(int amount, Allocator allocator) where T : struct
            => UnsafeUtility.Malloc(amount * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
    }
}