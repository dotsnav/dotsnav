using System;
using System.Collections.Generic;
using DotsNav.Core.Collections.BVH;
using DotsNav.Core.Data;
using Unity.Burst;
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
        NativeMultiHashMap<DynamicTree<Entity>, TreeOperation> _treeOperations;
        NativeList<DynamicTree<Entity>> _uniqueKeys;

        protected override void OnCreate()
        {
            _treeOperations = new NativeMultiHashMap<DynamicTree<Entity>, TreeOperation>(1024, Allocator.Persistent);
            _uniqueKeys = new NativeList<DynamicTree<Entity>>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _treeOperations.Dispose();
            _uniqueKeys.Dispose();
            Entities.WithBurst().ForEach((TreeSystemStateComponent c) => c.Tree.Dispose()).Run();
        }

        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;
            var parallelBuffer = ecbSource.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithName("Allocate_AgentTree")
                .WithBurst()
                .WithNone<TreeSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref AgentTreeComponent agentTree) =>
                {
                    agentTree.Tree = new DynamicTree<Entity>(Allocator.Persistent);
                    parallelBuffer.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = agentTree.Tree});
                })
                .ScheduleParallel();

            Entities
                .WithName("Dispose_AgentTree")
                .WithBurst()
                .WithNone<AgentTreeComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                {
                    state.Tree.Dispose();
                    parallelBuffer.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var agentTreeLookup = GetComponentDataFromEntity<AgentTreeComponent>(true);
            var inputDependency = Dependency;
            var treeOperations = _treeOperations;
            treeOperations.Clear();
            var treeOperationsWriter = treeOperations.AsParallelWriter();

            Entities
                .WithName("Insert_Agent")
                .WithBurst()
                .WithNone<AgentSystemStateComponent>()
                .WithReadOnly(agentTreeLookup)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref AgentTreeElementComponent element) =>
                {
                    var tree = agentTreeLookup[element.TreeEntity].Tree;
                    element.TreeRef = tree;
                    treeOperationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, translation.Value.xz, radius.Value, element.TreeEntity));
                })
                .ScheduleParallel();

            Entities
                .WithName("Destroy_Agent")
                .WithBurst()
                .WithNone<AgentTreeElementComponent>()
                .ForEach((AgentSystemStateComponent state) =>
                {
                    treeOperationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Destroy, state.Id));
                })
                .ScheduleParallel();

            Entities
                .WithName("Update_Agent")
                .WithBurst()
                .WithReadOnly(agentTreeLookup)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref AgentTreeElementComponent element, ref AgentSystemStateComponent state) =>
                {
                    var pos = translation.Value.xz;

                    if (element.TreeEntity == state.TreeEntity)
                    {
                        var displacement = pos - state.PreviousPosition;
                        treeOperationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Move, state.Id, pos, displacement, radius.Value));
                    }
                    else
                    {
                        var oldTree = state.TreeRef;
                        var newTree = agentTreeLookup[element.TreeEntity].Tree;
                        element.TreeRef = newTree;
                        state.TreeRef = newTree;
                        state.TreeEntity = element.TreeEntity;
                        treeOperationsWriter.Add(oldTree, new TreeOperation(TreeOperationType.Destroy, state.Id));
                        treeOperationsWriter.Add(newTree, new TreeOperation(TreeOperationType.Reinsert, entity, pos, radius.Value, element.TreeEntity));
                    }

                    state.PreviousPosition = pos;
                })
                .ScheduleParallel();

            var uniqueKeys = _uniqueKeys;

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    var result = treeOperations.GetKeyArray(Allocator.Temp);
                    result.Sort();
                    var uniques = result.Unique();
                    uniqueKeys.CopyFrom(result.GetSubArray(0, uniques));
                })
                .Schedule();

            Dependency = new TreeOperationJob
                {
                    Operations = treeOperations,
                    Keys = uniqueKeys.AsDeferredJobArray(),
                    Ecb = ecbSource.CreateCommandBuffer().AsParallelWriter()
                }
                .Schedule(uniqueKeys, 1, Dependency);

            ecbSource.AddJobHandleForProducer(Dependency);
            OutputDependecy = Dependency;
        }

        [BurstCompile]
        struct TreeOperationJob : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeArray<DynamicTree<Entity>> Keys;
            [ReadOnly]
            public NativeMultiHashMap<DynamicTree<Entity>, TreeOperation> Operations;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var tree = Keys[index];
                var enumerator = Operations.GetValuesForKey(tree);

                while (enumerator.MoveNext())
                {
                    var op = enumerator.Current;
                    switch (op.Type)
                    {
                        case TreeOperationType.Insert:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            var id = tree.CreateProxy(aabb, op.Agent);
                            var state = new AgentSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = tree};
                            Ecb.AddComponent(index, op.Agent, state);
                            break;
                        }
                        case TreeOperationType.Move:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            tree.MoveProxy(op.Id, aabb, op.Displacement);
                            break;
                        }
                        case TreeOperationType.Reinsert:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            var id = tree.CreateProxy(aabb, op.Agent);
                            var state = new AgentSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = tree};
                            Ecb.AddComponent(index, op.Agent, state);
                            break;
                        }
                        case TreeOperationType.Destroy:
                        {
                            tree.DestroyProxy(op.Id);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        readonly struct TreeOperation
        {
            public readonly TreeOperationType Type;
            public readonly int Id;
            public readonly Entity Agent;
            public readonly Entity TreeEntity;
            public readonly float2 Pos;
            public readonly float2 Displacement;
            public readonly float Radius;

            /// <summary>
            /// Insert
            /// </summary>
            public TreeOperation(TreeOperationType type, Entity agent, float2 pos, float radius, Entity treeEntity)
            {
                Type = type;
                Agent = agent;
                Pos = pos;
                Radius = radius;
                TreeEntity = treeEntity;
                Id = default;
                Displacement = default;
            }

            /// <summary>
            /// Destroy
            /// </summary>
            public TreeOperation(TreeOperationType type, int id)
            {
                Type = type;
                Id = id;
                Radius = default;
                Agent = default;
                Pos = default;
                Displacement = default;
                TreeEntity = default;
            }

            /// <summary>
            /// Move
            /// </summary>
            public TreeOperation(TreeOperationType type, int id, float2 pos, float2 displacement, float radius)
            {
                Type = type;
                Id = id;
                Pos = pos;
                Displacement = displacement;
                Radius = radius;
                Agent = default;
                TreeEntity = default;
            }
        }

        enum TreeOperationType
        {
            Insert = 3,
            Move = 2,
            Reinsert = 4,
            Destroy = 1,
        }

        struct TreeSystemStateComponent : ISystemStateComponentData
        {
            public DynamicTree<Entity> Tree;
        }

        struct AgentSystemStateComponent : ISystemStateComponentData
        {
            public int Id;
            public float2 PreviousPosition;
            public DynamicTree<Entity> TreeRef;
            public Entity TreeEntity;
        }
    }
}