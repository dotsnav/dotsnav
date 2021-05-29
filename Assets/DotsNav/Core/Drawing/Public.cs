using DotsNav.Core.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Core.Drawing
{
    // struct Color
    // {
    //     internal static ColorIndex Quantize(float4 rgba)
    //     {
    //         var oldi = 0;
    //         var oldd = math.lengthsq(rgba - Unmanaged.Instance.Data.ColorData[0]);
    //         for (var i = 1; i < Unmanaged.KMaxColors; ++i)
    //         {
    //             var newd = math.lengthsq(rgba - Unmanaged.Instance.Data.ColorData[0]);
    //             if (newd < oldd)
    //             {
    //                 oldi = i;
    //                 oldd = newd;
    //             }
    //         }
    //         return new ColorIndex {Value = oldi};
    //     }
    // }

    struct Arrows
    {
        Lines _lines;
        internal Arrows(int count)
        {
            _lines = new Lines(count * 5);
        }

        internal void Draw(float3 x, float3 v, Color color)
        {
            var x0 = x;
            var x1 = x + v;

            _lines.Draw(x0, x1, color);

            var length = Math.NormalizeWithLength(v, out var dir);
            Math.CalculatePerpendicularNormalized(dir, out var perp, out var perp2);
            float3 scale = length * 0.2f;

            _lines.Draw(x1, x1 + (perp - dir) * scale, color);
            _lines.Draw(x1, x1 - (perp + dir) * scale, color);
            _lines.Draw(x1, x1 + (perp2 - dir) * scale, color);
            _lines.Draw(x1, x1 - (perp2 + dir) * scale, color);
        }
    }

    struct Arrow
    {
        internal static void Draw(float3 x, float3 v, Color color)
        {
            new Arrows(1).Draw(x, v, color);
        }
    }

    struct Planes
    {
        Lines _lines;
        internal Planes(int count)
        {
            _lines = new Lines(count * 9);
        }

        internal void Draw(float3 x, float3 v, Color color)
        {
            var x0 = x;
            var x1 = x + v;

            _lines.Draw(x0, x1, color);

            var length = Math.NormalizeWithLength(v, out var dir);
            Math.CalculatePerpendicularNormalized(dir, out var perp, out var perp2);
            float3 scale = length * 0.2f;

            _lines.Draw(x1, x1 + (perp - dir) * scale, color);
            _lines.Draw(x1, x1 - (perp + dir) * scale, color);
            _lines.Draw(x1, x1 + (perp2 - dir) * scale, color);
            _lines.Draw(x1, x1 - (perp2 + dir) * scale, color);

            perp *= length;
            perp2 *= length;

            _lines.Draw(x0 + perp + perp2, x0 + perp - perp2, color);
            _lines.Draw(x0 + perp - perp2, x0 - perp - perp2, color);
            _lines.Draw(x0 - perp - perp2, x0 - perp + perp2, color);
            _lines.Draw(x0 - perp + perp2, x0 + perp + perp2, color);
        }
    }

    struct Plane
    {
        internal static void Draw(float3 x, float3 v, Color color)
        {
            new Planes(1).Draw(x, v, color);
        }
    }

    struct Arcs
    {
        Lines _lines;
        const int Res = 64;

        internal Arcs(int count)
        {
            _lines = new Lines(count * (2 + Res));
        }

        internal void Draw(float3 center, float3 normal, float3 arm, float angle, Color color, bool delimit = false)
        {
            delimit &= angle < 2 * math.PI;
            var q = quaternion.AxisAngle(normal, angle / Res);
            var currentArm = arm;
            if (delimit)
                _lines.Draw(center, center + currentArm, color);
            for (var i = 0; i < Res; i++)
            {
                var nextArm = math.mul(q, currentArm);
                _lines.Draw(center + currentArm, center + nextArm, color);
                currentArm = nextArm;
            }
            if (delimit)
                _lines.Draw(center, center + currentArm, color);
        }

        internal static void Draw(NativeList<Line> lines, float3 center, float3 normal, float3 arm, float angle, Color color, float radius = 1, bool delimit = false)
        {
            delimit &= angle < 2 * math.PI;
            var res = math.clamp((int) (radius * 24), 16, 64);
            var q = quaternion.AxisAngle(normal, angle / res);
            var currentArm = arm;
            if (delimit)
                lines.Add(new Line(center, center + currentArm, color));
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, currentArm);
                lines.Add(new Line(center + currentArm, center + nextArm, color));
                currentArm = nextArm;
            }
            if (delimit)
                lines.Add(new Line(center, center + currentArm, color));
        }
    }

    struct Arc
    {
        internal static void Draw(float3 center, float3 normal, float3 arm, float angle, Color color, bool delimit = false)
        {
            new Arcs(1).Draw(center, normal, arm, angle, color, delimit);
        }

        internal static void Draw(NativeList<Line> lines, float3 center, float3 normal, float3 arm, float angle, Color color, float radius = 1, bool delimit = false)
        {
            Arcs.Draw(lines, center, normal, arm, angle, color, radius, delimit);
        }
    }

    struct Boxes
    {
        Lines _lines;

        internal Boxes(int count)
        {
            _lines = new Lines(count * 12);
        }

        internal void Draw(float3 size, float3 center, quaternion orientation, Color color)
        {
            var mat = math.float3x3(orientation);
            var x = mat.c0 * size.x * 0.5f;
            var y = mat.c1 * size.y * 0.5f;
            var z = mat.c2 * size.z * 0.5f;
            var c0 = center - x - y - z;
            var c1 = center - x - y + z;
            var c2 = center - x + y - z;
            var c3 = center - x + y + z;
            var c4 = center + x - y - z;
            var c5 = center + x - y + z;
            var c6 = center + x + y - z;
            var c7 = center + x + y + z;

            _lines.Draw(c0, c1, color); // ring 0
            _lines.Draw(c1, c3, color);
            _lines.Draw(c3, c2, color);
            _lines.Draw(c2, c0, color);

            _lines.Draw(c4, c5, color); // ring 1
            _lines.Draw(c5, c7, color);
            _lines.Draw(c7, c6, color);
            _lines.Draw(c6, c4, color);

            _lines.Draw(c0, c4, color); // between rings
            _lines.Draw(c1, c5, color);
            _lines.Draw(c2, c6, color);
            _lines.Draw(c3, c7, color);
        }
    }

    struct Box
    {
        internal static void Draw(float3 size, float3 center, quaternion orientation, Color color)
        {
            new Boxes(1).Draw(size, center, orientation, color);
        }
    }

    struct Cones
    {
        Lines _lines;
        const int Res = 16;

        internal Cones(int count)
        {
            _lines = new Lines(count * Res * 2);
        }

        internal void Draw(float3 point, float3 axis, float angle, Color color)
        {
            var scale = Math.NormalizeWithLength(axis, out var dir);
            float3 arm;
            {
                Math.CalculatePerpendicularNormalized(dir, out var perp1, out _);
                arm = math.mul(quaternion.AxisAngle(perp1, angle), dir) * scale;
            }
            var q = quaternion.AxisAngle(dir, 2.0f * math.PI / Res);

            for (var i = 0; i < Res; i++)
            {
                var nextArm = math.mul(q, arm);
                _lines.Draw(point, point + arm, color);
                _lines.Draw(point + arm, point + nextArm, color);
                arm = nextArm;
            }
        }
    }

    struct Cone
    {
        internal static void Draw(float3 point, float3 axis, float angle, Color color)
        {
            new Cones(1).Draw(point, axis, angle, color);
        }
    }

    struct Lines
    {
        Unit _unit;
        internal Lines(int count)
        {
            _unit = Unmanaged.Instance.Data.LineBufferAllocations.AllocateAtomic(count);
        }

        internal unsafe void Draw(NativeArray<Line> lines)
        {
            var linesToCopy = math.min(lines.Length, _unit.End - _unit.Next);
            Unmanaged.Instance.Data.LineBuffer.CopyFrom(lines.GetUnsafeReadOnlyPtr(), linesToCopy, _unit.Next);
            _unit.Next += linesToCopy;
        }

        internal void Draw(float3 begin, float3 end, Color color)
        {
            if (_unit.Next < _unit.End)
                Unmanaged.Instance.Data.LineBuffer.SetLine(new Line(begin, end, color), _unit.Next++);
        }
    }

    struct Line
    {
        public float4 Begin;
        public float4 End;

        internal Line(float2 begin, float2 end, Color color) : this(begin.ToXxY(), end.ToXxY(), color) { }

        internal Line(float3 begin, float3 end, Color color) : this()
        {
            var layer = (int) (color.a / 10);
            var a = color.a - layer * 10;
            var offset = layer * .001f;
            begin.y += offset;
            end.y += offset;
            var packedColor = ((int) (color.r * 63) << 18) | ((int) (color.g * 63) << 12) | ((int) (color.b * 63) << 6) | (int) (a * 63);
            Begin = new float4(begin, packedColor);
            End = new float4(end, packedColor);
        }

        internal static void Draw(float3 begin, float3 end, Color color)
        {
            new Lines(1).Draw(begin, end, color);
        }

        internal static void Draw(NativeArray<Line> lines)
        {
            new Lines(lines.Length).Draw(lines);
        }
    }
}
