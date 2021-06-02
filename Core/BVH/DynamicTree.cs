using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav.BVH
{
    unsafe struct DynamicTree<T> : IComparable<DynamicTree<T>>, IEquatable<DynamicTree<T>> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeDynamicTree<T>* _tree;

        public readonly bool IsCreated => _tree != null;

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

        public readonly int CreateProxy(AABB aabb, T userData)
        {
            return _tree->CreateProxy(aabb, userData);
        }

        public readonly void DestroyProxy(int proxyId)
        {
            _tree->DestroyProxy(proxyId);
        }

        public readonly bool MoveProxy(int proxyId, AABB aabb, float2 displacement)
        {
            return _tree->MoveProxy(proxyId, aabb, displacement);
        }

        public readonly T GetUserData(int proxyId)
        {
            return _tree->GetUserData(proxyId);
        }

        public readonly bool WasMoved(int proxyId)
        {
            return _tree->WasMoved(proxyId);
        }

        public readonly void ClearMoved(int proxyId)
        {
            _tree->ClearMoved(proxyId);
        }

        public readonly AABB GetFatAABB(int proxyId)
        {
            return _tree->GetFatAABB(proxyId);
        }

        public readonly void ShiftOrigin(float2 newOrigin)
        {
            _tree->ShiftOrigin(newOrigin);
        }

        public readonly void Query<TC>(TC callback, AABB aabb) where TC : IQueryResultCollector<T>
        {
            _tree->Query(callback, aabb);
        }

        public readonly void RayCast<TC>(TC callback, RayCastInput input) where TC : IRayCastResultCollector<T>
        {
            _tree->RayCast(callback, input);
        }

        public readonly int CompareTo(DynamicTree<T> other)
        {
            return _tree == other._tree ? 0 : _tree < other._tree ? -1 : 1;
        }

        public readonly bool Equals(DynamicTree<T> other)
        {
            return _tree == other._tree;
        }

        public override int GetHashCode()
        {
           return _tree == null ? 0 : ((ulong) _tree).GetHashCode();
        }
    }
}