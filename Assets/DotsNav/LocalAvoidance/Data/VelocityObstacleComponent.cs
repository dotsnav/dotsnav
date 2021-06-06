using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    struct VelocityObstacleComponent : IComponentData
    {
        public float2 Position;
        public float2 Velocity;
        public float Radius;
    }
}