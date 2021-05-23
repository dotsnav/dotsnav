using Unity.Entities;

namespace DotsNav.Data
{
    /// <summary>
    /// Add DynamicBuffer&lt;VertexAmountElement&gt; in addition to DynamicBuffer&lt;VertexElement&gt; to queue bulk insertion of permanent obstacles
    /// </summary>
    [InternalBufferCapacity(0)]
    struct VertexAmountElement : IBufferElementData
    {
        public readonly int Value;

        VertexAmountElement(int v)
        {
            Value = v;
        }

        public static implicit operator int(VertexAmountElement e) => e.Value;
        public static implicit operator VertexAmountElement(int v) => new VertexAmountElement(v);
    }
}