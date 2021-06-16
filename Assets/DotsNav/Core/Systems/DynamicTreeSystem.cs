using System;
using DotsNav.BVH;
using DotsNav.Collections;
using DotsNav.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    class DynamicTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        NativeMultiHashMap<DynamicTree<Entity>, TreeOperation> _operations;
        NativeList<DynamicTree<Entity>> _trees;
        EntityQuery _insertQuery;
        EntityQuery _destroyQuery;
        EntityQuery _updateQuery;

        protected override void OnCreate()
        {
            _operations = new NativeMultiHashMap<DynamicTree<Entity>, TreeOperation>(64, Allocator.Persistent);
            _trees = new NativeList<DynamicTree<Entity>>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _operations.Dispose();
            _trees.Dispose();
            Entities.WithBurst().ForEach((TreeSystemStateComponent c) => c.Tree.Dispose()).Run();
        }

        protected override void OnUpdate()
        {
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;

            var b0 = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithName("Allocate_Tree")
                .WithBurst()
                .WithNone<TreeSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicTreeComponent agentTree) =>
                {
                    agentTree.Tree = new DynamicTree<Entity>(Allocator.Persistent);
                    b0.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = agentTree.Tree});
                })
                .ScheduleParallel();

            var b1 = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithName("Dispose_Tree")
                .WithBurst()
                .WithNone<DynamicTreeComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                {
                    state.Tree.Dispose();
                    b1.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var treeLookup = GetComponentDataFromEntity<DynamicTreeComponent>(true);

            var operations = _operations;
            var minCapacity = _insertQuery.CalculateEntityCount() + _destroyQuery.CalculateEntityCount() + 2 * _updateQuery.CalculateEntityCount();
            if (operations.Capacity < minCapacity)
                operations.Capacity = minCapacity;
            operations.Clear();
            var operationsWriter = operations.AsParallelWriter();
            var localToWorldLookup = GetComponentDataFromEntity<LocalToWorld>(true);

            Entities
                .WithName("Insert")
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref DynamicTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].Tree;
                    element.TreeRef = tree;
                    var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                    var pos = math.transform(transform, translation.Value).xz;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, pos, radius.Value, element.Tree));
                })
                .ScheduleParallel();

            Entities
                .WithName("Destroy")
                .WithBurst()
                .WithNone<DynamicTreeElementComponent>()
                .WithStoreEntityQueryInField(ref _destroyQuery)
                .ForEach((ElementSystemStateComponent state) =>
                {
                    operationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Destroy, state.Id));
                })
                .ScheduleParallel();

            Entities
                .WithName("Update")
                .WithBurst()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _updateQuery)
                .ForEach((Entity entity, Translation translation, RadiusComponent radius, ref DynamicTreeElementComponent element, ref ElementSystemStateComponent state) =>
                {
                    if (element.Tree == state.TreeEntity)
                    {
                        var transform = math.inverse(localToWorldLookup[state.TreeEntity].Value);
                        var pos = math.transform(transform, translation.Value).xz;
                        var displacement = pos - state.PreviousPosition;
                        operationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Move, state.Id, pos, displacement, radius.Value));
                        state.PreviousPosition = pos;
                    }
                    else
                    {
                        var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                        var pos = math.transform(transform, translation.Value).xz;
                        var oldTree = state.TreeRef;
                        var newTree = treeLookup[element.Tree].Tree;
                        element.TreeRef = newTree;
                        state.TreeRef = newTree;
                        state.TreeEntity = element.Tree;
                        operationsWriter.Add(oldTree, new TreeOperation(TreeOperationType.Destroy, state.Id));
                        operationsWriter.Add(newTree, new TreeOperation(TreeOperationType.Reinsert, entity, pos, radius.Value, element.Tree));
                        state.PreviousPosition = pos;
                    }
                })
                .ScheduleParallel();

            var trees = _trees;
            var set = new HashSet<DynamicTree<Entity>>(32, Allocator.TempJob);

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    var enumerator = operations.GetKeyArray(Allocator.Temp);
                    for (int i = 0; i < enumerator.Length; i++)
                        set.TryAdd(enumerator[i]);
                    trees.Clear();
                    var e2 = set.GetEnumerator();
                    while (e2.MoveNext())
                        trees.Add(e2.Current);
                })
                .Schedule();

            Dependency = new TreeOperationJob
                {
                    Operations = operations,
                    Keys = trees.AsDeferredJobArray(),
                    Ecb = ecbSource.CreateCommandBuffer().AsParallelWriter()
                }
                .Schedule(trees, 1, Dependency);

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
                            var id = tree.CreateProxy(AABB.FromRadius(op.Pos, op.Radius), op.Agent);
                            var state = new ElementSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = tree};
                            Ecb.AddComponent(index, op.Agent, state);
                            break;
                        }
                        case TreeOperationType.Move:
                        {
                            tree.MoveProxy(op.Id, AABB.FromRadius(op.Pos, op.Radius), op.Displacement);
                            break;
                        }
                        case TreeOperationType.Reinsert:
                        {
                            var id = tree.CreateProxy(AABB.FromRadius(op.Pos, op.Radius), op.Agent);
                            var state = new ElementSystemStateComponent{Id = id, PreviousPosition = op.Pos, TreeEntity = op.TreeEntity, TreeRef = tree};
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
            Insert,
            Move,
            Reinsert,
            Destroy
        }

        struct TreeSystemStateComponent : ISystemStateComponentData
        {
            public DynamicTree<Entity> Tree;
        }

        struct ElementSystemStateComponent : ISystemStateComponentData
        {
            public int Id;
            public float2 PreviousPosition;
            public DynamicTree<Entity> TreeRef;
            public Entity TreeEntity;
        }
    }
}