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
        NativeList<Entity> _navmeshes;
        NativeMultiHashMap<Entity, Navmesh.Insertion> _insertions;
        NativeMultiHashMap<Entity, Entity> _removals;
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
            _navmeshes = new NativeList<Entity>(Allocator.Persistent);
            _insertions = new NativeMultiHashMap<Entity, Navmesh.Insertion>(64, Allocator.Persistent);
            _removals = new NativeMultiHashMap<Entity, Entity>(64, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _navmeshes.Dispose();
            _insertions.Dispose();
            _removals.Dispose();
        }

        protected override void OnUpdate()
        {
            var ecbSource = World.GetOrCreateSystem<DotsNavSystemGroup>().EcbSource;

            var insertions = _insertions;
            var minCapacity = _insertQuery0.CalculateEntityCount() +
                              _insertQuery1.CalculateEntityCount() +
                              _insertQuery2.CalculateEntityCount() +
                              _insertQuery3.CalculateEntityCount() +
                              _insertQuery4.CalculateEntityCount() +
                              _insertQuery5.CalculateEntityCount() +
                              _insertQuery6.CalculateEntityCount() +
                              _insertQuery7.CalculateEntityCount();
            if (insertions.Capacity < minCapacity)
                insertions.Capacity = minCapacity;
            insertions.Clear();
            var insertionsWriter = insertions.AsParallelWriter();

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
                .WithNone<LocalToWorld>()
                .WithNone<VertexAmountElement>()
                .WithStoreEntityQueryInField(ref _insertQuery0)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<VertexElement> vertices, ref NavmeshObstacleComponent element) =>
                {
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(entity, float4x4.identity, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {Navmesh = element.Navmesh});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithNone<LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery1)
                .ForEach((Entity entity, int entityInQueryIndex, VertexBlobComponent vertices, ref NavmeshObstacleComponent element) =>
                {
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(entity, float4x4.identity, (float2*) v.GetUnsafePtr(), v.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {Navmesh = element.Navmesh});
                })
                .ScheduleParallel();

            // Bulk Insert Without LocalToWorld
            Entities
                .WithBurst()
                .WithNone<LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery2)
                .ForEach((DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, NavmeshObstacleComponent element) =>
                {
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<LocalToWorld>()
                .WithStoreEntityQueryInField(ref _insertQuery3)
                .ForEach((ObstacleBlobComponent blob, NavmeshObstacleComponent element) =>
                {
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(float4x4.identity, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            // Insert With LocalToWorld
            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithNone<VertexAmountElement>()
                .WithStoreEntityQueryInField(ref _insertQuery4)
                .ForEach((Entity entity, int entityInQueryIndex, LocalToWorld ltw, DynamicBuffer<VertexElement> vertices, ref NavmeshObstacleComponent element) =>
                {
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(entity, ltw.Value, (float2*) vertices.GetUnsafeReadOnlyPtr(), vertices.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {Navmesh = element.Navmesh});
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<SystemStateComponent>()
                .WithStoreEntityQueryInField(ref _insertQuery5)
                .ForEach((Entity entity, int entityInQueryIndex, LocalToWorld ltw, VertexBlobComponent vertices, ref NavmeshObstacleComponent element) =>
                {
                    ref var v = ref vertices.BlobRef.Value.Vertices;
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(entity, ltw.Value, (float2*) v.GetUnsafePtr(), v.Length));
                    buffer.AddComponent(entityInQueryIndex, entity, new SystemStateComponent {Navmesh = element.Navmesh});
                })
                .ScheduleParallel();

            // Bulk Insert With LocalToWorld
            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref _insertQuery6)
                .ForEach((LocalToWorld ltw, DynamicBuffer<VertexElement> v, DynamicBuffer<VertexAmountElement> a, NavmeshObstacleComponent element) =>
                {
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithStoreEntityQueryInField(ref _insertQuery7)
                .ForEach((LocalToWorld ltw, ObstacleBlobComponent blob, NavmeshObstacleComponent element) =>
                {
                    ref var v = ref blob.BlobRef.Value.Vertices;
                    ref var a = ref blob.BlobRef.Value.Amounts;
                    insertionsWriter.Add(element.Navmesh, new Navmesh.Insertion(ltw.Value, (float2*) v.GetUnsafePtr(), (int*) a.GetUnsafePtr(), a.Length));
                })
                .ScheduleParallel();


            Entities
                .WithName("Destroy")
                .WithBurst()
                .WithNone<NavmeshObstacleComponent>()
                .WithStoreEntityQueryInField(ref _destroyQuery)
                .ForEach((Entity entity, int entityInQueryIndex, SystemStateComponent state) =>
                {
                    removalsWriter.Add(state.Navmesh, entity);
                    buffer.RemoveComponent<SystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var navmeshes = _navmeshes;
            var set = new HashSet<Entity>(32, Allocator.TempJob);

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    var ops = insertions.GetKeyArray(Allocator.Temp);
                    for (int i = 0; i < ops.Length; i++)
                        set.TryAdd(ops[i]);

                    ops = removals.GetKeyArray(Allocator.Temp);
                    for (int i = 0; i < ops.Length; i++)
                        set.TryAdd(ops[i]);

                    navmeshes.Clear();
                    var keys = set.GetEnumerator();
                    while (keys.MoveNext())
                        navmeshes.Add(keys.Current);
                })
                .Schedule();

            Dependency = new TreeOperationJob
                {
                    Operations = insertions,
                    Removals = removals,
                    Keys = navmeshes.AsDeferredJobArray(),
                    NavmeshLookup = GetComponentDataFromEntity<NavmeshComponent>(),
                    LocalToWorldLookup = GetComponentDataFromEntity<LocalToWorld>(true),
                    DestroyedLookup = GetBufferFromEntity<DestroyedTriangleElement>(true)
                }
                .Schedule(navmeshes, 1, Dependency);

            ecbSource.AddJobHandleForProducer(Dependency);
        }

        [BurstCompile]
        struct TreeOperationJob : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeArray<Entity> Keys;
            [ReadOnly]
            public NativeMultiHashMap<Entity, Navmesh.Insertion> Operations;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<NavmeshComponent> NavmeshLookup;
            [NativeDisableContainerSafetyRestriction]
            public BufferFromEntity<DestroyedTriangleElement> DestroyedLookup;
            [ReadOnly]
            public NativeMultiHashMap<Entity, Entity> Removals;
            [ReadOnly]
            public ComponentDataFromEntity<LocalToWorld> LocalToWorldLookup;

            public void Execute(int index)
            {
                var entity = Keys[index];

                // todo this occurs when all entities are destroyed, is there a better way to prevent this?
                if (!NavmeshLookup.HasComponent(entity))
                    return;

                var navmesh = NavmeshLookup[entity];
                var insertions = Operations.GetValuesForKey(entity);
                var destroyedTriangles = DestroyedLookup[entity];
                var ltwInv = math.inverse(LocalToWorldLookup[entity].Value);

                if (navmesh.Navmesh->IsEmpty)
                {
                    navmesh.Navmesh->Load(insertions, destroyedTriangles, ltwInv);
                }
                else
                {
                    var removals = Removals.GetValuesForKey(entity);
                    navmesh.Navmesh->Update(insertions, removals, destroyedTriangles, ltwInv);
                }
            }
        }

        struct SystemStateComponent : ISystemStateComponentData
        {
            public Entity Navmesh;
        }
    }
}