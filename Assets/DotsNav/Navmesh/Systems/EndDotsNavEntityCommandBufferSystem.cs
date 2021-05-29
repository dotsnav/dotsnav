using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    /// <summary>
    /// Runs as last system in DotsNavSystemGroup. Assign to DotsNavSystemGroup.EcbSource to us as an alternative
    /// to EndFixedStepSimulationEntityCommandBufferSystem which is used by default.
    /// </summary>
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    public class EndDotsNavEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
}