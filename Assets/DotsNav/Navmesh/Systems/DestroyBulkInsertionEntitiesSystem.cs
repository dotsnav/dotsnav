using DotsNav.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    partial class DestroyBulkInsertionEntitiesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSource = EcbUtility.Get(World);
            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithBurst()
                .WithAny<VertexAmountElement, ObstacleBlobComponent>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    buffer.DestroyEntity(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            ecbSource.AddJobHandleForProducer(Dependency);
        }
    }
}