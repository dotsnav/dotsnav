using Unity.Entities;
using Unity.Mathematics;

public struct VelocityObstacleComponent : IComponentData
{
    public float2 Position;
    public float2 Velocity;
    public float Radius;
}

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