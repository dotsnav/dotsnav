using DotsNav.Core.Data;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

public class RVOAgent : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] float Radius;
    [SerializeField] float PrefSpeed;
    [SerializeField] float MaxSpeed;
    [SerializeField] int MaxNeighbours;
    [SerializeField] float NeighbourDist;
    [SerializeField] float TimeHorizon;
    [SerializeField] float TimeHorizonObst;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RVOComponent
        {
            PrefSpeed = PrefSpeed,
            NeighbourDist = NeighbourDist,
            InvTimeHorizon = 1 / TimeHorizon,
            MaxSpeed = MaxSpeed,
            InvTimeHorizonObst = 1 / TimeHorizonObst,
            MaxNeighbours = MaxNeighbours,
        });

        dstManager.AddComponentData(entity, new RadiusComponent(Radius));
        dstManager.AddComponent<DirectionComponent>(entity);
        dstManager.AddComponent<AgentTreeComponent>(entity);
        dstManager.AddComponent<VelocityObstacleComponent>(entity);
    }
}