using DotsNav.Drawing;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.LocalAvoidance.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup))]
    partial struct DrawObstacleTreeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DrawObstacleTreeJob().Schedule();
            DotsNavRenderer.Handle.Data = JobHandle.CombineDependencies(DotsNavRenderer.Handle.Data, state.Dependency);
        }
        
        [BurstCompile]
        unsafe partial struct DrawObstacleTreeJob : IJobEntity
        {
            void Execute(ObstacleTreeComponent tree, DrawComponent color, LocalToWorld ltw)
            {
                var lines = new Lines(tree.TreeRef.Count);
                var e = tree.TreeRef.GetEnumerator(Allocator.Temp);
                while (e.MoveNext())
                {
                    var a = math.transform(ltw.Value, e.Current.Point.ToXxY());
                    var b = math.transform(ltw.Value, e.Current.Next->Point.ToXxY());
                    lines.Draw(a, b, color.Color);
                }
            }
        }
    }
}