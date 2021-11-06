using DotsNav;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class TraversalSectors : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public Transform C;
    public Transform S1;
    public Transform S2;
    public Transform V;
    public bool lhs;
    
    void Update()
    {
        var a = ((float3)A.position).xz;
        var b = ((float3)B.position).xz;
        var c = ((float3)C.position).xz;
        var s1 = ((float3)S1.position).xz;
        var s2 = ((float3)S2.position).xz;
        var v = ((float3)V.position).xz;

        DebugUtil.DrawLine(s1, s2, Color.red);
        Math.CircleFromPoints(a, b, c, out var p, out var r);
        DebugUtil.DrawCircle((float2)p, (float)r);

        var ba = a - b;
        var bc = c - b;
        var validBacProjection = Math.ProjectSeg(a, c, b, out var bac);

        if (validBacProjection)
        {
            var bbac = bac - b;
            
            var bbaca = Math.Angle(bbac);
            var lba = math.length(ba);
            var lbc = math.length(bc);

            float2 shortestEdge;
            float clearance;

            if (lba <= lbc)
            {
                shortestEdge = ba;
                clearance = lba;
            }
            else
            {
                shortestEdge = bc;
                clearance = lbc;
            }
            
            var angle = Math.Angle(bbac, shortestEdge);
            var startRot = bbaca + angle;
            var rot = bbaca - angle - startRot;
            DebugUtil.DrawCircle(b, clearance, startRot, rot);
            DebugUtil.DrawCircle(b + (c - 2 * bac + a), clearance, startRot, rot);
            
            // todo check all edges crossing traversal zones to find s1, s2
            var validBsProjection = Math.ProjectSeg(s1, s2, b, out var bi);
            clearance = math.min(clearance, math.length(bi - b));

            if (validBsProjection)
            {
                var s12 = s2 - s1;
                
                // The paper says R is delimited by a line parallel to s passing by b.
                // When cl(a, b, c) == dist(b, a) and dist(b, a) < dist(b, s) this allows
                // for vertices in R violating definition 3.4: dist(v, s) < cl(a, b, c).
                // R will be delimited by a line parallel to s with distance cl(a, b, c)
                // Technially r1 should be at the intersection point with bc, but as bc
                // is enclosed in the Delaunay circle of triangle abc this is irrelevant.
                
                // R is triangle r1, r2, c
                var r1 = bi + math.normalize(b - bi) * clearance;
                DebugUtil.DrawLine(r1, r1 + s12);

                var ac = c - a;
                var aco = Math.PerpCw(ac);
                DebugUtil.DrawLine(c, c + aco);

                var s12o = Math.PerpCw(s12);
                DebugUtil.DrawLine(c, c + s12o);

                var r2 = Math.Angle(aco, s12o) < 0
                    ? Math.IntersectLineLine(r1, r1 + s12, c, c + aco) 
                    : Math.IntersectLineLine(r1, r1 + s12, c, c + s12o);

                DebugUtil.DrawPoint(r1, Color.magenta);
                DebugUtil.DrawPoint(r2, Color.magenta);

                if (Math.TriContains(r1, r2, c, v))
                {
                    // todo check all edges crossing R to find v, e and u
                    // todo check v is not shared by _two_ collinear constraints
                    var e = c;
                    var u = b;

                    var vi = Math.ProjectLine(s1, s2, v);
                    var lvs = math.length(vi - v);
                    var lve = math.length(e - v);

                    if (lvs < lve)
                    {
                        Assert.IsTrue(lvs <= clearance); // , $"lvs: {lvs} <= clearance: {clearance}");
                        Math.CircleFromPoints(u, v, e, out var centre, out var radius);
                        var t = Math.IntersectLineCircle(s1, s2, centre, radius, out var x1, out var x2);
                        Assert.IsTrue(t == 2);
                        var pRef = (x1 + x2) / 2;
                        DebugUtil.DrawPoint(pRef, Color.magenta);
                    }
                }
            }
        }
        
        DebugUtil.DrawLine(a, b, Color.black);
        DebugUtil.DrawLine(b, c, Color.black);
        DebugUtil.DrawLine(c, a, Color.black);
    }
}
