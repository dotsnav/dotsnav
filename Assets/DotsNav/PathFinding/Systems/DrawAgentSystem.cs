using DotsNav.Data;
using DotsNav.Drawing;
using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    partial class DrawAgentSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ltwLookup = GetComponentDataFromEntity<LocalToWorld>(true);

            Entities
                .WithBurst()
                .WithReadOnly(ltwLookup)
                .ForEach((PathQueryComponent agent, RadiusComponent radius, AgentDrawComponent debug, DynamicBuffer<PathSegmentElement> path, NavmeshAgentComponent navmesh) =>
                {
                    if (!debug.Draw || path.Length == 0)
                        return;

                    var ltw = ltwLookup[navmesh.Navmesh].Value;

                    var lines = new NativeList<Line>(Allocator.Temp);
                    var color = debug.Color;
                    if (color.a < 10)
                        color.a += 10;

                    for (int j = 0; j < path.Length; j++)
                    {
                        var segment = path[j];
                        var perp = math.normalize(Math.PerpCcw(segment.To - segment.From)) * radius.Value;
                        lines.Add(new Line(math.transform(ltw, (segment.From + perp).ToXxY()), math.transform(ltw, (segment.To + perp).ToXxY()), color));
                        lines.Add(new Line(math.transform(ltw, (segment.From - perp).ToXxY()), math.transform(ltw, (segment.To - perp).ToXxY()), color));
                    }

                    var up = math.rotate(ltw, new float3(0, 1, 0));

                    for (int j = 1; j < path.Length; j++)
                    {
                        var from = path[j - 1].To;
                        var c = path[j].Corner;
                        var to = path[j].From;
                        var angle = (Angle) Math.Angle(to - c) - Math.Angle(from - c);
                        Arc.Draw(lines, math.transform(ltw, c.ToXxY()), up, math.rotate(ltw, 2 * (from - c).ToXxY()), angle, color, radius.Value, debug.Delimit);
                    }

                    var arm = math.rotate(ltw, new float3(0, 0, radius.Value));
                    Arc.Draw(lines, math.transform(ltw, path[0].From.ToXxY()), up, arm, 2 * math.PI, color, radius.Value, debug.Delimit);
                    Arc.Draw(lines, math.transform(ltw, path[path.Length - 1].To.ToXxY()), up, arm, 2 * math.PI, color, radius.Value, debug.Delimit);

                    Line.Draw(lines);
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}