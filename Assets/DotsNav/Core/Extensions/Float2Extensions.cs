using Unity.Mathematics;

namespace DotsNav
{
    static class FloatExtensions
    {
        public static bool IsNumber(this float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }

    static class Float2Extensions
    {
        public static float3 ToXxY(this float2 f, float y = 0) => new float3(f.x, y, f.y);
        public static bool2 IsNumber(this float2 f) => new bool2(f.x.IsNumber(), f.y.IsNumber());
        public static bool2 IsNan(this float2 f) => new bool2(float.IsNaN(f.x), float.IsNaN(f.y));
        public static bool2 IsInfitiy(this float2 f) => new bool2(float.IsInfinity(f.x), float.IsInfinity(f.y));
        public static bool2 IsNegativeInfinity(this float2 f) => new bool2(float.IsNegativeInfinity(f.x), float.IsNegativeInfinity(f.y));
        public static bool2 IsPositiveInfinity(this float2 f) => new bool2(float.IsPositiveInfinity(f.x), float.IsPositiveInfinity(f.y));
    }
}