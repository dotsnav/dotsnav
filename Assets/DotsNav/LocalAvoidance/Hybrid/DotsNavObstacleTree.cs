using DotsNav.Navmesh.Hybrid;

namespace DotsNav.LocalAvoidance.Hybrid
{
    class ObstacleTreeConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavObstacleTree obstacleTree) =>
            {
                var entity = GetPrimaryEntity(obstacleTree);
                obstacleTree.Entity = entity;
                obstacleTree.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new ObstacleTreeComponent());
            });
        }
    }

    public class DotsNavObstacleTree : EntityLifetimeBehaviour
    {
    }
}