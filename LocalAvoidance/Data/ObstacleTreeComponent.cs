using Unity.Entities;

namespace DotsNav.LocalAvoidance.Data
{
    public struct ObstacleTreeComponent : IComponentData
    {
        internal ObstacleTree TreeRef;
    }
}