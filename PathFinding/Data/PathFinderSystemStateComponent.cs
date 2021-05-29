using DotsNav.Core;
using DotsNav.Core.Collections;
using Unity.Collections;
using Unity.Entities;

namespace DotsNav.PathFinding.Data
{
    struct PathFinderSystemStateComponent : ISystemStateComponentData
    {
        public List<PathFinderInstance> Instances;

        const string PathFinderDataNotInitializedMessage = "PathFinderComponent not initialized correctly, please use the constructor";

        public void Allocate(PathFinderComponent data)
        {
            Assert.IsTrue(data.MaxInstances > 0, PathFinderDataNotInitializedMessage);
            Instances = new List<PathFinderInstance>(data.MaxInstances, Allocator.Persistent);
            for (int i = 0; i < data.MaxInstances; i++)
                Instances.Add(new PathFinderInstance(Allocator.Persistent));
        }

        public void Dispose()
        {
            for (int i = 0; i < Instances.Length; i++)
                Instances[i].Dispose();
            Instances.Dispose();
        }
    }
}