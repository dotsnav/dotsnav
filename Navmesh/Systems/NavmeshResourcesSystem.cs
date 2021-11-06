using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderFirst = true)]
    [AlwaysUpdateSystem]
    unsafe class NavmeshResourcesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;
            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref NavmeshComponent data) =>
                {
                    data.Navmesh = (Navmesh*) Mem.Malloc<Navmesh>(Allocator.Persistent);
                    *data.Navmesh = new Navmesh(data);
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent{Navmesh = data.Navmesh});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<NavmeshComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, SystemStateComponent state) =>
                {
                    state.Navmesh->Dispose();
                    buffer.RemoveComponent<SystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();
            ecbSource.AddJobHandleForProducer(Dependency);
        }

        struct SystemStateComponent : ISystemStateComponentData
        {
            public Navmesh* Navmesh;
        }

        protected override void OnDestroy()
        {
            Entities
                .WithBurst()
                .ForEach((NavmeshComponent resources)
                    => resources.Navmesh->Dispose())
                .Run();
        }
    }
}