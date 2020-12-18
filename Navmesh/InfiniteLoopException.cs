using System;

namespace DotsNav
{
    class InfiniteLoopException : Exception
    {
        public InfiniteLoopException(string msg = null) : base(msg)
        {
        }
    }
}