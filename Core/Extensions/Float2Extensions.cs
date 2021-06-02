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
        public static bool IsNumber(this float2 f) => f.x.IsNumber() && f.y.IsNumber();
        public static bool IsNan(this float2 f) => float.IsNaN(f.x) || float.IsNaN(f.y);
        public static bool IsInfitiy(this float2 f) => float.IsInfinity(f.x) || float.IsInfinity(f.y);
        public static bool IsNegativeInfinity(this float2 f) => float.IsNegativeInfinity(f.x) || float.IsNegativeInfinity(f.y);
        public static bool IsPositiveInfinity(this float2 f) => float.IsPositiveInfinity(f.x) || float.IsPositiveInfinity(f.y);
    }
}