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

using Unity.Collections;
using Unity.Mathematics;

namespace DotsNav.BVH
{
    struct UnsafeDynamicTree<T> where T : unmanaged
    {
        const float AABBExtension = .1f;
        const float AABBMultiplier = 4;

        Tree<T> _tree;

        public bool IsCreated => _tree.IsCreated;
        internal Allocator Allocator => _tree.Allocator;

        public UnsafeDynamicTree(Allocator allocator)
        {
            _tree = new Tree<T>(allocator);
        }

        public void Dispose()
        {
            _tree.Dispose();
        }

        public int Count => _tree.Count;

        // Create a proxy in the tree as a leaf node. We return the index
        // of the node instead of a pointer so that we can grow
        // the node pool.
        public int CreateProxy(AABB aabb, T userData)
        {
            // Fatten the aabb.
            var r = new float2(AABBExtension);
            aabb.LowerBound -= r;
            aabb.UpperBound += r;
            return _tree.Insert(aabb, userData);
        }

        public void DestroyProxy(int proxyId)
        {
            _tree.Remove(proxyId);
        }

        public bool MoveProxy(int proxyId, AABB aabb, float2 displacement)
        {
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

            var treeAABB = _tree.GetAABB(proxyId);
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

            _tree.Move(proxyId, fatAABB);

            return true;
        }

        public T GetUserData(int proxyId) => _tree.GetUserData(proxyId);

        public bool WasMoved(int proxyId) => _tree.WasMoved(proxyId);

        public void ClearMoved(int proxyId) => _tree.ClearMoved(proxyId);

        public AABB GetFatAABB(int proxyId) => _tree.GetAABB(proxyId);

        public void ShiftOrigin(float2 newOrigin) => _tree.ShiftOrigin(newOrigin);

        public void Query<TC>(TC callback, AABB aabb) where TC : IQueryResultCollector<T> => _tree.Query(callback, aabb);

        public void RayCast<TC>(TC callback, RayCastInput input) where TC : IRayCastResultCollector<T> => _tree.RayCast(callback, input);
    }
}