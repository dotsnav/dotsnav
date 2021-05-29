using DotsNav.Core.Data;
using DotsNav.Core.Systems;
using DotsNav.PathFinding.Data;
using Unity.Entities;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    class PathFinderHybridWriteSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((PathQueryComponent query, Hybrid.DotsNavAgent agent, DynamicBuffer<PathSegmentElement> segments, DirectionComponent direction) =>
                {
                    if (query.Version > agent.Version)
                    {
                        agent.Version = query.Version;
                        agent.Segments.Clear();
                        for (int i = 0; i < segments.Length; i++)
                            agent.Segments.Add(segments[i]);
                    }

                    agent.State = query.State;
                    agent.Direction = direction.Value;
                })
                .Run();
        }
    }
}