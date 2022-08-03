using DotsNav.Data;
using DotsNav.Navmesh.Data;
using DotsNav.Navmesh.Systems;
using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateAfter(typeof(UpdateNavmeshSystem))]
    public partial class PathFinderSystem : SystemBase
    {
        NativeList<Entity> _buffer;
        NativeQueue<Entity> _queue;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<PathFinderComponent>();
            RequireSingletonForUpdate<PathFinderSystemStateComponent>();
            _buffer = new NativeList<Entity>(Allocator.Persistent);
            _queue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _buffer.Dispose();
            _queue.Dispose();
        }

        protected override void OnUpdate()
        {
            var data = GetSingleton<PathFinderComponent>();
            var resources = GetSingleton<PathFinderSystemStateComponent>();
            var destroyed = GetBufferFromEntity<DestroyedTriangleElement>(true);
            var buffer = _buffer;
            var queue = _queue;
            var writer = queue.AsParallelWriter();

            Entities
                .WithBurst()
                .WithName("GatherAgentsToRecalculate")
                .WithReadOnly(destroyed)
                .ForEach((Entity entity, int nativeThreadIndex, NavmeshAgentComponent navmesh, ref PathQueryComponent query, ref DynamicBuffer<TriangleElement> triangles) =>
                {
                    if (query.State == PathQueryState.PathFound)
                    {
                        var seq0 = destroyed[navmesh.Navmesh].Reinterpret<int>();
                        var seq1 = triangles.Reinterpret<int>();

                        if (SortedSequencesContainIdenticalElement(seq0, seq1))
                            query.State = PathQueryState.Invalidated;
                    }

                    if ((query.State & data.RecalculateFlags & ~PathQueryState.Inactive) != 0)
                        writer.Enqueue(entity);

                })
                .ScheduleParallel();

            Job
                .WithBurst()
                .WithCode(() =>
                {
                    buffer.Clear();
                    while (queue.TryDequeue(out var e))
                        buffer.Add(e);
                })
                .Schedule();

            Dependency = new FindPathJob
                {
                    Agents = buffer.AsDeferredJobArray(),
                    NavmeshElements = GetComponentDataFromEntity<NavmeshAgentComponent>(true),
                    Navmeshes = GetComponentDataFromEntity<NavmeshComponent>(true),
                    LTWLookup = GetComponentDataFromEntity<LocalToWorld>(true),
                    TranslationLookup = GetComponentDataFromEntity<Translation>(true),
                    Queries = GetComponentDataFromEntity<PathQueryComponent>(),
                    Radii = GetComponentDataFromEntity<RadiusComponent>(true),
                    PathSegments = GetBufferFromEntity<PathSegmentElement>(),
                    TriangleIds = GetBufferFromEntity<TriangleElement>(),
                    PathFinder = resources,
                }
                .Schedule(buffer, 1, Dependency);
        }


        [BurstCompile]
        struct FindPathJob : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeArray<Entity> Agents;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<PathQueryComponent> Queries;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<RadiusComponent> Radii;
            [NativeDisableContainerSafetyRestriction]
            public BufferFromEntity<PathSegmentElement> PathSegments;
            [NativeDisableContainerSafetyRestriction]
            public BufferFromEntity<TriangleElement> TriangleIds;
            public PathFinderSystemStateComponent PathFinder;

            [NativeSetThreadIndex]
            int _threadId;

            [ReadOnly]
            public ComponentDataFromEntity<NavmeshAgentComponent> NavmeshElements;
            [ReadOnly]
            public ComponentDataFromEntity<NavmeshComponent> Navmeshes;
            [ReadOnly]
            public ComponentDataFromEntity<LocalToWorld> LTWLookup;
            [ReadOnly]
            public ComponentDataFromEntity<Translation> TranslationLookup;

            public unsafe void Execute(int index)
            {
                Assert.IsTrue(_threadId > 0 && _threadId <= PathFinder.Instances.Length);
                var agent = Agents[index];
                var query = Queries[agent];
                var segments = PathSegments[agent];
                segments.Clear();
                var ids = TriangleIds[agent];
                ids.Clear();
                var instanceIndex = _threadId - 1;
                var instance = PathFinder.Instances[instanceIndex];

                var navmeshEntity = NavmeshElements[agent].Navmesh;
                var ltw = math.inverse(LTWLookup[navmeshEntity].Value);
                var pos = TranslationLookup[agent];
                query.State = instance.FindPath(math.transform(ltw, pos.Value).xz, math.transform(ltw, query.To).xz, Radii[agent].Value, segments, ids, Navmeshes[navmeshEntity].Navmesh, out _);
                if (query.State == PathQueryState.PathFound)
                    ++query.Version;
                Queries[agent] = query;
            }
        }

        static bool SortedSequencesContainIdenticalElement(DynamicBuffer<int> seq0, DynamicBuffer<int> seq1)
        {
            if (seq0.Length == 0 || seq1.Length == 0)
                return false;

            var i0 = 0;
            var i1 = 0;

            while (true)
            {
                var v0 = seq0[i0];
                var v1 = seq1[i1];

                if (v0 == v1)
                    return true;

                if (v0 < v1)
                {
                    if (i0 < seq0.Length - 1)
                        ++i0;
                    else
                        return false;
                }
                else if (i1 < seq1.Length - 1)
                    ++i1;
                else
                    return false;
            }
        }
    }
}