using Unity.Entities;

namespace DotsNav.LocalAvoidance.Data
{
    struct ObstacleTreeComponent : IComponentData
    {
        internal ObstacleTree TreeRef;
    }
}