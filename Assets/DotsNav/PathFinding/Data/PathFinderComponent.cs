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
        public PathQueryState RecalculateFlags;
        public const PathQueryState DefaultRecalculateFlags = ~(PathQueryState.Inactive | PathQueryState.PathFound);

        public PathFinderComponent(PathQueryState recalculateFlags)
        {
            MaxInstances = JobsUtility.JobWorkerMaximumCount + 2;
            RecalculateFlags = recalculateFlags;
        }
    }
}