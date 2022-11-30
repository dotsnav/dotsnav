using Unity.Entities;

namespace DotsNav.PathFinding.Data
{
    /// <summary>
    /// Ids of the triangles traversed by an agent's path are stored in a DynamicBuffer&lt;TriangleId&gt;
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct TriangleElement : IBufferElementData
    {
        public readonly int Value;

        TriangleElement(int v)
        {
            Value = v;
        }

        public static implicit operator int(TriangleElement e) => e.Value;
        public static implicit operator TriangleElement(int v) => new(v);
    }
}