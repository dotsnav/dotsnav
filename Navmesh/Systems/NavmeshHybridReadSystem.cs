using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Navmesh.Hybrid;
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
                .ForEach((DotsNavPlane plane, DotsNavNavmesh hybrid, ref NavmeshDrawComponent debug) =>
                {
                    debug.DrawMode = hybrid.DrawMode;
                    debug.ConstrainedColor = plane.ConstrainedColor;
                    debug.UnconstrainedColor = plane.UnconstrainedColor;
                })
                .Run();
        }
    }
}