using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Writer = Unity.Collections.NativeQueue<DotsNav.CollisionDetection.Hybrid.RayCastHit>.ParallelWriter;

namespace DotsNav.CollisionDetection.Hybrid
{
    [BurstCompile]
    struct SegmentCastJob : IJob
    {
        public SegmentCast Input;
        public Navmesh Navmesh;

        public void Execute()
        {
            Navmesh.CastSegment(Input, Allocator.Temp);
        }
    }

    struct SegmentCast : ISegmentCast
    {
        readonly bool _all;
        [NativeDisableContainerSafetyRestriction] // todo unity believes _output is readonly
        readonly NativeList<RayCastHit> _output;

        public SegmentCast(float2 origin, float2 destination, bool all, NativeList<RayCastHit> output)
        {
            Origin = origin;
            Destination = destination;
            _all = all;
            _output = output;
        }

        public float2 Origin { get; }
        public float2 Destination { get; }

        public unsafe bool RegisterCollision(float2 position, Edge* edge)
        {
            var constraints = edge->Constraints;
            for (int i = 0; i < constraints.Length; i++)
                _output.Add(new RayCastHit(position, constraints[i]));
            return _all;
        }
    }
}
