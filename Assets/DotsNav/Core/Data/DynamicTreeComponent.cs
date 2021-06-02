using DotsNav.BVH;
using Unity.Entities;

namespace DotsNav.Data
{
    public struct DynamicTreeComponent : IComponentData
    {
        internal DynamicTree<Entity> Tree;
    }
}