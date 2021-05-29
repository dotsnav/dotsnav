using DotsNav.Core;
using DotsNav.Core.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav
{
    static class DebugUtil
    {
        internal static void DrawCircle(float2 pos, float radius, Color color, float start = 0, float total = 2 * math.PI, bool delimit = true)
            => DrawEllipse(pos.ToXxY(), new float3(0, 0, 1), new float3(0, 1, 0), radius, radius, color, start, total, delimit);

        internal static void DrawCircle(float2 pos, float radius, float start = 0, float total = 2 * math.PI)
            => DrawEllipse(pos.ToXxY(), new float3(0, 0, 1), new float3(0, 1, 0), radius, radius, Color.white, start, total);

        internal static void DrawCircle(float3 pos, float radius, Color color)
            => DrawEllipse(pos, new float3(0, 0, 1), new float3(0, 1, 0), radius, radius, color);

        internal static void DrawCircle(float3 pos, float radius)
            => DrawEllipse(pos, new float3(0, 0, 1), new float3(0, 1, 0), radius, radius, Color.white);

        internal static void DrawEllipse(float3 pos, float3 forward, float3 up, float radiusX, float radiusY, Color color, float start = 0, float total = 2 * math.PI, bool delimit = true)
        {
            Assert.IsTrue(total <= 2 * math.PI);
            var segments = math.max(15, (int) (math.clamp(4 * Math.Circumference(radiusX), 25, 250) * (total / (2 * math.PI)))) * 2;
            var angle = 0f;
            var rot = quaternion.LookRotation(forward, up);
            var lastPoint = float3.zero;
            var thisPoint = float3.zero;

            for (var i = 0; i < segments + 1; i++)
            {
                thisPoint.x = math.sin(start + angle) * radiusX;
                thisPoint.z = math.cos(start + angle) * radiusY;

                if (i > 0)
                    DrawLine(math.rotate(rot, lastPoint) + pos, math.rotate(rot, thisPoint) + pos, color);

                lastPoint = thisPoint;
                angle += total / segments;
            }

            if (delimit && total < 2 * math.PI)
            {
                thisPoint.x = math.sin(start) * radiusX;
                thisPoint.z = math.cos(start) * radiusY;
                DrawLine(math.rotate(rot, thisPoint) + pos, pos, color);

                thisPoint.x = math.sin(start + total) * radiusX;
                thisPoint.z = math.cos(start + total) * radiusY;
                DrawLine(math.rotate(rot, thisPoint) + pos, pos, color);
            }
        }

        internal static void DrawPoint(float2 pos, Color color, float size = .05f) => DrawPoint(pos.ToXxY(), quaternion.identity, color, size);
        internal static void DrawPoint(float3 pos, Color color, float size = .05f) => DrawPoint(pos, quaternion.identity, color, size);
        internal static void DrawPoint(float2 pos, float size = .05f) => DrawPoint(pos, Color.white, size);
        internal static void DrawPoint(float3 pos, float size = .05f) => DrawPoint(pos, Color.white, size);

        internal static void DrawPoint(float3 pos, quaternion q, Color color, float size = .05f)
        {
            var s = size / 2;
            var bl = pos + math.rotate(q, new float3(-s, 0, -s));
            var br = pos + math.rotate(q, new float3(s, 0, -s));
            var tl = pos + math.rotate(q, new float3(-s, 0, s));
            var tr = pos + math.rotate(q, new float3(s, 0, s));
            DrawLine(bl, tr, color);
            DrawLine(tl, br, color);
        }

        internal static void DrawArrow(float2 from, float2 to, float size = .025f) => DrawArrow(from.ToXxY(), to.ToXxY(), size);
        internal static void DrawArrow(float3 from, float3 to, float size = .025f) => DrawArrow(from, to, Color.magenta, size);
        internal static void DrawArrow(float2 from, float2 to, Color color, float size = .025f) => DrawArrow(from.ToXxY(), to.ToXxY(), color, size);

        internal static void DrawArrow(float3 from, float3 to, Color color, float size = .025f)
        {
            var t0 = new float3(-size, 0, -size);
            var t1 = new float3(+size, 0, -size);
            var q = quaternion.LookRotationSafe(to - from, new float3(0, 1, 0));
            DrawLine(from, to, color);
            DrawLine(to, to + math.rotate(q, t0), color);
            DrawLine(to, to + math.rotate(q, t1), color);
        }

        internal static void DrawLine(double2 a, double2 b) => DrawLine((float2) a, (float2) b, Color.white);
        internal static void DrawLine(double2 a, double2 b, Color color) => DrawLine((float2) a, (float2) b, color);
        internal static void DrawLine(float2 a, float2 b) => DrawLine(a, b, Color.white);
        internal static void DrawLine(float2 a, float2 b, Color color) => DrawLine(a.ToXxY(), b.ToXxY(), color);
        internal static void DrawLine(float3 a, float3 b) => DrawLine(a, b, Color.white);

        internal static void DrawLine(float3 a, float3 b, Color color) => Debug.DrawLine(a, b, color, 0, false);

        internal static void DrawCircle(float2 p0, float2 p1, float2 p2) => DrawCircle(p0, p1, p2, Color.white);

        internal static void DrawCircle(float2 p0, float2 p1, float2 p2, Color color)
        {
            Math.CircleFromPoints(p0, p1, p2, out var c, out var r);
            DrawCircle((float2) c, (float) r, color);
        }

        internal static void DrawCircle(float2 c, float radius, float2 from, float2 to)
            => DrawCircle(c, radius, from, to, Color.white);

        internal static void DrawCircle(float2 c, float radius, float2 from, float2 to, Color color, bool delimit = true)
        {
            var ba = Math.Angle(from - c);
            var bc = Math.Angle(to - c);
            var fromAngle = math.min(ba, bc);
            var total = math.abs(ba - bc);
            if (total > math.PI)
            {
                total = math.abs(total - 2 * math.PI);
                fromAngle = math.max(ba, bc);
            }

            DrawCircle(c, radius, color, fromAngle, total, delimit);
        }

        internal static void Draw(Quad r)
        {
            DrawLine(r.A, r.C);
            DrawLine(r.C, r.D);
            DrawLine(r.D, r.B);
            DrawLine(r.B, r.A);
        }
    }
}