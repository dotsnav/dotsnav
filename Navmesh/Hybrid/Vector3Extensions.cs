using Unity.Mathematics;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector3 ToXxY(this Vector2 v, float y = 0) => new Vector3(v.x, y, v.y);
}

public static class Vector3Extensions
{
    // todo use vector2?
    public static float2 xz(this Vector3 v) => new float2(v.x, v.z);
}
