namespace DotsNav.Hybrid
{
    class AgentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavAgent agent) =>
            {
                var entity = GetPrimaryEntity(agent);
                agent.World = DstEntityManager.World;
                agent.Entity = entity;
            });
        }
    }

    public class DotsNavAgent : EntityLifetimeBehaviour
    {
    }
}