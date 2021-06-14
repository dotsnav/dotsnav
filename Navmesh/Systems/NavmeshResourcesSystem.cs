using DotsNav.Data;
using Unity.Entities;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [AlwaysUpdateSystem]
    class NavmeshResourcesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;
            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<Navmesh>()
                .ForEach((Entity entity, int entityInQueryIndex, in NavmeshComponent data) =>
                {
                    var resources = new Navmesh();
                    resources.Allocate(data);
                    buffer.AddComponent(entityInQueryIndex, entity, resources);
                })
                .ScheduleParallel();

            buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<NavmeshComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, Navmesh resources) =>
                {
                    resources.Dispose();
                    buffer.RemoveComponent<Navmesh>(entityInQueryIndex, entity);
                })
                .Schedule();
            ecbSource.AddJobHandleForProducer(Dependency);
        }

        protected override void OnDestroy()
        {
            Entities
                .WithBurst()
                .ForEach((Navmesh resources)
                    => resources.Dispose())
                .Run();
        }
    }
}