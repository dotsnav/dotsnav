using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace DotsNav.Navmesh.Systems
{
    [BurstCompile, UpdateInGroup(typeof(DotsNavSystemGroup))]
    [RequireMatchingQueriesForUpdate] // todo doesnt work?
    public unsafe partial struct UpdateNavmeshSystem : ISystem
    {
        EntityQuery _navmeshQuery;
        EntityQuery _destroyQuery;
        EntityQuery _insertQuery;
        EntityQuery _insertBulkQuery;
        EntityQuery _blobQuery;
        EntityQuery _blobBulkQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _navmeshQuery = 
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<NavmeshComponent>()
                    .Build(ref state);

            _destroyQuery =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<CleanUpComponent>()
                    .WithNone<NavmeshObstacleComponent>()
                    .Build(ref state);

            _insertQuery =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .WithNone<VertexAmountElement>()
                    .Build(ref state);

            _insertBulkQuery =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexElement>()
                    .WithAll<VertexAmountElement>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);

            _blobQuery =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PlaneComponent>()
                    .WithAll<VertexBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);

            _blobBulkQuery =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PlaneComponent>()
                    .WithAll<ObstacleBlobComponent>()
                    .WithAll<NavmeshObstacleComponent>()
                    .WithAll<LocalToWorld>()
                    .WithNone<CleanUpComponent>()
                    .Build(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<PlaneComponent> planes, Allocator.TempJob);
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<CleanUpComponent> removals, Allocator.TempJob);
            var dependencies = new NativeList<JobHandle>(Allocator.Temp);
            // Debug.Log($"update {planes.Length}");

            var planeEntities = _navmeshQuery.ToEntityArray(Allocator.Temp);
            
            foreach (var planeEntity in planeEntities)
            {
                // if (plane.Entity == Entity.Null)
                //     continue;

                // var destroyIsEmpty = true;
                var destroyIsEmpty = true;
                var exists = false;
                foreach (var removal in removals)
                {
                    if (removal.Plane == planeEntity)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    _destroyQuery.SetSharedComponentFilter(new CleanUpComponent { Plane = planeEntity });
                    destroyIsEmpty = _destroyQuery.IsEmpty;
                }
                
                
                //         Debug.Log($"found plane {plane.Entity}");

                var insertIsEmpty  = true;
                var insertBulkIsEmpty  = true;
                var blobIsEmpty  = true;
                var blobBulkIsEmpty  = true;
                
                exists = false;
                foreach (var plane1 in planes)
                {
                    if (plane1.Entity == planeEntity)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    var plane = new PlaneComponent { Entity = planeEntity };
                    _insertQuery.SetSharedComponentFilter(plane);
                    insertIsEmpty = _insertQuery.IsEmpty;
                    _insertBulkQuery.SetSharedComponentFilter(plane);
                    insertBulkIsEmpty = _insertBulkQuery.IsEmpty;
                    _blobQuery.SetSharedComponentFilter(plane);
                    blobIsEmpty = _blobQuery.IsEmpty;
                    _blobBulkQuery.SetSharedComponentFilter(plane);
                    blobBulkIsEmpty = _blobBulkQuery.IsEmpty;
                }

                if (destroyIsEmpty && insertIsEmpty && insertBulkIsEmpty && blobIsEmpty && blobBulkIsEmpty)
                    continue;

                var data = new NativeReference<JobData>(Allocator.TempJob);
                var dependency = new PreJob
                {
                    Plane = planeEntity,
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
                Debug.Log($"running destroy job {Data.Value.Plane}");
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
            
            void Execute(Entity entity, in DynamicBuffer<VertexElement> vertices, in LocalToWorld localToWorld)
            {
                var navmesh = Data.Value.Navmesh;
                var ltw = math.mul(Data.Value.PlaneLtwInv, localToWorld.Value);
                Buffer.AddSharedComponent(entity, new CleanUpComponent { Plane = Data.Value.Plane });

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
            
            void Execute(in DynamicBuffer<VertexElement> vertices, in DynamicBuffer<VertexAmountElement> amounts, in LocalToWorld localToWorld)
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
            
            void Execute(in Entity entity, in VertexBlobComponent blob, in LocalToWorld localToWorld)
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
            
            void Execute(in ObstacleBlobComponent blob, in LocalToWorld localToWorld)
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
            [NativeDisableContainerSafetyRestriction]
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
}