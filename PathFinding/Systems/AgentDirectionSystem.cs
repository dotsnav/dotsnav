using DotsNav.Data;
using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateAfter(typeof(PathFinderSystem))]
    class AgentDirectionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ltwLookup = GetComponentDataFromEntity<LocalToWorld>(true);

            Entities
                .WithBurst()
                .WithReadOnly(ltwLookup)
                .ForEach((RadiusComponent radius, Translation translation, NavmeshAgentComponent navmesh, DynamicBuffer<PathSegmentElement> path, ref PathQueryComponent agent, ref DirectionComponent data) =>
                {
                    if (agent.State != PathQueryState.PathFound)
                        return;

                    var inv = math.inverse(ltwLookup[navmesh.Navmesh].Value);
                    var p = math.transform(inv, translation.Value).xz;

                    var dest = path[path.Length - 1].To;
                    if (math.all(p == dest))
                    {
                        data.Value = float2.zero;
                        return;
                    }

                    var segment = path[0];
                    var closest = Math.ClosestPointOnLineSegment(p, segment.From, segment.To);
                    var dsq = math.distancesq(p, closest);

                    while (path.Length > 1)
                    {
                        var segment1 = path[1];
                        var point1 = Math.ClosestPointOnLineSegment(p, segment1.From, segment1.To);
                        var dsq1 = math.distancesq(p, point1);
                        if (dsq < dsq1)
                            break;
                        // todo store segments in reverse order or keep an index?
                        path.RemoveAt(0);
                        segment = segment1;
                        closest = point1;
                        dsq = dsq1;
                    }

                    if (dsq > radius * radius)
                        agent.State = PathQueryState.Invalidated;

                    Angle dir;

                    if (math.all(closest == segment.To) && path.Length > 1)
                    {
                        var corner = path[1].Corner;
                        var fromAngle = Math.Angle(segment.To - corner);
                        var toAngle = Math.Angle(path[1].From - corner);
                        dir = Angle.Clamp(Math.Angle(p - corner), fromAngle, toAngle);
                        var left = Math.CcwFast(segment.From, segment.To, corner);
                        dir += left ? -math.PI / 2 : math.PI / 2;
                    }
                    else
                    {
                        dir = Math.Angle(segment.To - p);
                    }

                    data.Value = dir.ToVector();
                })
                .ScheduleParallel();
        }
    }
}