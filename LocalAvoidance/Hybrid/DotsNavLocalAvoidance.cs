using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Hybrid;

namespace DotsNav.LocalAvoidance.Hybrid
{
    class LocalAvoidanceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavLocalAvoidance obstacleTree) =>
            {
                var entity = GetPrimaryEntity(obstacleTree);
                obstacleTree.Entity = entity;
                obstacleTree.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new ObstacleTreeComponent());
                DstEntityManager.AddComponentData(entity, new DynamicTreeComponent());
            });
        }
    }

    public class DotsNavLocalAvoidance : EntityLifetimeBehaviour
    {
    }
}