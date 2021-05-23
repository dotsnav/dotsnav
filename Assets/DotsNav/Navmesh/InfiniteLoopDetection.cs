using System.Diagnostics;
using Unity.Burst;

namespace DotsNav
{
    /// <summary>
    /// Throws InfiniteLoopException after specied amount of calls to Register since the last call to Reset.
    /// Set INFINITE_LOOP_DETECTION to enable.
    /// </summary>
    static class InfiniteLoopDetection
    {
        static int _i;

        // todo dont use strings so we can remove [BurstDiscard]
    
        [Conditional("INFINITE_LOOP_DETECTION")]
        [BurstDiscard]
        public static void Reset()
        {
            _i = 0;
        }

        [Conditional("INFINITE_LOOP_DETECTION")]
        [BurstDiscard]
        public static void Register(int max, string message)
        {
            if (_i++ >= max)
                throw new InfiniteLoopException(message);
        }
    }
}