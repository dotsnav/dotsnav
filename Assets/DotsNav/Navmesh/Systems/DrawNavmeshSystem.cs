using DotsNav.Drawing;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    partial class DrawNavmeshSystem : SystemBase
    {
        protected override unsafe void OnUpdate()
        {
            Entities
                .WithBurst()
                // .ForEach((NavmeshComponent navmesh, LocalToWorld ltw, NavmeshDrawComponent data) => todo ltw
                .ForEach((NavmeshComponent navmesh, NavmeshDrawComponent data) =>
                {
                    if (data.DrawMode == DrawMode.None || navmesh.Navmesh == null)
                        return;

                    var enumerator = navmesh.Navmesh->GetEdgeEnumerator();
                    var lines = new NativeList<Line>(navmesh.Navmesh->Vertices * 3, Allocator.Temp);

                    while (enumerator.MoveNext())
                    {
                        var edge = enumerator.Current;
                        if (data.DrawMode == DrawMode.Constrained && !edge->Constrained)
                            continue;

                        var c = edge->Constrained ? data.ConstrainedColor : data.UnconstrainedColor;
                        c.a += edge->Constrained ? 30 : 0;
                        
                        // todo ltw
                        // var a = math.transform(ltw.Value, edge->Org->Point.ToXxY());
                        // var b = math.transform(ltw.Value, edge->Dest->Point.ToXxY());
                        // lines.Add(new Line(a, b, c));
                        
                        lines.Add(new Line(edge->Org->Point.ToXxY(), edge->Dest->Point.ToXxY(), c));
                    }

                    Line.Draw(lines);
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}