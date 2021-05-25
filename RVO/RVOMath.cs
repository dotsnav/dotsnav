using Unity.Mathematics;

static class RVOMath
{

    internal static float Determinant(float2 vector1, float2 vector2)
        => vector1.x * vector2.y - vector1.y * vector2.x;
}