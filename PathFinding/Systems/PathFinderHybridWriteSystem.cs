using DotsNav.Data;
using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.PathFinding
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    class PathFinderHybridWriteSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((AgentComponent query, Hybrid.DotsNavAgent agent, DynamicBuffer<PathSegmentElement> segments, AgentDirectionComponent direction) =>
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