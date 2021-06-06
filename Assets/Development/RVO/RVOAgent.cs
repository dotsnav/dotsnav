using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

public class RVOAgent : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] float Radius;
    [SerializeField] float MaxSpeed;
    [SerializeField] int MaxNeighbours;
    [SerializeField] float NeighbourDist;
    [SerializeField] float TimeHorizon;
    [SerializeField] float TimeHorizonObst;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AgentComponent
        {
            NeighbourDist = NeighbourDist,
            TimeHorizon = TimeHorizon,
            MaxSpeed = MaxSpeed,
            TimeHorizonObst = TimeHorizonObst,
            MaxNeighbours = MaxNeighbours,
        });

        dstManager.AddComponentData(entity, new RadiusComponent(Radius));
        dstManager.AddComponent<VelocityObstacleComponent>(entity);
    }
}