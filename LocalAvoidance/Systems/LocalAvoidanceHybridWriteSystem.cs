using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Hybrid;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    class LocalAvoidanceHybridWriteSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((VelocityComponent velocity, DotsNavLocalAvoidanceAgent monoAgent) =>
                {
                    monoAgent.Velocity = velocity.WorldSpace;
                })
                .Run();
        }
    }
}