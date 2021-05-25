using DotsNav.Collections;
using DotsNav.Data;
using DotsNav.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav
{
    /// <summary>
    /// Provides access to edges and vertices in the triangulation. This component is created and destroyed automatically, see NavmeshData
    /// </summary>
    public unsafe partial struct Navmesh : ISystemStateComponentData
    {
        const int CrepMinCapacity = 4; // todo what's a good number, shrink when reusing?

        public float2 Max;
        public float2 Min => -Max;
        public float2 Size => 2 * Max;
        public int Vertices => _verticesSeq.Length;

        float _e;
        float _collinearMargin;

        UnsafeHashMap<Entity, IntPtr> _constraints;
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
        Stack<UnsafeList> _creps;
        internal HashSet<int> DestroyedTriangles;
        Deque<IntPtr> _refinementQueue;

        int _mark;
        int _edgeId;
        int _triangleId;

        int NextMark => ++_mark;
        int NextEdgeId => ++_edgeId;
        int NextTriangleId => ++_triangleId;
        internal bool IsEmpty => Vertices == 8;
        internal bool IsInitialized => Vertices > 0;

        internal void Allocate(NavmeshComponent component)
        {
            Assert.IsTrue(math.all(component.Size > 0));
            Assert.IsTrue(component.ExpectedVerts > 0);

            Max = component.Size / 2;
            _e = component.MergePointsDistance;
            _collinearMargin = component.CollinearMargin;

            _vertices = new BlockPool<Vertex>(component.ExpectedVerts, Allocator.Persistent);
            _verticesSeq = new UnsafeList<IntPtr>(component.ExpectedVerts, Allocator.Persistent);
            _quadEdges = new BlockPool<QuadEdge>(3 * component.ExpectedVerts, Allocator.Persistent);
            _constraints = new UnsafeHashMap<Entity, IntPtr>(component.ExpectedVerts, Allocator.Persistent);
            V = new HashSet<IntPtr>(16, Allocator.Persistent);
            C = new HashSet<IntPtr>(16, Allocator.Persistent);
            _edgeSearch = new EdgeSearch(100, 100, Allocator.Persistent);
            _qt = new QuadTree(math.max(component.Size.x, component.Size.y), 100, 10, Allocator.Persistent);
            _flipStack = new PtrStack<Edge>(32, Allocator.Persistent);
            _insertedPoints = new UnsafeList<Point>(64, Allocator.Persistent);
            _open = new PtrStack<Vertex>(64, Allocator.Persistent);
            _vlist = new UnsafeList<IntPtr>(64, Allocator.Persistent);
            _elist = new UnsafeList<IntPtr>(64, Allocator.Persistent);
            _creps = new Stack<UnsafeList>(2*component.ExpectedVerts, Allocator.Persistent);
            for (int i = 0; i < 2 * component.ExpectedVerts; i++)
                _creps.Push(new UnsafeList(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), CrepMinCapacity, Allocator.Persistent));
            DestroyedTriangles = new HashSet<int>(64, Allocator.Persistent);
            _refinementQueue = new Deque<IntPtr>(24, Allocator.Persistent);

            BuildBoundingBoxes();
        }

        void BuildBoundingBoxes()
        {
            var bmin = Min - 1;
            var bmax = Max + 1;

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

            var bounds = stackalloc float2[] {Min, new float2(Max.x, Min.y), Max, new float2(Min.x, Max.y), Min};
            Insert(bounds, 0, 5, Entity.Null);
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

        public bool Contains(float2 p) => Math.Contains(p, Min, Max);

        internal void Load(NativeList<float2> vertices, NativeList<int> amounts, NativeList<Entity> entities, DynamicBuffer<DestroyedTriangleElement> destroyed, BufferFromEntity<VertexElement> vertexInputLookup, BufferFromEntity<VertexAmountElement> vertexAmountLookup, NativeArray<Entity> bufferEntities, ComponentDataFromEntity<ObstacleBlobComponent> bulkBlobs, NativeArray<Entity> blobEntities)
        {
            DestroyedTriangles.Clear();

            if (vertices.Length == 0 && bufferEntities.Length == 0)
                return;

            {
                var start = 0;
                var ptr = (float2*) vertices.GetUnsafeReadOnlyPtr();
                for (int i = 0; i < amounts.Length; i++)
                {
                    var amount = amounts[i];
                    Insert(ptr, start, amount, entities[i]);
                    start += amount;
                }
            }

            for (int i = 0; i < bufferEntities.Length; i++)
            {
                var e = bufferEntities[i];
                var verts = vertexInputLookup[e];
                var amountsBuffer = vertexAmountLookup[e];

                var start = 0;
                var ptr = (float2*) verts.GetUnsafeReadOnlyPtr();
                for (int j = 0; j < amountsBuffer.Length; j++)
                {
                    var amount = amountsBuffer[j];
                    Insert(ptr, start, amount, Entity.Null);
                    start += amount;
                }
            }

            for (int i = 0; i < blobEntities.Length; i++)
            {
                var e = blobEntities[i];
                ref var blob = ref bulkBlobs[e].BlobRef.Value;
                var ptr = (float2*) blob.Vertices.GetUnsafePtr();
                var start = 0;
                for (int j = 0; j < blob.Amounts.Length; j++)
                {
                    var amount = blob.Amounts[j];
                    Insert(ptr, start, amount, Entity.Null);
                    start += amount;
                }
            }

            GlobalRefine(destroyed);
        }

        internal void Update(NativeList<float2> vertices, NativeList<int> amounts, NativeList<Entity> entities, NativeQueue<Entity> toRemove, DynamicBuffer<DestroyedTriangleElement> destroyed, BufferFromEntity<VertexElement> vertexInputLookup, BufferFromEntity<VertexAmountElement> vertexAmountLookup, NativeArray<Entity> bufferEntities, ComponentDataFromEntity<ObstacleBlobComponent> bulkBlobs, NativeArray<Entity> blobEntities)
        {
            DestroyedTriangles.Clear();

            if (vertices.Length == 0 && toRemove.Count == 0)
                return;

            V.Clear();

            if (toRemove.Count > 0)
            {
                while (toRemove.TryDequeue(out var r))
                    RemoveConstraint(r);

                RemoveRefinements();
            }

            C.Clear();

            {
                var start = 0;
                var ptr = (float2*) vertices.GetUnsafeReadOnlyPtr();
                for (int i = 0; i < amounts.Length; i++)
                {
                    var amount = amounts[i];
                    Insert(ptr, start, amount, entities[i]);
                    start += amount;
                    SearchDisturbances();
                }
            }

            for (int i = 0; i < bufferEntities.Length; i++)
            {
                var e = bufferEntities[i];
                var verts = vertexInputLookup[e];
                var amountsBuffer = vertexAmountLookup[e];

                var start = 0;
                var ptr = (float2*) verts.GetUnsafeReadOnlyPtr();
                for (int j = 0; j < amountsBuffer.Length; j++)
                {
                    var amount = amountsBuffer[j];
                    Insert(ptr, start, amount, Entity.Null);
                    start += amount;
                    SearchDisturbances();
                }
            }

            for (int i = 0; i < blobEntities.Length; i++)
            {
                var e = blobEntities[i];
                ref var blob = ref bulkBlobs[e].BlobRef.Value;
                var ptr = (float2*) blob.Vertices.GetUnsafePtr();
                var start = 0;
                for (int j = 0; j < blob.Amounts.Length; j++)
                {
                    var amount = blob.Amounts[j];
                    Insert(ptr, start, amount, Entity.Null);
                    start += amount;
                    SearchDisturbances();
                }
            }

            LocalRefinement(destroyed);
        }

        /// <summary>
        /// Allows enumeration of all edges in the navmesh
        /// </summary>
        /// <param name="sym">Set to true to enumerate symetric edges, i.e. enumerate edge(x,y) and edge(y,x)</param>
        public EdgeEnumerator GetEdgeEnumerator(bool sym = false) => new EdgeEnumerator(_verticesSeq, Max, sym);
    }
}