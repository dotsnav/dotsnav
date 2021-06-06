// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Diagnostics;
using DotsNav.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav.BVH
{
    unsafe struct Tree<T> where T : unmanaged
    {
        const int NullNode = -1;

        int _root;
        [NativeDisableUnsafePtrRestriction]
        Node* _nodes;
        int _nodeCount;
        int _nodeCapacity;

        int _freeList;

        internal readonly Allocator Allocator;

        public bool IsCreated => _nodes != null;
        public int Count { get; private set; }

        public Tree(Allocator allocator)
        {
            Allocator = allocator;
            _root = NullNode;

            _nodeCapacity = 16;
            _nodeCount = 0;
            _nodes = (Node*) Mem.Malloc<Node>(_nodeCapacity, allocator);
            UnsafeUtility.MemClear(_nodes, _nodeCapacity * sizeof(Node));

            // Build a linked list for the free list.
            for (var i = 0; i < _nodeCapacity - 1; ++i)
            {
                _nodes[i].Next = i + 1;
                _nodes[i].Height = -1;
            }

            _nodes[_nodeCapacity - 1].Next = NullNode;
            _nodes[_nodeCapacity - 1].Height = -1;
            _freeList = 0;

            Count = 0;
        }

        public void Dispose()
        {
            // This frees the entire tree in one shot.
            UnsafeUtility.Free(_nodes, Allocator);
            _nodes = null;
        }

        public int Insert(AABB aabb, T userData)
        {
            var id = AllocateNode();

            _nodes[id].AABB = aabb;
            _nodes[id].UserData = userData;
            _nodes[id].Height = 0;
            _nodes[id].Moved = true;

            InsertLeaf(id);
            ++Count;
            return id;
        }

        public void Remove(int id)
        {
            Assert.IsTrue(0 <= id && id < _nodeCapacity);
            Assert.IsTrue(_nodes[id].IsLeaf);

            RemoveLeaf(id);
            FreeNode(id);
            --Count;
        }

        internal void Move(int id, AABB aabb)
        {
            RemoveLeaf(id);
            _nodes[id].AABB = aabb;
            InsertLeaf(id);
            _nodes[id].Moved = true;
        }

        public T GetUserData(int id)
        {
            Assert.IsTrue(0 <= id && id < _nodeCapacity);
            return _nodes[id].UserData;
        }

        internal bool WasMoved(int id)
        {
            Assert.IsTrue(0 <= id && id < _nodeCapacity);
            return _nodes[id].Moved;
        }

        internal void ClearMoved(int id)
        {
            Assert.IsTrue(0 <= id && id < _nodeCapacity);
            _nodes[id].Moved = false;
        }

        public AABB GetAABB(int id)
        {
            Assert.IsTrue(0 <= id && id < _nodeCapacity);
            return _nodes[id].AABB;
        }

        public void ShiftOrigin(float2 newOrigin)
        {
            for (var i = 0; i < _nodeCapacity; ++i)
            {
                _nodes[i].AABB.LowerBound -= newOrigin;
                _nodes[i].AABB.UpperBound -= newOrigin;
            }
        }

        public void Query<TC>(TC callback, AABB aabb) where TC : IQueryResultCollector<T>
        {
            var stack = new Collections.Stack<int>(256, Allocator.Temp);
            stack.Push(_root);

            while (stack.Count > 0)
            {
                var nodeId = stack.Pop();
                if (nodeId == NullNode)
                    continue;

                var node = _nodes + nodeId;

                if (TestOverlap(node->AABB, aabb))
                {
                    if (node->IsLeaf)
                    {
                        var proceed = callback.QueryCallback(node->UserData);
                        if (proceed == false)
                            return;
                    }
                    else
                    {
                        stack.Push(node->Child1);
                        stack.Push(node->Child2);
                    }
                }
            }
        }

        public void RayCast<TC>(TC callback, RayCastInput input) where TC : IRayCastResultCollector<T>
        {
            var p1 = input.P1;
            var p2 = input.P2;
            var r = p2 - p1;
            Assert.IsTrue(math.any(r != 0.0f));
            r = math.normalize(r);

            // v is perpendicular to the segment.
            var v = Cross(1.0f, r);
            var absV = math.abs(v);

            // Separating axis for segment (Gino, p80).
            // |dot(v, p1 - c)| > dot(|v|, h)

            var maxFraction = input.MaxFraction;

            // Build a bounding box for the segment.
            AABB segmentAABB;
            {
                var t = p1 + maxFraction * (p2 - p1);
                segmentAABB.LowerBound = math.min(p1, t);
                segmentAABB.UpperBound = math.max(p1, t);
            }

            var stack = new Collections.Stack<int>(256, Allocator.Temp);
            stack.Push(_root);

            while (stack.Count > 0)
            {
                var nodeId = stack.Pop();
                if (nodeId == NullNode)
                    continue;

                var node = _nodes + nodeId;

                if (TestOverlap(node->AABB, segmentAABB) == false)
                    continue;

                // Separating axis for segment (Gino, p80).
                // |dot(v, p1 - c)| > dot(|v|, h)
                var c = node->AABB.GetCenter();
                var h = node->AABB.GetExtents();
                var separation = math.abs(math.dot(v, p1 - c)) - math.dot(absV, h);
                if (separation > 0.0f)
                    continue;

                if (node->IsLeaf)
                {
                    RayCastInput subInput;
                    subInput.P1 = input.P1;
                    subInput.P2 = input.P2;
                    subInput.MaxFraction = maxFraction;

                    var value = callback.RayCastCallback(subInput, node->UserData);

                    if (value == 0.0f)
                    {
                        // The client has terminated the ray cast.
                        return;
                    }

                    if (value > 0.0f)
                    {
                        // Update segment bounding box.
                        maxFraction = value;
                        var t = p1 + maxFraction * (p2 - p1);
                        segmentAABB.LowerBound = math.min(p1, t);
                        segmentAABB.UpperBound = math.max(p1, t);
                    }
                }
                else
                {
                    stack.Push(node->Child1);
                    stack.Push(node->Child2);
                }
            }
        }

        static bool TestOverlap(AABB a, AABB b)
        {
            var d1 = b.LowerBound - a.UpperBound;
            var d2 = a.LowerBound - b.UpperBound;

            if (d1.x > 0.0f || d1.y > 0.0f)
                return false;

            if (d2.x > 0.0f || d2.y > 0.0f)
                return false;

            return true;
        }

        static float2 Cross(float s, float2 a) => new float2(-s * a.y, s * a.x);

        // Allocate a node from the pool. Grow the pool if necessary.
        int AllocateNode()
        {
            // Expand the node pool as needed.
            if (_freeList == NullNode)
            {
                Assert.IsTrue(_nodeCount == _nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                var oldNodes = _nodes;
                _nodeCapacity *= 2;
                _nodes = (Node*) Mem.Malloc<Node>(_nodeCapacity, Allocator);
                UnsafeUtility.MemCpy(_nodes, oldNodes, _nodeCount * sizeof(Node));
                UnsafeUtility.Free(oldNodes, Allocator);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (var i = _nodeCount; i < _nodeCapacity - 1; ++i)
                {
                    _nodes[i].Next = i + 1;
                    _nodes[i].Height = -1;
                }

                _nodes[_nodeCapacity - 1].Next = NullNode;
                _nodes[_nodeCapacity - 1].Height = -1;
                _freeList = _nodeCount;
            }

            // Peel a node off the free list.
            var nodeId = _freeList;
            _freeList = _nodes[nodeId].Next;
            _nodes[nodeId].Parent = NullNode;
            _nodes[nodeId].Child1 = NullNode;
            _nodes[nodeId].Child2 = NullNode;
            _nodes[nodeId].Height = 0;
            _nodes[nodeId].UserData = default;
            _nodes[nodeId].Moved = false;
            ++_nodeCount;
            return nodeId;
        }

        // Return a node to the pool.
        void FreeNode(int nodeId)
        {
            Assert.IsTrue(0 <= nodeId && nodeId < _nodeCapacity);
            Assert.IsTrue(0 < _nodeCount);
            _nodes[nodeId].Next = _freeList;
            _nodes[nodeId].Height = -1;
            _freeList = nodeId;
            --_nodeCount;
        }

        void InsertLeaf(int leaf)
        {
            if (_root == NullNode)
            {
                _root = leaf;
                _nodes[_root].Parent = NullNode;
                return;
            }

            // Find the best sibling for this node
            var leafAABB = _nodes[leaf].AABB;
            var index = _root;
            while (_nodes[index].IsLeaf == false)
            {
                var child1 = _nodes[index].Child1;
                var child2 = _nodes[index].Child2;

                var area = _nodes[index].AABB.GetPerimeter();

                AABB combinedAABB = default;
                combinedAABB.Combine(_nodes[index].AABB, leafAABB);
                var combinedArea = combinedAABB.GetPerimeter();

                // Cost of creating a new parent for this node and the new leaf
                var cost = 2.0f * combinedArea;

                // Minimum cost of pushing the leaf further down the tree
                var inheritanceCost = 2.0f * (combinedArea - area);

                // Cost of descending into child1
                float cost1;
                if (_nodes[child1].IsLeaf)
                {
                    AABB aabb = default;
                    aabb.Combine(leafAABB, _nodes[child1].AABB);
                    cost1 = aabb.GetPerimeter() + inheritanceCost;
                }
                else
                {
                    AABB aabb = default;
                    aabb.Combine(leafAABB, _nodes[child1].AABB);
                    var oldArea = _nodes[child1].AABB.GetPerimeter();
                    var newArea = aabb.GetPerimeter();
                    cost1 = (newArea - oldArea) + inheritanceCost;
                }

                // Cost of descending into child2
                float cost2;
                if (_nodes[child2].IsLeaf)
                {
                    AABB aabb = default;
                    aabb.Combine(leafAABB, _nodes[child2].AABB);
                    cost2 = aabb.GetPerimeter() + inheritanceCost;
                }
                else
                {
                    AABB aabb = default;
                    aabb.Combine(leafAABB, _nodes[child2].AABB);
                    var oldArea = _nodes[child2].AABB.GetPerimeter();
                    var newArea = aabb.GetPerimeter();
                    cost2 = newArea - oldArea + inheritanceCost;
                }

                // Descend according to the minimum cost.
                if (cost < cost1 && cost < cost2)
                    break;

                // Descend
                index = cost1 < cost2 ? child1 : child2;
            }

            var sibling = index;

            // Create a new parent.
            var oldParent = _nodes[sibling].Parent;
            var newParent = AllocateNode();
            _nodes[newParent].Parent = oldParent;
            _nodes[newParent].UserData = default;
            _nodes[newParent].AABB.Combine(leafAABB, _nodes[sibling].AABB);
            _nodes[newParent].Height = _nodes[sibling].Height + 1;

            if (oldParent != NullNode)
            {
                // The sibling was not the root.
                if (_nodes[oldParent].Child1 == sibling)
                    _nodes[oldParent].Child1 = newParent;
                else
                    _nodes[oldParent].Child2 = newParent;

                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].Parent = newParent;
                _nodes[leaf].Parent = newParent;
            }
            else
            {
                // The sibling was the root.
                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].Parent = newParent;
                _nodes[leaf].Parent = newParent;
                _root = newParent;
            }

            // Walk back up the tree fixing heights and AABBs
            index = _nodes[leaf].Parent;
            while (index != NullNode)
            {
                index = Balance(index);

                var child1 = _nodes[index].Child1;
                var child2 = _nodes[index].Child2;

                Assert.IsTrue(child1 != NullNode);
                Assert.IsTrue(child2 != NullNode);

                _nodes[index].Height = 1 + math.max(_nodes[child1].Height, _nodes[child2].Height);
                _nodes[index].AABB.Combine(_nodes[child1].AABB, _nodes[child2].AABB);

                index = _nodes[index].Parent;
            }

            //Validate();
        }

        void RemoveLeaf(int leaf)
        {
            if (leaf == _root)
            {
                _root = NullNode;
                return;
            }

            var parent = _nodes[leaf].Parent;
            var grandParent = _nodes[parent].Parent;
            var sibling = _nodes[parent].Child1 == leaf ? _nodes[parent].Child2 : _nodes[parent].Child1;

            if (grandParent != NullNode)
            {
                // Destroy parent and connect sibling to grandParent.
                if (_nodes[grandParent].Child1 == parent)
                    _nodes[grandParent].Child1 = sibling;
                else
                    _nodes[grandParent].Child2 = sibling;

                _nodes[sibling].Parent = grandParent;
                FreeNode(parent);

                // Adjust ancestor bounds.
                var index = grandParent;
                while (index != NullNode)
                {
                    index = Balance(index);

                    var child1 = _nodes[index].Child1;
                    var child2 = _nodes[index].Child2;

                    _nodes[index].AABB.Combine(_nodes[child1].AABB, _nodes[child2].AABB);
                    _nodes[index].Height = 1 + math.max(_nodes[child1].Height, _nodes[child2].Height);

                    index = _nodes[index].Parent;
                }
            }
            else
            {
                _root = sibling;
                _nodes[sibling].Parent = NullNode;
                FreeNode(parent);
            }

            //Validate();
        }

        // Perform a left or right rotation if node A is imbalanced.
        // Returns the new root index.
        int Balance(int iA)
        {
            Assert.IsTrue(iA != NullNode);

            var A = _nodes + iA;
            if (A->IsLeaf || A->Height < 2)
                return iA;

            var iB = A->Child1;
            var iC = A->Child2;
            Assert.IsTrue(0 <= iB && iB < _nodeCapacity);
            Assert.IsTrue(0 <= iC && iC < _nodeCapacity);

            var B = _nodes + iB;
            var C = _nodes + iC;

            var balance = C->Height - B->Height;

            // Rotate C up
            if (balance > 1)
            {
                var iF = C->Child1;
                var iG = C->Child2;
                var F = _nodes + iF;
                var G = _nodes + iG;
                Assert.IsTrue(0 <= iF && iF < _nodeCapacity);
                Assert.IsTrue(0 <= iG && iG < _nodeCapacity);

                // Swap A and C
                C->Child1 = iA;
                C->Parent = A->Parent;
                A->Parent = iC;

                // A's old parent should point to C
                if (C->Parent != NullNode)
                {
                    if (_nodes[C->Parent].Child1 == iA)
                    {
                        _nodes[C->Parent].Child1 = iC;
                    }
                    else
                    {
                        Assert.IsTrue(_nodes[C->Parent].Child2 == iA);
                        _nodes[C->Parent].Child2 = iC;
                    }
                }
                else
                {
                    _root = iC;
                }

                // Rotate
                if (F->Height > G->Height)
                {
                    C->Child2 = iF;
                    A->Child2 = iG;
                    G->Parent = iA;
                    A->AABB.Combine(B->AABB, G->AABB);
                    C->AABB.Combine(A->AABB, F->AABB);

                    A->Height = 1 + math.max(B->Height, G->Height);
                    C->Height = 1 + math.max(A->Height, F->Height);
                }
                else
                {
                    C->Child2 = iG;
                    A->Child2 = iF;
                    F->Parent = iA;
                    A->AABB.Combine(B->AABB, F->AABB);
                    C->AABB.Combine(A->AABB, G->AABB);

                    A->Height = 1 + math.max(B->Height, F->Height);
                    C->Height = 1 + math.max(A->Height, G->Height);
                }

                return iC;
            }

            // Rotate B up
            if (balance < -1)
            {
                var iD = B->Child1;
                var iE = B->Child2;
                var D = _nodes + iD;
                var E = _nodes + iE;
                Assert.IsTrue(0 <= iD && iD < _nodeCapacity);
                Assert.IsTrue(0 <= iE && iE < _nodeCapacity);

                // Swap A and B
                B->Child1 = iA;
                B->Parent = A->Parent;
                A->Parent = iB;

                // A's old parent should point to B
                if (B->Parent != NullNode)
                {
                    if (_nodes[B->Parent].Child1 == iA)
                    {
                        _nodes[B->Parent].Child1 = iB;
                    }
                    else
                    {
                        Assert.IsTrue(_nodes[B->Parent].Child2 == iA);
                        _nodes[B->Parent].Child2 = iB;
                    }
                }
                else
                {
                    _root = iB;
                }

                // Rotate
                if (D->Height > E->Height)
                {
                    B->Child2 = iD;
                    A->Child1 = iE;
                    E->Parent = iA;
                    A->AABB.Combine(C->AABB, E->AABB);
                    B->AABB.Combine(A->AABB, D->AABB);

                    A->Height = 1 + math.max(C->Height, E->Height);
                    B->Height = 1 + math.max(A->Height, D->Height);
                }
                else
                {
                    B->Child2 = iE;
                    A->Child1 = iD;
                    D->Parent = iA;
                    A->AABB.Combine(C->AABB, D->AABB);
                    B->AABB.Combine(A->AABB, E->AABB);

                    A->Height = 1 + math.max(C->Height, D->Height);
                    B->Height = 1 + math.max(A->Height, E->Height);
                }

                return iB;
            }

            return iA;
        }

        int Height => _root == NullNode ? 0 : _nodes[_root].Height;

        float GetAreaRatio()
        {
            if (_root == NullNode)
                return 0.0f;

            var root = _nodes + _root;
            var rootArea = root->AABB.GetPerimeter();

            var totalArea = 0.0f;
            for (var i = 0; i < _nodeCapacity; ++i)
            {
                var node = _nodes + i;
                if (node->Height < 0)
                {
                    // Free node in pool
                    continue;
                }

                totalArea += node->AABB.GetPerimeter();
            }

            return totalArea / rootArea;
        }

        // Compute the height of a sub-tree.
        int ComputeHeight(int nodeId)
        {
            Assert.IsTrue(0 <= nodeId && nodeId < _nodeCapacity);
            var node = _nodes + nodeId;

            if (node->IsLeaf)
                return 0;

            var height1 = ComputeHeight(node->Child1);
            var height2 = ComputeHeight(node->Child2);
            return 1 + math.max(height1, height2);
        }

        int ComputeHeight() => ComputeHeight(_root);

        void ValidateStructure(int index)
        {
            if (index == NullNode)
                return;

            if (index == _root)
                Assert.IsTrue(_nodes[index].Parent == NullNode);

            var node = _nodes + index;

            var child1 = node->Child1;
            var child2 = node->Child2;

            if (node->IsLeaf)
            {
                Assert.IsTrue(child1 == NullNode);
                Assert.IsTrue(child2 == NullNode);
                Assert.IsTrue(node->Height == 0);
                return;
            }

            Assert.IsTrue(0 <= child1 && child1 < _nodeCapacity);
            Assert.IsTrue(0 <= child2 && child2 < _nodeCapacity);

            Assert.IsTrue(_nodes[child1].Parent == index);
            Assert.IsTrue(_nodes[child2].Parent == index);

            ValidateStructure(child1);
            ValidateStructure(child2);
        }

        void ValidateMetrics(int index)
        {
            if (index == NullNode)
                return;

            var node = _nodes + index;

            var child1 = node->Child1;
            var child2 = node->Child2;

            if (node->IsLeaf)
            {
                Assert.IsTrue(child1 == NullNode);
                Assert.IsTrue(child2 == NullNode);
                Assert.IsTrue(node->Height == 0);
                return;
            }

            Assert.IsTrue(0 <= child1 && child1 < _nodeCapacity);
            Assert.IsTrue(0 <= child2 && child2 < _nodeCapacity);

            var height1 = _nodes[child1].Height;
            var height2 = _nodes[child2].Height;
            var height = 1 + math.max(height1, height2);
            Assert.IsTrue(node->Height == height);

            AABB aabb = default;
            aabb.Combine(_nodes[child1].AABB, _nodes[child2].AABB);

            Assert.IsTrue(math.all(aabb.LowerBound == node->AABB.LowerBound));
            Assert.IsTrue(math.all(aabb.UpperBound == node->AABB.UpperBound));

            ValidateMetrics(child1);
            ValidateMetrics(child2);
        }

        [Conditional("VALIDATE_DYNAMIC_TREE")]
        void Validate()
        {
            ValidateStructure(_root);
            ValidateMetrics(_root);

            var freeCount = 0;
            var freeIndex = _freeList;
            while (freeIndex != NullNode)
            {
                Assert.IsTrue(0 <= freeIndex && freeIndex < _nodeCapacity);
                freeIndex = _nodes[freeIndex].Next;
                ++freeCount;
            }

            Assert.IsTrue(Height == ComputeHeight());

            Assert.IsTrue(_nodeCount + freeCount == _nodeCapacity);
        }

        internal int GetMaxBalance()
        {
            var maxBalance = 0;
            for (var i = 0; i < _nodeCapacity; ++i)
            {
                var node = _nodes + i;
                if (node->Height <= 1)
                    continue;

                Assert.IsTrue(node->IsLeaf == false);

                var child1 = node->Child1;
                var child2 = node->Child2;
                var balance = math.abs(_nodes[child2].Height - _nodes[child1].Height);
                maxBalance = math.max(maxBalance, balance);
            }

            return maxBalance;
        }

        internal void RebuildBottomUp()
        {
            var nodes = (int*) Mem.Malloc<int>(_nodeCount, Allocator.Temp);
            var count = 0;

            // Build array of leaves. Free the rest.
            for (var i = 0; i < _nodeCapacity; ++i)
            {
                if (_nodes[i].Height < 0)
                {
                    // free node in pool
                    continue;
                }

                if (_nodes[i].IsLeaf)
                {
                    _nodes[i].Parent = NullNode;
                    nodes[count] = i;
                    ++count;
                }
                else
                {
                    FreeNode(i);
                }
            }

            while (count > 1)
            {
                var minCost = float.MaxValue;
                int iMin = -1, jMin = -1;
                for (var i = 0; i < count; ++i)
                {
                    var aabbi = _nodes[nodes[i]].AABB;

                    for (var j = i + 1; j < count; ++j)
                    {
                        var aabbj = _nodes[nodes[j]].AABB;
                        AABB b = default;
                        b.Combine(aabbi, aabbj);
                        var cost = b.GetPerimeter();
                        if (cost < minCost)
                        {
                            iMin = i;
                            jMin = j;
                            minCost = cost;
                        }
                    }
                }

                var index1 = nodes[iMin];
                var index2 = nodes[jMin];
                var child1 = _nodes + index1;
                var child2 = _nodes + index2;

                var parentIndex = AllocateNode();
                var parent = _nodes + parentIndex;
                parent->Child1 = index1;
                parent->Child2 = index2;
                parent->Height = 1 + math.max(child1->Height, child2->Height);
                parent->AABB.Combine(child1->AABB, child2->AABB);
                parent->Parent = NullNode;

                child1->Parent = parentIndex;
                child2->Parent = parentIndex;

                nodes[jMin] = nodes[count - 1];
                nodes[iMin] = parentIndex;
                --count;
            }

            _root = nodes[0];

            Validate();
        }

        internal struct Node
        {
            internal bool IsLeaf => Child1 == NullNode;

            internal AABB AABB;

            internal T UserData;

            internal int Parent;
            internal int Next
            {
                get => Parent;
                set => Parent = value;
            }

            internal int Child1;
            internal int Child2;

            // leaf = 0, free node = -1
            internal int Height;

            internal bool Moved;
        }

        public Enumerator GetEnumerator(Allocator allocator)
        {
            return new Enumerator(_root, _nodes, allocator);
        }

        public struct Enumerator
        {
            public T Current;
            readonly Node* _nodes;
            readonly Stack<Node> _stack;

            internal Enumerator(int root, Node* nodes, Allocator allocator)
            {
                _nodes = nodes;
                Current = default;
                _stack = new Stack<Node>(16, allocator);
                if (root != NullNode)
                    _stack.Push(_nodes[root]);
            }

            public bool MoveNext()
            {
                while (_stack.Count > 0)
                {
                    var node = _stack.Pop();

                    if (node.IsLeaf)
                    {
                        Current = node.UserData;
                        return true;
                    }

                    _stack.Push(_nodes[node.Child1]);
                    if (node.Child2 != NullNode)
                        _stack.Push(_nodes[node.Child2]);
                }

                return false;
            }

            public void Dispose()
            {
                _stack.Dispose();
            }
        }
    }
}