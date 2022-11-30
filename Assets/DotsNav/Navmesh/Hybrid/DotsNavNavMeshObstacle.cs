using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.Navmesh.Hybrid
{
    /// <summary>
    /// Create to triggers insertion of a navmesh obstacle. Destroy to trigger removal of a navmesh obstacle.
    /// </summary>
    [RequireComponent(typeof(DotsNavObstacle))]
    public class DotsNavNavMeshObstacle : MonoBehaviour, IToEntity
    {
        public void Convert(EntityManager entityManager, Entity entity)
        {
            entityManager.AddComponentData(entity, new NavmeshObstacleComponent());
        }
    }
}