using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [UpdateAfter(typeof(PlaneConversionSystem))]
    class LocalAvoidanceAgentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavAgent agent, DotsNavLocalAvoidanceAgent localAvoidanceAgent) =>
            {
                var tree = agent.Plane.Entity;
                var entity = GetPrimaryEntity(localAvoidanceAgent);
                DstEntityManager.AddComponentData(entity, new DynamicTreeElementComponent {Tree = tree});
                DstEntityManager.AddComponentData(entity, new ObstacleTreeAgentComponent {Tree = tree});

                DstEntityManager.AddComponentData(entity, new RVOSettingsComponent
                {
                    NeighbourDist = localAvoidanceAgent.NeighbourDist,
                    TimeHorizon = localAvoidanceAgent.TimeHorizon,
                    TimeHorizonObst = localAvoidanceAgent.TimeHorizonObst,
                    MaxNeighbours = localAvoidanceAgent.MaxNeighbours,
                });

                DstEntityManager.AddComponentData(entity, new MaxSpeedComponent {Value = localAvoidanceAgent.MaxSpeed});
                DstEntityManager.AddComponent<VelocityObstacleComponent>(entity);
                DstEntityManager.AddComponent<PreferredVelocityComponent>(entity);
                DstEntityManager.AddComponent<VelocityComponent>(entity);
            });
        }
    }

    [RequireComponent(typeof(DotsNavAgent))]
    public class DotsNavLocalAvoidanceAgent : MonoBehaviour
    {
        public float MaxSpeed;
        public int MaxNeighbours;
        public float NeighbourDist;
        public float TimeHorizon;
        public float TimeHorizonObst;
        public Vector3 Velocity { get; internal set; }
    }
}