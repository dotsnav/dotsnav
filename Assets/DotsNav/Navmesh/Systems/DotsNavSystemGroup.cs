using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    /// <summary>
    /// All DotsNav operations occur in this System Group, which is updated in FixedStepSimulationSystemGroup by default.
    /// Alternatively remove this group from FixedStepSimulationSystemGroup's update list and re-insert or update manually.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public class DotsNavSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// The EntityCommandBufferSystem used by all DotsNav jobs, default is EndFixedStepSimulationEntityCommandBufferSystem
        /// </summary>
        public static EntityCommandBufferSystem EcbSource;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSource = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        }
    }
}