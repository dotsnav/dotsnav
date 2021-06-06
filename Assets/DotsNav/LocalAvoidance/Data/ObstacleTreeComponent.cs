using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    struct ObstacleTreeComponent : IComponentData
    {
        internal ObstacleTree TreeRef;
    }
}