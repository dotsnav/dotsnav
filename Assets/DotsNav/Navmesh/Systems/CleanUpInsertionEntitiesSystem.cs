using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace DotsNav.Navmesh.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    partial struct CleanUpInsertionEntitiesSystem : ISystem
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

            new RemoveVertexBufferJob { Buffer = ecb }.Schedule();
            new DestroyBulkEntitiesJob { Buffer = ecb }.Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(VertexElement))]
        [WithNone(typeof(VertexAmountElement))]
        partial struct RemoveVertexBufferJob : IJobEntity
        {
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity)
            {
                Buffer.RemoveComponent<VertexElement>(entity);
            }
        }

        [BurstCompile]
        [WithAny(typeof(VertexAmountElement), typeof(ObstacleBlobComponent))]
        partial struct DestroyBulkEntitiesJob : IJobEntity
        {
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity)
            {
                Buffer.DestroyEntity(entity);
            }
        }
    }
}