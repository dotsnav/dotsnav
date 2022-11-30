using Unity.Mathematics;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector3 ToXxY(this Vector2 v, float y = 0) => new(v.x, y, v.y);
}

public static class Vector3Extensions
{
    // todo use vector2?
    public static float2 xz(this Vector3 v) => new(v.x, v.z);
}
