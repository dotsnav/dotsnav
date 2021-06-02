using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderFirst = true)]
    class NavmeshHybridReadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((Hybrid.DotsNavNavmesh hybrid, ref NavmeshDrawComponent debug) =>
                {
                    debug.DrawMode = hybrid.DrawMode;
                    debug.ConstrainedColor = hybrid.ConstrainedColor;
                    debug.UnconstrainedColor = hybrid.UnconstrainedColor;
                })
                .Run();
        }
    }
}