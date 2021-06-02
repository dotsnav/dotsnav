using UnityEngine;
using DotsNav;
using DotsNav.Navmesh.Hybrid;

[ExecuteInEditMode]
class TangentsTest : MonoBehaviour
{
    public Transform C0;
    public Transform C1;
    public Transform P;
    public Transform C;
    public float R;

    void Update()
    {
        var p0 = C0.position.xz();
        var p1 = C1.position.xz();
        var c = C.position.xz();
        var p = P.position.xz();

        DebugUtil.DrawCircle(p0, R, Color.green);
        DebugUtil.DrawCircle(p1, R, Color.red);
        DebugUtil.DrawCircle(c, R);

        Math.GetOuterTangentRight(p0, p1, R, out var ol0, out var ol1);
        DebugUtil.DrawLine(ol0, ol1, Color.black);
        DebugUtil.DrawPoint(ol0, Color.green);
        DebugUtil.DrawPoint(ol1, Color.red);

        Math.GetOuterTangentLeft(p0, p1, R, out var or0, out var or1);
        DebugUtil.DrawLine(or0, or1, Color.white);
        DebugUtil.DrawPoint(or0, Color.green);
        DebugUtil.DrawPoint(or1, Color.red);

        Math.GetInnerTangentRight(p0, p1, R, out var il0, out var il1);
        DebugUtil.DrawLine(il0, il1, Color.black);
        DebugUtil.DrawPoint(il0, Color.green);
        DebugUtil.DrawPoint(il1, Color.red);

        Math.GetInnerTangentLeft(p0, p1, R, out var ir0, out var ir1);
        DebugUtil.DrawLine(ir0, ir1, Color.white);
        DebugUtil.DrawPoint(ir0, Color.green);
        DebugUtil.DrawPoint(ir1, Color.red);

        var left = Math.GetTangentLeft(p, c, R);
        DebugUtil.DrawLine(p, left, Color.white);

        var right = Math.GetTangentRight(p, c, R);
        DebugUtil.DrawLine(p, right, Color.black);
    }
}