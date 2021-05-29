using System;
using DotsNav.Navmesh;
using Unity.Mathematics;

namespace DotsNav.PathFinding
{
    unsafe struct PossibleDisturbance : IComparable<PossibleDisturbance>
    {
        public readonly Vertex* Vertex;
        public double DistFromStart;
        public float2 Opposite;

        public PossibleDisturbance(Vertex* vertex) : this()
        {
            Vertex = vertex;
        }

        public int CompareTo(PossibleDisturbance other)
        {
            return DistFromStart.CompareTo(other.DistFromStart);
        }
    }
}