using System;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    class LocalAvoidanceAgentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavLocalAvoidanceAgent agent) =>
            {
                var tree = agent.LocalAvoidance.Entity;
                var entity = GetPrimaryEntity(agent);
                DstEntityManager.AddComponentData(entity, new DynamicTreeElementComponent {Tree = tree});
                DstEntityManager.AddComponentData(entity, new ObstacleTreeAgentComponent {Tree = tree});

                DstEntityManager.AddComponentData(entity, new LocalAvoidanceSettings
                {
                    NeighbourDist = agent.NeighbourDist,
                    TimeHorizon = agent.TimeHorizon,
                    TimeHorizonObst = agent.TimeHorizonObst,
                    MaxNeighbours = agent.MaxNeighbours,
                });

                DstEntityManager.AddComponentData(entity, new RadiusComponent(agent.Radius));
                DstEntityManager.AddComponentData(entity, new MaxSpeedComponent {Value = agent.MaxSpeed});
                DstEntityManager.AddComponent<VelocityObstacleComponent>(entity);
                DstEntityManager.AddComponent<PreferredVelocityComponent>(entity);
                DstEntityManager.AddComponent<VelocityComponent>(entity);
            });
        }
    }

    [RequireComponent(typeof(DotsNavAgent))]
    public class DotsNavLocalAvoidanceAgent : MonoBehaviour
    {
        public DotsNavLocalAvoidance LocalAvoidance;
        public float Radius;
        public float MaxSpeed;
        public int MaxNeighbours;
        public float NeighbourDist;
        public float TimeHorizon;
        public float TimeHorizonObst;
        [NonSerialized]
        public float2 Velocity;
    }
}