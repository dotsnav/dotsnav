using DotsNav.Core;
using DotsNav.Core.Collections;
using DotsNav.PathFinding.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.PathFinding
{
    readonly struct PathFinderInstance
    {
        readonly List<Gate> _channel;
        readonly Funnel _funnel;
        readonly ChannelSearch _astar;
        readonly Deque<Funnel.Node> _path;

        public PathFinderInstance(Allocator allocator)
        {
            _channel = new List<Gate>(64, allocator);
            _path = new Deque<Funnel.Node>(64, allocator);
            _funnel = new Funnel(64, allocator);
            _astar = new ChannelSearch(100, 10, allocator);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _funnel.Dispose();
            _astar.Dispose();
            _path.Dispose();
        }

        public PathQueryState FindPath(float2 from, float2 to, float radius, DynamicBuffer<PathSegmentElement> segments, DynamicBuffer<TriangleElement> triangleIds, Navmesh.Navmesh.Navmesh navmesh, out int cost)
        {
            triangleIds.Clear();
            var result = _astar.Search(from, to, navmesh, _channel, radius, triangleIds, out cost);
            if (result == PathQueryState.PathFound)
            {
                _funnel.GetPath(_channel, from, to, radius, _path);
                triangleIds.Reinterpret<int>().AsNativeArray().Sort();
            }

            for (int i = 0; i < _path.Count; i++)
            {
                var w = _path.FromFront(i);
                Assert.IsTrue(!float.IsNaN(w.To.x) && !float.IsNaN(w.To.y));

                segments.Add(new PathSegmentElement
                {
                    Corner = i > 0 ? _path.FromFront(i - 1).Vertex : w.From,
                    From = w.From,
                    To = w.To
                });
            }

            _astar.Clear();
            _channel.Clear();
            _funnel.Clear();
            _path.Clear();
            return result;
        }
    }
}