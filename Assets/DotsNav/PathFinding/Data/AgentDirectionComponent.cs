using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Data
{
    /// <summary>
    /// When a Translation and DynamicBuffer&lt;PathSegmentElement&gt; are present the direction needed to follow the path is calculated
    /// </summary>
    public struct AgentDirectionComponent : IComponentData
    {
        public float2 Value;

        AgentDirectionComponent(float2 v)
        {
            Value = v;
        }

        public static implicit operator float2(AgentDirectionComponent e) => e.Value;
        public static implicit operator AgentDirectionComponent(float2 v) => new AgentDirectionComponent(v);
    }
}