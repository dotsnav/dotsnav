using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace DotsNav.Navmesh.Systems
{
    [BurstCompile, UpdateInGroup(typeof(DotsNavSystemGroup))]
    [RequireMatchingQueriesForUpdate] // todo doesnt work?
    public unsafe partial struct UpdateNavmeshSystem2 : ISystem
    {
        EntityQuery _insertQuery;
        EntityQuery _insertBulkQuery;
        EntityQuery _destroyQuery;
        EntityQuery _blobQuery;
        EntityQuery _blobBulkQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _insertQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .WithNone<VertexAmountElement>()
                    .Build(ref state);

            _insertBulkQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<VertexAmountElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _blobQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _blobBulkQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<ObstacleBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _destroyQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<CleanUpComponent>()
                    .WithNone<NavmeshObstacleComponent>()
                    .Build(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // todo in loadtest sample every second insert causes an exception accessing "planes" after it is deallocated
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<PlaneComponent> planes, Allocator.Temp);
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<CleanUpComponent> removals, Allocator.Temp);
            var dependencies = new NativeList<JobHandle>(Allocator.Temp);

            foreach (var plane in planes)
            {
                if (plane.Entity == Entity.Null)
                    continue;

                var destroyIsEmpty = true;
                foreach (var removal in removals)
                {
                    if (removal.Plane == plane.Entity)
                    {
                        _destroyQuery.SetSharedComponentFilter(removal);
                        destroyIsEmpty = _destroyQuery.IsEmpty;
                        break;
                    }
                }
                
                _insertQuery.SetSharedComponentFilter(plane);
                var insertIsEmpty = _insertQuery.IsEmpty;
                _insertBulkQuery.SetSharedComponentFilter(plane);
                var insertBulkIsEmpty = _insertBulkQuery.IsEmpty;
                _blobQuery.SetSharedComponentFilter(plane);
                var blobIsEmpty = _blobQuery.IsEmpty;
                _blobBulkQuery.SetSharedComponentFilter(plane);
                var blobBulkIsEmpty = _blobBulkQuery.IsEmpty;

                if (destroyIsEmpty && insertIsEmpty && insertBulkIsEmpty && blobIsEmpty && blobBulkIsEmpty)
                    continue;

                var data = new NativeReference<JobData>(Allocator.TempJob);
                var dependency = new PreJob
                {
                    Plane = plane.Entity,
                    Data = data,
                    NavmeshLookup = state.GetComponentLookup<NavmeshComponent>(true),
                    LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>(true)
                }.Schedule(state.Dependency);

                var ecb = HasSingleton<RunnerSingleton>() 
                    ? GetSingletonRW<EndDotsNavEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged) 
                    : GetSingletonRW<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
                
                if (!destroyIsEmpty)
                {
                    dependency = new DestroyJob
                    {
                        Data = data,
                        Buffer = ecb
                    }.Schedule(_destroyQuery, dependency);

                    dependency = new RemoveRefinementsJob { Data = data }.Schedule(dependency);
                }
                
                if (!insertIsEmpty)
                {
                    dependency = new InsertJob
                    {
                        Data = data,
                        Buffer = ecb
                    }.Schedule(_insertQuery, dependency);
                }

                if (!insertBulkIsEmpty) 
                    dependency = new InsertBulkJob { Data = data }.Schedule(_insertBulkQuery, dependency);

                if (!blobIsEmpty)
                {
                    dependency = new BlobJob
                    {
                        Data = data,
                        Buffer = ecb
                    }.Schedule(_blobQuery, dependency);
                }
                
                if (!blobBulkIsEmpty) 
                    dependency = new BlobBulkJob() { Data = data }.Schedule(_blobBulkQuery, dependency);
                
                dependency = new PostJob
                {
                    Data = data,
                    DestroyedTriangleBufferLookup = state.GetBufferLookup<DestroyedTriangleElement>(),
                }.Schedule(dependency);
                
                dependencies.Add(dependency);
            }
            
            state.Dependency = JobHandle.CombineDependencies(dependencies);
        }

        [BurstCompile]
        struct PreJob : IJob
        {
            public Entity Plane;
            public NativeReference<JobData> Data;
            [ReadOnly] public ComponentLookup<NavmeshComponent> NavmeshLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            
            public void Execute()
            {
                var navmesh = NavmeshLookup[Plane].Navmesh;
                var planeLtwInv = math.inverse(LocalToWorldLookup[Plane].Value);
                navmesh->DestroyedTriangles.Clear();
                navmesh->V.Clear();

                Data.Value = new JobData
                {
                    Plane = Plane,
                    Navmesh = navmesh,
                    Empty = navmesh->IsEmpty,
                    PlaneLtwInv = planeLtwInv,
                };
            }
        }

        [BurstCompile]
        partial struct DestroyJob : IJobEntity
        {
            public NativeReference<JobData> Data;
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity)
            {
                Data.Value.Navmesh->RemoveConstraint(entity);
                Buffer.RemoveComponent<CleanUpComponent>(entity);
            }
        }

        [BurstCompile]
        struct RemoveRefinementsJob : IJob
        {
            public NativeReference<JobData> Data;

            public void Execute()
            {
                Data.Value.Navmesh->RemoveRefinements();
            }
        }
        
        [BurstCompile]
        partial struct InsertJob : IJobEntity
        {
            public NativeReference<JobData> Data;
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity, DynamicBuffer<VertexElement> vertices, LocalToWorld localToWorld)
            {
                var navmesh = Data.Value.Navmesh;
                var ltw = math.mul(Data.Value.PlaneLtwInv, localToWorld.Value);
                Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Data.Value.Plane});

                if (Data.Value.Empty)
                {
                    navmesh->Insert((float2*)vertices.GetUnsafeReadOnlyPtr(), 0, vertices.Length, entity, ltw);
                }
                else
                {
                    navmesh->C.Clear();
                    navmesh->Insert((float2*)vertices.GetUnsafeReadOnlyPtr(), 0, vertices.Length, entity, ltw);
                    navmesh->SearchDisturbances();
                }
            }
        }
        
        [BurstCompile]
        partial struct InsertBulkJob : IJobEntity
        {
            public NativeReference<JobData> Data;
            
            void Execute(DynamicBuffer<VertexElement> vertices, DynamicBuffer<VertexAmountElement> amounts, LocalToWorld localToWorld)
            {
                var navmesh = Data.Value.Navmesh;
                var ptr = (float2*)vertices.GetUnsafeReadOnlyPtr();
                var ltw = math.mul(Data.Value.PlaneLtwInv, localToWorld.Value);
                var start = 0;
                
                if (Data.Value.Empty)
                {
                    foreach (var amount in amounts)
                    {
                        navmesh->Insert(ptr, start, amount, Entity.Null, ltw);
                        start += amount;
                    }
                }
                else
                {
                    foreach (var amount in amounts)
                    {
                        navmesh->C.Clear();
                        navmesh->Insert(ptr, start, amount, Entity.Null, ltw);
                        navmesh->SearchDisturbances();
                        start += amount;
                    }
                }
            }
        }
        
        [BurstCompile]
        partial struct BlobJob : IJobEntity
        {
            public NativeReference<JobData> Data;
            public EntityCommandBuffer Buffer;
            
            void Execute(Entity entity, VertexBlobComponent blob, LocalToWorld localToWorld)
            {
                var navmesh = Data.Value.Navmesh;
                var ltw = math.mul(Data.Value.PlaneLtwInv, localToWorld.Value);
                Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Data.Value.Plane});
                ref var vertices = ref blob.BlobRef.Value.Vertices;

                if (Data.Value.Empty)
                {
                    navmesh->Insert((float2*)vertices.GetUnsafePtr(), 0, vertices.Length, entity, ltw);
                }
                else
                {
                    navmesh->C.Clear();
                    navmesh->Insert((float2*)vertices.GetUnsafePtr(), 0, vertices.Length, entity, ltw);
                    navmesh->SearchDisturbances();
                }
            }
        }
        
        [BurstCompile]
        partial struct BlobBulkJob : IJobEntity
        {
            public NativeReference<JobData> Data;
            
            void Execute(ObstacleBlobComponent blob, LocalToWorld localToWorld)
            {
                var navmesh = Data.Value.Navmesh;
                var vertices = (float2*)blob.BlobRef.Value.Vertices.GetUnsafePtr();
                ref var a = ref blob.BlobRef.Value.Amounts;
                var ltw = math.mul(Data.Value.PlaneLtwInv, localToWorld.Value);
                var start = 0;
                
                if (Data.Value.Empty)
                {
                    for (var i = 0; i < a.Length; i++)
                    {
                        var amount = a[i];
                        navmesh->Insert(vertices, start, amount, Entity.Null, ltw);
                        start += amount;
                    }
                }
                else
                {
                    for (var i = 0; i < a.Length; i++)
                    {
                        var amount = a[i];
                        navmesh->C.Clear();
                        navmesh->Insert(vertices, start, amount, Entity.Null, ltw);
                        navmesh->SearchDisturbances();
                        start += amount;
                    }
                }
            }
        }

        [BurstCompile]
        struct PostJob : IJob
        {
            public NativeReference<JobData> Data;
            public BufferLookup<DestroyedTriangleElement> DestroyedTriangleBufferLookup;

            public void Execute()
            {
                var navmesh = Data.Value.Navmesh;
                
                if (Data.Value.Empty)
                    navmesh->GlobalRefine();
                else
                    navmesh->LocalRefinement();

                var destroyedTriangles = DestroyedTriangleBufferLookup[Data.Value.Plane];
                destroyedTriangles.Clear();
                var tris = navmesh->DestroyedTriangles.GetEnumerator();
                while (tris.MoveNext())
                    destroyedTriangles.Add(tris.Current);
                destroyedTriangles.Reinterpret<int>().AsNativeArray().Sort();
            }
        }

        struct JobData
        {
            public Entity Plane;
            public Navmesh* Navmesh;
            public bool Empty;
            public float4x4 PlaneLtwInv;
        }

        struct CleanUpComponent : ICleanupSharedComponentData
        {
            public Entity Plane;
        }
    }
    
    ///////////////////////////////////////////////////////
    
    [BurstCompile, UpdateInGroup(typeof(DotsNavSystemGroup)), DisableAutoCreation]
    public unsafe partial struct UpdateNavmeshSystem : ISystem
    {
        EntityQuery _insertQuery;
        EntityQuery _insertBulkQuery;
        EntityQuery _destroyQuery;
        EntityQuery _blobQuery;
        EntityQuery _blobBulkQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _insertQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithNone<CleanUpComponent>()
                    .WithNone<VertexAmountElement>()
                    .Build(ref state);

            _insertBulkQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<VertexAmountElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _blobQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _blobBulkQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<PlaneComponent>()
                    .WithAll<ObstacleBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
            
            _destroyQuery =
                new EntityQueryBuilder(Allocator.Persistent)
                    .WithAll<CleanUpComponent>()
                    .WithNone<NavmeshObstacleComponent>()
                    .Build(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // todo in loadtest sample every second insert causes an exception accessing "planes" after it is deallocated
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<PlaneComponent> planes, Allocator.Temp);
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<CleanUpComponent> removals, Allocator.Temp);
            var toRemove = new NativeList<ToRemove>(Allocator.Temp);
            
            foreach (var plane in removals)
            {
                if (plane.Plane == Entity.Null)
                    continue;
                _destroyQuery.SetSharedComponentFilter(plane);
                var removed = _destroyQuery.ToEntityArray(Allocator.TempJob);
                if (removed.Length > 0)
                    toRemove.Add(new ToRemove { Plane = plane.Plane, Obstacles = removed });
            }
            
            var dependencies = new NativeList<JobHandle>(Allocator.Temp);
            
            foreach (var plane in planes)
            {
                if (plane.Entity == Entity.Null)
                    continue;
                
                _insertQuery.SetSharedComponentFilter(plane);
                var insert = _insertQuery.ToEntityArray(Allocator.TempJob);
                
                _insertBulkQuery.SetSharedComponentFilter(plane);
                var insertBulk = _insertBulkQuery.ToEntityArray(Allocator.TempJob);
                
                _blobQuery.SetSharedComponentFilter(plane);
                var blob = _blobQuery.ToEntityArray(Allocator.TempJob);

                _blobBulkQuery.SetSharedComponentFilter(plane);
                var blobBulk = _blobBulkQuery.ToEntityArray(Allocator.TempJob);

                var destroy = new NativeArray<Entity>(0, Allocator.TempJob);
                foreach (var remove in toRemove)
                {
                    if (remove.Plane == plane.Entity)
                    {
                        destroy = remove.Obstacles;
                        break;
                    }
                }

                if (insert.Length == 0 && insertBulk.Length == 0 &&
                    blob.Length == 0 && blobBulk.Length == 0 &&
                    destroy.Length == 0)
                    continue;

                var ecb = HasSingleton<RunnerSingleton>() 
                    ? GetSingletonRW<EndDotsNavEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged) 
                    : GetSingletonRW<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

                dependencies.Add(new UpdateNavmeshJob
                {
                    Plane = plane.Entity,
                    Insert = insert, InsertBulk = insertBulk,
                    Blob = blob, BlobBulk = blobBulk,
                    Destroy = destroy,
                    Buffer = ecb,
                    DestroyedTriangleBufferLookup = state.GetBufferLookup<DestroyedTriangleElement>(),
                    NavmeshLookup = state.GetComponentLookup<NavmeshComponent>(true),
                    LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>(true),
                    VertexBufferLookup = state.GetBufferLookup<VertexElement>(true),
                    VertexAmountBufferLookup = state.GetBufferLookup<VertexAmountElement>(true),
                    VertexBlobLookup = state.GetComponentLookup<VertexBlobComponent>(true),
                    ObstacleBlobLookup = state.GetComponentLookup<ObstacleBlobComponent>(true),

                }.Schedule(state.Dependency));
            }
            
            state.Dependency = JobHandle.CombineDependencies(dependencies);
        }

        struct ToRemove
        {
            public Entity Plane;
            public NativeArray<Entity> Obstacles;
        }

        [BurstCompile]
        struct UpdateNavmeshJob : IJob
        {
            public Entity Plane;
            public NativeArray<Entity> Insert;
            public NativeArray<Entity> InsertBulk;
            public NativeArray<Entity> Blob;
            public NativeArray<Entity> BlobBulk;
            public NativeArray<Entity> Destroy;
            public EntityCommandBuffer Buffer;
            public BufferLookup<DestroyedTriangleElement> DestroyedTriangleBufferLookup;
            [ReadOnly] public ComponentLookup<NavmeshComponent> NavmeshLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public BufferLookup<VertexElement> VertexBufferLookup;
            [ReadOnly] public BufferLookup<VertexAmountElement> VertexAmountBufferLookup;
            [ReadOnly] public ComponentLookup<VertexBlobComponent> VertexBlobLookup;
            [ReadOnly] public ComponentLookup<ObstacleBlobComponent> ObstacleBlobLookup;

            public void Execute()
            {
                var navmesh = NavmeshLookup[Plane].Navmesh;
                var planeLtwInv = math.inverse(LocalToWorldLookup[Plane].Value);
                var destroyedTriangles = DestroyedTriangleBufferLookup[Plane];
                destroyedTriangles.Clear();
                navmesh->DestroyedTriangles.Clear();
                
                if (navmesh->IsEmpty)
                {
                    foreach (var entity in Insert)
                    {
                        var vertices = VertexBufferLookup[entity];
                        var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                        navmesh->Insert((float2*)vertices.GetUnsafeReadOnlyPtr(), 0, vertices.Length, entity, ltw);
                        Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Plane});
                    }

                    foreach (var entity in InsertBulk)
                    {
                        var ptr = (float2*)VertexBufferLookup[entity].GetUnsafeReadOnlyPtr();
                        var amounts = VertexAmountBufferLookup[entity];
                        var start = 0;
                        foreach (var amount in amounts)
                        {
                            var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                            navmesh->Insert(ptr, start, amount, Entity.Null, ltw);
                            start += amount;
                        }
                    }

                    foreach (var entity in Blob)
                    {
                        ref var v = ref VertexBlobLookup[entity].BlobRef.Value.Vertices;
                        var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                        navmesh->Insert((float2*)v.GetUnsafePtr(), 0, v.Length, entity, ltw);
                        Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Plane});
                    }
                    
                    foreach (var entity in BlobBulk)
                    {
                        var blob = ObstacleBlobLookup[entity];
                        var v = (float2*)blob.BlobRef.Value.Vertices.GetUnsafePtr();
                        ref var a = ref blob.BlobRef.Value.Amounts;
                        var start = 0;
                        for (int i = 0; i < a.Length; i++)
                        {
                            var amount = a[i];
                            var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                            navmesh->Insert(v, start, amount, Entity.Null, ltw);
                            start += amount;
                        }
                    }

                    navmesh->GlobalRefine();
                }
                else
                {
                    navmesh->V.Clear();

                    if (Destroy.Length > 0)
                    {
                        foreach (var entity in Destroy)
                        {
                            navmesh->RemoveConstraint(entity);
                            Buffer.RemoveComponent<CleanUpComponent>(entity);
                        }
                        navmesh->RemoveRefinements();
                    }

                    foreach (var entity in Insert)
                    {
                        var vertices = VertexBufferLookup[entity];
                        navmesh->C.Clear();
                        var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                        navmesh->Insert((float2*)vertices.GetUnsafeReadOnlyPtr(), 0, vertices.Length, entity, ltw);
                        navmesh->SearchDisturbances();
                        Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Plane});
                    }
                    
                    foreach (var entity in InsertBulk)
                    {
                        var ptr = (float2*)VertexBufferLookup[entity].GetUnsafeReadOnlyPtr();
                        var amounts = VertexAmountBufferLookup[entity];
                        var start = 0;
                        foreach (var amount in amounts)
                        {
                            navmesh->C.Clear();
                            var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                            navmesh->Insert(ptr, start, amount, Entity.Null, ltw);
                            navmesh->SearchDisturbances();
                            start += amount;
                        }
                    }
                    
                    foreach (var entity in Blob)
                    {
                        ref var v = ref VertexBlobLookup[entity].BlobRef.Value.Vertices;
                        navmesh->C.Clear();
                        var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                        navmesh->Insert((float2*)v.GetUnsafePtr(), 0, v.Length, entity, ltw);
                        navmesh->SearchDisturbances();
                        Buffer.AddSharedComponent(entity, new CleanUpComponent{Plane = Plane});
                    }
                    
                    foreach (var entity in BlobBulk)
                    {
                        var blob = ObstacleBlobLookup[entity];
                        var v = (float2*)blob.BlobRef.Value.Vertices.GetUnsafePtr();
                        ref var a = ref blob.BlobRef.Value.Amounts;
                        var start = 0;
                        for (int i = 0; i < a.Length; i++)
                        {
                            var amount = a[i];
                            navmesh->C.Clear();
                            var ltw = LocalToWorldLookup.TryGetComponent(entity, out var localToWorld) ? math.mul(planeLtwInv, localToWorld.Value) : planeLtwInv;
                            navmesh->Insert(v, start, amount, Entity.Null, ltw);
                            navmesh->SearchDisturbances();
                            start += amount;
                        }
                    }
                    
                    navmesh->LocalRefinement();
                }
                
                var tris = navmesh->DestroyedTriangles.GetEnumerator();
                while (tris.MoveNext())
                    destroyedTriangles.Add(tris.Current);
                destroyedTriangles.Reinterpret<int>().AsNativeArray().Sort();
            }
        }
        
        struct CleanUpComponent : ICleanupSharedComponentData
        {
            public Entity Plane;
        }
    }
}