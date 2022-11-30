using System.Numerics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace DotsNav
{
    static class Math
    {
        public static double TriArea(double2 a, double2 b, double2 c)
            => GeometricPredicates.Orient2D(a, b, c);

        public static bool Ccw(double2 a, double2 b, double2 c)
            => TriArea(a, b, c) > 0;

        public static bool CcwFast(double2 a, double2 b, double2 c)
            => GeometricPredicates.Orient2DFast(a, b, c) > 0;

        public static bool IntersectSegSeg(double2 p0, double2 p1, double2 p2, double2 p3, out float2 result)
        {
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

        public static bool IntersectSegSeg(double2 p0, double2 p1, double2 p2, double2 p3)
        {
            var s10 = p1 - p0;
            var s32 = p3 - p2;

            var denom = s10.x * s32.y - s32.x * s10.y;
            if (denom == 0)
                return false;
            var denomPositive = denom > 0;

            var s02 = p0 - p2;
            var sNumer = s10.x * s02.y - s10.y * s02.x;
            if (sNumer < 0 == denomPositive)
                return false;

            var tNumer = s32.x * s02.y - s32.y * s02.x;
            if (tNumer < 0 == denomPositive)
                return false;

            if (sNumer > denom == denomPositive || tNumer > denom == denomPositive)
                return false;

            return true;
        }

        public static double2 IntersectLineSegClamped(double2 l0, double2 l1, double2 s0, double2 s1)
        {
            var s10 = l1 - l0;
            var s32 = s1 - s0;
            var denom = s10.x * s32.y - s32.x * s10.y;

            if (denom == 0)
                return (s0 + s1) / 2;

            var denomPositive = denom > 0;
            var s02 = l0 - s0;
            var sNumer = s10.x * s02.y - s10.y * s02.x;

            if (sNumer < 0 == denomPositive)
                return s0;
            if (sNumer > denom == denomPositive )
                return s1;
            return s0 + sNumer / denom * s32;
        }

        public static bool CircumcircleContains(double2 a, double2 b, double2 c, double2 p)
        {
            Assert.IsTrue(Ccw(a, b, c));
            return GeometricPredicates.InCircle(a, b, c, p) > 0;
        }

        public static double2 ProjectLine(double2 v1, double2 v2, double2 p)
        {
            var e1 = v2 - v1;
            var e2 = p - v1;
            var val = math.dot(e1, e2);
            var len2 = math.lengthsq(e1);
            return new double2(v1 + val * e1 / len2);
        }

        public static bool ProjectSeg(double2 v1, double2 v2, double2 p, out float2 result)
        {
            var e1 = v2 - v1;
            var e2 = p - v1;
            var lengthsq = math.lengthsq(e1);
            var fraction = math.dot(e1, e2) / lengthsq;
            result = new float2(v1 + fraction * e1);
            return fraction >= 0 && fraction <= 1;
        }

        public static double ProjectSeg2(double2 v1, double2 v2, double2 p, out double2 result)
        {
            var e1 = v2 - v1;
            var e2 = p - v1;
            var lengthsq = math.lengthsq(e1);
            var fraction = math.dot(e1, e2) / lengthsq;
            result = new float2(v1 + fraction * e1);
            return fraction;
        }

        public static float Square(float f) => f * f;
        public static double Square(double f) => f * f;
        public static float Circumference(float r) => 2 * math.PI * r;

        public static void CircleFromPoints(double2 p1, double2 p2, double2 p3, out double2 centre, out double radius)
        {
            var offset = Square(p2.x) + Square(p2.y);
            var bc = (Square(p1.x) + Square(p1.y) - offset) / 2;
            var cd = (offset - Square(p3.x) - Square(p3.y)) / 2;
            var det = (p1.x - p2.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p2.y);
            Assert.IsTrue(det != 0);
            var idet = 1 / det;
            var centerx = (bc * (p2.y - p3.y) - cd * (p1.y - p2.y)) * idet;
            var centery = (cd * (p1.x - p2.x) - bc * (p2.x - p3.x)) * idet;
            radius = math.sqrt(Square(p2.x - centerx) + Square(p2.y - centery));
            centre = new double2(centerx, centery);
        }

        public static float Angle(float2 v) => math.atan2(v.x, v.y);

        public static float Angle(double2 from, double2 to)
        {
            var sin = to.x * from.y - from.x * to.y;
            var cos = to.x * from.x + to.y * from.y;
            return (float) math.atan2(sin, cos);
        }

        public static float2 Rotate(float length, float angle) => Rotate(angle) * length;
        public static float2 Rotate(float angle) => new(math.sin(angle), math.cos(angle));

        public static float2 Rotate(double2 v, double angleRadians)
        {
            var s = math.sin(-angleRadians);
            var c = math.cos(-angleRadians);
            return (float2) new double2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static Complex GetQ(float r, Complex cp, double d, Complex c)
            => c + r * (cp / (r + Complex.ImaginaryOne * d));

        static double LengthSq(Complex c)
            => c.Real * c.Real + c.Imaginary * c.Imaginary;

        public static int IntersectSegCircle(double2 E, double2 L, double2 C, double r)
        {
            // use epsilon to capture points on circle
            const float epsilon = 1e-6f;
            var d = L - E;
            var f = E - C;
            var a = math.dot(d, d);
            var b = math.dot(2 * f, d);
            var c = math.dot(f, f) - r * r;

            var discriminant = b * b - 4 * a * c;
            if (discriminant >= 0)
            {
                discriminant = math.sqrt(discriminant);

                var t1 = (-b - discriminant) / (2 * a);
                var t2 = (-b + discriminant) / (2 * a);
                if (t1 >= -epsilon && t1 <= 1 + epsilon)
                {
                    if (t2 >= -epsilon && t2 <= 1 + epsilon && discriminant > 0)
                        return 2;
                    return 1;
                }

                if (t2 >= -epsilon && t2 <= 1 + epsilon)
                    return 1;
            }

            return 0;
        }

        public static int IntersectLineCircle(double2 E, double2 L, double2 C, double r, out float2 r1, out float2 r2)
        {
            var d = L - E;
            var f = E - C;
            var a = math.dot(d, d);
            var b = math.dot(2 * f, d);
            var c = math.dot(f, f) - r * r;

            var discriminant = b * b - 4 * a * c;
            if (discriminant == 0)
            {
                discriminant = math.sqrt(discriminant);
                var t1 = (-b - discriminant) / (2 * a);
                r1 = (float2) (E + t1 * d);
                r2 = default;
                return 1;
            }

            if (discriminant > 0)
            {
                discriminant = math.sqrt(discriminant);
                var t1 = (-b - discriminant) / (2 * a);
                var t2 = (-b + discriminant) / (2 * a);
                r1 = (float2) (E + t1 * d);
                r2 = (float2) (E + t2 * d);
                return 2;
            }

            r1 = default;
            r2 = default;
            return 0;
        }

        static float PerpDot(float2 v1, float2 v2) => v1.x * v2.y - v1.y * v2.x;

        public static float2 IntersectLineLine(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            var v1 = p2 - p1;
            var v2 = p4 - p3;

            // Use parametric equation of lines to find intersection point
            var v = p3 - p1;
            var t = PerpDot(v, v2) / PerpDot(v1, v2);

            v1 *= t;
            v1 += p1;
            return v1;
        }

        // todo use orient fast
        // https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
        public static bool TriContains(float2 v1, float2 v2, float2 v3, float2 pt)
        {
            var d1 = Sign(pt, v1, v2);
            var d2 = Sign(pt, v2, v3);
            var d3 = Sign(pt, v3, v1);
            var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            var hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);

            float Sign(float2 p1, float2 p2, float2 p3)
                => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public static float2 PerpCw(float2 vector2) => new(vector2.y, -vector2.x);
        public static float2 PerpCcw(float2 vector2) => new(-vector2.y, vector2.x);

        public static void GetOuterTangentRight(float2 c0, float2 c1, float R, out float2 from, out float2 to)
        {
            var theta = math.atan2(c1.y - c0.y, c1.x - c0.x);
            var sin = R * math.sin(theta);
            var cos = R * math.cos(theta);
            from = new float2(c0.x + sin, c0.y - cos);
            to = new float2(c1.x + sin, c1.y - cos);
        }

        public static void GetOuterTangentLeft(float2 c0, float2 c1, float R, out float2 from, out float2 to)
        {
            var theta = math.atan2(c1.y - c0.y, c1.x - c0.x);
            var sin = R * math.sin(theta);
            var cos = R * math.cos(theta);
            from = new float2(c0.x - sin, c0.y + cos);
            to = new float2(c1.x - sin, c1.y + cos);
        }

        public static void GetInnerTangentRight(float2 c0, float2 c1, float R, out float2 from, out float2 to)
        {
            var p = (c0 + c1) / 2;
            var d = math.lengthsq(c1 - c0);
            var l = math.sqrt(d / 4 - Square(R));
            var a = math.atan2(R, l);
            var c = Rotate(math.normalize(c0 - p) * l, -a);
            from = p + c;
            to = p - c;
        }

        public static void GetInnerTangentLeft(float2 c0, float2 c1, float R, out float2 from, out float2 to)
        {
            var p = (c0 + c1) / 2;
            var d = math.lengthsq(c1 - c0);
            var l = math.sqrt(d / 4 - Square(R));
            var a = math.atan2(R, l);
            var c = Rotate(math.normalize(c0 - p) * l, a);
            from = p + c;
            to = p - c;
        }

        public static float2 GetTangentLeft(float2 point, float2 circle, float r)
        {
            var p = new Complex(point.x, point.y);
            var c = new Complex(circle.x, circle.y);
            var cp = p - c;
            var d = math.sqrt(LengthSq(cp) - Square(r));
            var q = GetQ(r, cp, d, c);
            Assert.IsTrue(!double.IsNaN(q.Real) && !double.IsNaN(q.Imaginary));
            return new float2((float) q.Real, (float) q.Imaginary);
        }

        public static float2 GetTangentRight(float2 point, float2 circle, float r)
        {
            var p = new Complex(point.x, point.y);
            var c = new Complex(circle.x, circle.y);
            var cp = p - c;
            var d = math.sqrt(LengthSq(cp) - Square(r));
            var q = GetQ(r, cp, -d, c);
            Assert.IsTrue(!double.IsNaN(q.Real) && !double.IsNaN(q.Imaginary));
            return new float2((float) q.Real, (float) q.Imaginary);
        }

        public static bool Contains(float2 p, float2 min, float2 max)
            => math.all(p >= min) && math.all(p <= max);

        public static float2 ClosestPointOnLineSegment(double2 p, double2 a, double2 b)
        {
            var ap = p - a;
            var ab = b - a;
            var f = math.dot(ap, ab) / math.lengthsq(ab);
            if (f <= 0)
                return (float2) a;
            if (f >= 1)
                return (float2) b;
            return (float2) (a + ab * f);
        }

        public static float2 ClosestPointOnLine(double2 p, double2 a, double2 b)
        {
            var ap = p - a;
            var ab = b - a;
            var f = math.dot(ap, ab) / math.lengthsq(ab);
            return (float2) (a + ab * f);
        }

        public static bool RectsOverlap(float2 minA, float2 maxA, float2 minB, float2 maxB)
        {
            // One rectangle is on left side of other
            if (minA.x >= maxB.x || minB.x >= maxA.x)
                return false;
            // One rectangle is above other
            if (minA.y >= maxB.y || minB.y >= maxA.y)
                return false;
            return true;
        }

        public static float MoveTowards(float current, float target, float maxDelta)
            => (double) math.abs(target - current) <= (double) maxDelta ? target : current + math.sign(target - current) * maxDelta;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizeWithLength(float3 v, out float3 n)
        {
            float lengthSq = math.lengthsq(v);
            float invLength = math.rsqrt(lengthSq);
            n = v * invLength;
            return lengthSq * invLength;
        }

        // Return two normals perpendicular to the input vector
        public static void CalculatePerpendicularNormalized(float3 v, out float3 p, out float3 q)
        {
            float3 vSquared = v * v;
            float3 lengthsSquared = vSquared + vSquared.xxx; // y = ||j x v||^2, z = ||k x v||^2
            float3 invLengths = math.rsqrt(lengthsSquared);

            // select first direction, j x v or k x v, whichever has greater magnitude
            float3 dir0 = new float3(-v.y, v.x, 0.0f);
            float3 dir1 = new float3(-v.z, 0.0f, v.x);
            bool cmp = (lengthsSquared.y > lengthsSquared.z);
            float3 dir = math.select(dir1, dir0, cmp);

            // normalize and get the other direction
            float invLength = math.select(invLengths.z, invLengths.y, cmp);
            p = dir * invLength;
            float3 cross = math.cross(v, dir);
            q = cross * invLength;
        }

        public static float2 Mul2D(float4x4 a, float2 b) => a.c0.xz * b.x + a.c2.xz * b.y + a.c3.xz;
    }
}