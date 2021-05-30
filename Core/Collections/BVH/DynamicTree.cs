using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav.Core.Collections.BVH
{
    public unsafe struct DynamicTree<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeDynamicTree<T>* _tree;

        public bool IsCreated => _tree != null;

        public DynamicTree(Allocator allocator)
        {
            _tree = (UnsafeDynamicTree<T>*) Mem.Malloc<UnsafeDynamicTree<T>>(allocator);
            *_tree = new UnsafeDynamicTree<T>(allocator);
        }

        public void Dispose()
        {
            _tree->Dispose();
            UnsafeUtility.Free(_tree, _tree->Allocator);
            _tree = null;
        }

        public int CreateProxy(AABB aabb, T userData)
        {
            return _tree->CreateProxy(aabb, userData);
        }

        public void DestroyProxy(int proxyId)
        {
            _tree->DestroyProxy(proxyId);
        }

        public bool MoveProxy(int proxyId, AABB aabb, float2 displacement)
        {
            return _tree->MoveProxy(proxyId, aabb, displacement);
        }

        public T GetUserData(int proxyId)
        {
            return _tree->GetUserData(proxyId);
        }

        public bool WasMoved(int proxyId)
        {
            return _tree->WasMoved(proxyId);
        }

        public void ClearMoved(int proxyId)
        {
            _tree->ClearMoved(proxyId);
        }

        public AABB GetFatAABB(int proxyId)
        {
            return _tree->GetFatAABB(proxyId);
        }

        public void ShiftOrigin(float2 newOrigin)
        {
            _tree->ShiftOrigin(newOrigin);
        }

        public void Query<TC>(TC callback, AABB aabb) where TC : IQueryResultCollector<T>
        {
            _tree->Query(callback, aabb);
        }

        public void RayCast<TC>(TC callback, RayCastInput input) where TC : IRayCastResultCollector<T>
        {
            _tree->RayCast(callback, input);
        }
    }
}