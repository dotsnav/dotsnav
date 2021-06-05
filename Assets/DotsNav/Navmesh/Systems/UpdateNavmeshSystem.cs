using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

namespace DotsNav.Navmesh.Systems
{
    /// <summary>
    /// Queued insertion and removal of obstacles are processed by this system
    /// </summary>
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    public class UpdateNavmeshSystem : SystemBase
    {
        NativeQueue<Entity> _toRemoveQueue;
        NativeList<float2> _vertexBuffer;
        NativeList<int> _amounts;
        NativeList<Entity> _entities;
        EntityQuery _bufferQuery;
        EntityQuery _blobQuery;

        protected override void OnCreate()
        {
            _toRemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);
            _vertexBuffer = new NativeList<float2>(Allocator.Persistent);
            _amounts = new NativeList<int>(Allocator.Persistent);
            _entities = new NativeList<Entity>(Allocator.Persistent);
            _bufferQuery = GetEntityQuery(ComponentType.ReadOnly<VertexElement>(), ComponentType.ReadOnly<VertexAmountElement>());
            _blobQuery = GetEntityQuery(ComponentType.ReadOnly<ObstacleBlobComponent>());
        }

        protected override void OnDestroy()
        {
            _toRemoveQueue.Dispose();
            _vertexBuffer.Dispose();
            _amounts.Dispose();
            _entities.Dispose();
        }

        protected override unsafe void OnUpdate()
        {
            var ecbSource = DotsNavSystemGroup.EcbSource;
            var buffer = ecbSource.CreateCommandBuffer().AsParallelWriter();
            var removeQueue = _toRemoveQueue;
            var toRemoveWriter = removeQueue.AsParallelWriter();

            var loadMarker = new ProfilerMarker("Loading");
            var updatingMarker = new ProfilerMarker("Updating");

            var vertexBuffer = _vertexBuffer;
            var amounts = _amounts;
            var entities = _entities;

            // Queue insert obstacles without LocalToWorld
            Entities
                .WithBurst()
                .WithNone<ObstacleSystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .ForEach((Entity entity, int entityInQueryIndex, ObstacleComponent data, VertexBlobComponent blob) =>
                {
                    ref var vertices = ref blob.BlobRef.Value.Vertices;
                    vertexBuffer.AddRange(vertices.GetUnsafePtr(), vertices.Length);
                    amounts.Add(vertices.Length);
                    entities.Add(entity);
                    buffer.AddComponent(entityInQueryIndex, entity, new ObstacleSystemStateComponent());
                })
                .Schedule();

            Entities
                .WithBurst()
                .WithNone<ObstacleSystemStateComponent>()
                .WithNone<Translation, Rotation, Scale>().WithNone<NonUniformScale, LocalToWorld>()
                .ForEach((Entity entity, int entityInQueryIndex, ObstacleComponent data, DynamicBuffer<VertexElement> vertices) =>
                {
                    vertexBuffer.AddRange(vertices.GetUnsafeReadOnlyPtr(), vertices.Length);
                    amounts.Add(vertices.Length);
                    entities.Add(entity);
                    buffer.AddComponent(entityInQueryIndex, entity, new ObstacleSystemStateComponent());
                })
                .Schedule();

            // Queue insert obstacles with LocalToWorld
            Entities
                .WithBurst()
                .WithNone<ObstacleSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ObstacleComponent data, VertexBlobComponent blob, LocalToWorld ltw) =>
                {
                    ref var vertices = ref blob.BlobRef.Value.Vertices;
                    for (int i = 0; i < vertices.Length; i++)
                        vertexBuffer.Add(Math.Mul2D(ltw.Value, vertices[i]));
                    buffer.AddComponent(entityInQueryIndex, entity, new ObstacleSystemStateComponent());
                })
                .Schedule();


            Entities
                .WithBurst()
                .WithNone<ObstacleSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ObstacleComponent data, DynamicBuffer<VertexElement> vertices, LocalToWorld ltw) =>
                {
                    for (int i = 0; i < vertices.Length; i++)
                        vertexBuffer.Add(Math.Mul2D(ltw.Value, vertices[i]));
                    amounts.Add(vertices.Length);
                    entities.Add(entity);
                    buffer.AddComponent(entityInQueryIndex, entity, new ObstacleSystemStateComponent());
                })
                .Schedule();

            // Queue remove obstacles
            Entities
                .WithBurst()
                .WithNone<ObstacleComponent>()
                .WithAll<ObstacleSystemStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    toRemoveWriter.Enqueue(entity);
                    buffer.RemoveComponent<ObstacleSystemStateComponent>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            var vertexBuffers = GetBufferFromEntity<VertexElement>(true);
            var vertexAmountBuffers = GetBufferFromEntity<VertexAmountElement>(true);
            var bufferEntities = _bufferQuery.ToEntityArray(Allocator.TempJob);

            var bulkBlobs = GetComponentDataFromEntity<ObstacleBlobComponent>(true);
            var blobEntities = _blobQuery.ToEntityArray(Allocator.TempJob);

            // Update navmesh
            Entities
                .WithBurst()
                .WithReadOnly(vertexBuffers)
                .WithReadOnly(vertexAmountBuffers)
                .WithReadOnly(bulkBlobs)
                .WithDisposeOnCompletion(bufferEntities)
                .WithDisposeOnCompletion(blobEntities)
                .ForEach(
                    (
                        int entityInQueryIndex,
                        DynamicBuffer<DestroyedTriangleElement> destroyed,
                        ref Navmesh navmesh
                    ) =>
                    {
                        destroyed.Clear();

                        if (navmesh.IsEmpty)
                        {
                            loadMarker.Begin();
                            navmesh.Load(vertexBuffer, amounts, entities, destroyed, vertexBuffers, vertexAmountBuffers, bufferEntities, bulkBlobs, blobEntities);
                            loadMarker.End();
                        }
                        else
                        {
                            updatingMarker.Begin();
                            navmesh.Update(vertexBuffer, amounts, entities, removeQueue, destroyed, vertexBuffers, vertexAmountBuffers, bufferEntities, bulkBlobs, blobEntities);
                            updatingMarker.End();
                        }

                        for (int i = 0; i < bufferEntities.Length; i++)
                            buffer.DestroyEntity(entityInQueryIndex, bufferEntities[i]);

                        for (int i = 0; i < blobEntities.Length; i++)
                            buffer.DestroyEntity(entityInQueryIndex, blobEntities[i]);

                        vertexBuffer.Clear();
                        amounts.Clear();
                        entities.Clear();
                    })
                .Schedule();

            ecbSource.AddJobHandleForProducer(Dependency);
        }
    }
}