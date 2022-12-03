using DotsNav.BVH;
using DotsNav.Data;
using DotsNav.Hybrid;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace DotsNav.Systems
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateBefore(typeof(DynamicTreeSystem))]
    partial struct DynamicTreeResourcesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = HasSingleton<RunnerSingleton>() 
                ? GetSingletonRW<EndDotsNavEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged) 
                : GetSingletonRW<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            new AllocateJob { Buffer = ecb }.Schedule();
            new DisposeJob { Buffer = ecb }.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(CleanupComponent))]
        unsafe partial struct AllocateJob : IJobEntity
        {
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity, ref DynamicTreeComponent agentTree)
            {
                agentTree.Tree = new DynamicTree<Entity>(Allocator.Persistent);
                Buffer.AddComponent(entity, new CleanupComponent {Tree = agentTree.Tree});
            }
        }
        
        [BurstCompile]
        [WithNone(typeof(DynamicTreeComponent))]
        unsafe partial struct DisposeJob : IJobEntity
        {
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity, ref CleanupComponent agentTree)
            {
                agentTree.Tree.Dispose();
                Buffer.RemoveComponent<CleanupComponent>(entity);
            }
        }
          
        struct CleanupComponent : ICleanupComponentData
        {
            public DynamicTree<Entity> Tree;
        }
    }
}