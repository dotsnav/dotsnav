using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Data
{
    /// <summary>
    /// Vertices in object space that can be shared between obstacles
    /// </summary>
    public struct VertexBlob
    {
        public BlobArray<float2> Vertices;
    }
}