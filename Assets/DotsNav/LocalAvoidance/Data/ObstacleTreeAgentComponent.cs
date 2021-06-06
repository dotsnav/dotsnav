using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    public struct ObstacleTreeAgentComponent : IComponentData
    {
        public Entity Tree;
    }
}