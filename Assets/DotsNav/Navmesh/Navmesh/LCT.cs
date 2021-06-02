using DotsNav.Navmesh.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Navmesh
{
    public unsafe partial struct Navmesh
    {
        internal static void ResetClearance(Edge* s)
        {
            var perp = Math.PerpCcw(s->Dest->Point - s->Org->Point);
            var o = s->Org->Point;
            var d = s->Dest->Point;
            ResetClearance(o, d, s, perp);
            ResetClearance(d, o, s->Sym, perp);
        }

        static void ResetClearance(double2 o, double2 d, Edge* t, float2 perp)
        {
            var rhs = t->LPrev;
            var lhs = t->LNext->Sym;
            var a = rhs->Dest->Point;
            var b = rhs->Org->Point;
            var c = lhs->Dest->Point;
            var f = Math.ProjectSeg2(o, d, b, out var bi);

            if (f >= 0 && f <= 1 &&
                !lhs->Constrained && !rhs->Constrained &&
                math.lengthsq(bi - b) < math.min(math.lengthsq(a - b), math.lengthsq(c - b)))
            {
                lhs->ClearanceLeft = -1;
                rhs->ClearanceRight = -1;
            }

            if (f >= 0 && !rhs->Constrained)
            {
                var r = Math.IntersectLineSegClamped(o, o + perp, b, (a + b) / 2);
                Math.ProjectSeg2(o, d, r, out var ri);
                if (math.lengthsq(ri - r) < math.min(math.lengthsq(b - r), math.lengthsq(a - r)))
                    ResetClearance(o, d, rhs->Sym, perp);
            }

            if (f <= 1 && !lhs->Constrained)
            {
                var l = Math.IntersectLineSegClamped(d, d + perp, b, (c + b) / 2);
                Math.ProjectSeg2(o, d, l, out var li);
                if (math.lengthsq(li - l) < math.min(math.lengthsq(b - l), math.lengthsq(c - l)))
                    ResetClearance(o, d, lhs, perp);
            }
        }

        internal void CheckEdgeForDisturbances(Edge* edge, NativeList<Disturbance> disturbances)
        {
            if (!edge->ONext->Constrained && !edge->DNext->Constrained)
            {
                var a = edge->ONext->Dest->Point;
                var b = edge->Org->Point;
                var c = edge->Dest->Point;
                if (CheckTraversal(a, b, c, edge, false, out var disturbance))
                    disturbances.Add(disturbance);
            }

            if (!edge->OPrev->Constrained && !edge->DPrev->Constrained)
            {
                var a = edge->OPrev->Dest->Point;
                var b = edge->Org->Point;
                var c = edge->Dest->Point;
                if (CheckTraversal(a, b, c, edge, true, out var disturbance))
                    disturbances.Add(disturbance);
            }
        }

        internal static float GetLocalClearance(double2 a, double2 b, double2 c, Edge* edge)
        {
            var lba = math.lengthsq(a - b);
            var lbc = math.lengthsq(c - b);
            var clearance = math.sqrt(math.min(lba, lbc));
            var constraint = TryGetConstraint(clearance, b, edge);
            if (constraint == null)
                return (float) clearance;
            return (float) math.length(Math.ClosestPointOnLineSegment(b, constraint->Org->Point, constraint->Dest->Point) - b);
        }

        bool CheckTraversal(float2 a, float2 b, float2 c, Edge* exit, bool lhs, out Disturbance disturbance)
        {
            if (!Math.ProjectSeg(a, c, b, out var bac))
            {
                disturbance = default;
                return false;
            }

            var mirror = b + (c - 2 * bac + a);
            var lba = math.lengthsq(a - b);
            var lbc = math.lengthsq(c - b);
            var clearance = math.sqrt(math.min(lba, lbc));
            var constraint = TryGetConstraint(exit, clearance, b, mirror, lhs);

            if (constraint == null || constraint->RefineFailed)
            {
                disturbance = default;
                return false;
            }

            var s1 = constraint->Org->Point;
            var s2 = constraint->Dest->Point;

            if (!Math.ProjectSeg(s1, s2, b, out var bi))
            {
                disturbance = default;
                return false;
            }

            // The paper says R is delimited by a line parallel to s passing by b.
            // When cl(a, b, c) == dist(b, a) and dist(b, a) < dist(b, s) this allows
            // for vertices in R violating definition 3.4: dist(v, s) < cl(a, b, c).
            // R will be delimited by a line parallel to s at a distance of cl(a, b, c)
            // Technially r1 should be at the intersection point with bc, but as bc
            // is enclosed by the Delaunay circle of triangle abc this is irrelevant.

            // R is triangle r1, r2, c
            var bib = b - bi;
            var lbib = math.length(bib);
            clearance = math.min(clearance, lbib);
            var r1 = bi + bib / lbib * clearance;
            var acp = Math.PerpCw(c - a);
            var s12 = s2 - s1;
            var s12p = Math.PerpCw(s12);
            var r2 = Math.Angle(acp, s12p) < 0
                ? Math.IntersectLineLine(r1, r1 + s12, c, c + acp)
                : Math.IntersectLineLine(r1, r1 + s12, c, c + s12p);

            if (TryGetDisturbance(lhs ? exit->Sym : exit, r1, r2, c, out var e, out var u, out var vert, lhs))
            {
                var v = vert->Point;
                var vi = Math.ProjectLine(s1, s2, v);
                var lvs = math.length(vi - v);
                var lve = math.length(e - v);

                if (lvs < lve && lvs < clearance)
                {
                    Math.CircleFromPoints(u, v, e, out var centre, out var radius);
                    var t = Math.IntersectLineCircle(s1, s2, centre, radius, out var x1, out var x2);
                    Assert.IsTrue(t == 2);
                    var pRef = (x1 + x2) / 2;
                    if (ValidateRefinement(ref pRef, constraint))
                    {
                        disturbance = new Disturbance(vert, pRef, constraint);
                        return true;
                    }

                    constraint->RefineFailed = true;
                }
            }

            disturbance = default;
            return false;
        }

        bool ValidateRefinement(ref float2 pRef, Edge* c)
        {
            if (SplitIsRobust(pRef, c) && _qt.FindClosest(pRef, _e * _e) == null)
                return true;

            var fractions = stackalloc float[] {.5f, .25f, .75f, .375f, .625f};
            var od = c->Dest->Point - c->Org->Point;

            for (int i = 0; i < 5; i++)
            {
                pRef = c->Org->Point + fractions[i] * od;
                if (SplitIsRobust(pRef, c) && _qt.FindClosest(pRef, _e * _e) == null)
                    return true;
            }

            return false;
        }

        static bool SplitIsRobust(float2 point, Edge* edge)
        {
            // check both sides for highly irregular tris
            // todo try to remove assert from InsertpointInEdge/FlipEdges as this should only happen with constrained edges?
            return Math.TriArea(edge->Org->Point, edge->Dest->Point, point) == 0 ||
                   Math.TriArea(edge->LNext->Org->Point, edge->LNext->Dest->Point, point) > 0 &&
                   Math.TriArea(edge->LPrev->Org->Point, edge->LPrev->Dest->Point, point) > 0 &&
                   Math.TriArea(edge->RNext->Org->Point, edge->RNext->Dest->Point, point) < 0 &&
                   Math.TriArea(edge->RPrev->Org->Point, edge->RPrev->Dest->Point, point) < 0;

            // var o = edge->Org->Point;
            // var d = edge->Dest->Point;
            //
            // if (Math.TriArea(o, d, point) == 0)
            //     return true;
            //
            // if (Math.TriArea(o, d, point) > 0 &&
            //     Math.TriArea(edge->LNext->Org->Point, edge->LNext->Dest->Point, point) > 0 &&
            //     Math.TriArea(edge->LPrev->Org->Point, edge->LPrev->Dest->Point, point) > 0)
            //     return true;
            //
            // if (Math.TriArea(o, d, point) < 0 &&
            //     Math.TriArea(edge->RNext->Org->Point, edge->RNext->Dest->Point, point) < 0 &&
            //     Math.TriArea(edge->RPrev->Org->Point, edge->RPrev->Dest->Point, point) < 0)
            //     return true;
            //
            // return false;
        }

        static bool TryGetDisturbance(Edge* tri, float2 r1, float2 r2, float2 c, out float2 e, out float2 u, out Vertex* v, bool lhs)
        {
            var next = tri->RNext;
            v = next->Org;
            var vert = next->Org->Point; // using v causes burst error
            var p = tri->Org->Point;

            if (Math.TriContains(r1, r2, c, vert))
            {
                if (lhs)
                {
                    u = tri->Dest->Point;
                    e = tri->Org->Point;
                }
                else
                {
                    u = tri->Org->Point;
                    e = tri->Dest->Point;
                }

                return true;
            }

            if (!next->Constrained && Math.IntersectSegSeg(p, vert, c, r2))
                return TryGetDisturbance(next->Sym, r1, r2, c, out e, out u, out v, lhs);

            next = next->RNext;
            p = tri->Dest->Point;

            if (!next->Constrained && Math.IntersectSegSeg(p, vert, r1, r2))
                return TryGetDisturbance(next->Sym, r1, r2, c, out e, out u, out v, lhs);

            e = default;
            u = default;
            v = default;
            return false;
        }

        static Edge* TryGetConstraint(Edge* exit, double clearance, float2 corner, float2 mirror, bool lhs)
        {
            var edge = lhs ? exit->DNext : exit->DPrev->Sym;
            if (edge->Constrained)
                return edge;
            return CheckTriForConstraint(edge, clearance, corner, mirror);
        }

        static Edge* CheckTriForConstraint(Edge* edge, double clearance, float2 corner, float2 mirror)
        {
            edge = edge->RNext;
            var result = CheckEdgeForContraint(edge, clearance, corner, mirror);
            if (result != null)
                return result;
            return CheckEdgeForContraint(edge->RNext, clearance, corner, mirror);
        }

        static Edge* CheckEdgeForContraint(Edge* edge, double clearance, float2 corner, float2 mirror)
        {
            var s1 = edge->Org->Point;
            var s2 = edge->Dest->Point;

            if (!(Math.IntersectSegCircle(s1, s2, corner, clearance) == 2 ||
                  Math.IntersectSegCircle(s1, s2, mirror, clearance) == 2))
                return null;

            if (edge->Constrained)
                return edge;

            return CheckTriForConstraint(edge->Sym, clearance, corner, mirror);
        }

        public static Edge* TryGetConstraint(double clearance, double2 corner, Edge* edge)
        {
            if (edge->Constrained) // todo should we check distance here? We do so for any other constraint we find
                return edge;
            return CheckTriForConstraint(edge, clearance, corner);
        }

        static Edge* CheckTriForConstraint(Edge* edge, double clearance, double2 corner)
        {
            edge = edge->RNext;
            var result = CheckEdgeForContraint(edge, clearance, corner);
            if (result != null)
                return result;
            return CheckEdgeForContraint(edge->RNext, clearance, corner);
        }

        static Edge* CheckEdgeForContraint(Edge* edge, double clearance, double2 corner)
        {
            var s1 = edge->Org->Point;
            var s2 = edge->Dest->Point;

            if (Math.IntersectSegCircle(s1, s2, corner, clearance) != 2)
                return null;

            if (edge->Constrained)
                return edge;

            return CheckTriForConstraint(edge->Sym, clearance, corner);
        }

        void SearchDisturbances()
        {
            var e = C.GetEnumerator();
            while (e.MoveNext())
            {
                var s = (Edge*) e.Current;
                Propagate(s, s);
                Propagate(s, s->Sym);
            }
        }

        void Propagate(Edge* s, Edge* t)
        {
            var exit = t->LPrev;
            if (!exit->ONext->Constrained && !exit->DNext->Constrained)
                CheckTraversal(t->Dest->Point, exit, false, s);

            exit = t->LNext->Sym;
            if (!exit->OPrev->Constrained && !exit->DPrev->Constrained)
                CheckTraversal(t->Org->Point, exit, true, s);
        }

        void CheckTraversal(float2 a, Edge* exit, bool lhs, Edge* s)
        {
            var b = exit->Org->Point;
            var c = exit->Dest->Point;

            if (!Math.ProjectSeg(a, c, b, out var bac))
                return;

            var mirror = b + (c - 2 * bac + a);
            var lba = math.lengthsq(a - b);
            var lbc = math.lengthsq(c - b);
            var clearance = math.sqrt(math.min(lba, lbc));

            var s1 = s->Org->Point;
            var s2 = s->Dest->Point;

            if (Math.IntersectSegCircle(s1, s2, b, clearance) < 2 && Math.IntersectSegCircle(s1, s2, mirror, clearance) < 2)
                return;

            if (Math.ProjectSeg(s1, s2, b, out var bi))
            {
                var bib = b - bi;
                var lbib = math.length(bib);
                clearance = math.min(clearance, lbib);
                var r1 = bi + bib / lbib * clearance;
                var acp = Math.PerpCw(c - a);
                var s12 = s2 - s1;
                var s12p = Math.PerpCw(s12);
                var r2 = Math.Angle(acp, s12p) < 0
                    ? Math.IntersectLineLine(r1, r1 + s12, c, c + acp)
                    : Math.IntersectLineLine(r1, r1 + s12, c, c + s12p);

                if (TryGetDisturbance(lhs ? exit->Sym : exit, r1, r2, c, out var e, out _, out var vert, lhs))
                {
                    var v = vert->Point;
                    var vi = Math.ProjectLine(s1, s2, v);
                    var lvs = math.length(vi - v);
                    var lve = math.length(e - v);

                    if (lvs < lve && lvs < clearance)
                    {
                        V.TryAdd((IntPtr) vert);
                        return;
                    }
                }
            }

            Propagate(s, lhs ? exit : exit->Sym);
        }

        void RemoveRefinements()
        {
            var i = V.GetEnumerator();
            while (i.MoveNext())
                RemoveIfEligible((Vertex*) i.Current);
        }

        void LocalRefinement(DynamicBuffer<DestroyedTriangleElement> destroyed)
        {
            var verts = V.GetEnumerator();
            while (verts.MoveNext())
                _refinementQueue.PushBack(verts.Current);

            InfiniteLoopDetection.Reset();
            while (_refinementQueue.Count > 0)
            {
                InfiniteLoopDetection.Register(1000, "LocalRefinement");
                var v = (Vertex*) _refinementQueue.PopFront();
                var e = v->GetEdgeEnumerator();
                while (e.MoveNext())
                {
                    if (TriDisturbed(e.Current, out var vRef) || TravsDisturbed(e.Current, out vRef))
                    {
                        // todo are we adding duplicates here?
                        _refinementQueue.PushBack((IntPtr) v);
                        _refinementQueue.PushBack((IntPtr) vRef);
                        break;
                    }
                }
            }

            var tris = DestroyedTriangles.GetEnumerator();
            while (tris.MoveNext())
                destroyed.Add(tris.Current);
            destroyed.Reinterpret<int>().AsNativeArray().Sort();
        }

        bool TriDisturbed(Edge* tri, out Vertex* vRef)
        {
            var edge = tri;

            if (!edge->Constrained)
            {
                if (RhsDisturbed(edge, out vRef))
                    return true;
                if (LhsDisturbed(edge->Sym, out vRef))
                    return true;
            }

            edge = tri->LNext;
            if (!edge->Constrained)
            {
                if (RhsDisturbed(edge, out vRef))
                    return true;
                if (LhsDisturbed(edge->Sym, out vRef))
                    return true;
            }

            edge = tri->LNext->LNext;
            if (!edge->Constrained)
            {
                if (RhsDisturbed(edge, out vRef))
                    return true;
                if (LhsDisturbed(edge->Sym, out vRef))
                    return true;
            }

            vRef = default;
            return false;
        }

        bool LhsDisturbed(Edge* edge, out Vertex* vRef)
        {
            edge->ClearanceLeft = -1;

            if (!edge->OPrev->Constrained && !edge->DPrev->Constrained)
            {
                var a = edge->OPrev->Dest->Point;
                var b = edge->Org->Point;
                var c = edge->Dest->Point;
                if (CheckTraversal(a, b, c, edge, true, out var disturbance))
                {
                    vRef = InsertPointInEdge(disturbance.PRef, disturbance.Edge);
                    return true;
                }
            }

            vRef = default;
            return false;
        }

        bool RhsDisturbed(Edge* edge, out Vertex* vRef)
        {
            edge->ClearanceRight = -1;

            if (!edge->ONext->Constrained && !edge->DNext->Constrained)
            {
                var a = edge->ONext->Dest->Point;
                var b = edge->Org->Point;
                var c = edge->Dest->Point;
                if (CheckTraversal(a, b, c, edge, false, out var disturbance))
                {
                    vRef = InsertPointInEdge(disturbance.PRef, disturbance.Edge);
                    return true;
                }
            }

            vRef = default;
            return false;
        }

        bool TravsDisturbed(Edge* edge, out Vertex* vRef)
        {
            var v = edge->Org->Point;
            var check = edge->LNext->Sym;
            InfiniteLoopDetection.Reset();
            
            while (true)
            {
                InfiniteLoopDetection.Register(1000, "TravsDisturbed 0");

                if (check->Constrained)
                    break;

                if (!Math.ProjectSeg(check->Org->Point, check->Dest->Point, v, out _))
                {
                    check = check->OPrev->Sym;
                    if (check->Constrained || !Math.ProjectSeg(check->Org->Point, check->Dest->Point, v, out _))
                        break;
                }

                if (RhsDisturbed(check, out vRef))
                    return true;

                check = check->DPrev;
            }

            check = edge->LNext;
            InfiniteLoopDetection.Reset();

            while (true)
            {
                InfiniteLoopDetection.Register(1000, "TravsDisturbed 1");

                if (check->Constrained)
                    break;

                if (!Math.ProjectSeg(check->Org->Point, check->Dest->Point, v, out _))
                {
                    check = check->ONext->Sym;
                    if (check->Constrained || !Math.ProjectSeg(check->Org->Point, check->Dest->Point, v, out _))
                        break;
                }

                if (LhsDisturbed(check, out vRef))
                    return true;

                check = check->DNext;
            }

            vRef = default;
            return false;
        }

        void GlobalRefine(DynamicBuffer<DestroyedTriangleElement> destroyed)
        {
            var disturbances = new NativeList<Disturbance>(Allocator.Temp);
            var e = GetEdgeEnumerator(true);
            while (e.MoveNext())
                if (!e.Current->Constrained)
                    CheckEdgeForDisturbances(e.Current, disturbances);

            V.Clear();
            for (int i = 0; i < disturbances.Length; i++)
                V.TryAdd((IntPtr) disturbances[i].Vertex);
            LocalRefinement(destroyed);
        }
    }
}