using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using DotsNav;

[ExecuteInEditMode]
public class CircleSegmentIntersection : MonoBehaviour
{
    [Range(0, 2f * math.PI)] public float StartRot;

    [Range(0, 2f * math.PI)] public float Rot;

    public float R = 2.345f;

    public Transform A;
    public Transform B;
    public Transform C;

    void Update()
    {
        var p1 = ((float3) A.position).xz;
        var p2 = ((float3) B.position).xz;
        var p3 = ((float3) C.position).xz;

        DebugUtil.DrawLine(p1.ToXxY(), p2.ToXxY());
        DebugUtil.DrawCircle(p3, R, StartRot, Rot);

        var hits = new NativeList<float2>(Allocator.Persistent);
        IntersectSegCircleSeg(p1, p2, p3, R, NormalizeAngle(StartRot), NormalizeAngle(StartRot + Rot), hits);

        foreach (var hit in hits)
            DebugUtil.DrawPoint(hit, Color.magenta);

        hits.Dispose();
    }

    public static float NormalizeAngle(float a)
    {
        while (a > math.PI) a -= 2 * math.PI;
        while (a < -math.PI) a += 2 * math.PI;
        return a;
    }

    public static bool IntersectSegCircleSeg(float2 p1, float2 p2, float2 p3, float r, float start, float end)
    {
        Assert.IsTrue(start >= -math.PI && start <= math.PI);
        Assert.IsTrue(end >= -math.PI && end <= math.PI);

        if (Math.IntersectSegSeg(p1, p2, p3, p3 + Rotate(r, start)))
            return true;
        if (Math.IntersectSegSeg(p1, p2, p3, p3 + Rotate(r, end)))
            return true;

        switch (IntersectSegCircle(p1, p2, p3, r, out var e, out var f))
        {
            case 1:
                var a = Math.Angle(e - p3);
                if (SectorContains(start, end, a))
                    return true;
                break;
            case 2:
                if (SectorContains(start, end, Math.Angle(e - p3)))
                    return true;
                if (SectorContains(start, end, Math.Angle(f - p3)))
                    return true;
                break;
        }

        return false;
    }

    public static bool IntersectSegCircleSeg(float2 p1, float2 p2, float2 p3, float r, float start, float end,
        NativeList<float2> hits)
    {
        Assert.IsTrue(start >= -math.PI && start <= math.PI);
        Assert.IsTrue(end >= -math.PI && end <= math.PI);

        hits.Clear();

        if (Math.IntersectSegSeg(p1, p2, p3, p3 + Rotate(r, start), out var t1))
            hits.Add(t1);
        if (Math.IntersectSegSeg(p1, p2, p3, p3 + Rotate(r, end), out var t2))
            hits.Add(t2);

        switch (IntersectSegCircle(p1, p2, p3, r, out var e, out var f))
        {
            case 1:
                var a = Math.Angle(e - p3);
                if (SectorContains(start, end, a))
                    hits.Add(e);
                break;
            case 2:
                if (SectorContains(start, end, Math.Angle(e - p3)))
                    hits.Add(e);
                if (SectorContains(start, end, Math.Angle(f - p3)))
                    hits.Add(f);
                break;
        }

        return hits.Length > 0;
    }

    static bool SectorContains(float start, float end, float a)
        => start < end ? a >= start && a <= end : !(a > end && a < start);

    static float2 Rotate(float length, float angle)
        => new float2(length * math.sin(angle), length * math.cos(angle));

    // https://stackoverflow.com/questions/1073336/circle-line-segment-collision-detection-algorithm/1084899#1084899
    public static int IntersectSegCircle(float2 E, float2 L, float2 C, float r, out float2 r1, out float2 r2)
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

            // either solution may be on or off the ray so need to test both
            // t1 is always the smaller value, because BOTH discriminant and
            // a are nonnegative.
            var t1 = (-b - discriminant) / (2 * a);
            var t2 = (-b + discriminant) / (2 * a);

            // 3x HIT cases:
            //          -o->             --|-->  |            |  --|->
            // Impale(t1 hit,t2 hit), Poke(t1 hit,t2>1), ExitWound(t1<0, t2 hit),

            // 3x MISS cases:
            //       ->  o                     o ->              | -> |
            // FallShort (t1>1,t2>1), Past (t1<0,t2<0), CompletelyInside(t1<0, t2>1)

            if (t1 >= -epsilon && t1 <= 1 + epsilon)
            {
                // t1 is the intersection, and it's closer than t2
                // (since t1 uses -b - discriminant)

                // Poke
                r1 = E + t1 * d;

                if (t2 >= -epsilon && t2 <= 1 + epsilon && discriminant > 0)
                {
                    // Impale
                    r2 = E + t2 * d;
                    return 2;
                }

                r2 = default;
                return 1;
            }

            // here t1 didn't intersect so we are either started
            // inside the sphere or completely past it
            if (t2 >= -epsilon && t2 <= 1 + epsilon)
            {
                // ExitWound
                r1 = E + t2 * d;
                r2 = default;
                return 1;
            }
        }

        r1 = default;
        r2 = default;
        return 0;
    }
}