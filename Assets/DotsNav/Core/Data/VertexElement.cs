using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Data
{
    /// <summary>
    /// Add DynamicBuffer&lt;VertexElement&gt; in addition to ObstacleData to queue insertion of obstacles,
    /// or in addition to DynamicBuffer&lt;VertexAmountElement&gt; to queue bulk insertion of permanent obstacles
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct VertexElement : IBufferElementData
    {
        public float2 Value;

        VertexElement(float2 v)
        {
            Value = v;
        }

        public static implicit operator float2(VertexElement e) => e.Value;
        public static implicit operator VertexElement(float2 v) => new(v);
    }
}