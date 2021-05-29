using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Collections
{
    [NativeContainer]
    public unsafe struct NativeDynamicTree<T> where T : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeDynamicTree<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeDynamicTree<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
        [NativeDisableUnsafePtrRestriction]
        DynamicTree<T>* _tree;

        Allocator _allocator;

        public bool IsCreated => _tree != null;

        public NativeDynamicTree(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 2, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            _allocator = allocator;
            _tree = (DynamicTree<T>*) Mem.Malloc<DynamicTree<T>>(allocator);
            *_tree = new DynamicTree<T>(allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            _tree->Dispose();
            UnsafeUtility.Free(_tree, _allocator);
            _tree = null;
        }

        public int CreateProxy(AABB aabb, T userData)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            return _tree->CreateProxy(aabb, userData);
        }

        public void DestroyProxy(int proxyId)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _tree->DestroyProxy(proxyId);
        }

        public bool MoveProxy(int proxyId, AABB aabb, float2 displacement)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return _tree->MoveProxy(proxyId, aabb, displacement);
        }

        public T GetUserData(int proxyId)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return _tree->GetUserData(proxyId);
        }

        public bool WasMoved(int proxyId)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return _tree->WasMoved(proxyId);
        }

        public void ClearMoved(int proxyId)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _tree->ClearMoved(proxyId);
        }

        public AABB GetFatAABB(int proxyId)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return _tree->GetFatAABB(proxyId);
        }

        public void ShiftOrigin(float2 newOrigin)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _tree->ShiftOrigin(newOrigin);
        }

        public void Query<TC>(TC callback, AABB aabb) where TC : IQueryResultCollector<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            _tree->Query(callback, aabb);
        }

        public void RayCast<TC>(TC callback, RayCastInput input) where TC : IRayCastResultCollector<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            _tree->RayCast(callback, input);
        }
    }
}