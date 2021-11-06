using Unity.Entities;

namespace DotsNav.LocalAvoidance.Data
{
    public struct ObstacleTreeAgentComponent : IComponentData
    {
        public Entity Tree;
    }
}