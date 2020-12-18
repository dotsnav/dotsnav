using DotsNav.Data;
using Unity.Entities;

namespace DotsNav.Hybrid
{
    /// <summary>
    /// Create Burst compatible struct implementing this interface to bulk insert permanant obstacles through Navmesh.InsertObstacleBulk
    /// </summary>
    public interface IObstacleAdder
    {
        void Add(int index, DynamicBuffer<VertexElement> vertices);
    }
}