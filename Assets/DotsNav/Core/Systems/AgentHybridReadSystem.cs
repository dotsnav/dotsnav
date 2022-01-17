using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderFirst = true)]
    class AgentHybridReadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((DotsNavAgent monoAgent, ref RadiusComponent radius) =>
                {
                    radius.Value = monoAgent.Radius;
                    radius.Priority = monoAgent.Priority;
                })
                .Run();
        }
    }
}