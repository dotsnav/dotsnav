using System.Collections.Generic;
using DotsNav.Core.Collections.BVH;
using DotsNav.Core.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.Core.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    class AgentTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        readonly List<AgentTreeSharedComponent> _agentTreeSharedComponents = new List<AgentTreeSharedComponent>();
        NativeQueue<RemoveAgentData> _removeAgentQueue;

        protected override void OnCreate()
        {
            _removeAgentQueue = new NativeQueue<RemoveAgentData>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _removeAgentQueue.Dispose();
            Entities.WithBurst().ForEach((TreeSystemStateComponent c) => c.Tree.Dispose()).Run();
        }

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
                    agentTree.Tree = new DynamicTree<Entity>(Allocator.Persistent);
                    parallelBuffer.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = agentTree.Tree});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<AgentTreeComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                {
                    state.Tree.Dispose();
                    parallelBuffer.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();


            _agentTreeSharedComponents.Clear();
            EntityManager.GetAllUniqueSharedComponentData(_agentTreeSharedComponents);
            var agentTreeLookup = GetComponentDataFromEntity<AgentTreeComponent>();
            var buffer = ecbSource.CreateCommandBuffer();
            var agentToRemoveWriter = _removeAgentQueue.AsParallelWriter();

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
                        var id = tree.CreateProxy(aabb, entity);
                        buffer.AddComponent(entity, new AgentSystemStateComponent {Id = id, PreviousPosition = pos, TreeEntity = treeEntity});
                    })
                    .Schedule();

                Entities
                    .WithBurst()
                    .WithSharedComponentFilter(treeEntity)
                    .ForEach((Entity entity, ref AgentSystemStateComponent state, in Translation translation, in RadiusComponent radius) =>
                    {
                        var tree = agentTreeLookup[treeEntity].Tree;
                        var pos = translation.Value.xz;
                        var aabb = new AABB {LowerBound = pos - radius, UpperBound = pos + radius};
                        state.PreviousPosition = pos;

                        if (state.TreeEntity != treeEntity)
                        {
                            agentToRemoveWriter.Enqueue(new RemoveAgentData{Id = state.Id, Tree = agentTreeLookup[state.TreeEntity].Tree});
                            state.TreeEntity = treeEntity;
                            state.Id = tree.CreateProxy(aabb, entity);
                        }
                        else
                        {
                            tree.MoveProxy(state.Id, aabb, pos - state.PreviousPosition);
                        }
                    })
                    .Schedule();
            }

            // todo // as AgentTreeSharedComponent is already removed we can not run this in parallel
            // todo // filtered by tree, look in to shared system state components?
            Entities
                .WithBurst()
                .WithNone<AgentTreeSharedComponent>()
                .ForEach((Entity entity, AgentSystemStateComponent state) =>
                {
                    if (!agentTreeLookup.HasComponent(state.TreeEntity))
                        return;

                    var tree = agentTreeLookup[state.TreeEntity].Tree;
                    tree.DestroyProxy(state.Id);
                    buffer.RemoveComponent<AgentSystemStateComponent>(entity);
                })
                .Schedule();

            var removeAgentQueue = _removeAgentQueue;

            Dependency = Job
                .WithBurst()
                .WithCode(() =>
                {
                    while (removeAgentQueue.TryDequeue(out var t))
                    {
                        Debug.Log($"removing moved entity");
                        t.Tree.DestroyProxy(t.Id);
                    }
                })
                .Schedule(Dependency);

            ecbSource.AddJobHandleForProducer(Dependency);
            OutputDependecy = Dependency;
        }

        struct TreeSystemStateComponent : ISystemStateComponentData
        {
            public DynamicTree<Entity> Tree;
        }

        struct AgentSystemStateComponent : ISystemStateComponentData
        {
            public int Id;
            public float2 PreviousPosition;
            public Entity TreeEntity;
        }

        struct RemoveAgentData
        {
            public DynamicTree<Entity> Tree;
            public int Id;
        }
    }
}