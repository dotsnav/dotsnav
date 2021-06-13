using System;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.Navmesh.Data
{
    /// <summary>
    /// Create to trigger creation of a navmesh. Destroy to trigger destruction of a navmesh.
    /// </summary>
    public unsafe struct NavmeshComponent : IComponentData, IEquatable<NavmeshComponent>
    {
        /// <summary>
        /// Size of the navmesh to be created. The navmesh will be centered around the origin
        /// </summary>
        public readonly float2 Size;

        /// <summary>
        /// Vertices inserted with this range of an existing vertex are merged instead
        /// </summary>
        public readonly float MergePointsDistance;

        /// <summary>
        /// Margin for considering points collinear when removing intersections
        /// </summary>
        public readonly float CollinearMargin;

        /// <summary>
        /// Used to determine the sizes of initial allocations
        /// </summary>
        public readonly int ExpectedVerts;

        public NavmeshComponent(float2 size, int expectedVerts, float mergePointsDistance = 1e-3f, float collinearMargin = 1e-6f)
        {
            Size = size;
            ExpectedVerts = expectedVerts;
            MergePointsDistance = mergePointsDistance;
            CollinearMargin = collinearMargin;
            Navmesh = default;
        }

        // todo make non public
        public Navmesh* Navmesh;

        public bool Equals(NavmeshComponent other) => Navmesh == other.Navmesh;
    }
}