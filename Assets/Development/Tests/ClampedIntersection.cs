using Unity.Mathematics;
using UnityEngine;
using DotsNav;
using DotsNav.Navmesh.Hybrid;

[ExecuteInEditMode]
public class ClampedIntersection : MonoBehaviour
{
    public Transform L0;
    public Transform L1;
    public Transform S0;
    public Transform S1;
    public Transform R;

    void Update()
    {
        var l0P = L0.position.xz();
        var l1P = L1.position.xz();
        var dir = l1P - l0P;
        DebugUtil.DrawLine(l0P + 100 * dir, l0P - 100 * dir);
        DebugUtil.DrawLine(S0.position.xz(), S1.position.xz());
        R.position = IntersectSegSeg(l0P, l1P, S0.position.xz(), S1.position.xz()).ToXxY();
        R.position += new Vector3(0, .5f, 0);
    }

    public static float2 IntersectSegSeg(double2 l0, double2 l1, double2 s0, double2 s1)
    {
        var s10 = l1 - l0;
        var s32 = s1 - s0;
        var denom = s10.x * s32.y - s32.x * s10.y;

        if (denom == 0)
        {
            Assert.IsTrue(false);
            return (float2) ((s0 + s1) / 2); // Collinear
        }

        var denomPositive = denom > 0;
        var s02 = l0 - s0;
        var sNumer = s10.x * s02.y - s10.y * s02.x;

        if (sNumer < 0 == denomPositive)
            return (float2) s0;
        if (sNumer > denom == denomPositive )
            return (float2) s1;
        return (float2) (s0 + sNumer / denom * s32);
    }
}
