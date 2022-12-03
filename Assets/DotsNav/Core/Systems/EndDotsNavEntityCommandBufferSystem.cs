using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace DotsNav.Systems
{
    /// <summary>
    /// Runs as last system in DotsNavSystemGroup
    /// </summary>
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true), DisableAutoCreation]
    public class EndDotsNavEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal Allocator allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }
            
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }
            
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        
        protected override unsafe void OnCreate()
        {
            base.OnCreate();
            this.RegisterSingleton<Singleton>(ref *m_PendingBuffers, World.Unmanaged, $"{nameof(EndDotsNavEntityCommandBufferSystem)} {nameof(Singleton)}");
        }
    }
}