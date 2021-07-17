using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [UpdateAfter(typeof(PlaneConversionSystem))]
    class LocalAvoidanceObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<DotsNavLocalAvoidanceObstacle>()
                .ForEach((DotsNavObstacle obstacle) =>
            {
                var entity = GetPrimaryEntity(obstacle);
                DstEntityManager.AddComponentData(entity, new ObstacleTreeElementComponent{Tree = obstacle.Plane.Entity});
            });
        }
    }

    [RequireComponent(typeof(DotsNavObstacle))]
    public class DotsNavLocalAvoidanceObstacle : MonoBehaviour
    {
    }
}