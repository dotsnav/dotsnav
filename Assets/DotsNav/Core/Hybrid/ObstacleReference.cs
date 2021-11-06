using System;
using Unity.Entities;

namespace DotsNav.Core.Hybrid
{
    /// <summary>
    /// Used to indicate which obstacle should be removed. Obtained when inserting obstacles.
    /// </summary>
    public struct ObstacleReference : IEquatable<ObstacleReference>, IComparable<ObstacleReference>
    {
        /// <summary>
        /// The bounding box and bulk inserted obstacles are static and con not be removed from the navmesh
        /// </summary>
        public bool IsStatic => Value == Entity.Null;

        internal Entity Value;

        internal ObstacleReference(Entity entity)
        {
            Value = entity;
        }

        public bool Equals(ObstacleReference other) => Value.Equals(other.Value);
        public int CompareTo(ObstacleReference other) => Value.CompareTo(other.Value);
    }
}