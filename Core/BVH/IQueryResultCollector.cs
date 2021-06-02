
namespace DotsNav.BVH
{
    public interface IQueryResultCollector<in T>
    {
        bool QueryCallback(T node);
    }
}