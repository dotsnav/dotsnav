using System;
using Unity.Entities;

namespace DotsNav.Data
{
    /// <summary>
    /// Create to trigger insertion of an obstacle. Destroy to trigger removal of an obstacle.
    /// Add a DynamicBuffer&lt;VertexElement&gt; or VertexBlobComponent to supply vertices
    /// </summary>
    public struct NavmeshObstacleComponent : IComponentData, IEquatable<NavmeshObstacleComponent>
    {
        public Entity Navmesh;

        public bool Equals(NavmeshObstacleComponent other) => Navmesh.Equals(other.Navmesh);
    }
}