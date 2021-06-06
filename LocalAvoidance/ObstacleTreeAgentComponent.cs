using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    struct ObstacleTreeAgentComponent : IComponentData
    {
        public Entity Tree;
    }
}