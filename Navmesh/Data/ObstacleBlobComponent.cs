using Unity.Entities;

namespace DotsNav.Data
{
    /// <summary>
    /// Add to trigger bulk obstacle insertion
    /// </summary>
    public struct ObstacleBlobComponent : IComponentData
    {
        public BlobAssetReference<ObstacleBlob> BlobRef;
    }
}