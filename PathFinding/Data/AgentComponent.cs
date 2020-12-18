using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.PathFinding.Data
{
    /// <summary>
    /// Entities with Agent DynamicBuffer&lt;PathSegmentElement&gt; and DynamicBuffer&lt;TriangleElement&gt; are considered for
    /// path searches when their State is part of PathFinderComponent.RecalculateFlags
    /// </summary>
    public struct AgentComponent : IComponentData
    {
        public float2 From;
        public float2 To;
        public float Radius;

        /// <summary>
        /// When State is part of PathFinderComponent.RecalculateFlags a path search is performed
        /// </summary>
        public AgentState State;

        /// <summary>
        /// Increased when a path has been calculated
        /// </summary>
        public int Version;
    }
}