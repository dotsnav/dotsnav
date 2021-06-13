using System;
using DotsNav.Collections;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    public unsafe class ObstacleTreeSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        NativeMultiHashMap<ObstacleTree, TreeOperation> _operations;
        NativeList<ObstacleTree> _trees;
        EntityQuery _insertQuery0;
        EntityQuery _insertQuery1;
        EntityQuery _insertQuery2;
        EntityQuery _insertQuery3;
        EntityQuery _insertQuery4;
        EntityQuery _insertQuery5;
        EntityQuery _insertQuery6;
        EntityQuery _insertQuery7;
        EntityQuery _destroyQuery;

        protected override void OnCreate()
        {
            _operations = new NativeMultiHashMap<ObstacleTree, TreeOperation>(64, Allocator.Persistent);
            _trees = new NativeList<ObstacleTree>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _operations.Dispose();
            _trees.Dispose();
            Entities.WithBurst().ForEach((TreeSystemStateComponent c) => c.Tree.Dispose()).Run();
        }

        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;

            var b0 = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithName("Allocate_Tree")
                .WithBurst()
                .WithNone<TreeSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ObstacleTreeComponent tree) =>
                {
                    tree.TreeRef = new ObstacleTree(Allocator.Persistent);
                    b0.AddComponent(entityInQueryIndex, entity, new TreeSystemStateComponent {Tree = tree.TreeRef});
                })
                .ScheduleParallel();

            var b1 = ecbSource.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithName("Dispose_Tree")
                .WithBurst()
                .WithNone<ObstacleTreeComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, TreeSystemStateComponent state) =>
                {
                    state.Tree.Dispose();
                    b1.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var treeLookup = GetComponentDataFromEntity<ObstacleTreeComponent>(true);

            var operations = _operations;
            var minCapacity = _insertQuery0.CalculateEntityCount() +
                              _insertQuery1.CalculateEntityCount() +
                              _insertQuery2.CalculateEntityCount() +
                              _insertQuery3.CalculateEntityCount() +
                              _insertQuery4.CalculateEntityCount() +
                              _insertQuery5.CalculateEntityCount() +
                              _insertQuery6.CalculateEntityCount() +
                              _insertQuery7.CalculateEntityCount() +
                              _destroyQuery.CalculateEntityCount();
            if (operations.Capacity < minCapacity)
                operations.Capacity = minCapacity;
            operations.Clear();
            var operationsWriter = operations.AsParallelWriter();


            // Without LocalToWorld
            Entities
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery0)
                .ForEach((Entity entity, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, float4x4.identity, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery1)
                .ForEach((Entity entity, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, float4x4.identity, (float2*) v.GetUnsafePtr(), v.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery2)
                .ForEach((DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.BulkInsert, float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery3)
                .ForEach((ObstacleBlobComponent blob, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.BulkInsert, float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            // With LocalToWorld
            Entities
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery4)
                .ForEach((Entity entity, LocalToWorld ltw, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, ltw.Value, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery5)
                .ForEach((Entity entity, LocalToWorld ltw, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.Insert, entity, ltw.Value, (float2*) v.GetUnsafePtr(), v.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery6)
                .ForEach((LocalToWorld ltw, DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.BulkInsert, ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithReadOnly(treeLookup)
                .WithStoreEntityQueryInField(ref _insertQuery7)
                .ForEach((LocalToWorld ltw, ObstacleBlobComponent blob, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(tree, new TreeOperation(TreeOperationType.BulkInsert, ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();


            Entities
                .WithName("Destroy")
                .WithBurst()
                .WithNone<ObstacleTreeElementComponent>()
                .WithStoreEntityQueryInField(ref _destroyQuery)
                .ForEach((Entity entity, ElementSystemStateComponent state) =>
                {
                    operationsWriter.Add(state.TreeRef, new TreeOperation(TreeOperationType.Destroy, entity));
                })
                .ScheduleParallel();

            var trees = _trees;
            var set = new HashSet<ObstacleTree>(32, Allocator.TempJob);

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
                        case TreeOperationType.BulkInsert:
                        {
                            var offset = 0;
                            for (int i = 0; i < op.Amount; i++)
                            {
                                var amount = op.Amounts[i];
                                tree.InsertObstacle(op.Obstacle, op.Ltw, op.Vertices + offset, amount);
                                offset += amount;
                            }
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
            public readonly float4x4 Ltw;
            public readonly float2* Vertices;
            public readonly int* Amounts;
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
                Amounts = default;
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
                Amounts = default;
            }

            public TreeOperation(TreeOperationType type, float4x4 ltw, float2* verts, int* amounts, int length)
            {
                Type = type;
                Obstacle = default;
                Ltw = ltw;
                Vertices = verts;
                Amount = length;
                Amounts = amounts;
            }
        }

        enum TreeOperationType
        {
            Insert,
            BulkInsert,
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