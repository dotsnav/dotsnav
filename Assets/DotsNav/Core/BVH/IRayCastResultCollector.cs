namespace DotsNav.BVH
{
    interface IRayCastResultCollector<in T>
    {
        float RayCastCallback(RayCastInput subInput, T node);
    }
}