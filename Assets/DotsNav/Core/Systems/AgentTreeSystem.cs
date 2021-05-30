using System.Collections.Generic;
using DotsNav.Core.Collections.BVH;
using DotsNav.Core.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.Core.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    unsafe class AgentTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        readonly List<AgentTreeSharedComponent> _agentTreeSharedComponents = new List<AgentTreeSharedComponent>();

        // todo manage dependecies
        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;
            var parallelBuffer = ecbSource.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithBurst()
                .WithNone<TreeSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref AgentTreeComponent agentTree) =>
                {
                    // todo DynamicTree wrapper
                    agentTree.Tree = (DynamicTree<Entity>*) Mem.Malloc<DynamicTree<Entity>>(Allocator.Persistent);
                    *agentTree.Tree = new DynamicTree<Entity>(Allocator.Persistent);
                    parallelBuffer.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = agentTree.Tree});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<AgentTreeComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                {
                    state.Tree->Dispose();
                    UnsafeUtility.Free(state.Tree, Allocator.Persistent);
                    parallelBuffer.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();


            _agentTreeSharedComponents.Clear();
            EntityManager.GetAllUniqueSharedComponentData(_agentTreeSharedComponents);
            var agentTreeLookup = GetComponentDataFromEntity<AgentTreeComponent>();
            var buffer = ecbSource.CreateCommandBuffer();

            for (int i = 1; i < _agentTreeSharedComponents.Count; i++)
            {
                var treeEntity = _agentTreeSharedComponents[i];

                Entities
                    .WithBurst()
                    .WithNone<AgentSystemStateComponent>()
                    .WithSharedComponentFilter(treeEntity)
                    .ForEach((Entity entity, in Translation translation, in RadiusComponent radius) =>
                    {
                        var tree = agentTreeLookup[treeEntity].Tree;
                        var pos = translation.Value.xz;
                        var aabb = new AABB {LowerBound = pos - radius, UpperBound = pos + radius};
                        var id = tree->CreateProxy(aabb, entity);
                        buffer.AddComponent(entity, new AgentSystemStateComponent {Id = id, PreviousPosition = pos, TreeEntity = treeEntity});
                    })
                    .Schedule();

                Entities
                    .WithBurst()
                    .WithSharedComponentFilter(treeEntity)
                    .ForEach((ref AgentSystemStateComponent state, in Translation translation, in RadiusComponent radius) =>
                    {
                        var tree = agentTreeLookup[treeEntity].Tree;
                        var pos = translation.Value.xz;
                        var aabb = new AABB {LowerBound = pos - radius, UpperBound = pos + radius};
                        tree->MoveProxy(state.Id, aabb, pos - state.PreviousPosition);
                        state.PreviousPosition = pos;
                    })
                    .Schedule();
            }

            Entities
                .WithBurst()
                .WithNone<AgentTreeSharedComponent>()
                .ForEach((Entity entity, AgentSystemStateComponent state) =>
                {
                    if (!agentTreeLookup.HasComponent(state.TreeEntity))
                        return;

                    var tree = agentTreeLookup[state.TreeEntity].Tree;
                    tree->DestroyProxy(state.Id);
                    buffer.RemoveComponent<AgentSystemStateComponent>(entity);
                })
                .Schedule();

            ecbSource.AddJobHandleForProducer(Dependency);
            OutputDependecy = Dependency;
        }

        protected override void OnDestroy()
        {
            Entities
                .WithBurst()
                .ForEach((TreeSystemStateComponent c) => c.Tree->Dispose())
                .Run();
        }

        struct TreeSystemStateComponent : ISystemStateComponentData
        {
            public DynamicTree<Entity>* Tree;
        }

        struct AgentSystemStateComponent : ISystemStateComponentData
        {
            public int Id;
            public float2 PreviousPosition;
            public Entity TreeEntity;
        }
    }
}