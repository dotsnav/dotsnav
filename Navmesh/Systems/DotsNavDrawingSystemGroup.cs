using Unity.Entities;

namespace DotsNav.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    public class DotsNavDrawingSystemGroup : ComponentSystemGroup
    {
    }
}