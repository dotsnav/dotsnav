using Unity.Entities;

namespace DotsNav.LocalAvoidance.Data
{
    public struct ObstacleTreeElementComponent : IComponentData
    {
        public Entity Tree;
    }
}