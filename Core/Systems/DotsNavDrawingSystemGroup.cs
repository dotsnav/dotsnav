using Unity.Entities;

namespace DotsNav.Core.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    public class DotsNavDrawingSystemGroup : ComponentSystemGroup
    {
    }
}