using Unity.Entities;

struct SteeringComponent : IComponentData
{
    public float PreferredSpeed;
    public float BrakeSpeed;
}