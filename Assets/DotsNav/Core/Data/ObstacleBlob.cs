using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Data
{
    /// <summary>
    /// Used for bulk obstacle insertion
    /// </summary>
    public struct ObstacleBlob
    {
        public BlobArray<float2> Vertices;
        public BlobArray<int> Amounts;
    }
}