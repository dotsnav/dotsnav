using DotsNav.Core;
using DotsNav.Core.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Navmesh.Navmesh
{
    public unsafe partial struct Navmesh
    {
        static Edge* GetLeftEdge(Vertex* a, float2 p)
        {
            var result = a->Edge;
            var o = result->Org->Point;
            InfiniteLoopDetection.Reset();
            while (!Math.Ccw(o, p, result->Dest->Point))
            {
                InfiniteLoopDetection.Register(1000, "GetLeftEdge 0");
                result = result->ONext;
            }

            InfiniteLoopDetection.Reset();
            while (Math.Ccw(o, p, result->OPrev->Dest->Point))
            {
                InfiniteLoopDetection.Register(1000, "GetLeftEdge 1");
                result = result->OPrev;
            }

            return result;
        }

        static Edge* GetConnection(Vertex* a, Vertex* b)
        {
            var e = a->GetEdgeEnumerator();
            while (e.MoveNext())
                if (e.Current->Dest == b)
                    return e.Current;
            return null;
        }

        Edge* Connect(Vertex* a, Vertex* b)
        {
            Assert.IsTrue(a->Edge != null);
            Assert.IsTrue(b->Edge != null);
            return Connect(GetLeftEdge(a, b->Point)->Sym, GetLeftEdge(b, a->Point)->OPrev);
        }

        // Flips edge e counterclockwise inside its enclosing quadrilateral.
        // http://karlchenofhell.org/cppswp/lischinski.pdf
        void Swap(Edge* e)
        {
            e->Org->RemoveEdge(e);
            e->Dest->RemoveEdge(e->Sym);

            V.TryAdd((IntPtr) e->Org);
            V.TryAdd((IntPtr) e->Dest);

            var a = e->OPrev;
            var b = e->Sym->OPrev;
            Splice(e, a);
            Splice(e->Sym, b);
            Splice(e, a->LNext);
            Splice(e->Sym, b->LNext);
            SetEndPoints(e, a->Dest, b->Dest);

            V.TryAdd((IntPtr) a->Dest);
            V.TryAdd((IntPtr) b->Dest);

            DestroyedTriangle(e->TriangleId);
            DestroyedTriangle(e->Sym->TriangleId);

            NewTriangle(e);
            NewTriangle(e->Sym);
        }

        static void SetEndPoints(Edge* edge, Vertex* org, Vertex* dest)
        {
            SetOrg(edge, org);
            SetOrg(edge->Sym, dest);
        }

        static void SetOrg(Edge* edge, Vertex* v)
        {
            Assert.IsTrue(v != null);
            edge->Org = v;
            v->Edge = edge;
        }

        // https://www.researchgate.net/publication/2478154_Fully_Dynamic_Constrained_Delaunay_Triangulations
        void FlipEdges(float2 p)
        {
            while (_flipStack.Count > 0)
            {
                var e = _flipStack.Pop();
                Assert.IsTrue(Math.Ccw(e->Org->Point, e->Dest->Point, p));
                if (!e->Constrained && Math.CircumcircleContains(e->Org->Point, e->Dest->Point, p, e->DNext->Org->Point))
                {
                    _flipStack.Push(e->OPrev);
                    _flipStack.Push(e->DNext);
                    Assert.IsTrue(Math.Ccw(e->OPrev->Org->Point, e->OPrev->Dest->Point, p));
                    Assert.IsTrue(Math.Ccw(e->DNext->Org->Point, e->DNext->Dest->Point, p));
                    Swap(e);
                }
            }
        }

        // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.61.3862&rep=rep1&type=pdf
        // TriangulatePseudopolygonDelaunay()
        void RetriangulateFace(Edge* edge)
        {
            Assert.IsTrue(edge != null);
            Assert.IsTrue(edge != edge->LNext->LNext);
            if (edge->LNext->LNext->LNext == edge)
            {
                NewTriangle(edge);
                return;
            }

            InfiniteLoopDetection.Reset();
            while (!Math.Ccw(edge->Org->Point, edge->Dest->Point, edge->LNext->Dest->Point))
            {
                InfiniteLoopDetection.Register(1000, "RetriangulateFace 0");
                edge = edge->LNext;
            }

            var c = edge->LNext;
            var e = c;
            InfiniteLoopDetection.Reset();
            while (true)
            {
                InfiniteLoopDetection.Register(1000, "RetriangulateFace 1");
                e = e->LNext;
                if (e->LNext == edge)
                    break;
                if (Math.CircumcircleContains(edge->Org->Point, edge->Dest->Point, c->Dest->Point, e->Dest->Point))
                    c = e;
            }

            Assert.IsTrue(c != edge);
            var connected = false;
            if (c->LNext->LNext != edge)
            {
                V.TryAdd((IntPtr) edge->LPrev->Dest);
                V.TryAdd((IntPtr) c->LNext->Org);

                var b = Connect(edge->LPrev, c->LNext);
                RetriangulateFace(b);
                connected = true;
            }

            if (c != edge->LNext)
            {
                V.TryAdd((IntPtr) c->Dest);
                V.TryAdd((IntPtr) edge->LNext->Org);

                var a = Connect(c, edge->LNext);
                RetriangulateFace(a);
                connected = true;
            }

            if (connected)
                NewTriangle(edge);
        }

        Edge* RemoveVertex(Vertex* vert)
        {
            Assert.IsTrue(vert->Edge != null);
            Assert.IsTrue(vert->Edge->Org == vert);

            var remaining = vert->Edge->LNext;
            InfiniteLoopDetection.Reset();
            while (true)
            {
                InfiniteLoopDetection.Register(1000, "RemoveVertex");
                var e = vert->Edge;
                if (e == null)
                    break;
                RemoveEdge(e);
            }

            V.Remove((IntPtr) vert);

            Assert.IsTrue(vert->Edge == null);
            _qt.Remove(vert);
            var delPos = vert->SeqPos;
            ((Vertex*) _verticesSeq[_verticesSeq.Length - 1])->SeqPos = delPos;
            _verticesSeq.RemoveAtSwapBack(delPos);
            _vertices.Recycle(vert);
            return remaining;
        }

        /// <summary>
        /// Returns an edge for which the specified point is contained within it's left face. If the point lies
        /// on an edge this edge is returned. If the point lies on a vertex an arbitrary edge with identical origin is returned.
        /// </summary>
        public Edge* FindTriangleContainingPoint(float2 p) => FindTriangleContainingPoint(p, out _);

        /// <summary>
        /// Returns an edge for which the specified point is contained within it's left face. If the point lies
        /// on an edge this edge is returned. If the point lies on a vertex an arbitrary edge with identical origin is returned.
        /// </summary>
        /// <param name="collinear">True when the specified point lies on the returned edge</param>
        public Edge* FindTriangleContainingPoint(float2 p, out bool collinear)
        {
            var e = FindClosestVertex(p)->Edge;
            Assert.IsTrue(e != null);
            InfiniteLoopDetection.Reset();
            while (true)
            {
                InfiniteLoopDetection.Register(1000, "FindTriangleContainingPoint");

                Edge* collinearEdge = null;
                var orient = Math.TriArea(e->Org->Point, e->Dest->Point, p);

                if (orient == 0)
                {
                    collinearEdge = e;
                }
                else if (orient < 0)
                {
                    e = e->Sym;
                    continue;
                }

                orient = Math.TriArea(e->ONext->Org->Point, e->ONext->Dest->Point, p);

                if (orient == 0)
                {
                    collinearEdge = e->ONext;
                }
                else if (orient > 0)
                {
                    e = e->ONext;
                    continue;
                }

                orient = Math.TriArea(e->DPrev->Org->Point, e->DPrev->Dest->Point, p);

                if (orient == 0)
                {
                    collinear = true;
                    return e->DPrev;
                }

                if (orient > 0)
                {
                    e = e->DPrev;
                    continue;
                }

                if (collinearEdge != null)
                {
                    collinear = true;
                    return collinearEdge;
                }

                collinear = false;
                return e;
            }
        }

        /// <summary>
        /// Returns a pointer to the vertex closest to the specified point
        /// </summary>
        public Vertex* FindClosestVertex(float2 p)
        {
            Assert.IsTrue(Contains(p), "Trying to find the closest vertex to a point outside the navmesh");
            return _qt.FindClosest(p);
        }

        Vertex* InsertPoint(float2 p)
        {
            var closest = _qt.FindClosest(p);

            if (math.lengthsq(closest->Point - p) <= _e * _e)
                return closest;

            var e = closest->Edge;
            Assert.IsTrue(e != null);
            InfiniteLoopDetection.Reset();

            while (true)
            {
                InfiniteLoopDetection.Register(1000, "InsertPoint");

                Edge* inEdge = null;

                var orient = Math.TriArea(e->Org->Point, e->Dest->Point, p);

                if (orient == 0)
                    inEdge = e;

                if (orient < 0)
                {
                    e = e->Sym;
                    continue;
                }

                orient = Math.TriArea(e->ONext->Org->Point, e->ONext->Dest->Point, p);

                if (orient == 0)
                    inEdge = e->ONext;

                if (orient > 0)
                {
                    e = e->ONext;
                    continue;
                }

                orient = Math.TriArea(e->DPrev->Org->Point, e->DPrev->Dest->Point, p);

                if (orient == 0)
                    inEdge = e->DPrev;

                if (orient > 0)
                {
                    e = e->DPrev;
                    continue;
                }

                if (inEdge != null)
                {
                    Assert.IsTrue(SplitIsRobust(p, inEdge));
                    return InsertPointInEdge(p, inEdge);
                }

                return InsertPointInFace(p, e);
            }
        }

        Vertex* InsertPointInEdge(float2 point, Edge* edge)
        {
            _flipStack.Push(edge->ONext->Sym);
            _flipStack.Push(edge->DPrev->Sym);
            _flipStack.Push(edge->OPrev);
            _flipStack.Push(edge->DNext);

            for (var i = 0; i < _flipStack.Count; i++)
                Assert.IsTrue(Math.Ccw(_flipStack[i]->Org->Point, _flipStack[i]->Dest->Point, point));

            DestroyedTriangle(edge->TriangleId);
            DestroyedTriangle(edge->Sym->TriangleId);

            var crep = edge->QuadEdge->Crep;
            var e = edge->OPrev;
            C.Remove((IntPtr) edge);
            RemoveEdge(edge, false);
            var result = CreateVertex(point);
            V.TryAdd((IntPtr) result);
            V.TryAdd((IntPtr) e->Org);
            var newEdge = CreateEdge(e->Org, result);
            newEdge->QuadEdge->Crep = GetCrep(crep);
            Splice(newEdge, e);

            V.TryAdd((IntPtr) e->Dest);
            V.TryAdd((IntPtr) newEdge->Sym->Org);

            newEdge = Connect(e, newEdge->Sym);
            e = newEdge->OPrev;

            V.TryAdd((IntPtr) e->Dest);
            V.TryAdd((IntPtr) newEdge->Sym->Org);

            newEdge = Connect(e, newEdge->Sym);
            newEdge->QuadEdge->Crep = crep;
            e = newEdge->OPrev;

            V.TryAdd((IntPtr) e->Dest);
            V.TryAdd((IntPtr) newEdge->Sym->Org);

            Connect(e, newEdge->Sym);

            var te = result->Edge;
            NewTriangle(te);
            te = te->ONext;
            NewTriangle(te);
            te = te->ONext;
            NewTriangle(te);
            te = te->ONext;
            NewTriangle(te);

            FlipEdges(point);
            return result;
        }

        UnsafeList GetCrep(UnsafeList source)
        {
            var l = GetCrep();
            for (int i = 0; i < source.Length; i++)
                l.Add(source.Read<Entity>(i));
            return l;
        }

        UnsafeList GetCrep() => _creps.Count > 0 ? _creps.Pop() : new UnsafeList(UnsafeUtility.SizeOf<Entity>(), UnsafeUtility.AlignOf<Entity>(), CrepMinCapacity, Allocator.Persistent);

        Vertex* InsertPointInFace(float2 p, Edge* edge)
        {
            _flipStack.Push(edge->ONext->Sym);
            _flipStack.Push(edge);
            _flipStack.Push(edge->DPrev->Sym);

            for (var i = 0; i < _flipStack.Count; i++)
                Assert.IsTrue(Math.Ccw(_flipStack[i]->Org->Point, _flipStack[i]->Dest->Point, p));

            DestroyedTriangle(edge->TriangleId);

            var result = CreateVertex(p);

            V.TryAdd((IntPtr) result);
            V.TryAdd((IntPtr) edge->Org);
            V.TryAdd((IntPtr) edge->Dest);
            V.TryAdd((IntPtr) edge->LNext->Dest);

            var newEdge = CreateEdge(edge->Org, result);
            Splice(newEdge, edge);
            newEdge = Connect(edge, newEdge->Sym);
            Connect(newEdge->OPrev, newEdge->Sym);

            var te = result->Edge;
            NewTriangle(te);
            te = te->ONext;
            NewTriangle(te);
            te = te->ONext;
            NewTriangle(te);

            FlipEdges(p);
            return result;
        }

        struct Point
        {
            public Vertex* Vertex;
            public bool Modified;
            public bool FoundExisting;
            public float2 Before;
            public float2 After;
            public float2 P;

            public override string ToString()
                => $"Vert: {Vertex->ToString()}, P: {P}";
        }

        void InsertSegment(Vertex* a, Vertex* b, Entity id)
        {
            var dir = math.normalize(b->Point - a->Point);
            InsertSegmentRecursive(a, b, id, dir, a->Point, b->Point);
        }

        void InsertSegmentRecursive(Vertex* a, Vertex* b, Entity id, float2 dir, float2 start, float2 end)
        {
            _insertedPoints.Clear();
            _insertedPoints.Add(new Point {Vertex = a, P = a->Point});

            while (a != b)
            {
                var p0 = _insertedPoints[_insertedPoints.Length - 1];
                var p1 = GetNextPoint(a, b, start, end);

                if (!p0.Modified && !p1.Modified)
                {
                    if (p0.FoundExisting || p1.FoundExisting)
                        InsertSegmentRecursive(p0.Vertex, p1.Vertex, id, dir, start, end);
                    else
                        InsertSegmentNoConstraints(p0.Vertex, p1.Vertex, id);
                }
                else if (p0.Modified && !p1.Modified)
                {
                    if (GetSupport(p0.After + _e / 2 * dir, p1.P - _e * dir, dir, out var p))
                    {
                        var after = InsertPoint(p);
                        InsertSegmentRecursive(after, p1.Vertex, id, dir, start, end);
                        _edgeSearch.Search(p0.Vertex, after, id);
                    }
                    else
                    {
                        _edgeSearch.Search(p0.Vertex, p1.Vertex, id);
                    }
                }
                else if (!p0.Modified) // p1 modified
                {
                    if (GetSupport(p1.Before - _e / 2 * dir, p0.P + _e * dir, -dir, out var p))
                    {
                        var before = InsertPoint(p);
                        InsertSegmentRecursive(p0.Vertex, before, id, dir, start, end);
                        _edgeSearch.Search(before, p1.Vertex, id);
                    }
                    else
                    {
                        _edgeSearch.Search(p0.Vertex, p1.Vertex, id);
                    }
                }
                else // both modified
                {
                    if (GetSupport(p0.After + _e / 2 * dir, p1.P - _e * dir, dir, out var s1) &&
                        GetSupport(p1.Before - _e / 2 * dir, s1 + _e * dir, -dir, out var s2))
                    {
                        var v0 = InsertPoint(s1);
                        var v1 = InsertPoint(s2);
                        InsertSegmentRecursive(v0, v1, id, dir, start, end);
                        _edgeSearch.Search(p0.Vertex, v0, id);
                        _edgeSearch.Search(v1, p1.Vertex, id);
                    }
                    else
                    {
                        _edgeSearch.Search(p0.Vertex, p1.Vertex, id);
                    }
                }

                a = p1.Vertex;
                _insertedPoints.Add(p1);
            }
        }

        Point GetNextPoint(Vertex* a, Vertex* b, float2 start, float2 end)
        {
            InfiniteLoopDetection.Reset();
            var e = GetLeftEdge(a, b->Point);
            while (e->Dest != b)
            {
                InfiniteLoopDetection.Register(1000, "GetNextPoint");

                var d = Math.TriArea(a->Point, b->Point, e->Dest->Point);

                if (d < 0 && e->Constrained)
                {
                    var p = (float2) Math.IntersectLineSegClamped(start, end, e->Org->Point, e->Dest->Point);
                    var pointExists = TryGetPoint(p, e, out var v);

                    if (v != null)
                    {
                        if (_insertedPoints.Length > 1)
                        {
                            var prev = _insertedPoints[_insertedPoints.Length - 1].Vertex;
                            if (prev == v || e->Org == prev || e->Dest == prev)
                                continue;
                        }

                        if (_insertedPoints.Length > 2)
                        {
                            var prev = _insertedPoints[_insertedPoints.Length - 2].Vertex;
                            if (prev == v || e->Org == prev || e->Dest == prev)
                                continue;
                        }

                        return new Point
                        {
                            Vertex = v,
                            FoundExisting = true,
                            P = p
                        };
                    }

                    if (pointExists || !SplitIsRobust(p, e))
                    {
                        var pRef = CreatePRef(p, e);

                        if (_insertedPoints.Length > 1 && _insertedPoints[_insertedPoints.Length - 1].Vertex == pRef)
                            continue;

                        var point = new Point
                        {
                            Vertex = pRef,
                            Modified = true,
                            P = p
                        };

                        var proj = (float2) Math.ProjectLine(a->Point, b->Point, point.Vertex->Point);
                        var pproj = proj - p;

                        if (math.dot(b - a, pproj) < 0)
                        {
                            point.Before = proj;
                            point.After = p;
                        }
                        else
                        {
                            point.Before = p;
                            point.After = proj;
                        }

                        return point;
                    }

                    var vert = InsertPointInEdge(p, e);
                    return new Point
                    {
                        Vertex = vert,
                        P = p
                    };
                }

                e = d > 0 ? e->RPrev : e->ONext;
            }

            return new Point
            {
                Vertex = b,
                P = b->Point
            };
        }

        Vertex* CreatePRef(float2 p, Edge* e)
        {
            var stepSize = _e / 2;
            var po = e->Org->Point - p;
            var pd = e->Dest->Point - p;
            var dir = math.normalize(e->Dest->Point - e->Org->Point);
            var lpo = math.length(po) - _e;
            var lpd = math.length(pd) - _e;
            var offset = 0f;

            InfiniteLoopDetection.Reset();
            while (true)
            {
                InfiniteLoopDetection.Register(1000, "CreatePRef");

                offset += stepSize;

                if (offset >= lpo)
                    return e->Org;

                if (offset >= lpd)
                    return e->Dest;

                var pplus = p + offset * dir;
                var pointPresent = TryGetPoint(pplus, e, out var vertex);

                if (vertex != null)
                    return vertex;

                if (!pointPresent && SplitIsRobust(pplus, e))
                    return InsertPointInEdge(pplus, e);

                var pmin = p - offset * dir;
                pointPresent = TryGetPoint(pmin, e, out vertex);

                if (vertex != null)
                    return vertex;

                if (!pointPresent && SplitIsRobust(pmin, e))
                    return InsertPointInEdge(pmin, e);
            }
        }

        // todo qt is queried here and at callsite through InsertPoint
        bool GetSupport(float2 a, float2 b, float2 dir, out float2 p)
        {
            if (math.dot(b - a, dir) < 0)
            {
                p = default;
                return false;
            }

            var stepSize = _e / 2;
            var l = math.length(b - a);
            var offset = 0f;

            while (true)
            {
                p = a + offset * dir;
                // todo cache leaf node, these points are probably in the same bucket
                var closest = _qt.FindClosest(p);
                if (math.lengthsq(closest->Point - p) > _e * _e)
                    return true;

                offset += stepSize;
                if (offset >= l)
                {
                    p = default;
                    return false;
                }
            }
        }

        void InsertSegmentNoConstraints(Vertex* a, Vertex* b, Entity id)
        {
            var c = GetConnection(a, b);

            if (c != null)
            {
                C.TryAdd((IntPtr) c);
                if (!c->IsConstrainedBy(id))
                    c->AddConstraint(id);
                ResetClearance(c);
                return;
            }

            var e = GetLeftEdge(a, b->Point);

            InfiniteLoopDetection.Reset();
            while (e->Dest != b)
            {
                InfiniteLoopDetection.Register(1000, "InsertSegmentNoConstraints");

                var d = Math.TriArea(a->Point, b->Point, e->Dest->Point);
                var next = d > 0 ? e->RPrev : e->ONext;

                if (d < 0)
                {
                    Assert.IsTrue(!e->Constrained);
                    RemoveEdge(e);
                }
                else if (d == 0 && e->Dest != a)
                {
                    var t = e->Dest;
                    Connect(a, t, id);
                    a = t;
                }

                e = next;
            }

            Connect(a, b, id);
        }

        void Connect(Vertex* a, Vertex* b, Entity id)
        {
            var connection = GetConnection(a, b);
            if (connection == null)
            {
                V.TryAdd((IntPtr) a);
                V.TryAdd((IntPtr) b);

                connection = Connect(a, b);
                RetriangulateFace(connection);
                RetriangulateFace(connection->Sym);
            }

            // todo inline wasUnconstrained (so if moves above addconstraint)
            var wasUnconstrained = !connection->Constrained;
            connection->AddConstraint(id);
            if (wasUnconstrained)
                ResetClearance(connection);
            C.TryAdd((IntPtr) connection);
        }

        void Connect(Vertex* a, Vertex* b, UnsafeList crep)
        {
            var connection = GetConnection(a, b);
            if (connection == null)
            {
                V.TryAdd((IntPtr) a);
                V.TryAdd((IntPtr) b);

                connection = Connect(a, b);
                RetriangulateFace(connection);
                RetriangulateFace(connection->Sym);
            }

            connection->QuadEdge->Crep = crep;
            ResetClearance(connection);
            C.TryAdd((IntPtr) connection);
        }
    
        bool TryGetPoint(float2 p, Edge* e, out Vertex* v)
        {
            v = null;
            var closest = _qt.FindClosest(p);

            if (math.lengthsq(closest->Point - p) <= _e * _e)
            {
                var te = closest->Edge;
                do
                {
                    if (te->QuadEdge == e->QuadEdge)
                    {
                        v = closest;
                        break;
                    }

                    te = te->ONext;
                } while (te != closest->Edge);

                return true;
            }

            return false;
        }

        static readonly FixedString128 PointOutsideNavmeshMessage = "Trying to add a point outside the navmesh";

        void Insert(float2* points, int start, int amount, Entity cid)
        {
            Vertex* lastVert = null;
            var end = start + amount;
            Vertex* point = null;

            for (var i = start; i < end; i++)
            {
                var c = points[i];
                Assert.IsTrue(_verticesSeq.Length < 5 || Contains(c), PointOutsideNavmeshMessage);
                var vert = InsertPoint(c);
                Assert.IsTrue(vert != null);

                if (i == start)
                {
                    ++vert->ConstraintHandles;
                    _constraints[cid] = (IntPtr) vert;
                    point = vert;
                }

                if (lastVert != null && vert != lastVert)
                {
                    InsertSegment(lastVert, vert, cid);
                    point = null;
                }
                lastVert = vert;
            }

            if (point != null)
                ++point->PointConstraints;
        }

        void RemoveConstraint(Entity id)
        {
            _vlist.Clear();
            _elist.Clear();

            Assert.IsTrue(_constraints.ContainsKey(id), "Attempting to remove an unknown or static obstacle");
            var v = (Vertex*) _constraints[id];
            Assert.IsTrue(v->Edge != null);
            Assert.IsTrue(v->ConstraintHandles > 0);
            --v->ConstraintHandles;
            _constraints.Remove(id);
            var mark = NextMark;

            _open.Push(v);
            while (_open.Count > 0)
            {
                var vert = _open.Pop();
                var i = vert->GetEdgeEnumerator();
                while (i.MoveNext())
                {
                    if (i.Current->IsConstrainedBy(id) && i.Current->Mark != mark)
                    {
                        _elist.Add((IntPtr) i.Current);
                        i.Current->Mark = mark;
                        _open.Push(i.Current->Dest);
                    }
                }
            }

            if (_elist.Length == 0)
            {
                Assert.IsTrue(v->PointConstraints > 0);
                if (--v->PointConstraints == 0)
                    _vlist.Add((IntPtr) v);
            }
            else
            {
                for (var i = 0; i < _elist.Length; i++)
                {
                    var e = (Edge*) _elist[i];
                    if (e->Org->Mark != mark && e->Org->PointConstraints == 0)
                    {
                        _vlist.Add((IntPtr) e->Org);
                        e->Org->Mark = mark;
                    }

                    if (e->Dest->Mark != mark && e->Dest->PointConstraints == 0)
                    {
                        _vlist.Add((IntPtr) e->Dest);
                        e->Dest->Mark = mark;
                    }
                }

                for (var i = 0; i < _elist.Length; i++)
                {
                    var edge = (Edge*) _elist[i];
                    edge->RemoveConstraint(id);

                    if (!edge->Constrained)
                    {
                        V.TryAdd((IntPtr) edge->Org);
                        V.TryAdd((IntPtr) edge->Dest);
                        edge->RefineFailed = false;
                        ResetClearance(edge);
                        _flipStack.Push(edge);
                        FlipQuad();
                    }
                }
            }

            for (var i = 0; i < _vlist.Length; i++)
                RemoveIfEligible((Vertex*) _vlist[i]);
        }

        void RemoveIfEligible(Vertex* v)
        {
            if (v->PointConstraints > 0 || v->ConstraintHandles > 0)
                return;

            var amount = 0;
            var constrained = stackalloc Edge*[2];

            var e = v->GetEdgeEnumerator();
            while (e.MoveNext())
            {
                if (e.Current->Constrained)
                {
                    if (amount == 2)
                        return;
                    constrained[amount++] = e.Current;
                }
            }

            if (amount == 0)
            {
                e = v->GetEdgeEnumerator();
                while (e.MoveNext())
                    V.TryAdd((IntPtr) e.Current->Dest);
                var face = RemoveVertex(v);
                RetriangulateFace(face);
                return;
            }

            if (amount != 2 || !constrained[0]->ConstraintsEqual(constrained[1]))
                return;

            var e1 = constrained[0];
            var e2 = constrained[1];
            Assert.IsTrue(e1->Dest != v && e2->Dest != v);
            Assert.IsTrue(e1->Dest != e2->Dest);
            var d1 = e1->Dest->Point;
            var d2 = e2->Dest->Point;
            var collinear = Math.TriArea(d1, d2, v->Point);

            if (collinear == 0)
            {
                e = v->GetEdgeEnumerator();
                while (e.MoveNext())
                    V.TryAdd((IntPtr) e.Current->Dest);

                var v1 = e1->Dest;
                var v2 = e2->Dest;
                var crep = e1->QuadEdge->Crep;
                RemoveEdge(e1, false);
                RemoveVertex(v);
                var e3 = Connect(v1, v2);
                RetriangulateFace(e3);
                RetriangulateFace(e3->Sym);
                e3->QuadEdge->Crep = crep;
            }
            else
            {
                var t = collinear / math.length(d2 - d1);

                if (collinear > 0)
                {
                    if (t < _collinearMargin && Math.TriArea(d1, d2, e1->DPrev->Org->Point) < 0 && Math.TriArea(d1, d2, e2->DNext->Org->Point) < 0)
                    {
                        e = v->GetEdgeEnumerator();
                        while (e.MoveNext())
                            V.TryAdd((IntPtr) e.Current->Dest);
                        RemoveSemiCollinear(v, e1, e2);
                    }
                }
                else if (t > -_collinearMargin && Math.TriArea(d1, d2, e1->DNext->Org->Point) > 0 && Math.TriArea(d1, d2, e2->DPrev->Org->Point) > 0)
                {
                    e = v->GetEdgeEnumerator();
                    while (e.MoveNext())
                        V.TryAdd((IntPtr) e.Current->Dest);
                    RemoveSemiCollinear(v, e1, e2);
                }
            }
        }

        void RemoveSemiCollinear(Vertex* v, Edge* e1, Edge* e2)
        {
            var crep = GetCrep(e1->QuadEdge->Crep);
            var a = e1->Dest;
            var b = e2->Dest;
            e1->QuadEdge->Crep.Clear();
            e2->QuadEdge->Crep.Clear();
            _flipStack.Push(e1);
            _flipStack.Push(e2);
            FlipQuad();
            var face1 = RemoveVertex(v);
            RetriangulateFace(face1);
            InsertSegmentNoConstraints(a, b, crep);
        }

        void InsertSegmentNoConstraints(Vertex* a, Vertex* b, UnsafeList crep)
        {
            var c = GetConnection(a, b);

            if (c != null)
            {
                C.TryAdd((IntPtr) c);
                c->QuadEdge->Crep = crep;
                ResetClearance(c);
                return;
            }

            var e = GetLeftEdge(a, b->Point);

            InfiniteLoopDetection.Reset();
            while (e->Dest != b)
            {
                InfiniteLoopDetection.Register(1000, "InsertSegmentNoConstraints");

                var d = Math.TriArea(a->Point, b->Point, e->Dest->Point);
                var next = d > 0 ? e->RPrev : e->ONext;

                if (d < 0)
                {
                    Assert.IsTrue(!e->Constrained);
                    RemoveEdge(e);
                }
                else if (d == 0 && e->Dest != a)
                {
                    var t = e->Dest;
                    Connect(a, t, GetCrep(crep));
                    a = t;
                }

                e = next;
            }

            Connect(a, b, crep);
        }

        void FlipQuad()
        {
            while (_flipStack.Count > 0)
            {
                var edge = _flipStack.Pop();

                if (!edge->Constrained && Math.CircumcircleContains(edge->Org->Point, edge->Dest->Point, edge->ONext->Dest->Point, edge->DNext->Org->Point))
                {
                    _flipStack.Push(edge->OPrev);
                    _flipStack.Push(edge->DNext);
                    _flipStack.Push(edge->Sym->OPrev);
                    _flipStack.Push(edge->Sym->DNext);
                    Swap(edge);
                }
            }
        }

        Edge* CreateEdge(Vertex* a, Vertex* b)
        {
            var q = _quadEdges.Set(new QuadEdge {Crep = GetCrep(), Id = NextEdgeId});

            q->Edge0 = new Edge(q, 0);
            q->Edge1 = new Edge(q, 1);
            q->Edge2 = new Edge(q, 2);
            q->Edge3 = new Edge(q, 3);

            q->Edge0.Next = &q->Edge0;
            q->Edge1.Next = &q->Edge3;
            q->Edge2.Next = &q->Edge2;
            q->Edge3.Next = &q->Edge1;

            SetEndPoints(&q->Edge0, a, b);
            return &q->Edge0;
        }

        void RemoveEdge(Edge* e, bool recycleCrep = true)
        {
            DestroyedTriangle(e->TriangleId);
            DestroyedTriangle(e->Sym->TriangleId);

            e->Org->RemoveEdge(e);
            e->Dest->RemoveEdge(e->Sym);
            Splice(e, e->OPrev);
            Splice(e->Sym, e->Sym->OPrev);

            var qe = e->QuadEdge;
            if (recycleCrep)
            {
                qe->Crep.Clear();
                _creps.Push(qe->Crep);
            }

            _quadEdges.Recycle(qe);
        }

        void DestroyedTriangle(int tri)
        {
            DestroyedTriangles.TryAdd(tri);
        }

        Vertex* CreateVertex(float2 p)
        {
            var v = _vertices.Set(new Vertex {Point = p, SeqPos = _verticesSeq.Length});
            _verticesSeq.Add((IntPtr) v);
            _qt.Insert(v);
            return v;
        }

        /// <summary>
        /// Create a new Edge connecting the destination of a to the origin of b,
        /// such that all three edges have the same left face after the connection is complete.
        /// </summary>
        Edge* Connect(Edge* a, Edge* b)
        {
            Assert.IsTrue(GetConnection(a->Dest, b->Org) == null);
            var result = CreateEdge(a->Dest, b->Org);
            Splice(result, a->LNext);
            Splice(result->Sym, b);
            return result;
        }

        // This operator affects the two edge rings around the origins of a and b, and, independently, the two edge
        // rings around the left faces of a and b. In each case, (i) if the two rings are distinct, Splice will combine
        // them into one; (ii) if the two are the same ring, Splice will break it into two separate pieces.
        // Thus, Splice can be used both to attach the two edges together and to break them apart.
        // Guibus and Stolfi (1985 p.96)
        static void Splice(Edge* a, Edge* b)
        {
            var alpha = a->ONext->Rot;
            var beta = b->ONext->Rot;
            var temp1 = b->ONext;
            var temp2 = a->ONext;
            var temp3 = beta->ONext;
            var temp4 = alpha->ONext;
            a->Next = temp1;
            b->Next = temp2;
            alpha->Next = temp3;
            beta->Next = temp4;
        }

        void NewTriangle(Edge* e)
        {
            var tid = NextTriangleId;
            e->TriangleId = tid;
            e->LNext->TriangleId = tid;
            e->LPrev->TriangleId = tid;
        }
    }
}