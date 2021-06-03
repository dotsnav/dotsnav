using DotsNav.BVH;
using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    struct ObstacleTreeElementComponent : IComponentData
    {
        internal ObstacleTree TreeRef;

        public void Query<T>(T collector, AABB aabb) where T : IQueryResultCollector<IntPtr>
        {
            TreeRef.Query(collector, aabb);
        }
    }
}