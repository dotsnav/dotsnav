namespace DotsNav.Hybrid
{
    class PlaneConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavPlane localAvoidance) =>
            {
                var entity = GetPrimaryEntity(localAvoidance);
                localAvoidance.Entity = entity;
                localAvoidance.World = DstEntityManager.World;
            });
        }
    }

    public class DotsNavPlane : EntityLifetimeBehaviour
    {
    }
}