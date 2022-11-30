using Unity.Mathematics;
using System;
using DotsNav.Navmesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Writer = Unity.Collections.NativeQueue<DotsNav.CollisionDetection.Hybrid.RayCastHit>.ParallelWriter;

namespace DotsNav.CollisionDetection.Hybrid
{
    public unsafe class RayCastResult : IDisposable
    {
        public bool CollisionDetected => _output.Length > 0;
        public ReadOnly<RayCastHit> Hits => new((RayCastHit*) _output.GetUnsafeReadOnlyPtr(), _output.Length);
        readonly NativeList<RayCastHit> _output;

        internal RayCastResult(NativeList<RayCastHit> output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.Dispose();
        }
    }

    [BurstCompile]
    struct RayCastJob : IJob
    {
        public RayCast Input;
        public Navmesh.Navmesh Navmesh;

        public void Execute()
        {
            Navmesh.CastRay(Input, Allocator.Temp);
        }
    }

    struct RayCast : IRayCast
    {
        readonly bool _all;
        [NativeDisableContainerSafetyRestriction] // todo unity believes _output is readonly
        readonly NativeList<RayCastHit> _output;

        public float2 Origin { get; }
        public float2 Direction { get; }
        public float Distance { get; }

        public RayCast(float2 origin, float2 direction, float distance, bool all, NativeList<RayCastHit> output)
        {
            _all = all;
            _output = output;
            Origin = origin;
            Direction = direction;
            Distance = distance;
        }

        public unsafe bool RegisterCollision(float2 position, Edge* edge)
        {
            var constraints = edge->Constraints;
            for (int i = 0; i < constraints.Length; i++)
                _output.Add(new RayCastHit(position, constraints[i]));
            return _all;
        }
    }

    public struct RayCastHit
    {
        public readonly float2 Position;
        public readonly Entity Obstacle;

        public RayCastHit(float2 position, Entity constraint)
        {
            Position = position;
            Obstacle = constraint;
        }
    }
}
