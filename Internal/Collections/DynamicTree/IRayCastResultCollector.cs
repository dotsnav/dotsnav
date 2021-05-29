namespace DotsNav.Collections
{
    public interface IRayCastResultCollector<in T>
    {
        float RayCastCallback(RayCastInput subInput, T node);
    }
}