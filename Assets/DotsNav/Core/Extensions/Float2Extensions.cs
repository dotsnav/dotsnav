using Unity.Mathematics;

namespace DotsNav.Core.Extensions
{
    static class Float2Extensions
    {
        public static float3 ToXxY(this float2 f, float y = 0) => new float3(f.x, y, f.y);
    }
}
