using System;
using DotsNav.Collections;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    public unsafe class ObstacleTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        NativeMultiHashMap<ObstacleTree, TreeOperation> _operations;
        NativeList<ObstacleTree> _trees;
        EntityQuery _insertQuery;
        EntityQuery _destroyQuery;
        NativeArray<JobHandle> _dependencies;

        protected override void OnCreate()
        {
            _operations = new NativeMultiHashMap<ObstacleTree, TreeOperation>(64, Allocator.Persistent);
            _trees = new NativeList<ObstacleTree>(Allocator.Persistent);
            _dependencies = new NativeArray<JobHandle>(3, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _operations.Dispose();
            _trees.Dispose();
            _dependencies.Dispose();
            Entities.WithBurst().ForEach((TreeSystemStateComponent c) => c.Tree.Dispose()).Run();
        }

        // todo support obstacle blobs
        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;

            var parallelBuffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            _dependencies[0] =
                Entities
                    .WithName("Allocate_Tree")
                    .WithBurst()
                    .WithNone<TreeSystemStateComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex, ref ObstacleTreeComponent tree) =>
                    {
                        tree.TreeRef = new ObstacleTree(Allocator.Persistent);
                        parallelBuffer.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = tree.TreeRef});
                    })
                    .ScheduleParallel(Dependency);

            parallelBuffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            _dependencies[1] =
                Entities
                    .WithName("Dispose_Tree")
                    .WithBurst()
                    .WithNone<ObstacleTreeComponent>()
                    .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                    {
                        state.Tree.Dispose();
                        parallelBuffer.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                    })
                    .ScheduleParallel(Dependency);

            var treeLookup = GetComponentDataFromEntity<ObstacleTreeComponent>(true);

            var operations = _operations;
            var minCapacity = _insertQuery.CalculateEntityCount() + _destroyQuery.CalculateEntityCount();
            if (operations.Capacity < minCapacity)
                operations.Capacity = minCapacity;
            operations.Clear();
            var operationsWriter = operations.AsParallelWriter();

            _dependencies[0] =
                Entities
                    .WithBurst()
                    .WithNone<ElementSystemStateComponent>()
                    .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                    .WithReadOnly(treeLookup)
                    .WithStoreEntityQueryInField(ref _insertQuery)
                    .WithNativeDisableContainerSafetyRestriction(operationsWriter) // todo fix properly
                    .ForEach((Entity entity, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                    {
                        var tree = treeLookup[element.Tree].TreeRef;
                        operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, float4x4.identity, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    })
                    .ScheduleParallel(_dependencies[0]);

              _dependencies[0] =
                Entities
                    .WithBurst()
                    .WithNone<ElementSystemStateComponent>()
                    .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                    .WithReadOnly(treeLookup)
                    .WithStoreEntityQueryInField(ref _insertQuery)
                    .WithNativeDisableContainerSafetyRestriction(operationsWriter) // todo fix properly
                    .ForEach((Entity entity, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                    {
                        var tree = treeLookup[element.Tree].TreeRef;
                        ref var v = ref vertices.BlobRef.Value.Vertices;
                        operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, float4x4.identity, (float2*) v.GetUnsafePtr(), v.Length));
                    })
                    .ScheduleParallel(_dependencies[0]);

                _dependencies[0] =
                Entities
                    .WithBurst()
                    .WithNone<ElementSystemStateComponent>()
                    .WithReadOnly(treeLookup)
                    .WithStoreEntityQueryInField(ref _insertQuery)
                    .WithNativeDisableContainerSafetyRestriction(operationsWriter) // todo fix properly
                    .ForEach((Entity entity, LocalToWorld ltw, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                    {
                        var tree = treeLookup[element.Tree].TreeRef;
                        operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, ltw.Value, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    })
                    .ScheduleParallel(_dependencies[0]);

              _dependencies[0] =
                Entities
                    .WithBurst()
                    .WithNone<ElementSystemStateComponent>()
                    .WithReadOnly(treeLookup)
                    .WithStoreEntityQueryInField(ref _insertQuery)
                    .WithNativeDisableContainerSafetyRestriction(operationsWriter) // todo fix properly
                    .ForEach((Entity entity, LocalToWorld ltw, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                    {
                        var tree = treeLookup[element.Tree].TreeRef;
                        ref var v = ref vertices.BlobRef.Value.Vertices;
                        operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, ltw.Value, (float2*) v.GetUnsafePtr(), v.Length));
                    })
                    .ScheduleParallel(_dependencies[0]);


            _dependencies[0] =
                Entities
                    .WithName("Destroy")
                    .WithBurst()
                    .WithNone<ObstacleTreeElementComponent>()
                    .WithStoreEntityQueryInField(ref _destroyQuery)
                    .WithNativeDisableContainerSafetyRestriction(operationsWriter) // todo fix properly
                    .ForEach((Entity entity, ElementSystemStateComponent state) =>
                    {
                        operationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Destroy, entity));
                    })
                    .ScheduleParallel(_dependencies[0]);

            Dependency = JobHandle.CombineDependencies(_dependencies);

            var trees = _trees;
            var set = new HashSet<ObstacleTree>(1024, Allocator.TempJob);

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
            public NativeArray<ObstacleTree> Keys;
            [ReadOnly]
            public NativeMultiHashMap<ObstacleTree, TreeOperation> Operations;

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
                            tree.InsertObstacle(op.Obstacle, op.Ltw, op.Vertices, op.Amount);
                            var state = new ElementSystemStateComponent{TreeRef = tree};
                            Ecb.AddComponent(index, op.Obstacle, state);
                            break;
                        }
                        case TreeOperationType.Destroy:
                        {
                            tree.RemoveObstacle(op.Obstacle);
                            Ecb.RemoveComponent<ElementSystemStateComponent>(index, op.Obstacle);
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
            public readonly Entity Obstacle;
            public readonly float4x4 Ltw; // todo refactor
            public readonly float2* Vertices;
            public readonly int Amount;

            /// <summary>
            /// Insert
            /// </summary>
            public TreeOperation(TreeOperationType type, Entity obstacle, float4x4 ltw, float2* vertices, int amount)
            {
                Type = type;
                Obstacle = obstacle;
                Ltw = ltw;
                Vertices = vertices;
                Amount = amount;
            }

            /// <summary>
            /// Destroy
            /// </summary>
            public TreeOperation(TreeOperationType type, Entity obstacle)
            {
                Type = type;
                Obstacle = obstacle;
                Ltw = default;
                Vertices = default;
                Amount = default;
            }
        }

        enum TreeOperationType
        {
            Insert,
            Destroy
        }

        struct TreeSystemStateComponent : ISystemStateComponentData
        {
            public ObstacleTree Tree;
        }

        struct ElementSystemStateComponent : ISystemStateComponentData
        {
            public ObstacleTree TreeRef;
        }
    }
}