using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.PathFinding.Data;
using DotsNav.PathFinding.Hybrid;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    partial class PathFinderHybridWriteSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((PathQueryComponent query, DotsNavAgent a, DotsNavPathFindingAgent agent, DynamicBuffer<PathSegmentElement> segments, DirectionComponent direction) =>
                {
                    if (query.Version > agent.Version)
                    {
                        agent.Version = query.Version;
                        agent.Segments.Clear();
                        for (int i = 0; i < segments.Length; i++)
                            agent.Segments.Add(segments[i]);
                    }

                    agent.State = query.State;
                    agent.Direction = a.Plane.DirectionToWorldSpace(direction.Value);
                })
                .Run();
        }
    }
}