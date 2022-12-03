using Unity.Entities;

namespace DotsNav.Systems
{
    /// <summary>
    /// All DotsNav operations occur in this System Group, which is updated in FixedStepSimulationSystemGroup by default.
    /// Alternatively remove this group from FixedStepSimulationSystemGroup's update list and re-insert or update manually.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public class DotsNavSystemGroup : ComponentSystemGroup
    {
    }
}