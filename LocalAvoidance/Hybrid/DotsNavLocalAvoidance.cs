using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    class LocalAvoidanceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavLocalAvoidance localAvoidance) =>
            {
                var entity = GetPrimaryEntity(localAvoidance);
                localAvoidance.Entity = entity;
                localAvoidance.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new ObstacleTreeComponent());
                DstEntityManager.AddComponentData(entity, new DynamicTreeComponent());
                if (localAvoidance.DrawObstacleTree)
                    DstEntityManager.AddComponentData(entity, new DrawComponent {Color = localAvoidance.Color});
            });
        }
    }

    public class DotsNavLocalAvoidance : EntityLifetimeBehaviour
    {
        public bool DrawObstacleTree;
        public Color Color;
    }
}