using DotsNav.Core.Collections.BVH;
using Unity.Entities;

namespace DotsNav.Core.Data
{
    public struct DynamicTreeElementComponent : IComponentData
    {
        public Entity Tree;
        internal DynamicTree<Entity> TreeRef;
    }
}