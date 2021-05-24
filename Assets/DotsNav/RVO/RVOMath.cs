using Unity.Mathematics;

static class RVOMath
{
    internal const float RvoEpsilon = 0.00001f;

    internal static float Determinant(float2 vector1, float2 vector2)
        => vector1.x * vector2.y - vector1.y * vector2.x;

    internal static float DistSqPointLineSegment(float2 vector1, float2 vector2, float2 vector3)
    {
        var v1 = vector3 - vector1;
        var v2 = vector2 - vector1;
        var r = (v1.x * v2.x + v1.y * v2.y) / math.lengthsq(vector2 - vector1);

        if (r < 0.0f)
            return math.lengthsq(vector3 - vector1);

        return r > 1.0f
            ? math.lengthsq(vector3 - vector2)
            : math.lengthsq(vector3 - (vector1 + r * (vector2 - vector1)));
    }

    internal static float LeftOf(float2 a, float2 b, float2 c)
        => Determinant(a - c, b - a);

    internal static float Square(float f)
        => f * f;
}