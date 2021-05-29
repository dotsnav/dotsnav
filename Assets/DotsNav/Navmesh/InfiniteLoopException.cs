using System;

namespace DotsNav.Navmesh
{
    class InfiniteLoopException : Exception
    {
        public InfiniteLoopException(string msg = null) : base(msg)
        {
        }
    }
}