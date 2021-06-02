using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [AlwaysUpdateSystem]
    class NavmeshResourcesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;
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

            Entities
                .WithBurst()
                .WithNone<NavmeshComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, Navmesh resources) =>
                {
                    resources.Dispose();
                    buffer.RemoveComponent<Navmesh>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();
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