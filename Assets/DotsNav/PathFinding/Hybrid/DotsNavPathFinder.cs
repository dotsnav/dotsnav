using DotsNav.Hybrid;
using DotsNav.PathFinding.Data;
using Unity.Entities;

namespace DotsNav.PathFinding.Hybrid
{
    public class DotsNavPathFinder : ToEntity
    {
        /// <summary>
        /// Set to indicate which agent states result in recalculating an agent's path
        /// </summary>
        public RecalculateFlags RecalculateFlags = ~RecalculateFlags.PathFound;

        internal PathQueryState GetRecalculateFlags() => (PathQueryState) (int) RecalculateFlags | PathQueryState.Pending;

        protected override void Convert(EntityManager entityManager, Entity entity)
        {
            entityManager.AddComponentData(entity, new PathFinderComponent(GetRecalculateFlags()));
            entityManager.AddComponentObject(entity, this);
        }
    }
}