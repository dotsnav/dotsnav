using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.Navmesh.Hybrid
{
    [UpdateAfter(typeof(PlaneConversionSystem))]
    class NavMeshObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<DotsNavNavMeshObstacle>()
                .ForEach((DotsNavObstacle obstacle) =>
                {
                    var entity = GetPrimaryEntity(obstacle);
                    DstEntityManager.AddComponentData(entity, new NavmeshObstacleComponent {Navmesh = obstacle.Plane.Entity});
                });
        }
    }

    /// <summary>
    /// Create to triggers insertion of a navmesh obstacle. Destroy to trigger removal of a navmesh obstacle.
    /// </summary>
    [RequireComponent(typeof(DotsNavObstacle))]
    public class DotsNavNavMeshObstacle : MonoBehaviour
    {
    }
}