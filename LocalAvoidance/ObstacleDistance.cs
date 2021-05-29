using Unity.Entities;

namespace DotsNav.LocalAvoidance
{
    struct ObstacleDistance : IBufferElementData
    {
        public float Dist;
        public int Obstacle;

        public ObstacleDistance(float dist, int obstacle)
        {
            Dist = dist;
            Obstacle = obstacle;
        }
    }
}