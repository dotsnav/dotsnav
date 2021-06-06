namespace DotsNav.Hybrid
{
    class ObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavObstacle obstacle) =>
            {
                var entity = GetPrimaryEntity(obstacle);
                obstacle.Entity = entity;
                obstacle.World = DstEntityManager.World;
            });
        }
    }

    public class DotsNavObstacle : EntityLifetimeBehaviour
    {
    }
}