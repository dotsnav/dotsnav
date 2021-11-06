using DotsNav.Drawing;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    class DrawObstacleTreeSystem : SystemBase
    {
        protected override void OnCreate()
        {
        }

        protected override unsafe void OnUpdate()
        {
            Entities
                .WithBurst()
                .ForEach((ObstacleTreeComponent tree, DrawComponent color, LocalToWorld ltw) =>
                {
                    var lines = new Lines(tree.TreeRef.Count);
                    var e = tree.TreeRef.GetEnumerator(Allocator.Temp);
                    while (e.MoveNext())
                    {
                        var a = math.transform(ltw.Value, e.Current.Point.ToXxY());
                        var b = math.transform(ltw.Value, e.Current.Next->Point.ToXxY());
                        lines.Draw(a, b, color.Color);
                    }
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}