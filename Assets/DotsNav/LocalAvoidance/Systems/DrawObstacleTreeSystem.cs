using DotsNav.Drawing;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    partial class DrawObstacleTreeSystem : SystemBase
    {
        protected override void OnCreate()
        {
        }

        protected override unsafe void OnUpdate()
        {
            Entities
                .WithBurst()
                .ForEach((ObstacleTreeComponent tree, DrawComponent color) => // todo ltw , LocalToWorld ltw) =>
                {
                    var lines = new Lines(tree.TreeRef.Count);
                    var e = tree.TreeRef.GetEnumerator(Allocator.Temp);
                    while (e.MoveNext())
                    {
                        // todo ltw
                        // var a = math.transform(ltw.Value, e.Current.Point.ToXxY());
                        // var b = math.transform(ltw.Value, e.Current.Next->Point.ToXxY());
                        // lines.Draw(a, b, color.Color);
                        
                        lines.Draw(e.Current.Point.ToXxY(), e.Current.Next->Point.ToXxY(), color.Color);
                    }
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}