using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    unsafe struct ObstacleDistance : IBufferElementData
    {
        public float Dist;
        public Obstacle* Obstacle;

        public ObstacleDistance(float dist, Obstacle* obstacle)
        {
            Dist = dist;
            Obstacle = obstacle;
        }
    }
}