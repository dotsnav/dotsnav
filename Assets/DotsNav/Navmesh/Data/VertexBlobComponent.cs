using Unity.Entities;

namespace DotsNav.Data
{
    /// <summary>
    /// Add in addition to ObstacleComponent to supply vertices through an VertexBlob
    /// </summary>
    public struct VertexBlobComponent : IComponentData
    {
        public BlobAssetReference<VertexBlob> BlobRef;
    }
}