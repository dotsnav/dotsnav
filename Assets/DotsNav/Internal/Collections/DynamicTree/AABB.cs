using Unity.Mathematics;

namespace DotsNav.Collections
{
    public struct AABB
    {
        // Verify that the bounds are sorted.
        bool IsValid()
        {
            var d = UpperBound - LowerBound;
            var valid = d.x >= 0.0f && d.y >= 0.0f;
            valid = valid && math.all(math.isfinite(LowerBound)) && math.all(math.isfinite(UpperBound));
            return valid;
        }

        // Get the center of the AABB.
        internal float2 GetCenter() => 0.5f * (LowerBound + UpperBound);

        // Get the extents of the AABB (half-widths).
        internal float2 GetExtents() => 0.5f * (UpperBound - LowerBound);

        // Get the perimeter length
        internal float GetPerimeter()
        {
            var wx = UpperBound.x - LowerBound.x;
            var wy = UpperBound.y - LowerBound.y;
            return 2.0f * (wx + wy);
        }

        // Combine an AABB into this one.
        void Combine(AABB aabb)
        {
            LowerBound = math.min(LowerBound, aabb.LowerBound);
            UpperBound = math.max(UpperBound, aabb.UpperBound);
        }

        // Combine two AABBs into this one.
        internal void Combine(AABB aabb1, AABB aabb2)
        {
            LowerBound = math.min(aabb1.LowerBound, aabb2.LowerBound);
            UpperBound = math.max(aabb1.UpperBound, aabb2.UpperBound);
        }

        // Does this aabb contain the provided AABB.
        internal bool Contains(AABB aabb)
        {
            var result = true;
            result = result && LowerBound.x <= aabb.LowerBound.x;
            result = result && LowerBound.y <= aabb.LowerBound.y;
            result = result && aabb.UpperBound.x <= UpperBound.x;
            result = result && aabb.UpperBound.y <= UpperBound.y;
            return result;
        }

        internal float2 LowerBound;

        //< the lower vertex
        internal float2 UpperBound; //< the upper vertex
    }
}