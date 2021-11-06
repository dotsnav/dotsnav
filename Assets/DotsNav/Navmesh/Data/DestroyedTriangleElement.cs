using Unity.Entities;

namespace DotsNav.Navmesh.Data
{
    /// <summary>
    /// Ids of destroyed triangles are output to a DynamicBuffer&lt;DestroyedTriangleElement&gt; each navmesh update
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct DestroyedTriangleElement : IBufferElementData
    {
        public int Value;

        DestroyedTriangleElement(int v)
        {
            Value = v;
        }

        public static implicit operator int(DestroyedTriangleElement e) => e.Value;
        public static implicit operator DestroyedTriangleElement(int v) => new DestroyedTriangleElement(v);
    }
}