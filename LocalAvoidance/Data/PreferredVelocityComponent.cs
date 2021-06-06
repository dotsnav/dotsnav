using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    public struct PreferredVelocityComponent : IComponentData
    {
        public float2 Value;
    }
}