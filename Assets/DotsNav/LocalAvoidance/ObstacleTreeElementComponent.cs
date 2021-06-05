using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    struct ObstacleTreeElementComponent : IComponentData
    {
        public Entity Tree;
    }

    struct ObstacleTreeAgentComponent : IComponentData
    {
        public Entity Tree;
    }
}