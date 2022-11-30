using System;
using DotsNav.Navmesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DotsNav.CollisionDetection.Hybrid
{
    public unsafe class DiscCastResult : IDisposable
    {
        public bool CollisionDetected => _output.Length > 0;
        public ReadOnly<Entity> Obstacles => new((Entity*) _output.GetUnsafeReadOnlyPtr(), _output.Length);
        readonly NativeList<Entity> _output;

        internal DiscCastResult(NativeList<Entity> output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.Dispose();
        }
    }

    [BurstCompile]
    struct DiscCastJob : IJob
    {
        public DiscCast Input;
        public Navmesh.Navmesh Navmesh;

        public void Execute()
        {
            Navmesh.CastDisc(Input, Allocator.Temp);
        }
    }

    struct DiscCast : IDiscCast
    {
        readonly bool _all;

        // todo unity believes _output is readonly
        //readonly NativeHashSet<Entity> _output;
        [NativeDisableContainerSafetyRestriction]
        readonly NativeList<Entity> _output;

        public DiscCast(float2 origin, float radius, bool all, NativeList<Entity> output)
        {
            Origin = origin;
            Radius = radius;
            _all = all;
            _output = output;
        }

        public float2 Origin { get; }
        public float Radius { get; }

        public unsafe bool RegisterCollision(Edge* edge)
        {
            var constraints = edge->Constraints;
            for (int i = 0; i < constraints.Length; i++)
                //_output.Add(constraints[i]);
                if (!_output.Contains(constraints[i]))
                    _output.Add(constraints[i]);
            return _all;
        }
    }
}
