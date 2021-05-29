using System;
using System.Linq;
using DotsNav;
using DotsNav.Core.Predicates;
using DotsNav.Navmesh.Hybrid;
using Unity.Mathematics;
using UnityEngine;
using Math = DotsNav.Core.Math;

[ExecuteInEditMode]
public class SameTriangle : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public Transform C;
    public Transform S;
    public Transform G;
    public float R = 1;
    public Transform[] Edges;

    void Update()
    {
        var a = A.position.xz();
        var b = B.position.xz();
        var c = C.position.xz();
        var s = S.position.xz();
        var g = G.position.xz();
        DebugUtil.DrawCircle(a, b, c, Color.black);

        DebugUtil.DrawLine(A.position.xz(), B.position.xz(), Color.black);
        DebugUtil.DrawLine(B.position.xz(), C.position.xz(), Color.black);
        DebugUtil.DrawLine(C.position.xz(), A.position.xz(), Color.black);
        DebugUtil.DrawLine(S.position.xz(), G.position.xz());
        DebugUtil.DrawCircle(S.position.xz(), R);
        DebugUtil.DrawCircle(G.position.xz(), R);

        foreach (var edge in GetEdges())
            DebugUtil.DrawLine(edge.Item1, edge.Item2, Color.red);

        FindPath();

        void FindPath()
        {


            var sg = g - s;
            var perp = Math.PerpCcw(sg);
            var pd = math.normalize(perp) * 2 * R;

            var sp = s + perp;
            var gp = g + perp;

            double2 bl = default;
            double2 tl = default;
            double2 br = default;
            double2 tr = default;

            var topFound = false;
            var bottomFound = false;

            CheckVertex(a);
            CheckVertex(b);
            CheckVertex(c);

            if (!bottomFound)
            {
                bl = IntersectTri(false, s, sp);
                br = IntersectTri(false, g, gp);
            }

            if (!topFound)
            {
                tl = IntersectTri(true, s, sp);
                tr = IntersectTri(true, g, gp);
            }

            void CheckVertex(double2 v)
            {
                if (GeometricPredicates.Orient2DFast(s, sp, v) <= 0 && GeometricPredicates.Orient2DFast(g, gp, v) >= 0)
                {
                    if (GeometricPredicates.Orient2DFast(s, g, v) > 0)
                    {
                        tl = Math.ProjectLine(s, sp, v + pd);
                        tr = Math.ProjectLine(g, gp, v + pd);
                        topFound = true;
                    }
                    else
                    {
                        bl = Math.ProjectLine(s, sp, v - pd);
                        br = Math.ProjectLine(g, gp, v - pd);
                        bottomFound = true;
                    }
                }
            }

            double2 IntersectTri(bool up, double2 l0, double2 l1)
            {
                if (IntersectLineSeg(l0, l1, a, b, out var r))
                {
                    var orient = GeometricPredicates.Orient2DFast(s, g, r);
                    if ((up ? orient > 0 : orient < 0) || orient == 0 && (up ? math.dot(sg, b - a) < 0 : math.dot(sg, b - a) > 0))
                        return up ? r + pd : r - pd;
                }

                if (IntersectLineSeg(l0, l1, b, c, out r))
                {
                    var orient = GeometricPredicates.Orient2DFast(s, g, r);
                    if ((up ? orient > 0 : orient < 0) || orient == 0 && (up ? math.dot(sg, c - b) < 0 : math.dot(sg, c - b) > 0))
                        return up ? r + pd : r - pd;
                }

                if (IntersectLineSeg(l0, l1, c, a, out r))
                {
                    var orient = GeometricPredicates.Orient2DFast(s, g, r);
                    if ((up ? orient > 0 : orient < 0) || orient == 0 && (up ? math.dot(sg, a - c) < 0 : math.dot(sg, a - c) > 0))
                        return up ? r + pd : r - pd;
                }

                throw new BreakDebuggerException();
            }

            // DebugUtil.DrawLine(bl, br);
            // DebugUtil.DrawLine(br, tr);
            // DebugUtil.DrawLine(tr, tl);
            // DebugUtil.DrawLine(tl, bl);
        }
    }

    System.Collections.Generic.List<Tuple<float2, float2>> GetEdges() =>
        Edges.Select(e => new Tuple<float2, float2>(e.position.xz(), e.GetChild(0).position.xz())).ToList();
    
    static bool IntersectLineSeg(double2 l0, double2 l1, double2 s0, double2 s1, out double2 p)
    {
        if (math.all(s0 == l0) || math.all(s0 == l1))
        {
            p = s0;
            return true;
        }

        if (math.all(s1 == l0) || math.all(s1 == l1))
        {
            p = s1;
            return true;
        }

        var s10 = l1 - l0;
        var s32 = s1 - s0;
        var denom = s10.x * s32.y - s32.x * s10.y;

        if (denom == 0)
        {
            p = default;
            return false;
        }

        var denomPositive = denom > 0;
        var s02 = l0 - s0;
        var sNumer = s10.x * s02.y - s10.y * s02.x;

        if (sNumer < 0 == denomPositive || sNumer > denom == denomPositive)
        {
            p = default;
            return false;
        }

        p = s0 + sNumer / denom * s32;
        return true;
    }
}
