
namespace DotsNav.Core.Collections.BVH
{
    public interface IQueryResultCollector<in T>
    {
        bool QueryCallback(T node);
    }
}