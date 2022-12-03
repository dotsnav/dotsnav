using DotsNav.Hybrid;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav
{
    public static class EcbUtility
    {
        public static EntityCommandBufferSystem Get(World world)
        {
            if (world.EntityManager.CreateEntityQuery(typeof(RunnerSingleton)).HasSingleton<RunnerSingleton>())
                return world.GetOrCreateSystemManaged<EndDotsNavEntityCommandBufferSystem>();
            return world.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        }
    }
}