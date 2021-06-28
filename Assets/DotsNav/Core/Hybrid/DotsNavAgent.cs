using DotsNav.Data;

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
                Assert.IsTrue(agent.Radius > 0, "Radius must be larger than 0");
                DstEntityManager.AddComponentData(entity, new RadiusComponent {Value = agent.Radius});
            });
        }
    }

    public class DotsNavAgent : EntityLifetimeBehaviour
    {
        public float Radius = .5f;
    }
}