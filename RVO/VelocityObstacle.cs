using Unity.Entities;
using Unity.Mathematics;

struct VelocityObstacle
{
    public readonly Entity Id;
    public readonly float2 Position;
    public float Dist;
    public readonly float2 Velocity;
    public readonly float Radius;

    public VelocityObstacle(Entity id, float2 position, float2 velocity, float radius)
    {
        Id = id;
        Position = position;
        Velocity = velocity;
        Radius = radius;
        Dist = 0;
    }
}