using DotsNav.Data;
using DotsNav.Drawing;
using DotsNav.PathFinding.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using static DotsNav.Math;

namespace DotsNav.PathFinding.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    partial struct DrawAgentSystem : ISystem
    {
        ComponentLookup<LocalToWorld> _localToWorldLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localToWorldLookup.Update(ref state);
            new DrawAgentJob { LtwLookup = _localToWorldLookup }.Schedule();
            DotsNavRenderer.Handle.Data = JobHandle.CombineDependencies(DotsNavRenderer.Handle.Data, state.Dependency);
        }

        [BurstCompile]
        partial struct DrawAgentJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            
            void Execute(RadiusComponent radius, AgentDrawComponent settings, DynamicBuffer<PathSegmentElement> path, NavmeshAgentComponent navmesh)
            {
                if (!settings.Draw || path.Length == 0)
                    return;

                var ltw = LtwLookup[navmesh.Navmesh].Value;

                var lines = new NativeList<Line>(Allocator.Temp);
                var color = settings.Color;
                if (color.a < 10)
                    color.a += 10;

                for (int j = 0; j < path.Length; j++)
                {
                    var segment = path[j];
                    var perp = normalize(PerpCcw(segment.To - segment.From)) * radius;
                    lines.Add(new Line(transform(ltw, (segment.From + perp).ToXxY()), transform(ltw, (segment.To + perp).ToXxY()), color));
                    lines.Add(new Line(transform(ltw, (segment.From - perp).ToXxY()), transform(ltw, (segment.To - perp).ToXxY()), color));
                }

                var up = rotate(ltw, new float3(0, 1, 0));

                for (int j = 1; j < path.Length; j++)
                {
                    var f = path[j - 1].To;
                    var c = path[j].Corner;
                    var t = path[j].From;
                    var a = (Angle) Angle(t - c) - Angle(f - c);
                    Arc.Draw(lines, transform(ltw, c.ToXxY()), up, rotate(ltw, 2 * (f - c).ToXxY()), a, color, radius, settings.Delimit);
                }

                var arm = rotate(ltw, new float3(0, 0, radius));
                Arc.Draw(lines, transform(ltw, path[0].From.ToXxY()), up, arm, 2 * PI, color, radius, settings.Delimit);
                Arc.Draw(lines, transform(ltw, path[^1].To.ToXxY()), up, arm, 2 * PI, color, radius, settings.Delimit);

                Line.Draw(lines);
            }
        }
    }
}