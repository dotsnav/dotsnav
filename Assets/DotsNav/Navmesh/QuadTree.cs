using DotsNav.Collections;
using DotsNav.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav
{
    readonly unsafe struct QuadTree
    {
        readonly int _bucketSize;
        readonly Allocator _allocator;
        [NativeDisableUnsafePtrRestriction]
        readonly QuadTreeNode* _root;
        readonly BlockPool<QuadTreeNode> _nodes;
        readonly Stack<IntPtr> _chunks;

        public QuadTree(float size, int initialCapacity, int bucketSize, Allocator allocator) : this()
        {
            _bucketSize = bucketSize;
            _allocator = allocator;
            var buckets = (int) math.ceil((float) initialCapacity / bucketSize);
            _chunks = new Stack<IntPtr>(buckets, allocator);
            for (int i = 0; i < buckets; i++) 
                _chunks.Push((IntPtr) Malloc());
            _nodes = new BlockPool<QuadTreeNode>((int) (1.3334f * buckets), allocator);
            _root = _nodes.Set(new QuadTreeNode(0, size / 2, GetChunk(), null));
        }

        Vertex** GetChunk()
        {
            if (_chunks.Count > 0)
                return (Vertex**) _chunks.Pop();
            return Malloc();
        }

        Vertex** Malloc() => (Vertex**) Util.Malloc<IntPtr>(_bucketSize, _allocator);

        public Vertex* FindClosest(float2 p, float rangeSq = float.MaxValue)
        {
            var node = _root;
            while (!node->IsLeaf)
                node = node->GetChild(p);

            Vertex* closest = null;
            var dist = rangeSq;

            for (int i = 0; i < node->Count; i++)
            {
                var v = node->Data[i];
                var d = math.lengthsq(v->Point - p);
                if (d < dist)
                {
                    closest = v;
                    dist = d;
                }
            }

            if (node->Parent != null)
            {
                var dd = math.sqrt(dist) / 2;
                var min = p - dd;
                var max = p + dd;

                if (math.any(min < node->Origin - node->HalfSize) || math.any(max >= node->Origin + node->HalfSize))
                {
                    do
                        node = node->Parent;
                    while
                        (node->Parent != null && (math.any(min < node->Origin - node->HalfSize) || math.any(max >= node->Origin + node->HalfSize)));

                    FindClosest(node, p, min, max, ref dist, ref closest);
                }
            }

            return closest;
        }

        static void FindClosest(QuadTreeNode* n, float2 p, float2 min, float2 max, ref float dist, ref Vertex* closest)
        {
            if (n->IsLeaf)
            {
                for (int i = 0; i < n->Count; i++)
                {
                    var v = n->Data[i];
                    var d = math.lengthsq(v->Point - p);
                    if (d < dist)
                    {
                        closest = v;
                        dist = d;
                    }
                }

                return;
            }

            if (Math.RectsOverlap(min, max, n->Origin - n->HalfSize, n->Origin))
                FindClosest(n->BL, p, min, max, ref dist, ref closest);

            if (Math.RectsOverlap(min, max, n->Origin - new float2(n->HalfSize, 0), n->Origin + new float2(0, n->HalfSize)))
                FindClosest(n->TL, p, min, max, ref dist, ref closest);

            if (Math.RectsOverlap(min, max, n->Origin - new float2(0, n->HalfSize), n->Origin + new float2(n->HalfSize, 0)))
                FindClosest(n->BR, p, min, max, ref dist, ref closest);

            if (Math.RectsOverlap(min, max, n->Origin, n->Origin + n->HalfSize))
                FindClosest(n->TR, p, min, max, ref dist, ref closest);
        }

        public void Insert(Vertex* v)
        {
            var p = v->Point;
            var node = _root;
            while (!node->IsLeaf)
                node = node->GetChild(p);

            while (node->Count == _bucketSize)
            {
                var hs = node->HalfSize / 2;
                var o = node->Origin;
                node->BL = _nodes.Set(new QuadTreeNode(o - hs, hs, node->Data, node));
                node->TL = _nodes.Set(new QuadTreeNode(o + new float2(-hs, hs), hs, GetChunk(), node));
                node->BR = _nodes.Set(new QuadTreeNode(o + new float2(hs, -hs), hs, GetChunk(), node));
                node->TR = _nodes.Set(new QuadTreeNode(o + hs, hs, GetChunk(), node));
                node->Count = 0;

                for (int i = 0; i < _bucketSize; i++)
                {
                    var pt = node->Data[i]->Point;
                    if (pt.x < o.x)
                    {
                        if (pt.y < o.y)
                            node->BL->Data[node->BL->Count++] = node->BL->Data[i];
                        else
                            node->TL->Data[node->TL->Count++] = node->BL->Data[i];
                    }
                    else
                    {
                        if (pt.y < o.y)
                            node->BR->Data[node->BR->Count++] = node->BL->Data[i];
                        else
                            node->TR->Data[node->TR->Count++] = node->BL->Data[i];
                    }
                }

                node = node->GetChild(p);
            }

            node->Data[node->Count++] = v;
        }

        public void Remove(Vertex* v)
        {
            var p = v->Point;
            var node = _root;
            while (!node->IsLeaf)
                node = node->GetChild(p);

            for (int i = 0; i < node->Count; i++)
            {
                if (node->Data[i] == v)
                {
                    node->Data[i] = node->Data[--node->Count];
                    break;
                }

                Assert.IsTrue(i != node->Count - 1);
            }

            var parent = node->Parent;

            while (node->Count == 0 &&
                   parent != null &&
                   parent->BL->IsLeaf && parent->BL->Count == 0 &&
                   parent->TL->IsLeaf && parent->TL->Count == 0 &&
                   parent->BR->IsLeaf && parent->BR->Count == 0 &&
                   parent->TR->IsLeaf && parent->TR->Count == 0)
            {
                parent->Data = parent->BL->Data;
                _nodes.Recycle(parent->BL);
                parent->BL = null;
                _chunks.Push((IntPtr) parent->TL->Data);
                _nodes.Recycle(parent->TL);
                _chunks.Push((IntPtr) parent->BR->Data);
                _nodes.Recycle(parent->BR);
                _chunks.Push((IntPtr) parent->TR->Data);
                _nodes.Recycle(parent->TR);
                node = parent;
                parent = node->Parent;
            }
        }

        public void Dispose()
        {
            Dispose(_root);
            _nodes.Dispose();
            _chunks.Dispose();
        }

        void Dispose(QuadTreeNode* n)
        {
            if (n->IsLeaf)
            {
                UnsafeUtility.Free(n->Data, _allocator);
                return;
            }

            Dispose(n->BL);
            Dispose(n->TL);
            Dispose(n->BR);
            Dispose(n->TR);
        }

        public void Clear()
        {
            Clear(_root);
            _root->Count = 0;
            _root->BL = null;
            _root->Data = GetChunk();
        }

        void Clear(QuadTreeNode* n)
        {
            if (n->IsLeaf)
            {
                _chunks.Push((IntPtr) n->Data);
                return;
            }

            Clear(n->BL);
            Clear(n->TL);
            Clear(n->BR);
            Clear(n->TR);
            _nodes.Recycle(n->BL);
            _nodes.Recycle(n->TL);
            _nodes.Recycle(n->BR);
            _nodes.Recycle(n->TR);
        }
    }

    unsafe struct QuadTreeNode
    {
        public readonly float2 Origin;
        public readonly float HalfSize;
        public readonly QuadTreeNode* Parent;

        public QuadTreeNode* BL;
        public QuadTreeNode* BR;
        public QuadTreeNode* TL;
        public QuadTreeNode* TR;

        public Vertex** Data;
        public int Count;

        public QuadTreeNode(float2 origin, float halfSize, Vertex** data, QuadTreeNode* parent) : this()
        {
            Origin = origin;
            HalfSize = halfSize;
            Data = data;
            Parent = parent;
        }

        public bool IsLeaf => BL == null;

        public QuadTreeNode* GetChild(float2 p)
        {
            if (p.x < Origin.x)
                return p.y < Origin.y ? BL : TL;
            return p.y < Origin.y ? BR : TR;
        }
    }
}