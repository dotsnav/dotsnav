using DotsNav;
using UnityEngine;

class DrawCircle : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public Transform C;

    void Update()
    {
        if (A && B && C)
            DebugUtil.DrawCircle(A.position.xz(), B.position.xz(), C.position.xz());
    }
}