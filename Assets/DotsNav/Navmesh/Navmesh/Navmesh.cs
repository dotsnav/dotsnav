using System;
using DotsNav.Collections;
using DotsNav.Navmesh.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Navmesh
{
    /// <summary>
    /// Provides access to edges and vertices in the triangulation
    /// </summary>
    public unsafe partial struct Navmesh
    {
        const int CrepMinCapacity = 4; // todo what's a good number, shrink when reusing?

        public float2 Extent;
        public int Vertices => _verticesSeq.Length;

        float _e;
        float _collinearMargin;

        UnsafeParallelHashMap<Entity, IntPtr> _constraints;
        BlockPool<Vertex> _vertices;
        BlockPool<QuadEdge> _quadEdges;
        UnsafeList<IntPtr> _verticesSeq;

        HashSet<IntPtr> V;
        HashSet<IntPtr> C;

        QuadTree _qt;
        EdgeSearch _edgeSearch;
        PtrStack<Edge> _flipStack;
        UnsafeList<Point> _insertedPoints;
        PtrStack<Vertex> _open;
        UnsafeList<IntPtr> _vlist;
        UnsafeList<IntPtr> _elist;
        Stack<UnsafeList<Entity>> _creps;
        internal HashSet<int> DestroyedTriangles;
        Deque<IntPtr> _refinementQueue;

        int _mark;
        int _edgeId;
        int _triangleId;

        int NextMark => ++_mark;
        int NextEdgeId => ++_edgeId;
        int NextTriangleId => ++_triangleId;
        internal bool IsEmpty => Vertices == 8;

        internal Navmesh(NavmeshComponent component)
        {
            Assert.IsTrue(math.all(component.Size > 0));
            Assert.IsTrue(component.ExpectedVerts > 0);

            Extent = component.Size / 2;
            _e = component.MergePointsDistance;
            _collinearMargin = component.CollinearMargin;

            const int blockSize = 128;
            var initialBlocks = (int) math.ceil((float) component.ExpectedVerts / blockSize);
            _vertices = new BlockPool<Vertex>(blockSize, initialBlocks, Allocator.Persistent);
            _verticesSeq = new UnsafeList<IntPtr>(component.ExpectedVerts, Allocator.Persistent);
            _quadEdges = new BlockPool<QuadEdge>(3 * blockSize, initialBlocks, Allocator.Persistent);
            _constraints = new UnsafeParallelHashMap<Entity, IntPtr>(component.ExpectedVerts, Allocator.Persistent);
            V = new HashSet<IntPtr>(16, Allocator.Persistent);
            C = new HashSet<IntPtr>(16, Allocator.Persistent);
            _edgeSearch = new EdgeSearch(100, 100, Allocator.Persistent);
            _qt = new QuadTree(math.max(component.Size.x, component.Size.y), 100, 10, Allocator.Persistent);
            _flipStack = new PtrStack<Edge>(32, Allocator.Persistent);
            _insertedPoints = new UnsafeList<Point>(64, Allocator.Persistent);
            _open = new PtrStack<Vertex>(64, Allocator.Persistent);
            _vlist = new UnsafeList<IntPtr>(64, Allocator.Persistent);
            _elist = new UnsafeList<IntPtr>(64, Allocator.Persistent);
            _creps = new Stack<UnsafeList<Entity>>(2*component.ExpectedVerts, Allocator.Persistent);
            for (int i = 0; i < 2 * component.ExpectedVerts; i++)
                _creps.Push(new UnsafeList<Entity>(CrepMinCapacity, Allocator.Persistent));
            DestroyedTriangles = new HashSet<int>(64, Allocator.Persistent);
            _refinementQueue = new Deque<IntPtr>(24, Allocator.Persistent);

            _mark = default;
            _edgeId = default;
            _triangleId = default;

            BuildBoundingBoxes();
        }

        void BuildBoundingBoxes()
        {
            var bmin = -Extent - 1;
            var bmax = Extent + 1;

            var bottomLeft = CreateVertex(bmin);
            var bottomRight = CreateVertex(new float2(bmax.x, bmin.y));
            var topRight = CreateVertex(bmax);
            var topleft = CreateVertex(new float2(bmin.x, bmax.y));

            var bottom = CreateEdge(bottomLeft, bottomRight);
            var right = CreateEdge(bottomRight, topRight);
            var top = CreateEdge(topRight, topleft);
            var left = CreateEdge(topleft, bottomLeft);

            bottom->AddConstraint(Entity.Null);
            right->AddConstraint(Entity.Null);
            top->AddConstraint(Entity.Null);
            left->AddConstraint(Entity.Null);

            Splice(bottom->Sym, right);
            Splice(right->Sym, top);
            Splice(top->Sym, left);
            Splice(left->Sym, bottom);

            Connect(right, bottom);

            var bounds = stackalloc float2[] {-Extent, new float2(Extent.x, -Extent.y), Extent, new float2(-Extent.x, Extent.y), -Extent};
            Insert(bounds, 0, 5, Entity.Null, float4x4.identity);
        }

        internal void Dispose()
        {
            var e = GetEdgeEnumerator();
            while (e.MoveNext())
                e.Current->QuadEdge->Crep.Dispose();

            for (int i = 0; i < _creps.Count; i++)
                _creps[i].Dispose();
            _creps.Dispose();

            _vertices.Dispose();
            _verticesSeq.Dispose();
            _constraints.Dispose();
            _quadEdges.Dispose();
            V.Dispose();
            C.Dispose();
            _qt.Dispose();
            _edgeSearch.Dispose();
            _flipStack.Dispose();
            _insertedPoints.Dispose();
            _open.Dispose();
            _vlist.Dispose();
            _elist.Dispose();
            DestroyedTriangles.Dispose();
            _refinementQueue.Dispose();
        }

        public bool Contains(float2 p) => Math.Contains(p, -Extent, Extent);

        internal void Load<T>(T enumerator, float4x4 ltwInv) where T : System.Collections.Generic.IEnumerator<Insertion>
        {
            DestroyedTriangles.Clear();

            while (enumerator.MoveNext())
            {
                var op = enumerator.Current;
                var ltw = math.mul(ltwInv, op.Ltw);

                switch (op.Type)
                {
                    case InsertionType.Insert:
                        Insert(op.Vertices, 0, op.Amount, op.Obstacle, ltw);
                        break;
                    case InsertionType.BulkInsert:
                        var start = 0;
                        for (int i = 0; i < op.Amount; i++)
                        {
                            var amount = op.Amounts[i];
                            Insert(op.Vertices, start, amount, Entity.Null, ltw);
                            start += amount;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            GlobalRefine();
        }

        internal void Update<T>(T enumerator, NativeMultiHashMap<Entity, Entity>.Enumerator removals, float4x4 ltwInv) where T : System.Collections.Generic.IEnumerator<Insertion>
        {
            DestroyedTriangles.Clear();

            V.Clear();

            var removed = false;
            while (removals.MoveNext())
            {
                RemoveConstraint(removals.Current);
                removed = true;
            }
            if (removed)
                RemoveRefinements();

            while (enumerator.MoveNext())
            {
                var op = enumerator.Current;
                var ltw = math.mul(ltwInv, op.Ltw);

                switch (op.Type)
                {
                    case InsertionType.Insert:
                        C.Clear();
                        Insert(op.Vertices, 0, op.Amount, op.Obstacle, ltw);
                        SearchDisturbances();
                        break;
                    case InsertionType.BulkInsert:
                        var start = 0;
                        for (int i = 0; i < op.Amount; i++)
                        {
                            var amount = op.Amounts[i];
                            C.Clear();
                            Insert(op.Vertices, start, amount, Entity.Null, ltw);
                            start += amount;
                            SearchDisturbances();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            LocalRefinement();
        }

        /// <summary>
        /// Allows enumeration of all edges in the navmesh
        /// </summary>
        /// <param name="sym">Set to true to enumerate symetric edges, i.e. enumerate edge(x,y) and edge(y,x)</param>
        public EdgeEnumerator GetEdgeEnumerator(bool sym = false) => new(_verticesSeq, Extent, sym);
    }
}