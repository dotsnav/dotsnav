using DotsNav.Data;
using DotsNav.Drawing;
using Unity.Entities;

namespace DotsNav.Systems
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