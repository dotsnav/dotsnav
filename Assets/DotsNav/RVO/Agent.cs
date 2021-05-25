using Unity.Entities;
using Unity.Mathematics;

public struct Agent : IComponentData
{
    public float2 Position;
    public float2 PrefVelocity;
    public float PrefSpeed;
    public float NeighbourDist;
    public float InvTimeHorizon;
    public float MaxSpeed;
    public float Radius;
    public float2 Velocity;
    public float InvTimeHorizonObst;
}