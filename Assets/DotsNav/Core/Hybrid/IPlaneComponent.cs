using Unity.Entities;

namespace DotsNav.Core.Hybrid
{
    public interface IPlaneComponent
    {
        void InsertObstacle(Entity obstacle, EntityManager em);
    }
}