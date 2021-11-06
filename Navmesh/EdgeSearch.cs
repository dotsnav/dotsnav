using DotsNav.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Navmesh
{
    readonly struct EdgeSearch
    {
        readonly PriorityQueue<Step> _open;
        readonly HashSet<IntPtr> _closed;
        readonly List<Step> _steps;

        public EdgeSearch(int expectedNodes, int intialSteps, Allocator allocator) : this()
        {
            _open = new PriorityQueue<Step>(expectedNodes, allocator);
            _closed = new HashSet<IntPtr>(expectedNodes, allocator);
            _steps = new List<Step>(intialSteps, allocator);
        }

        public unsafe bool Search(Vertex* start, Vertex* goal, Entity constraintId)
        {
            Assert.IsTrue(start != goal);
            Clear();
            var open = _open;
            var closed = _closed;
            var steps = _steps;
            var goalPos = goal->Point;
            var step1 = new Step(start, 0, 0, math.length(goalPos - start->Point), -1, null);
            open.Insert(step1);
            steps.Add(step1);

            var iii = 0;
            while (open.Count > 0)
            {
                if (iii++ == 10000)
                    throw new InfiniteLoopException($"Edge search from: {start->ToString()} to: {goal->ToString()}");

                var step = open.Extract();

                if (step.Vertex == goal)
                {
                    while (step.Previous != -1)
                    {
                        if (!step.Edge->IsConstrainedBy(constraintId))
                        {
                            step.Edge->AddConstraint(constraintId);
                            Navmesh.ResetClearance(step.Edge);
                        }

                        step = steps[step.Previous];
                    }

                    return true;
                }

                closed.TryAdd((IntPtr) step.Vertex);

                var e = step.Vertex->Edge;
                var ii = 0;
                do
                {
                    if (ii++ == 10000)
                        throw new InfiniteLoopException($"Edge search from: {start->ToString()} to: {goal->ToString()}");

                    if (!closed.Contains((IntPtr) e->Dest))
                    {
                        var newStep = new Step(
                            e->Dest,
                            steps.Length,
                            step.G + math.length(e->Dest->Point - e->Org->Point),
                            math.length(goalPos - e->Dest->Point),
                            step.Id,
                            e);

                        steps.Add(newStep);
                        open.InsertOrLowerKey(newStep);
                    }

                    e = e->ONext;
                } while (e != step.Vertex->Edge);
            }

            return false;
        }

        void Clear()
        {
            _open.Clear();
            _closed.Clear();
            _steps.Clear();
        }

        public void Dispose()
        {
            _open.Dispose();
            _closed.Dispose();
            _steps.Dispose();
        }

        readonly unsafe struct Step : PriorityQueue<Step>.IElement
        {
            public int Id { get; }
            public readonly Vertex* Vertex;
            public readonly float G;
            public readonly int Previous;
            readonly float _gPlusH;
            public readonly Edge* Edge;

            public Step(Vertex* vertex, int stepId, float g, float h, int previous, Edge* edge)
            {
                Vertex = vertex;
                Id = stepId;
                G = g;
                _gPlusH = g + h;
                Previous = previous;
                Edge = edge;
            }

            public int CompareTo(Step other)
                => _gPlusH.CompareTo(other._gPlusH);
        }
    }
}