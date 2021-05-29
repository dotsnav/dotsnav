using DotsNav.Core.Systems;
using DotsNav.Navmesh.Data;
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
                .WithNone<Navmesh.Navmesh>()
                .ForEach((Entity entity, int entityInQueryIndex, in NavmeshComponent data) =>
                {
                    var resources = new Navmesh.Navmesh();
                    resources.Allocate(data);
                    buffer.AddComponent(entityInQueryIndex, entity, resources);
                })
                .ScheduleParallel();

            buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithBurst()
                .WithNone<NavmeshComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, Navmesh.Navmesh resources) =>
                {
                    resources.Dispose();
                    buffer.RemoveComponent<Navmesh.Navmesh>(entityInQueryIndex, entity);
                })
                .Schedule();
            ecbSource.AddJobHandleForProducer(Dependency);
        }

        protected override void OnDestroy()
        {
            Entities
                .WithBurst()
                .ForEach((Navmesh.Navmesh resources)
                    => resources.Dispose())
                .Run();
        }
    }
}