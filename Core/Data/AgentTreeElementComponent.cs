using DotsNav.Core.Collections.BVH;
using Unity.Entities;

namespace DotsNav.Core.Data
{
    public struct AgentTreeElementComponent : IComponentData
    {
        public Entity TreeEntity;
        internal DynamicTree<Entity> TreeRef;
    }
}