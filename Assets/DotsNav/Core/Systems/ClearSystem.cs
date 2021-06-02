using Unity.Entities;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavDrawingSystemGroup), OrderFirst = true)]
    class ClearSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            DotsNavRenderer.Clear();
        }
    }
}