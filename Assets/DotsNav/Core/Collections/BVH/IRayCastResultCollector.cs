namespace DotsNav.Core.Collections.BVH
{
    public interface IRayCastResultCollector<in T>
    {
        float RayCastCallback(RayCastInput subInput, T node);
    }
}