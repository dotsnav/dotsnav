using Unity.Mathematics;
using UnityEngine;

public static class DemoMath
{
    public static bool Contains(Vector2 p, Vector2 min, Vector2 max) => p.x >= min.x && p.y >= min.y && p.x <= max.x && p.y <= max.y;

    public static Vector2 Rotate(Vector2 v, double angleRadians)
    {
        var s = math.sin(-angleRadians);
        var c = math.cos(-angleRadians);
        return (float2) new double2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    public static bool IntersectSegSeg(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, out Vector2 result)
    {
        double2 p0 = (float2) v0;
        double2 p1 = (float2) v1;
        double2 p2 = (float2) v2;
        double2 p3 = (float2) v3;

        var s10 = p1 - p0;
        var s32 = p3 - p2;

        var denom = s10.x * s32.y - s32.x * s10.y;
        if (denom == 0)
        {
            result = default;
            return false;
        }

        var denomPositive = denom > 0;

        var s02 = p0 - p2;
        var sNumer = s10.x * s02.y - s10.y * s02.x;
        if (sNumer < 0 == denomPositive)
        {
            result = default;
            return false;
        }

        var tNumer = s32.x * s02.y - s32.y * s02.x;
        if (tNumer < 0 == denomPositive)
        {
            result = default;
            return false;
        }

        if (sNumer > denom == denomPositive || tNumer > denom == denomPositive)
        {
            result = default;
            return false;
        }

        result = (float2) (p0 + tNumer / denom * s10);
        return true;
    }
}