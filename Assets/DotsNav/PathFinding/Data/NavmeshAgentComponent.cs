using Unity.Entities;

namespace DotsNav.PathFinding.Data
{
    public struct NavmeshAgentComponent : IComponentData
    {
        public Entity Navmesh;
    }
}