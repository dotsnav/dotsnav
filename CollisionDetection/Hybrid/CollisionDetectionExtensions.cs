using DotsNav.Navmesh.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DotsNav.CollisionDetection.Hybrid
{
    public static class CollisionDetectionExtensions
    {
        /// <summary>
        /// Casts a ray and reports any collisions found. The entire line segment should be inside the navmesh.
        /// Collisions are reported in order of their distance from the origin of the line segment.
        /// </summary>
        /// <param name="from">The origin of the line segment, needs to be contained in the navmesh</param>
        /// <param name="to">The destination of the line segment, needs to be contained in the navmesh</param>
        /// <param name="collectAllHits">Set to false to stop searching after the first collision</param>
        public static RayCastResult CastSegment(this DotsNavNavmesh navmesh, Vector2 from, Vector2 to, bool collectAllHits = false)
        {
            var output = new NativeList<RayCastHit>(Allocator.Persistent);
            if (navmesh.IsInitialized)
                new SegmentCastJob
                    {
                        Input = new SegmentCast(from, to, collectAllHits, output),
                        Navmesh = navmesh.GetNativeNavmesh(),
                    }
                    .Run();
            return new RayCastResult(output);
        }

        /// <summary>
        /// Casts a ray and reports any collisions found. The resulting line segment's origin should be inside the navmesh. If the destination is outside the navmesh
        /// the line segment is truncated. Collisions are reported in order of their distance from the origin of the line segment.
        /// </summary>
        /// <param name="from">The origin of the line segment, needs to be contained in the navmesh</param>
        /// <param name="direction">The of the ray, needs to be normalized</param>
        /// <param name="distance">The distance of the ray, needs to be zero or larger</param>
        /// <param name="collectAllHits">Set to false to stop searching after the first collision</param>
        public static RayCastResult CastRay(this DotsNavNavmesh navmesh, Vector2 from, Vector2 direction, float distance = float.MaxValue, bool collectAllHits = false)
        {
            var output = new NativeList<RayCastHit>(Allocator.Persistent);
            if (navmesh.IsInitialized)
                new RayCastJob()
                    {
                        Input = new RayCast(from, direction, distance, collectAllHits, output),
                        Navmesh = navmesh.GetNativeNavmesh(),
                    }
                    .Run();
            return new RayCastResult(output);
        }

        /// <summary>
        /// Cast a disc and report any collisions, this includes edges fully contained by the disc. Collisions are reported in no particular order.
        /// </summary>
        /// <param name="centre">Centre of the disc</param>
        /// <param name="radius">Radius of the dics, needs to be zero or larger</param>
        /// <param name="collectAllHits">Set to false to stop searching after the first collision</param>
        /// <returns></returns>
        public static DiscCastResult CastDisc(this DotsNavNavmesh navmesh, Vector2 centre, float radius, bool collectAllHits)
        {
            var output = new NativeList<Entity>(16, Allocator.Persistent);
            if (navmesh.IsInitialized)
                new DiscCastJob
                    {
                        Input = new DiscCast(centre, radius, collectAllHits, output),
                        Navmesh = navmesh.GetNativeNavmesh(),
                    }
                    .Run();
            return new DiscCastResult(output);
        }
    }
}