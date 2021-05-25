using Unity.Entities;

namespace DotsNav.Collections
{
    public interface IQueryResultCollector
    {
        bool QueryCallback(Entity node);
    }
}