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
    public unsafe partial class ObstacleTreeSystem : SystemBase
    {
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
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;

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
                    if (state.Tree.Count > 0)
                        return;

                    state.Tree.Dispose();
                    b1.RemoveComponent<TreeSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var treeLookup = GetComponentDataFromEntity<ObstacleTreeComponent>(true);
            var localToWorldLookup = GetComponentDataFromEntity<LocalToWorld>(true);

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
                .WithNone<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery0)
                .ForEach((Entity entity, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                    operationsWriter.Add(tree, new TreeOperation(entity, transform, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ElementSystemStateComponent>()
                .WithNone<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery1)
                .ForEach((Entity entity, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(tree, new TreeOperation(entity, transform, (float2*) v.GetUnsafePtr(), v.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery2)
                .ForEach((DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                    operationsWriter.Add(tree, new TreeOperation(transform, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery3)
                .ForEach((ObstacleBlobComponent blob, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.inverse(localToWorldLookup[element.Tree].Value);
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(tree, new TreeOperation(transform, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            // With LocalToWorld
            Entities
                .WithBurst()
                .WithAll<LocalToWorld>()
                .WithNone<ElementSystemStateComponent>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery4)
                .ForEach((Entity entity, DynamicBuffer<VertexElement> vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.mul(math.inverse(localToWorldLookup[element.Tree].Value), localToWorldLookup[entity].Value);
                    operationsWriter.Add(tree, new TreeOperation(entity, transform, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithAll<LocalToWorld>()
                .WithNone<ElementSystemStateComponent>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery5)
                .ForEach((Entity entity, VertexBlobComponent vertices, ref ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.mul(math.inverse(localToWorldLookup[element.Tree].Value), localToWorldLookup[entity].Value);
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(tree, new TreeOperation(entity, transform, (float2*) v.GetUnsafePtr(), v.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithAll<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery6)
                .ForEach((Entity entity, DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.mul(math.inverse(localToWorldLookup[element.Tree].Value), localToWorldLookup[entity].Value);
                    operationsWriter.Add(tree, new TreeOperation(transform, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithAll<LocalToWorld>()
                .WithReadOnly(treeLookup)
                .WithReadOnly(localToWorldLookup)
                .WithStoreEntityQueryInField(ref _insertQuery7)
                .ForEach((Entity entity, ObstacleBlobComponent blob, ObstacleTreeElementComponent element) =>
                {
                    var tree = treeLookup[element.Tree].TreeRef;
                    var transform = math.mul(math.inverse(localToWorldLookup[element.Tree].Value), localToWorldLookup[entity].Value);
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(tree, new TreeOperation(transform, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();


            Entities
                .WithName("Destroy")
                .WithBurst()
                .WithNone<ObstacleTreeElementComponent>()
                .WithStoreEntityQueryInField(ref _destroyQuery)
                .ForEach((Entity entity, ElementSystemStateComponent state) =>
                {
                    operationsWriter.Add(state.TreeRef, new TreeOperation(entity));
                })
                .ScheduleParallel();

            var trees = _trees;

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    var set = new NativeHashSet<ObstacleTree>(32, Allocator.Temp);
                    var enumerator = operations.GetKeyArray(Allocator.Temp);
                    for (int i = 0; i < enumerator.Length; i++)
                        set.Add(enumerator[i]);
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
            public TreeOperation(Entity obstacle, float4x4 ltw, float2* vertices, int amount)
            {
                Type = TreeOperationType.Insert;
                Obstacle = obstacle;
                Ltw = ltw;
                Vertices = vertices;
                Amount = amount;
                Amounts = default;
            }

            /// <summary>
            /// Destroy
            /// </summary>
            public TreeOperation(Entity obstacle)
            {
                Type = TreeOperationType.Destroy;
                Obstacle = obstacle;
                Ltw = default;
                Vertices = default;
                Amount = default;
                Amounts = default;
            }

            /// <summary>
            /// Bulk Insert
            /// </summary>
            public TreeOperation(float4x4 ltw, float2* verts, int* amounts, int length)
            {
                Type = TreeOperationType.BulkInsert;
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