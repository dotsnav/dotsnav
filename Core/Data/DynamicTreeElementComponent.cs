using DotsNav.BVH;
using Unity.Entities;

namespace DotsNav.Data
{
    public struct DynamicTreeElementComponent : IComponentData
    {
        public Entity Tree;
        internal DynamicTree<Entity> TreeRef;

        public void Query<T>(T collector, AABB aabb) where T : IQueryResultCollector<Entity>
        {
            TreeRef.Query(collector, aabb);
        }
    }
}