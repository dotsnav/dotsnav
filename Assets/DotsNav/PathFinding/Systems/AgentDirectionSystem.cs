using DotsNav.Core;
using DotsNav.Data;
using DotsNav.PathFinding;
using DotsNav.PathFinding.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateAfter(typeof(PathFinderSystem))]
    class AgentDirectionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithBurst()
                .ForEach((PathQueryComponent agent, RadiusComponent radius, Translation translation, DynamicBuffer<PathSegmentElement> path, ref DirectionComponent data) =>
                {
                    if (agent.State != PathQueryState.PathFound)
                        return;

                    var p = translation.Value.xz;

                    var dest = path[path.Length - 1].To;
                    if (math.all(p == dest))
                    {
                        data.Value = float2.zero;
                        return;
                    }

                    var distSq = float.MaxValue;
                    var closest = default(float2);
                    Angle direction = 0f;

                    for (int i = 0; i < path.Length; i++)
                    {
                        var segment = path[i];
                        var point = Math.ClosestPointOnLineSegment(p, segment.From, segment.To);
                        Angle dir;

                        if (math.all(point == segment.To) && i < path.Length - 1)
                        {
                            var corner = path[i + 1].Corner;
                            var fromAngle = Math.Angle(segment.To - corner);
                            var toAngle = Math.Angle(path[i + 1].From - corner);
                            dir = Angle.Clamp(Math.Angle(p - corner), fromAngle, toAngle);
                            point = corner + Math.Rotate(radius, dir);
                            var left = Math.CcwFast(segment.From, segment.To, corner);
                            dir += left ? -math.PI / 2 : math.PI / 2;
                        }
                        else
                        {
                            dir = Math.Angle(segment.To - segment.From);
                        }

                        var dsq = math.distancesq(p, point);

                        if (dsq > distSq)
                        {
                            data.Value = GetDirection(direction);
                            return;
                        }

                        closest = point;
                        direction = dir;
                        distSq = dsq;
                    }

                    data.Value = GetDirection(Math.Angle(dest - p));

                    float2 GetDirection(Angle angle)
                    {
                        var distToPath = math.distance(p, closest);
                        var f = math.min(1, distToPath / (radius / 2));
                        var angleToPath = Math.Angle(closest - p);
                        return Angle.Lerp(angle, angleToPath, f).ToVector();
                    }
                })
                .ScheduleParallel();
        }
    }
}