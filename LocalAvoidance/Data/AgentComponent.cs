using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    public struct AgentComponent : IComponentData
    {
        public float2 PrefVelocity;
        public float PrefSpeed;
        public float NeighbourDist;
        public float InvTimeHorizon;
        public float MaxSpeed;
        public float2 Velocity;
        public float InvTimeHorizonObst;
        public int MaxNeighbours;
    }
}