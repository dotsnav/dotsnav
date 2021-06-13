using System;
using DotsNav.Collections;
using DotsNav.Data;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.Navmesh.Systems
{
    /// <summary>
    /// Queued insertion and removal of obstacles are processed by this system
    /// </summary>
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    public unsafe class UpdateNavmeshSystem : SystemBase
    {
        public JobHandle OutputDependecy;
        NativeMultiHashMap<Entity, Operation> _operations;
        NativeMultiHashMap<Entity, Entity> _removals;
        NativeList<Entity> _trees;
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
            _operations = new NativeMultiHashMap<Entity, Operation>(64, Allocator.Persistent);
            _removals = new NativeMultiHashMap<Entity, Entity>(64, Allocator.Persistent);
            _trees = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _operations.Dispose();
            _removals.Dispose();
            _trees.Dispose();
        }

        protected override void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;

            var operations = _operations;
            var minCapacity = _insertQuery0.CalculateEntityCount() +
                              _insertQuery1.CalculateEntityCount() +
                              _insertQuery2.CalculateEntityCount() +
                              _insertQuery3.CalculateEntityCount() +
                              _insertQuery4.CalculateEntityCount() +
                              _insertQuery5.CalculateEntityCount() +
                              _insertQuery6.CalculateEntityCount() +
                              _insertQuery7.CalculateEntityCount();
            if (operations.Capacity < minCapacity)
                operations.Capacity = minCapacity;
            operations.Clear();
            var operationsWriter = operations.AsParallelWriter();

            var removals = _removals;
            minCapacity = _destroyQuery.CalculateEntityCount();
            if (removals.Capacity < minCapacity)
                removals.Capacity = minCapacity;
            removals.Clear();
            var removalsWriter = removals.AsParallelWriter();


            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();

            // Insert Without LocalToWorld
            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery0)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<VertexElement> vertices, ref NavmeshObstacleComponent element) =>
                {
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.Insert, entity, float4x4.identity, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {TreeRef = element.Navmesh});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery1)
                .ForEach((Entity entity, int entityInQueryIndex, VertexBlobComponent vertices, ref NavmeshObstacleComponent element) =>
                {
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.Insert, entity, float4x4.identity, (float2*) v.GetUnsafePtr(), v.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {TreeRef = element.Navmesh});
                })
                .ScheduleParallel();

            // Bulk Insert Without LocalToWorld
            Entities
                .WithBurst()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery2)
                .ForEach((DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, NavmeshObstacleComponent element) =>
                {
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.BulkInsert, float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery3)
                .ForEach((ObstacleBlobComponent blob, NavmeshObstacleComponent element) =>
                {
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.BulkInsert, float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            // Insert With LocalToWorld
            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithStoreEntityQueryInField(ref _insertQuery4)
                .ForEach((Entity entity, int entityInQueryIndex, LocalToWorld ltw, DynamicBuffer<VertexElement> vertices, ref NavmeshObstacleComponent element) =>
                {
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.Insert, entity, ltw.Value, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {TreeRef = element.Navmesh});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithStoreEntityQueryInField(ref _insertQuery5)
                .ForEach((Entity entity, int entityInQueryIndex, LocalToWorld ltw, VertexBlobComponent vertices, ref NavmeshObstacleComponent element) =>
                {
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.Insert, entity, ltw.Value, (float2*) v.GetUnsafePtr(), v.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {TreeRef = element.Navmesh});
                })
                .ScheduleParallel();

            // Bulk Insert With LocalToWorld
            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref _insertQuery6)
                .ForEach((LocalToWorld ltw, DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, NavmeshObstacleComponent element) =>
                {
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.BulkInsert, ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref _insertQuery7)
                .ForEach((LocalToWorld ltw, ObstacleBlobComponent blob, NavmeshObstacleComponent element) =>
                {
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    operationsWriter.Add(element.Navmesh, new Operation(OperationType.BulkInsert, ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();


            Entities
                .WithName("Destroy")
                .WithBurst()
                .WithNone<NavmeshObstacleComponent>()
                .WithStoreEntityQueryInField(ref _destroyQuery)
                .ForEach((Entity entity, int entityInQueryIndex, SystemStateComponent state) =>
                {
                    removalsWriter.Add(state.TreeRef, entity);
                    buffer.RemoveComponent<SystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var trees = _trees;
            var set = new HashSet<Entity>(32, Allocator.TempJob);

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    var enumerator = operations.GetKeyArray(Allocator.Temp);
                    for (int i = 0; i < enumerator.Length; i++)
                        set.TryAdd(enumerator[i]);
                    enumerator = removals.GetKeyArray(Allocator.Temp);
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
                    Removals = removals,
                    Keys = trees.AsDeferredJobArray(),
                    NavmeshLookup = GetComponentDataFromEntity<NavmeshComponent>(true),
                    DestroyedLookup = GetBufferFromEntity<DestroyedTriangleElement>(true)
                }
                .Schedule(trees, 1, Dependency);

            ecbSource.AddJobHandleForProducer(Dependency);
            OutputDependecy = Dependency;
        }

        [BurstCompile]
        struct TreeOperationJob : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeArray<Entity> Keys;
            [ReadOnly]
            public NativeMultiHashMap<Entity, Operation> Operations;
            [ReadOnly]
            public ComponentDataFromEntity<NavmeshComponent> NavmeshLookup;
            [NativeDisableContainerSafetyRestriction]
            public BufferFromEntity<DestroyedTriangleElement> DestroyedLookup;
            [ReadOnly]
            public NativeMultiHashMap<Entity, Entity> Removals;

            public void Execute(int index)
            {
                var entity = Keys[index];
                var tree = NavmeshLookup[entity];
                var enumerator = Operations.GetValuesForKey(entity);
                var destroyed = DestroyedLookup[entity];

                if (tree.Navmesh->IsEmpty)
                    tree.Navmesh->Load(enumerator, destroyed);
                else
                    tree.Navmesh->Update(enumerator, Removals.GetValuesForKey(entity), destroyed);
            }
        }

        internal readonly struct Operation
        {
            public readonly OperationType Type;
            public readonly Entity Obstacle;
            public readonly float4x4 Ltw;
            public readonly float2* Vertices;
            public readonly int* Amounts;
            public readonly int Amount;

            /// <summary>
            /// Insert
            /// </summary>
            public Operation(OperationType type, Entity obstacle, float4x4 ltw, float2* vertices, int amount)
            {
                Type = type;
                Obstacle = obstacle;
                Ltw = ltw;
                Vertices = vertices;
                Amount = amount;
                Amounts = default;
            }

            /// <summary>
            /// Bulk Insert
            /// </summary>
            public Operation(OperationType type, float4x4 ltw, float2* verts, int* amounts, int length)
            {
                Type = type;
                Obstacle = default;
                Ltw = ltw;
                Vertices = verts;
                Amount = length;
                Amounts = amounts;
            }
        }

        internal enum OperationType
        {
            Insert,
            BulkInsert,
        }

        struct SystemStateComponent : ISystemStateComponentData
        {
            public Entity TreeRef;
        }
    }
}