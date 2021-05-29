using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    public class DotsNavDrawingSystemGroup : ComponentSystemGroup
    {
    }
}