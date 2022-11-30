using Unity.Mathematics;
using System;
using DotsNav.Navmesh;
using Unity.Collections;

namespace DotsNav.CollisionDetection
{
    /// <summary>
    /// Implement IDiscCast and use Navmesh.Intersect to perform a disc cast
    /// </summary>
    public unsafe interface IDiscCast
    {
        float2 Origin { get; }
        float Radius { get; }

        /// <summary>
        /// This method is invoked when a collision is detected. Collisions are reported in no particular order.
        /// </summary>
        /// <param name="edge">The edge collided with</param>
        /// <returns>Return true to continue searching for collisions</returns>
        bool RegisterCollision(Edge* edge);
    }

    public static unsafe partial class CollisionDetectionExtensions
    {
        /// <summary>
        /// Reports all edges intersecting specified disc while IDiscCast.RegisterCollision returns true.
        /// A NativeQueue and NativeHashSet will be created with the specified allocator, and disposed before returning.
        /// </summary>
        /// <param name="discCast">Cast data</param>
        /// <param name="allocator">Allocator used to create internal buffers</param>
        public static void CastDisc<T>(this Navmesh.Navmesh navmesh, T discCast, Allocator allocator) where T : IDiscCast
        {
            var open = new NativeList<IntPtr>(allocator);
            var closed = new NativeParallelHashSet<int>(32, allocator);
            navmesh.CastDisc(discCast, open, closed);
            open.Dispose();
            closed.Dispose();
        }

        /// <summary>
        /// Reports all edges intersecting disc while IDiscCast.RegisterCollision returns true, this includes fully contained edges.
        /// </summary>
        /// <param name="discCast">Cast data</param>
        /// <param name="open">NativeQueue for internal use, is cleared before use</param>
        /// <param name="closed">NativeHashSet for internal use, is cleared before use</param>
        public static void CastDisc<T>(this Navmesh.Navmesh navmesh, T discCast, NativeList<IntPtr> open, NativeParallelHashSet<int> closed) where T : IDiscCast
        {
            var o = discCast.Origin;
            var r = discCast.Radius;
            var tri = navmesh.FindTriangleContainingPoint(o);

            open.Clear();
            closed.Clear();

            Check(tri);
            Check(tri->LNext);
            Check(tri->LPrev);

            while (open.Length > 0)
            {
                tri = (Edge*) open[^1];
                open.Resize(open.Length - 1, NativeArrayOptions.UninitializedMemory);
                Check(tri->LNext);
                Check(tri->LPrev);
            }

            void Check(Edge* edge)
            {
                if (closed.Contains(edge->QuadEdgeId))
                    return;

                if (IntersectSegDisc(edge->Org->Point, edge->Dest->Point, o, r))
                {
                    open.Add((IntPtr) edge->Sym);
                    if (edge->Constrained)
                        discCast.RegisterCollision(edge);
                }

                closed.Add(edge->QuadEdgeId);
            }
        }

        static bool IntersectSegDisc(double2 p0, double2 p1, double2 centre, double radius)
        {
            var d = p1 - p0;
            var f = p0 - centre;
            var a = math.dot(d, d);
            var b = math.dot(2 * f, d);
            var c = math.dot(f, f) - radius * radius;
            var discriminant = b * b - 4 * a * c;

            if (discriminant >= 0)
            {
                discriminant = math.sqrt(discriminant);

                var t1 = (-b - discriminant) / (2 * a);
                var t2 = (-b + discriminant) / (2 * a);

                if (t1 >= 0 && t1 <= 1)
                    return true;

                if (t2 >= 0 && t2 <= 1)
                    return true;

                if (t1 < 0 && t2 > 1)
                    return true;
            }

            return false;
        }
    }
}