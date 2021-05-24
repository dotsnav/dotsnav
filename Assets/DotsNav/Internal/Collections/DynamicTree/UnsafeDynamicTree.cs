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
using DotsNav.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Debug = UnityEngine.Debug;

namespace DotsNav.Collections
{
    public unsafe struct UnsafeDynamicTree
    {
        const int NullNode = -1;
        const float AABBExtension = .1f;
        const float AABBMultiplier = 4;
        
        int _root;

        [NativeDisableUnsafePtrRestriction]
        Node* _nodes;
        int _nodeCount;
        int _nodeCapacity;

        int _freeList;

        int _insertionCount;
        internal readonly Allocator Allocator;

        public UnsafeDynamicTree(Allocator allocator)
        {
            Allocator = allocator;
            _root = NullNode;

            _nodeCapacity = 16;
            _nodeCount = 0;
            _nodes = (Node*) Util.Malloc<Node>(_nodeCapacity, allocator);
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

            _insertionCount = 0;
        }

        public void Dispose()
        {
            // This frees the entire tree in one shot.
            UnsafeUtility.Free(_nodes, Allocator);
        }
        
        // Create a proxy in the tree as a leaf node. We return the index
        // of the node instead of a pointer so that we can grow
        // the node pool.
        public int CreateProxy(AABB aabb, Entity userData)
        {
            var proxyId = AllocateNode();

            // Fatten the aabb.
            var r = new float2(AABBExtension);
            _nodes[proxyId].AABB.LowerBound = aabb.LowerBound - r;
            _nodes[proxyId].AABB.UpperBound = aabb.UpperBound + r;
            _nodes[proxyId].UserData = userData;
            _nodes[proxyId].Height = 0;
            _nodes[proxyId].Moved = true;

            InsertLeaf(proxyId);

            return proxyId;
        }

        public void DestroyProxy(int proxyId)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);
            Assert.IsTrue(_nodes[proxyId].IsLeaf);

            RemoveLeaf(proxyId);
            FreeNode(proxyId);
        }

        public bool MoveProxy(int proxyId, AABB aabb, float2 displacement)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);

            Assert.IsTrue(_nodes[proxyId].IsLeaf);

            // Extend AABB
            AABB fatAABB;
            var r = new float2(AABBExtension);
            fatAABB.LowerBound = aabb.LowerBound - r;
            fatAABB.UpperBound = aabb.UpperBound + r;

            // Predict AABB movement
            var d = AABBMultiplier * displacement;

            if (d.x < 0.0f)
                fatAABB.LowerBound.x += d.x;
            else
                fatAABB.UpperBound.x += d.x;

            if (d.y < 0.0f)
                fatAABB.LowerBound.y += d.y;
            else
                fatAABB.UpperBound.y += d.y;

            var treeAABB = _nodes[proxyId].AABB;
            if (treeAABB.Contains(aabb))
            {
                // The tree AABB still contains the object, but it might be too large.
                // Perhaps the object was moving fast but has since gone to sleep.
                // The huge AABB is larger than the new fat AABB.
                AABB hugeAABB;
                hugeAABB.LowerBound = fatAABB.LowerBound - 4.0f * r;
                hugeAABB.UpperBound = fatAABB.UpperBound + 4.0f * r;

                if (hugeAABB.Contains(treeAABB))
                {
                    // The tree AABB contains the object AABB and the tree AABB is
                    // not too large. No tree update needed.
                    return false;
                }

                // Otherwise the tree AABB is huge and needs to be shrunk
            }

            RemoveLeaf(proxyId);

            _nodes[proxyId].AABB = fatAABB;

            InsertLeaf(proxyId);

            _nodes[proxyId].Moved = true;

            return true;
        }
        
        public Entity GetUserData(int proxyId)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].UserData;
        }

        public bool WasMoved(int proxyId)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].Moved;
        }

        public void ClearMoved(int proxyId)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);
            _nodes[proxyId].Moved = false;
        }

        public AABB GetFatAABB(int proxyId)
        {
            Assert.IsTrue(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].AABB;
        }
        
        public void ShiftOrigin(float2 newOrigin)
        {
            for (var i = 0; i < _nodeCapacity; ++i)
            {
                _nodes[i].AABB.LowerBound -= newOrigin;
                _nodes[i].AABB.UpperBound -= newOrigin;
            }
        }

        public void Query<T>(T callback, AABB aabb) where T : IQueryResultCollector
        {
            var stack = new Stack<int>(256, Allocator.Temp);
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

        public void RayCast<T>(T callback, RayCastInput input) where T : IRayCastResultCollector
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

            var stack = new Stack<int>(256, Allocator.Temp);
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
                _nodes = (Node*) Util.Malloc<Node>(_nodeCapacity, Allocator);
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
            _nodes[nodeId].UserData = Entity.Null;
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
            ++_insertionCount;

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
            _nodes[newParent].UserData = Entity.Null;
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
            var nodes = (int*) Util.Malloc<int>(_nodeCount, Allocator.Temp);
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
        
        struct Node
        {
            internal bool IsLeaf => Child1 == NullNode;

            // Enlarged AABB
            internal AABB AABB;

            internal Entity UserData;

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
    }
}