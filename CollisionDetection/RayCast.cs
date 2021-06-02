using Unity.Mathematics;
using System;
using DotsNav.Navmesh;
using Unity.Collections;
using Unity.Assertions;

namespace DotsNav.CollisionDetection
{
    /// <summary>
    /// Implement IRayCast and use Navmesh.CastRay to perform a ray cast. If the resulting line segment's destination
    /// falls outside the navmesh the line segment is truncated
    /// </summary>
    public unsafe interface IRayCast
    {
        float2 Origin { get; }
        float2 Direction { get; }
        float Distance { get; }

        /// <summary>
        /// This method is invoked when a collision is detected. Collisions are reported in order of their distance from Origin.
        /// </summary>
        /// <param name="position">Location of the collision</param>
        /// <param name="edge">The edge collided with</param>
        /// <returns>Return true to continue searching for collisions</returns>
        bool RegisterCollision(float2 position, Edge* edge);
    }

    public static unsafe partial class CollisionDetectionExtensions
    {
        /// <summary>
        /// Reports all edges intersecting specified ray while ISegmentCast.RegisterCollision returns true.
        /// Collisions are reported in order of their distance from rayCast.Origin.
        /// </summary>
        /// <param name="segmentCast">Cast data</param>
        /// <param name="allocator">Allocator used to create internal buffers</param>
        public static void CastRay<T>(this Navmesh.Navmesh navmesh, T rayCast, Allocator allocator) where T : IRayCast
        {
            var open = new NativeList<IntPtr>(allocator);
            navmesh.CastRay(rayCast, open);
            open.Dispose();
        }

        /// <summary>
        /// Reports all edges intersecting specified ray while ISegmentCast.RegisterCollision returns true.
        /// Collisions are reported in order of their distance from rayCast.Origin.
        /// </summary>
        /// <param name="segmentCast">Cast data</param>
        /// <param name="open">NativeList for internal use, is cleared before use</param>
        public static void CastRay<T>(this Navmesh.Navmesh navmesh, T rayCast, NativeList<IntPtr> open) where T : IRayCast
        {
            Assert.IsTrue(rayCast.Distance >= 0, "Ray cast distance should be larger than or equal to zero");
            Assert.IsTrue(math.abs(1 - math.length(rayCast.Direction)) < .001f, "Ray cast direction should be normalized");

            open.Clear();
            var h = navmesh.Max;
            var dist = math.min(math.max(h.x, h.y) * 4, rayCast.Distance);
            var org = rayCast.Origin;
            var dest = org + rayCast.Direction * dist;

            if (!navmesh.Contains(dest))
            {
                var d = dest;
                if (!IntersectSegSeg(org, d, -h, new double2(h.x, -h.y), out dest)
                    && !IntersectSegSeg(org, d, new double2(h.x, -h.y), h, out dest)
                    && !IntersectSegSeg(org, d, h, new double2(-h.x, h.y), out dest))
                    IntersectSegSeg(org, d, new double2(-h.x, h.y), -h, out dest);
            }

            var segmentCast = new RayCastWrapper<T>(dest, rayCast);
            navmesh.CastSegment(segmentCast, open);
        }

        struct RayCastWrapper<T> : ISegmentCast where T : IRayCast
        {
            public float2 Origin => _rayCast.Origin;
            public float2 Destination { get; }
            readonly T _rayCast;

            public RayCastWrapper(float2 dest, T rayCast)
            {
                Destination = dest;
                _rayCast = rayCast;
            }

            public bool RegisterCollision(float2 position, Edge* edge) => _rayCast.RegisterCollision(position, edge);
        }
    }
}