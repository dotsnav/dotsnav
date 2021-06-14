using DotsNav.Drawing;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    class DrawNavmeshSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<NavmeshDrawComponent>();
        }

        protected override unsafe void OnUpdate()
        {
            var data = GetSingleton<NavmeshDrawComponent>();
            if (data.DrawMode == DrawMode.None)
                return;
            var constrainedColor = data.ConstrainedColor;
            var unconstrainedColor = data.UnconstrainedColor;

            Entities
                .WithBurst()
                .ForEach((NavmeshComponent navmesh, LocalToWorld ltw) =>
                {
                    var enumerator = navmesh.Navmesh->GetEdgeEnumerator();
                    var lines = new NativeList<Line>(navmesh.Navmesh->Vertices * 3, Allocator.Temp);
                    while (enumerator.MoveNext())
                    {
                        var edge = enumerator.Current;
                        if (data.DrawMode == DrawMode.Constrained && !edge->Constrained)
                            continue;

                        var c = edge->Constrained ? constrainedColor : unconstrainedColor;
                        c.a += edge->Constrained ? 30 : 0;
                        var a = math.transform(ltw.Value, edge->Org->Point.ToXxY());
                        var b = math.transform(ltw.Value, edge->Dest->Point.ToXxY());
                        lines.Add(new Line(a, b, c));
                    }

                    Line.Draw(lines);
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}