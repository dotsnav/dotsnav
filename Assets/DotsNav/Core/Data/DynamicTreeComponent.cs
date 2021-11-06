using DotsNav.BVH;
using Unity.Entities;

namespace DotsNav.Data
{
    public struct DynamicTreeComponent : IComponentData
    {
        internal DynamicTree<Entity> Tree;

        public void Query<T>(T collector, AABB aabb) where T : IQueryResultCollector<Entity>
        {
            Tree.Query(collector, aabb);
        }
    }
}