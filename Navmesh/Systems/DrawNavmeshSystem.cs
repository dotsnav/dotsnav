using DotsNav.Drawing;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
                .WithAll<NavmeshComponent>()
                .ForEach((Navmesh navmesh) =>
                {
                    var enumerator = navmesh.GetEdgeEnumerator();
                    var lines = new NativeList<Line>(navmesh.Vertices * 3, Allocator.Temp);
                    while (enumerator.MoveNext())
                    {
                        var edge = enumerator.Current;
                        if (data.DrawMode == DrawMode.Constrained && !edge->Constrained)
                            continue;

                        var c = edge->Constrained ? constrainedColor : unconstrainedColor;
                        c.a += edge->Constrained ? 30 : 0;
                        lines.Add(new Line(edge->Org->Point.ToXxY(), edge->Dest->Point.ToXxY(), c));
                    }

                    Line.Draw(lines);
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}