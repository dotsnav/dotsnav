using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [RequireComponent(typeof(DotsNavAgent))]
    public class DotsNavLocalAvoidanceAgent : MonoBehaviour, IToEntity
    {
        public float MaxSpeed;
        public int MaxNeighbours;
        public float NeighbourDist;
        public float TimeHorizon;
        public float TimeHorizonObst;
        public Vector3 Velocity { get; internal set; }
        
        public void Convert(EntityManager entityManager, Entity entity)
        {
            var tree = GetComponent<DotsNavAgent>().Plane.Entity;
            entityManager.AddComponentData(entity, new DynamicTreeElementComponent {Tree = tree});
            entityManager.AddComponentData(entity, new ObstacleTreeAgentComponent {Tree = tree});
            
            entityManager.AddComponentData(entity, new RVOSettingsComponent
            {
                NeighbourDist = NeighbourDist,
                TimeHorizon = TimeHorizon,
                TimeHorizonObst = TimeHorizonObst,
                MaxNeighbours = MaxNeighbours,
            });
            
            entityManager.AddComponentData(entity, new MaxSpeedComponent {Value = MaxSpeed});
            entityManager.AddComponent<VelocityObstacleComponent>(entity);
            entityManager.AddComponent<PreferredVelocityComponent>(entity);
            entityManager.AddComponent<VelocityComponent>(entity);
            entityManager.AddComponentObject(entity, this);
        }
    }
}