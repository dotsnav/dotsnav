using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    public struct VelocityComponent : IComponentData
    {
        public float3 Value;
    }
}