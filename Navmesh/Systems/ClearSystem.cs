using DotsNav.Navmesh.Data;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup), OrderFirst = true)]
    class ClearSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<NavmeshDrawComponent>();
        }

        protected override void OnUpdate()
        {
            DotsNavRenderer.Clear();
        }
    }
}