using DotsNav.Data;
using DotsNav.Navmesh.Data;
using Unity.Entities;

namespace DotsNav.Navmesh.Hybrid
{
    /// <summary>
    /// Create Burst compatible struct implementing this interface to bulk insert permanant obstacles through Navmesh.InsertObstacleBulk
    /// </summary>
    public interface IObstacleAdder
    {
        void Add(int index, DynamicBuffer<VertexElement> vertices);
    }
}