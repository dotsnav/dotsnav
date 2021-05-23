using DotsNav.Assertions;
using Unity.Mathematics;

namespace DotsNav
{
    /// <summary>
    /// A vertex in the triangulation. Updating the navmesh invalidates this structure.
    /// </summary>
    public unsafe struct Vertex
    {
        /// <summary>
        /// Returns the position of this vertex
        /// </summary>
        public float2 Point { get; internal set; }

        internal Edge* Edge;
        internal int SeqPos;
        internal int Mark;
        internal int PointConstraints;
        internal int ConstraintHandles;

        /// <summary>
        /// Allows for the enumeration of all edges that share this vertex as their origin:
        /// <para>for (var e = v.GetEdgeEnumerator(); e.Current != null; e.MoveNext())</para>
        /// </summary>
        public EdgeEnumerator GetEdgeEnumerator()
        {
            fixed (Vertex* p = &this)
                return new EdgeEnumerator(p);
        }

        internal void RemoveEdge(Edge* e) =>
            Edge = e->ONext == e ? null : e->ONext;

        public override string ToString() =>
            $"{Point.x:F}, {Point.y:F}";

        /// <summary>
        /// Allows for enumerating all edges that share this vertex as their origin. Updating the navmesh invalidates this structure.
        /// </summary>
        public struct EdgeEnumerator
        {
            /// <summary>
            /// Current edge being enumerated. Null when all edges have been enumerated.
            /// </summary>
            public Edge* Current { get; private set; }
            readonly Edge* _start;
            bool _started;

            internal EdgeEnumerator(Vertex* v)
            {
                Assert.IsTrue(v != null);
                _start = v->Edge;
                _started = false;
                Current = null;
            }

            public bool MoveNext()
            {
                if (!_started)
                {
                    Current = _start;
                    _started = true;
                    return true;
                }
                
                if (Current != null)
                {
                    Current = Current->ONext;
                    
                    if (Current != _start)
                        return true;

                    Current = null;
                }

                return false;
            }
        }
    }
}