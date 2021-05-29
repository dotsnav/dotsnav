using DotsNav;
using DotsNav.Navmesh.Hybrid;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class DrawCircleSegment : MonoBehaviour
{
    public Transform C;
    public Transform From;
    public Transform To;

    void Update()
    {
        var from = From.position.xz();
        var c = C.position.xz();
        var to = To.position.xz();
        DebugUtil.DrawCircle(c, math.length(from - c), from, to);
    }
}
