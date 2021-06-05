using Unity.Entities;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [UpdateAfter(typeof(ObstacleTreeConversionSystem))]
    class LocalAvoidanceObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavLocalAvoidanceObstacle localAvoidance) =>
            {
                var entity = GetPrimaryEntity(localAvoidance);
                DstEntityManager.AddComponentData(entity, new ObstacleTreeElementComponent{Tree = GetPrimaryEntity(localAvoidance.ObstacleTree)});
            });
        }
    }

    public class DotsNavLocalAvoidanceObstacle : MonoBehaviour
    {
        public DotsNavObstacleTree ObstacleTree;
    }
}