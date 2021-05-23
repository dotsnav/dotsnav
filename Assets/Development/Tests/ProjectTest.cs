using Unity.Mathematics;
using UnityEngine;
using DotsNav;

[ExecuteInEditMode]
public class ProjectTest : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public Transform C;

    void Update()
    {
        var a = A.position.xz();
        var b = B.position.xz();
        DebugUtil.DrawLine(a, b);
        var c = C.position.xz();
        Math.ProjectSeg(a, b, c, out var r);
        DebugUtil.DrawPoint((float2) r);
        DebugUtil.DrawPoint(a);
        DebugUtil.DrawPoint(b);
        DebugUtil.DrawPoint(c);
    }
}
