using System;
using System.Diagnostics;
using DotsNav.Collections;
using DotsNav.Drawing;
using DotsNav.Navmesh;
using DotsNav.PathFinding.Data;
using DotsNav.Predicates;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using IntPtr = DotsNav.IntPtr;
using Math = DotsNav.Math;

namespace DotsNav.PathFinding
{
    readonly unsafe struct ChannelSearch
    {
        readonly PriorityQueue<Step> _open;
        readonly HashSet<int> _closed;
        readonly HashSet<int> _closedEdges;
        readonly HashSet<IntPtr> _closedVerts;
        readonly List<Step> _steps;
        readonly List<PossibleDisturbance> _verts;
        readonly List<int> _validGoalEdges;

        public ChannelSearch(int maxNodes, int intialSteps, Allocator allocator) : this()
        {
            _open = new PriorityQueue<Step>(maxNodes);
            _closed = new HashSet<int>(maxNodes, allocator);
            _closedEdges = new HashSet<int>(36, allocator);
            _closedVerts = new HashSet<IntPtr>(36, allocator);
            _steps = new List<Step>(intialSteps, allocator);
            _verts = new List<PossibleDisturbance>(allocator);
            _validGoalEdges = new List<int>(allocator);
        }

        public PathQueryState Search(float2 start, float2 goal, Navmesh.Navmesh navmesh, List<Gate> path, float radius, DynamicBuffer<TriangleElement> triangleIds, out int cost)
        {
            var open = _open;
            var closed = _closed;
            var steps = _steps;
            var diameter = 2 * radius;
            var verts = _verts;
            var validGoalEdges = _validGoalEdges;
            cost = 80;

            if (!navmesh.Contains(start))
                return PathQueryState.StartInvalid;

            if (!navmesh.Contains(goal))
                return PathQueryState.GoalInvalid;

            var startEdge = navmesh.FindTriangleContainingPoint(start);
            if (!EndpointValid(start, radius, startEdge))
                return PathQueryState.StartInvalid;
            var startId = startEdge->TriangleId;

            var goalEdge = navmesh.FindTriangleContainingPoint(goal);
            if (!EndpointValid(goal, radius, goalEdge))
                return PathQueryState.GoalInvalid;
            var goalId = goalEdge->TriangleId;

            var foundDisturbance = false;
            if (startId == goalId)
            {
                var sg = goal - start;
                var r = TransformRect((start + goal) / 2, new float2(diameter, math.length(sg)), Math.Angle(sg));
                verts.Clear();
                VerticesInQuad(r, startEdge, verts);
                for (int i = 0; i < verts.Length; i++)
                {
                    var v = verts[i];
                    var p = v.Vertex->Point;
                    var psg = Math.ProjectLine(start, goal, p) - start;
                    v.DistFromStart = math.lengthsq(psg);

                    if (CheckForDisturbance(p, start, goal, out var opp, startEdge, diameter))
                    {
                        foundDisturbance = true;
                        break;
                    }

                    v.Opposite = opp;
                    verts[i] = v;
                }

                if (!foundDisturbance)
                {
                    verts.Sort<PossibleDisturbance>();

                    for (int i = 0; i < verts.Length; i++)
                    {
                        var v = verts[i];
                        var p = v.Vertex->Point;
                        var e = GeometricPredicates.Orient2DFast(start, goal, p) > 0 ? new Gate {Left = p, Right = v.Opposite} : new Gate {Left = v.Opposite, Right = p};

                        if (path.Length > 0)
                        {
                            var gate = new Gate {Left = path[path.Length - 1].Left, Right = e.Right};
                            path.Add(gate);
                            DebugDraw(gate.Left, gate.Right, Color.magenta);
                        }

                        path.Add(e);
                        DebugDraw(e.Left, e.Right, Color.magenta);
                    }

                    triangleIds.Add(startId);
                    return PathQueryState.PathFound;
                }
            }

            var ob = this;
            GetValidGoalEdges(_validGoalEdges);

            if (_validGoalEdges.Length == 0)
                return PathQueryState.NoPath;

            ExpandInitial(startEdge->Sym);
            ExpandInitial(startEdge->LNext->Sym);
            ExpandInitial(startEdge->LPrev->Sym);
            closed.TryAdd(startId);
            closed.TryAdd(goalId);

            while (open.Count > 0)
            {
                var step = open.Extract();
                var id = step.Id;

                if (id == goalId)
                {
                    var e = step.Edge->TriangleId == goalId ? step.Edge : step.Edge->Sym;

                    while (step.Previous != -1)
                    {
                        path.Add(new Gate {Left = step.Edge->Org->Point, Right = step.Edge->Dest->Point});
                        triangleIds.Add(step.Edge->TriangleId);
                        step = steps[step.Previous];
                    }

                    path.Add(new Gate {Left = step.Edge->Org->Point, Right = step.Edge->Dest->Point});
                    triangleIds.Add(step.Edge->TriangleId);
                    triangleIds.Add(startId);

                    AddEndpointEdges(goal, e, true);
                    e = step.Edge->TriangleId == startId ? step.Edge : step.Edge->Sym;
                    AddEndpointEdges(start, e, false);

                    path.Reverse();
                    return PathQueryState.PathFound;
                }

                closed.TryAdd(id);

                var next = step.Edge->LNext;
                if (!next->Constrained)
                    Expand(next->Sym, next->ClearanceRight);

                next = step.Edge->LPrev->Sym;
                if (!next->Constrained)
                    Expand(next, next->ClearanceLeft);

                ++cost;

                void Expand(Edge* edge, float clearance)
                {
                    if (clearance < diameter)
                        return;

                    if (edge->TriangleId == goalId)
                    {
                        if (!validGoalEdges.Contains(edge->QuadEdgeId))
                            return;
                    }
                    else if (closed.Contains(edge->TriangleId))
                        return;

                    var newStep = new Step
                    (
                        edge,
                        steps.Length,
                        step.G + C(step.ReferencePoint, edge, out var referencePoint),
                        H(referencePoint, goal),
                        step.StepId,
                        referencePoint
                    );

                    steps.Add(newStep);
                    open.InsertOrLowerKey(newStep);
                }
            }

            return PathQueryState.NoPath;

            void AddEndpointEdges(float2 endpoint, Edge* edge, bool reverse)
            {
                var q = new Quad
                {
                    A = edge->Org->Point,
                    B = edge->Dest->Point,
                    C = Math.GetTangentRight(edge->Dest->Point, endpoint, radius),
                    D = Math.GetTangentLeft(edge->Org->Point, endpoint, radius)
                };

                DebugDraw(q, Color.yellow);

                var s = endpoint;
                var g = Math.ClosestPointOnLineSegment(s, edge->Org->Point, edge->Dest->Point);

                verts.Clear();
                ob.VerticesInQuad(q, edge, verts);

                for (int i = verts.Length - 1; i >= 0; i--)
                {
                    var v = verts[i];

                    if (v.Vertex == edge->Org || v.Vertex == edge->Dest)
                    {
                        verts.RemoveAt(i);
                        continue;
                    }

                    var p = v.Vertex->Point;
                    v.DistFromStart = math.distancesq(p, s);
                    CheckForDisturbance(p, s, g, out var opp, edge, diameter);
                    v.Opposite = opp;
                    verts[i] = v;
                }

                if (verts.Length > 1)
                    verts.Sort<PossibleDisturbance>();

                if (verts.Length > 0)
                {

                    for (int i = verts.Length - 1; i >= 0; i--)
                    {
                        var p = verts[i].Vertex->Point;

                        if (reverse) // goal
                        {
                            var gate = Math.CcwFast(s, g, p)
                                ? new Gate {Left = path[0].Left, Right = p, IsGoalGate = true}
                                : new Gate {Left = p, Right = path[0].Right, IsGoalGate = true};

                            DebugDraw(gate.Left, gate.Right, Color.cyan);
                            path.Insert(0, gate);
                        }
                        else
                        {
                            var gate = Math.CcwFast(s, g, p)
                                ? new Gate {Left = p, Right = path.Last().Right}
                                : new Gate {Left = path.Last().Left, Right = p};

                            DebugDraw(gate.Left, gate.Right, Color.magenta);
                            path.Add(gate);
                        }
                    }
                }
            }

            void GetValidGoalEdges(List<int> valid)
            {
                valid.Clear();
                ValidEdge(goalEdge);
                ValidEdge(goalEdge->LNext);
                ValidEdge(goalEdge->LPrev);

                void ValidEdge(Edge* e)
                {
                    if (!e->Constrained && !EndpointDisturbed(e, goal))
                        valid.Add(e->QuadEdgeId);
                }
            }

            void ExpandInitial(Edge* edge)
            {
                if (edge->Constrained || edge->TriangleId == goalId && !validGoalEdges.Contains(edge->QuadEdgeId))
                    return;

                if (EndpointDisturbed(edge->Sym, start))
                    return;

                var newStep = new Step
                (
                    edge,
                    steps.Length,
                    C(start, edge, out var referencePoint),
                    H(referencePoint, goal),
                    -1,
                    referencePoint
                );

                steps.Add(newStep);
                open.InsertOrLowerKey(newStep);
            }

            bool EndpointDisturbed(Edge* edge, float2 endpoint)
            {
                var q = new Quad
                {
                    A = edge->Org->Point,
                    B = edge->Dest->Point,
                    C = Math.GetTangentRight(edge->Dest->Point, endpoint, radius),
                    D = Math.GetTangentLeft(edge->Org->Point, endpoint, radius)
                };
                verts.Clear();
                ob.VerticesInQuad(q, edge, verts);
                for (int i = 0; i < verts.Length; i++)
                {
                    var v = verts[i];
                    var p = v.Vertex->Point;
                    if (CheckForDisturbance(p, endpoint, (edge->Org->Point + edge->Dest->Point) / 2, out _, edge, diameter))
                        return true;
                }

                return false;
            }

            float C(float2 from, Edge* edge, out float2 referencePoint)
            {
                var o = edge->Org->Point;
                var d = edge->Dest->Point;
                var od = d - o;
                var offset = math.normalize(od) * radius;
                o += offset;
                d -= offset;
                if (!Math.IntersectSegSeg(from, goal, o, d, out referencePoint))
                {
                    // metric 4 from paper
                    // referencePoint = math.lengthsq(goal - o) < math.lengthsq(goal - d) ? o : d;
                    referencePoint = Math.ClosestPointOnLineSegment(from, o, d);
                }

                return math.length(referencePoint - from);
            }

            float H(float2 p0, float2 p1)
            {
                return math.length(p1 - p0);
            }

            bool CheckForDisturbance(float2 v, float2 s, float2 g, out float2 opposite, Edge* tri, float d)
            {
                Edge* e;

                if (Math.Ccw(tri->Org->Point, tri->Dest->Point, v) && Math.ProjectSeg(tri->Org->Point, tri->Dest->Point, v, out _))
                    e = tri;
                else if (Math.Ccw(tri->LNext->Org->Point, tri->LNext->Dest->Point, v) && Math.ProjectSeg(tri->LNext->Org->Point, tri->LNext->Dest->Point, v, out _))
                    e = tri->LNext;
                else if (Math.Ccw(tri->LPrev->Org->Point, tri->LPrev->Dest->Point, v) && Math.ProjectSeg(tri->LPrev->Org->Point, tri->LPrev->Dest->Point, v, out _))
                    e = tri->LPrev;
                else
                {
                    if (math.any(tri->Org->Point != v) && math.any(tri->Dest->Point != v))
                        e = tri;
                    else if (math.any(tri->LNext->Org->Point != v) && math.any(tri->LNext->Dest->Point != v))
                        e = tri->LNext;
                    else if (math.any(tri->LPrev->Org->Point != v) && math.any(tri->LPrev->Dest->Point != v))
                        e = tri->LPrev;
                    else
                        throw new Exception();
                }

                var sgi = Math.ProjectLine(e->Org->Point, e->Dest->Point, v);
                opposite = (float2) (sgi + math.normalize(sgi - v) * d);

                var c = Navmesh.Navmesh.TryGetConstraint(math.length(opposite - v), v, e->Sym);

                if (c == null)
                    return false;

                opposite = (float2) Math.ProjectLine(c->Org->Point, c->Dest->Point, v);
                return math.length(opposite - v) < d && Math.IntersectSegSeg(v, opposite, s, g);
            }
        }

        [Conditional("FUNNEL_DEBUG")]
        static void DebugDraw(float2 from, float2 to, Color color)
        {
            Line.Draw(from.ToXxY(.01f), to.ToXxY(.01f), color);
        }

        [Conditional("FUNNEL_DEBUG")]
        static void DebugDraw(Quad q, Color color)
        {
            DebugDraw(q.A, q.B, color);
            DebugDraw(q.B, q.C, color);
            DebugDraw(q.C, q.D, color);
            DebugDraw(q.D, q.A, color);
        }

        static bool EndpointValid(float2 p, float r, Edge* tri)
        {
            return EndpointValidRecursive(p, r, tri->Sym) &&
                   EndpointValidRecursive(p, r, tri->LNext->Sym) &&
                   EndpointValidRecursive(p, r, tri->LPrev->Sym);
        }

        static bool EndpointValidRecursive(float2 p, float r, Edge* tri)
        {
            return Math.IntersectSegCircle(tri->Org->Point, tri->Dest->Point, p, r) == 0 &&
                   math.length(p - tri->Org->Point) > r &&
                   math.length(p - tri->Dest->Point) > r ||
                   !tri->Constrained &&
                   EndpointValidRecursive(p, r, tri->LNext->Sym) &&
                   EndpointValidRecursive(p, r, tri->LPrev->Sym);
        }

        static Quad TransformRect(float2 translation, float2 size, float angle)
        {
            var h = size / 2;
            return new Quad
            {
                A = Math.Rotate(-h, angle) + translation,
                B = Math.Rotate(new float2(h.x, -h.y), angle) + translation,
                C = Math.Rotate(h, angle) + translation,
                D = Math.Rotate(new float2(-h.x, h.y), angle) + translation
            };
        }

        // todo quad can be concave, use appropriate contains check below?
        void VerticesInQuad(Quad r, Edge* e, List<PossibleDisturbance> verts)
        {
            VerticesInQuadCheckOrg(r, e->Sym, verts);
            VerticesInQuadCheckOrg(r, e->LNext->Sym, verts);
            VerticesInQuadCheckOrg(r, e->LPrev->Sym, verts);
            _closedEdges.Clear();
            _closedVerts.Clear();
        }

        void VerticesInQuadCheckOrg(Quad r, Edge* e, List<PossibleDisturbance> verts)
        {
            var v = e->Org;
            if (Contains(r, v->Point) && !_closedVerts.Contains((IntPtr) v))
            {
                _closedVerts.TryAdd((IntPtr) v);
                verts.Add(new PossibleDisturbance(v));
            }

            VerticesInQuadRecursive(r, e, verts);
        }

        void VerticesInQuadRecursive(Quad r, Edge* e, List<PossibleDisturbance> verts)
        {
            if (_closedEdges.Contains(e->QuadEdgeId))
                return;
            _closedEdges.TryAdd(e->QuadEdgeId);

            if (Intersect(r, e->Org->Point, e->Dest->Point))
            {
                var v = e->LNext->Dest;
                if (Contains(r, v->Point) && !_closedVerts.Contains((IntPtr) v))
                {
                    _closedVerts.TryAdd((IntPtr) v);
                    verts.Add(new PossibleDisturbance(v));
                }

                VerticesInQuadRecursive(r, e->LNext->Sym, verts);
                VerticesInQuadRecursive(r, e->LPrev->Sym, verts);
            }
        }

        static bool Intersect(Quad r, float2 p0, float2 p1)
        {
            return Contains(r, p0) ||
                   Contains(r, p1) ||
                   Math.IntersectSegSeg(r.A, r.B, p0, p1) ||
                   Math.IntersectSegSeg(r.B, r.C, p0, p1) ||
                   Math.IntersectSegSeg(r.C, r.D, p0, p1) ||
                   Math.IntersectSegSeg(r.D, r.A, p0, p1);
        }

        static bool Contains(Quad r, float2 p)
        {
            return GeometricPredicates.Orient2DFast(r.A, r.B, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.B, r.C, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.C, r.D, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.D, r.A, p) >= 0;
        }

        public void Clear()
        {
            _open.Clear();
            _closed.Clear();
            _steps.Clear();
            _verts.Clear();
            _validGoalEdges.Clear();
        }

        public void Dispose()
        {
            _open.Dispose();
            _closed.Dispose();
            _steps.Dispose();
            _verts.Dispose();
            _validGoalEdges.Dispose();
            _closedEdges.Dispose();
            _closedVerts.Dispose();
        }

        readonly struct Step : PriorityQueue<Step>.IElement
        {
            // todo confusing, probably better off making this StepId and add TriangleId, could forego index in priorityqueue
            public int Id => Edge->TriangleId;
            public readonly Edge* Edge;
            public readonly int StepId;
            public readonly float G;
            public readonly int Previous;
            public readonly float2 ReferencePoint;
            readonly float _gPlusH;

            public Step(Edge* edge, int stepId, float g, float h, int previous, float2 referencePoint)
            {
                Edge = edge;
                StepId = stepId;
                G = g;
                _gPlusH = g + h;
                Previous = previous;
                ReferencePoint = referencePoint;
            }

            public int CompareTo(Step other)
                => _gPlusH.CompareTo(other._gPlusH);
        }
    }
}