using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [RequireComponent(typeof(DotsNavObstacle))]
    public class DotsNavLocalAvoidanceObstacle : MonoBehaviour, IToEntity
    {
        public void Convert(EntityManager entityManager, Entity entity)
        {
            var obstacle = GetComponent<DotsNavObstacle>();
            entityManager.AddComponentData(entity, new ObstacleTreeElementComponent{Tree = obstacle.Plane.Entity});
        }
    }
}