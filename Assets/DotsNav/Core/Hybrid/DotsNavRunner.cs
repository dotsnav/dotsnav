using DotsNav.Systems;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.Hybrid
{
    /// <summary>
    /// Provides control over when DotsNav updates are performed
    /// </summary>
    public class DotsNavRunner : MonoBehaviour
    {
        public UpdateMode Mode;

        DotsNavSystemGroup _dotsNavSystemGroup;
        Entity _singleton;

        protected void Awake()
        {
            var world = World.All[0];
            _dotsNavSystemGroup = world.GetOrCreateSystemManaged<DotsNavSystemGroup>();
            world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>().RemoveSystemFromUpdateList(_dotsNavSystemGroup);
            _dotsNavSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystemManaged<EndDotsNavEntityCommandBufferSystem>());
            _singleton = world.EntityManager.CreateSingleton<RunnerSingleton>();
        }

        public void Step()
        {
            Assert.IsTrue(Mode == UpdateMode.Manual, $"Manually updating DotsNav requires UpdateMode to be Manual");
            _dotsNavSystemGroup.Update();
        }

        void Update()
        {
            if (Mode == UpdateMode.Update)
                _dotsNavSystemGroup.Update();
        }

        void FixedUpdate()
        {
            if (Mode == UpdateMode.FixedUpdate)
                _dotsNavSystemGroup.Update();
        }

        void OnDestroy()
        {
            var worlds = World.All;
            if (worlds.Count == 0)
                return;
            var world = worlds[0];
            world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>().AddSystemToUpdateList(_dotsNavSystemGroup);
            world.DestroySystemManaged(world.GetExistingSystemManaged<EndDotsNavEntityCommandBufferSystem>());
            world.EntityManager.DestroyEntity(_singleton);
        }

        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            Manual
        }
    }
    
    public struct RunnerSingleton : IComponentData { }
}