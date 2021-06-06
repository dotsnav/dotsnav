using Unity.Mathematics;

namespace DotsNav.LocalAvoidance.Data
{
    struct VelocityObstacle
    {
        readonly VelocityObstacleComponent _obstacle;
        public float Dist;

        public float2 Position => _obstacle.Position;
        public float2 Velocity => _obstacle.Velocity;
        public float Radius => _obstacle.Radius;

        public VelocityObstacle(VelocityObstacleComponent obstacle)
        {
            _obstacle = obstacle;
            Dist = 0;
        }
    }
}