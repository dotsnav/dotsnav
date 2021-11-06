using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    public struct VelocityComponent : IComponentData
    {
        public float2 Value;
        public float3 WorldSpace;
    }
}