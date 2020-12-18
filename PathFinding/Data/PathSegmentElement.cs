using System;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.PathFinding.Data
{
    /// <summary>
    /// Describes a segment of a path consisting of a line segment preceeded by a circle segment connecting the
    /// previous and current line segment
    /// </summary>
    [InternalBufferCapacity(0), Serializable]
    public struct PathSegmentElement : IBufferElementData
    {
        public bool InitialSegment => math.all(Corner == From);

        /// <summary>
        /// The centre of the circle of which a segment connects the previous segment's To to the current segment's From
        /// </summary>
        public float2 Corner;

        /// <summary>
        /// The starting point of the path segment's line segment
        /// </summary>
        public float2 From;

        /// <summary>
        /// The end point of the path segment's line segment
        /// </summary>
        public float2 To;
    }
}