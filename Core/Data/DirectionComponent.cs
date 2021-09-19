using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Data
{
    /// <summary>
    /// When a Translation and DynamicBuffer&lt;PathSegmentElement&gt; are present the direction needed to follow the path is calculated
    /// </summary>
    public struct DirectionComponent : IComponentData
    {
        public float2 Value;
        internal int QueryVersion;
        internal int SegmentIndex;
        public static implicit operator float2(DirectionComponent e) => e.Value;
    }
}