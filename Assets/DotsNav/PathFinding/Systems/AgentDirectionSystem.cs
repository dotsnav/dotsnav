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
    public partial class AgentDirectionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ltwLookup = GetComponentLookup<LocalToWorld>(true);

            Entities
                .WithBurst()
                .WithReadOnly(ltwLookup)
                .ForEach((RadiusComponent radius, TransformAspect translation, NavmeshAgentComponent navmesh, DynamicBuffer<PathSegmentElement> path, ref PathQueryComponent agent, ref DirectionComponent data) =>
                {
                    if (agent.State != PathQueryState.PathFound)
                        return;

                    if (data.QueryVersion < agent.Version)
                    {
                        data.SegmentIndex = 0;
                        data.QueryVersion = agent.Version;
                    }

                    var inv = math.inverse(ltwLookup[navmesh.Navmesh].Value);
                    var p = math.transform(inv, translation.Position).xz;

                    var dest = path[^1].To;
                    if (math.all(p == dest))
                    {
                        data.Value = float2.zero;
                        return;
                    }

                    var segment = path[data.SegmentIndex];
                    var closest = Math.ClosestPointOnLineSegment(p, segment.From, segment.To);
                    data.DistanceFromPathSquared = math.distancesq(p, closest);

                    while (data.SegmentIndex < path.Length - 1)
                    {
                        var segment1 = path[data.SegmentIndex + 1];
                        var point1 = Math.ClosestPointOnLineSegment(p, segment1.From, segment1.To);
                        var dsq1 = math.distancesq(p, point1);

                        if (data.DistanceFromPathSquared < dsq1)
                            break;

                        ++data.SegmentIndex;
                        segment = segment1;
                        closest = point1;
                        data.DistanceFromPathSquared = dsq1;
                    }

                    Angle dir;

                    if (math.all(closest == segment.To) && data.SegmentIndex < path.Length - 1)
                    {
                        var corner = path[data.SegmentIndex + 1].Corner;
                        var fromAngle = Math.Angle(segment.To - corner);
                        var toAngle = Math.Angle(path[data.SegmentIndex + 1].From - corner);
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