using DotsNav.Core.Collections.BVH;
using Unity.Entities;

namespace DotsNav.Core.Data
{
    public struct AgentTreeComponent : IComponentData
    {
        internal DynamicTree<Entity> Tree;
    }
}