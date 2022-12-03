using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    partial class PathFinderResourceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSource = EcbUtility.Get(World);
            
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

            var buffer1 = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<PathFinderComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, PathFinderSystemStateComponent resources) =>
                {
                    resources.Dispose();
                    buffer1.RemoveComponent<PathFinderSystemStateComponent>(entityInQueryIndex, entity);
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