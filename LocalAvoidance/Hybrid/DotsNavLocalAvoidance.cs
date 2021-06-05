using DotsNav.Data;
using DotsNav.Navmesh.Hybrid;
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
                DstEntityManager.AddComponentData(entity, new DynamicTreeElementComponent {Tree = localAvoidance.DynamicTree.Entity});
                DstEntityManager.AddComponentData(entity, new ObstacleTreeAgentComponent {Tree = localAvoidance.ObstacleTree.Entity});
                DstEntityManager.AddComponentData(entity, new DirectionComponent());
            });
        }
    }

    public class DotsNavLocalAvoidance : MonoBehaviour
    {
        public DotsNavDynamicTree DynamicTree;
        public DotsNavObstacleTree ObstacleTree;
    }
}