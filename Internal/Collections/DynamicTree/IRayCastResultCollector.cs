using Unity.Entities;

namespace DotsNav.Collections
{
    public interface IRayCastResultCollector
    {
        float RayCastCallback(RayCastInput subInput, Entity node);
    }
}