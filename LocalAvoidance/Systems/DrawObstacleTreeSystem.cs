using DotsNav.Drawing;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
                .ForEach((ObstacleTreeComponent tree, DrawComponent color) =>
                {
                    var lines = new Lines(tree.TreeRef.Count);
                    var e = tree.TreeRef.GetEnumerator(Allocator.Temp);
                    while (e.MoveNext())
                        lines.Draw(e.Current.Point.ToXxY(), e.Current.Next->Point.ToXxY(), color.Color);
                })
                .Schedule();

            DotsNavRenderer.Handle = JobHandle.CombineDependencies(DotsNavRenderer.Handle, Dependency);
        }
    }
}