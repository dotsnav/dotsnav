using DotsNav.Core.Collections.BVH;
using DotsNav.Core.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.Core.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    class AgentTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        public NativeDynamicTree<Entity> Tree;

        protected override void OnCreate()
        {
            Tree  = new NativeDynamicTree<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            Tree.Dispose();
        }

        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;
            var buffer = ecbSource.CreateCommandBuffer();
            var tree = Tree;

            Entities
                .WithBurst()
                .WithAll<AgentTreeComponent>()
                .WithNone<AgentTreeSystemStateComponent>()
                .ForEach((Entity entity, in Translation translation, in RadiusComponent radius) =>
                {
                    var pos = translation.Value.xz;
                    var aabb = new AABB {LowerBound = pos - radius, UpperBound = pos + radius};
                    var id = tree.CreateProxy(aabb, entity);
                    buffer.AddComponent(entity, new AgentTreeSystemStateComponent {Id = id, PreviousPosition = pos});
                })
                .Schedule();

            Entities
                .WithBurst()
                .WithNone<AgentTreeComponent>()
                .ForEach((Entity entity, AgentTreeSystemStateComponent state) =>
                {
                    tree.DestroyProxy(state.Id);
                    buffer.RemoveComponent<AgentTreeSystemStateComponent>(entity);
                })
                .Schedule();

            ecbSource.AddJobHandleForProducer(Dependency);

            Entities
                .WithBurst()
                .ForEach((ref AgentTreeSystemStateComponent state, in Translation translation, in RadiusComponent radius) =>
                {
                    var pos = translation.Value.xz;
                    var aabb = new AABB {LowerBound = pos - radius, UpperBound = pos + radius};
                    tree.MoveProxy(state.Id, aabb, pos - state.PreviousPosition);
                    state.PreviousPosition = pos;
                })
                .Schedule();

            OutputDependecy = Dependency;
        }

        struct AgentTreeSystemStateComponent : ISystemStateComponentData
        {
            public int Id;
            public float2 PreviousPosition;
        }
    }
}