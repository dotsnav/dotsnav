using Unity.Mathematics;

namespace DotsNav.Core.Collections.BVH
{
    public struct RayCastInput
    {
        public float2 P1;
        public float2 P2;
        public float MaxFraction;
    }
}