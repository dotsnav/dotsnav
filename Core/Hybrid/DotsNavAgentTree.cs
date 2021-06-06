using DotsNav.Data;

namespace DotsNav.Navmesh.Hybrid
{
    class DynamicTreeConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavAgentTree obstacleTree) =>
            {
                var entity = GetPrimaryEntity(obstacleTree);
                obstacleTree.Entity = entity;
                obstacleTree.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new DynamicTreeComponent());
            });
        }
    }

    public class DotsNavAgentTree : EntityLifetimeBehaviour
    {
    }
}