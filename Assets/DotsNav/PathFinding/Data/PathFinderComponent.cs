using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;

namespace DotsNav.PathFinding.Data
{
    /// <summary>
    /// Create to trigger the creation of a path finder. Destroy to trigger destruction a path finder. RecalculateFlags
    /// determine which agent states are considered for path searches
    /// </summary>
    public struct PathFinderComponent : IComponentData
    {
        internal readonly int MaxInstances;

        /// <summary>
        /// Use to indicate paths for agents in which states should be recomputed
        /// </summary>
        public AgentState RecalculateFlags;
        public const AgentState DefaultRecalculateFlags = ~(AgentState.Inactive | AgentState.PathFound);

        public PathFinderComponent(AgentState recalculateFlags)
        {
            MaxInstances = JobsUtility.JobWorkerMaximumCount + 2;
            RecalculateFlags = recalculateFlags;
        }
    }
}