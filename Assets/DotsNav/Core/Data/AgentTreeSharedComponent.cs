using Unity.Entities;

namespace DotsNav.Core.Data
{
    public struct AgentTreeSharedComponent : ISharedComponentData
    {
        public Entity Value;

        public AgentTreeSharedComponent(Entity value)
        {
            Value = value;
        }

        public static implicit operator Entity(AgentTreeSharedComponent e) => e.Value;
        public static implicit operator AgentTreeSharedComponent(Entity v) => new AgentTreeSharedComponent(v);
    }
}