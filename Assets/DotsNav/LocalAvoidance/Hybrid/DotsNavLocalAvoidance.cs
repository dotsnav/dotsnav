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
                DstEntityManager.AddComponentData(entity, new DynamicTreeElementComponent {Tree = localAvoidance.AgentTree.Entity});
                DstEntityManager.AddComponentData(entity, new ObstacleTreeAgentComponent {Tree = localAvoidance.ObstacleTree.Entity});
            });
        }
    }

    public class DotsNavLocalAvoidance : MonoBehaviour
    {
        public DotsNavAgentTree AgentTree;
        public DotsNavObstacleTree ObstacleTree;
    }
}