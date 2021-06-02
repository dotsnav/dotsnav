using DotsNav.Core.Collections.BVH;
using Unity.Entities;

namespace DotsNav.Core.Data
{
    public struct DynamicTreeComponent : IComponentData
    {
        internal DynamicTree<Entity> Tree;
    }
}