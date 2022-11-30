using DotsNav.Hybrid;
using Unity.Entities;

namespace DotsNav.Core.Hybrid
{
    public interface IPlaneComponent : IToEntity
    {
        void InsertObstacle(EntityManager em, Entity plane, Entity obstacle);
    }
}