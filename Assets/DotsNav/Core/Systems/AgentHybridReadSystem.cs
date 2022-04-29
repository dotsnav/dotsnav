using DotsNav.Data;
using DotsNav.Hybrid;
using Unity.Entities;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderFirst = true)]
    partial class AgentHybridReadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((DotsNavAgent monoAgent, ref RadiusComponent radius) =>
                {
                    radius.Value = monoAgent.Radius;
                })
                .Run();
        }
    }
}