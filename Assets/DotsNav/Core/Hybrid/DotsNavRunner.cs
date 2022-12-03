using DotsNav.Systems;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.Hybrid
{
    public class DotsNavRunner : MonoBehaviour
    {
        /// <summary>
        /// Determines when queued updates should be processed. When using manual also set // todo
        /// </summary>
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

        /// <summary>
        /// Call to trigger the insertion and removal of obstacles and path finder update
        /// </summary>
        public void ProcessModifications()
        {
            Assert.IsTrue(Mode == UpdateMode.Manual, $"Manually updating DotsNav requires UpdateMode to be Manual");
            ProcessModificationsInternal();
        }

        void Update()
        {
            if (Mode == UpdateMode.Update)
                ProcessModificationsInternal();
        }

        void FixedUpdate()
        {
            if (Mode == UpdateMode.FixedUpdate)
                ProcessModificationsInternal();
        }

        void ProcessModificationsInternal()
        {
            _dotsNavSystemGroup.Update();
        }

        void OnDestroy()
        {
            var worlds = World.All;

            if (worlds.Count == 0) // todo prevent silly exception
                return;

            var world = worlds[0];
            var group = world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            group.AddSystemToUpdateList(_dotsNavSystemGroup);
            world.DestroySystemManaged(world.GetExistingSystemManaged<EndDotsNavEntityCommandBufferSystem>());
            world.EntityManager.DestroyEntity(_singleton);
        }

        /// <summary>
        /// Determines when queued updates should be processed
        /// </summary>
        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            Manual
        }
    }
    
    public struct RunnerSingleton : IComponentData { }
}