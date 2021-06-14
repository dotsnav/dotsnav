using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.PathFinding
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    class PathFinderResourceSystem : SystemBase
    {
        EntityCommandBufferSystem _ecbSource;

        protected override void OnUpdate()
        {
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;
            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<PathFinderSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, in PathFinderComponent data) =>
                {
                    var resources = new PathFinderSystemStateComponent();
                    resources.Allocate(data);
                    buffer.AddComponent(entityInQueryIndex, entity, resources);
                })
                .ScheduleParallel();

            buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<PathFinderComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, PathFinderSystemStateComponent resources) =>
                {
                    resources.Dispose();
                    buffer.RemoveComponent<PathFinderSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();
            ecbSource.AddJobHandleForProducer(Dependency);
        }

        protected override void OnDestroy()
        {
            Entities
                .WithBurst()
                .ForEach((PathFinderSystemStateComponent resources)
                    => resources.Dispose())
                .Run();
        }
    }
}