using DotsNav.Hybrid;
using DotsNav.PathFinding.Data;
using UnityEngine;

namespace DotsNav.PathFinding.Hybrid
{
    class PathFinderConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavPathFinder pathFinder) =>
            {
                var entity = GetPrimaryEntity(pathFinder);
                DstEntityManager.AddComponentData(entity, new PathFinderComponent(pathFinder.GetRecalculateFlags()));
            });
        }
    }

    /// <summary>
    /// Create to triggers insertion of the path finder. Destroy to trigger disposal of the path finder. Use RecalculateFlags to
    /// indicate which agent states result in recalculating an agent's path
    /// </summary>
    public class DotsNavPathFinder : EntityLifetimeBehaviour
    {
        /// <summary>
        /// Set to indicate which agent states result in recalculating an agent's path
        /// </summary>
        public RecalculateFlags RecalculateFlags = ~RecalculateFlags.PathFound;

        bool _created;

        internal PathQueryState GetRecalculateFlags() => (PathQueryState) (int) RecalculateFlags | PathQueryState.Pending;

        protected override void Awake()
        {
            base.Awake();

            if (_created)
            {
                Debug.LogError("Only one pathfinder is allowed");
                DestroyImmediate(this);
            }

            _created = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _created = false;
        }
    }
}