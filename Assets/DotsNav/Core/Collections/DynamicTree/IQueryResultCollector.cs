
namespace DotsNav.Collections
{
    public interface IQueryResultCollector<in T>
    {
        bool QueryCallback(T node);
    }
}