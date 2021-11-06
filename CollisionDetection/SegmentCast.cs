using Unity.Mathematics;
using System;
using DotsNav.Navmesh;
using Unity.Collections;
using Unity.Assertions;

namespace DotsNav.CollisionDetection
{
    /// <summary>
    /// Implement ISegmentCast and use Navmesh.CastSegment to perform a segment cast
    /// </summary>
    public unsafe interface ISegmentCast
    {
        float2 Origin { get; }
        float2 Destination { get; }

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
        /// Reports all edges intersecting specified line segment while ISegmentCast.RegisterCollision returns true.
        /// Collisions are reported in order of their distance from segmentCast.Origin.
        /// </summary>
        /// <param name="segmentCast">Cast data</param>
        /// <param name="allocator">Allocator used to create internal buffers</param>
        public static void CastSegment<T>(this Navmesh.Navmesh navmesh, T segmentCast, Allocator allocator) where T : ISegmentCast
        {
            var open = new NativeList<IntPtr>(allocator);
            navmesh.CastSegment(segmentCast, open);
            open.Dispose();
        }

        /// <summary>
        /// Reports all edges intersecting specified line segment while ISegmentCast.RegisterCollision returns true.
        /// Collisions are reported in order of their distance from ISegmentCast.Origin.
        /// </summary>
        /// <param name="segmentCast">Cast data</param>
        /// <param name="open">NativeList for internal use, is cleared before use</param>
        public static void CastSegment<T>(this Navmesh.Navmesh navmesh, T segmentCast, NativeList<IntPtr> open) where T : ISegmentCast
        {
            var o = segmentCast.Origin;
            var d = segmentCast.Destination;
            var tri = navmesh.FindTriangleContainingPoint(o, out var startCollinear);
            var goalEdge = navmesh.FindTriangleContainingPoint(d, out var goalCollinear);

            if (ReachedGoal(tri) || startCollinear && ReachedGoal(tri->Sym))
            {
                RegisterCollinearGoal();
                return;
            }

            open.Clear();

            if (math.all(o == tri->Org->Point))
            {
                if (ExpandVertex(tri->Org))
                {
                    RegisterCollinearGoal();
                    return;
                }
            }
            else if (math.all(o == tri->Dest->Point))
            {
                if (ExpandVertex(tri->Dest))
                {
                    RegisterCollinearGoal();
                    return;
                }
            }
            else if (startCollinear)
            {
                open.Add((IntPtr) tri->LNext);
                open.Add((IntPtr) tri->LPrev);
                open.Add((IntPtr) tri->Sym->LNext);
                open.Add((IntPtr) tri->Sym->LPrev);
            }
            else
            {
                open.Add((IntPtr) tri);
                open.Add((IntPtr) tri->LNext);
                open.Add((IntPtr) tri->LPrev);
            }

            do
            {
                var found = false;

                for (int i = 0; i < open.Length; i++)
                {
                    var e = (Edge*) open[i];
                    if (IntersectSegSeg(o, d, e->Org->Point, e->Dest->Point, out var p))
                    {
                        if (e->Constrained)
                            if (!segmentCast.RegisterCollision(p, e))
                                return;
                        tri = e->Sym;

                        open.Clear();

                        if (math.all(p == tri->Org->Point))
                        {
                            o = p;
                            if (ExpandVertex(tri->Org))
                            {
                                RegisterCollinearGoal();
                                return;
                            }
                        }
                        else if (math.all(p == tri->Dest->Point))
                        {
                            o = p;
                            if (ExpandVertex(tri->Dest))
                            {
                                RegisterCollinearGoal();
                                return;
                            }
                        }
                        else
                        {
                            open.Add((IntPtr) tri->LNext);
                            open.Add((IntPtr) tri->LPrev);
                        }

                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found);
            } while (!ReachedGoal(tri));

            RegisterCollinearGoal();

            bool ExpandVertex(Vertex* vertex)
            {
                var enumerator = vertex->GetEdgeEnumerator();
                while (enumerator.MoveNext())
                {
                    if (ReachedGoal(enumerator.Current))
                        return true;
                    open.Add((IntPtr) enumerator.Current->LNext);
                }

                return false;
            }

            bool ReachedGoal(Edge* edge) => edge->TriangleId == goalEdge->TriangleId || goalCollinear && edge->TriangleId == goalEdge->Sym->TriangleId;

            void RegisterCollinearGoal()
            {
                if (goalCollinear)
                    segmentCast.RegisterCollision(segmentCast.Destination, goalEdge);
            }
        }

        static bool IntersectSegSeg(double2 p0, double2 p1, double2 p2, double2 p3, out float2 result)
        {
            var s10 = p1 - p0;
            var s32 = p3 - p2;
            var denom = s10.x * s32.y - s32.x * s10.y;

            if (denom == 0) // Collinear
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
    }
}