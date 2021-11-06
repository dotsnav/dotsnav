using Unity.Mathematics;
using UnityEngine;
using DotsNav;
using DotsNav.PathFinding;

[ExecuteInEditMode]
public class RectFromCircle : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public Transform C;
    public Transform D;
    public Transform E;

    public float R;

    void Update()
    {
        var a = A.position.xz();
        var b = B.position.xz();
        DebugUtil.DrawCircle(a, R);
        DebugUtil.DrawCircle(b, R);
        var r = TransformRect((a + b) / 2, new float2(2 * R, math.length(b - a)), Math.Angle(b - a));
        DebugUtil.Draw(r);
        DebugUtil.DrawLine(C.position.xz(), D.position.xz());
        E.gameObject.SetActive(Intersect(r, C.position.xz(), D.position.xz()));

        // DebugUtil.DrawCircle(r.A, .5f);
        // DebugUtil.DrawCircle(r.B, .5f, Color.yellow);
        // DebugUtil.DrawCircle(r.C, .5f, Color.red);
    }

    static bool Intersect(Quad r, float2 p0, float2 p1)
    {
        return Contains(p0) ||
               Contains(p1) ||
               Math.IntersectSegSeg(r.A, r.C, p0, p1) ||
               Math.IntersectSegSeg(r.C, r.D, p0, p1) ||
               Math.IntersectSegSeg(r.D, r.B, p0, p1) ||
               Math.IntersectSegSeg(r.B, r.A, p0, p1);

        bool Contains(float2 p)
        {
            return GeometricPredicates.Orient2DFast(r.A, r.C, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.C, r.D, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.D, r.B, p) >= 0 &&
                   GeometricPredicates.Orient2DFast(r.B, r.A, p) >= 0;
        }
    }

    static Quad TransformRect(float2 translation, float2 size, float angle)
    {
        var h = size / 2;

        return new Quad
        {
            A = Math.Rotate(-h, angle) + translation,
            B = Math.Rotate(new float2(-h.x, h.y), angle) + translation,
            C = Math.Rotate(new float2(h.x, -h.y), angle) + translation,
            D = Math.Rotate(h, angle) + translation
        };
    }
}