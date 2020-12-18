using DotsNav.Assertions;
using Unity.Entities;

namespace DotsNav
{
    /// <summary>
    /// A directed edge in the triangulation. Traversal operators assume counter clockwise rotation,
    /// e.g. ONext will return the first edge in counter clockwise order around the origin, and OPrev will return the first edge in clockwise order.
    /// Updating the navmesh invalidates this structure.
    /// </summary>
    public unsafe struct Edge
    {
        internal readonly QuadEdge* QuadEdge;
        internal Edge* Next;
        readonly int _num;
        float _clearanceLeft;
        float _clearanceRight;

        internal Edge(QuadEdge* quadEdge, int num) : this()
        {
            QuadEdge = quadEdge;
            _num = num;
            _clearanceLeft = -1;
            _clearanceRight = -1;
        }

        /// <summary>
        /// Returns the origin vertex.
        /// </summary>
        public Vertex* Org { get; internal set; }

        /// <summary>
        /// Returns the destination vertex.
        /// </summary>
        public Vertex* Dest => Sym->Org;

        /// <summary>
        /// Returns a value unique to this edge's quadedge, i.e. this value will be the same for edge(x,y) and edge(y,x).
        /// This value is not stable and can change when updating the navmesh.
        /// </summary>
        public int QuadEdgeId => QuadEdge->Id;

        /// <summary>
        /// Returns a stricly increasing value unique to this edge's left face, i.e. this value will be the same for this edge,
        /// LNext and LPrev, with the last triangle created having a higher value than any previously created triangle.
        /// </summary>
        public int TriangleId { get; internal set; }

        /// <summary>
        /// Returns the amount of clearance when traversing this edge while moving left.
        /// </summary>
        public float ClearanceLeft
        {
            get
            {
                if (_clearanceLeft == -1)
                    _clearanceLeft = Navmesh.GetLocalClearance(OPrev->Dest->Point, Org->Point, Dest->Point, DNext);
                return _clearanceLeft;
            }
            internal set => _clearanceLeft = value;
        }

        /// <summary>
        /// Returns the amount of clearance when traversing this edge while moving right.
        /// </summary>
        public float ClearanceRight
        {
            get
            {
                if (_clearanceRight == -1)
                    _clearanceRight = Navmesh.GetLocalClearance(ONext->Dest->Point, Org->Point, Dest->Point, DPrev->Sym);
                return _clearanceRight;
            }
            internal set => _clearanceRight = value;
        }

        /// <summary>
        /// True for one of a quadedge's two directed edges, i.e. true for either edge(x,y) or edge(y,x).
        /// This value is not stable and can change when updating the navmesh.
        /// </summary>
        public bool IsPrimary => _num == 0;

        internal bool RefineFailed
        {
            get => QuadEdge->RefineFailed;
            set => QuadEdge->RefineFailed = value;
        }

        internal int Mark
        {
            get => QuadEdge->Mark;
            set => QuadEdge->Mark = value;
        }

        public ReadOnly<Entity> Constraints => new ReadOnly<Entity>((Entity*) QuadEdge->Crep.Ptr, QuadEdge->Crep.Length);
        public bool Constrained => QuadEdge->Crep.Length > 0;
        public bool IsConstrainedBy(Entity id) => QuadEdge->Crep.Contains(id);
        public bool ConstraintsEqual(Edge* edge) => QuadEdge->Crep.SequenceEqual<Entity>(edge->QuadEdge->Crep);

        internal void AddConstraint(Entity id) => QuadEdge->Crep.InsertSorted(id);
        internal void RemoveConstraint(Entity id) => QuadEdge->Crep.Remove(id);

        /// <summary>
        /// Returns the symmetric edge.
        /// </summary>
        public Edge* Sym => GetEdge((_num + 2) & 3);

        /// <summary>
        /// Returns the dual-edge pointing from right to left.
        /// </summary>
        internal Edge* Rot => GetEdge((_num + 1) & 3);

        /// <summary>
        /// Returns the dual-edge pointing from left to right.
        /// </summary>
        Edge* InvRot => GetEdge((_num + 3) & 3);

        Edge* GetEdge(int i) => (Edge*) ((byte*) QuadEdge + i * sizeof(Edge));

        /// <summary>
        /// Returns the next edge about the origin with the same origin.
        /// </summary>
        public Edge* ONext => Next;

        /// <summary>
        /// Returns the previous edge about the origin with the same origin.
        /// </summary>
        public Edge* OPrev => Rot->ONext->Rot;

        /// <summary>
        /// Returns the next edge about the Right face with the same right face.
        /// </summary>
        public Edge* RNext => Rot->ONext->InvRot;

        /// <summary>
        /// Returns the previous edge about the Right face with the same right face.
        /// </summary>
        public Edge* RPrev => Sym->ONext;

        /// <summary>
        /// Returns the next edge about the destination with the same desination.
        /// </summary>
        public Edge* DNext => Sym->ONext->Sym;

        /// <summary>
        /// Returns the previous edge about the destination with the same destination.
        /// </summary>
        public Edge* DPrev => InvRot->ONext->InvRot;

        /// <summary>
        /// Returns the next edge about the left face with the same left face.
        /// </summary>
        public Edge* LNext => InvRot->ONext->Rot;

        /// <summary>
        /// Returns the previous edge about the left face with the same left face.
        /// </summary>
        public Edge* LPrev => ONext->Sym;

        public override string ToString()
            => $"Edge: {Org->ToString()} => {Dest->ToString()}";
    }
}