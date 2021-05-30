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
        NativeQueue<TreeOperation> _treeOperations;

        protected override void OnCreate()
        {
            _treeOperations = new NativeQueue<TreeOperation>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _treeOperations.Dispose();
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

            var treeOperations = _treeOperations;
            var treeOperationsWriter = treeOperations.AsParallelWriter();
            var agentTreeLookup = GetComponentDataFromEntity<AgentTreeComponent>(true);
            var inputDependency = Dependency;

            Entities
                .WithName("Insert_Agent")
                .WithBurst()
                .WithNone<AgentSystemStateComponent>()
                .WithReadOnly(agentTreeLookup)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref AgentTreeElementComponent element) =>
                {
                    var tree = agentTreeLookup[element.Tree].Tree;
                    element.TreeRef = tree;
                    treeOperationsWriter.Enqueue(new TreeOperation(TreeOperationType.Insert, tree, entity, translation.Value.xz, radius.Value, element.Tree));
                })
                .ScheduleParallel();

            Entities
                .WithName("Destroy_Agent")
                .WithBurst()
                .WithNone<AgentTreeElementComponent>()
                .ForEach((AgentSystemStateComponent state) =>
                {
                    treeOperationsWriter.Enqueue(new TreeOperation(TreeOperationType.Destroy, state.TreeRef, state.Id));
                })
                .ScheduleParallel();

            Entities
                .WithName("Update_Agent")
                .WithBurst()
                .WithReadOnly(agentTreeLookup)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref AgentTreeElementComponent element, ref AgentSystemStateComponent state) =>
                {
                    var pos = translation.Value.xz;

                    if (element.Tree == state.TreeEntity)
                    {
                        var displacement = pos - state.PreviousPosition;
                        treeOperationsWriter.Enqueue(new TreeOperation(TreeOperationType.Move, element.TreeRef, state.Id, pos, displacement, radius.Value));
                    }
                    else
                    {
                        var oldTree = state.TreeRef;
                        var newTree = agentTreeLookup[element.Tree].Tree;
                        element.TreeRef = newTree;
                        state.TreeRef = newTree;
                        state.TreeEntity = element.Tree;
                        treeOperationsWriter.Enqueue(new TreeOperation(TreeOperationType.Destroy, oldTree, state.Id));
                        treeOperationsWriter.Enqueue(new TreeOperation(TreeOperationType.Reinsert, newTree, entity, pos, radius.Value, element.Tree));
                    }

                    state.PreviousPosition = pos;
                })
                .ScheduleParallel();

            var sorted = new NativeList<TreeOperation>(Allocator.TempJob);
            var chunks = new NativeList<int2>(Allocator.TempJob);

            Job
                .WithName("Sort_TreeOperations")
                .WithBurst()
                .WithCode(() =>
                {
                    var t = treeOperations.ToArray(Allocator.Temp);
                    sorted.CopyFrom(t);
                    treeOperations.Clear();

                    if (sorted.Length < 2)
                    {
                        if (sorted.Length == 1)
                            chunks.Add(new int2(0, 1));
                        return;
                    }

                    sorted.Sort(new TreeOperation.Comparer());

                    var prev = sorted[0].Tree;
                    var start = 0;
                    var amount = 1;
                    for (int i = 1; i < sorted.Length; i++)
                    {
                        var tree = sorted[i].Tree;
                        if (tree.Equals(prev))
                        {
                            ++amount;
                        }
                        else
                        {
                            chunks.Add(new int2(start, start += amount));
                            amount = 1;
                            prev = tree;
                        }
                    }
                    chunks.Add(new int2(start, start + amount));
                })
                .Schedule();

            Dependency = new TreeOperationJob
                {
                    Operations = sorted.AsDeferredJobArray(),
                    Chunks = chunks.AsDeferredJobArray(),
                    Ecb = ecbSource.CreateCommandBuffer().AsParallelWriter()
                }
                .Schedule(chunks, 1, Dependency);

            ecbSource.AddJobHandleForProducer(Dependency);
            OutputDependecy = Dependency;
        }

        [BurstCompile]
        struct TreeOperationJob : IJobParallelForDefer
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<TreeOperation> Operations;
            public NativeArray<int2> Chunks;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var chunk = Chunks[index];
                for (int i = chunk.x; i < chunk.y; i++)
                {
                    var op = Operations[i];
                    switch (op.Type)
                    {
                        case TreeOperationType.Insert:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            var id = op.Tree.CreateProxy(aabb, op.Agent);
                            var state = new AgentSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = op.Tree};
                            Ecb.AddComponent(i, op.Agent, state);
                            break;
                        }
                        case TreeOperationType.Move:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            op.Tree.MoveProxy(op.Id, aabb, op.Displacement);
                            break;
                        }
                        case TreeOperationType.Reinsert:
                        {
                            var aabb = new AABB(op.Pos, op.Radius);
                            var id = op.Tree.CreateProxy(aabb, op.Agent);
                            var state = new AgentSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = op.Tree};
                            Ecb.AddComponent(i, op.Agent, state);
                            break;
                        }
                        case TreeOperationType.Destroy:
                        {
                            op.Tree.DestroyProxy(op.Id);
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
            public readonly DynamicTree<Entity> Tree;
            public readonly int Id;
            public readonly Entity Agent;
            public readonly Entity TreeEntity;
            public readonly float2 Pos;
            public readonly float2 Displacement;
            public readonly float Radius;

            /// <summary>
            /// Insert
            /// </summary>
            public TreeOperation(TreeOperationType type, DynamicTree<Entity> tree, Entity agent, float2 pos, float radius, Entity treeEntity)
            {
                Type = type;
                Tree = tree;
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
            public TreeOperation(TreeOperationType type, DynamicTree<Entity> tree, int id)
            {
                Type = type;
                Tree = tree;
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
            public TreeOperation(TreeOperationType type, DynamicTree<Entity> tree, int id, float2 pos, float2 displacement, float radius)
            {
                Type = type;
                Tree = tree;
                Id = id;
                Pos = pos;
                Displacement = displacement;
                Radius = radius;
                Agent = default;
                TreeEntity = default;
            }

            public struct Comparer : IComparer<TreeOperation>
            {
                public int Compare(TreeOperation x, TreeOperation y) => x.Tree.Equals(y.Tree) ? x.Type - y.Type : x.Tree.CompareTo(y.Tree);
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