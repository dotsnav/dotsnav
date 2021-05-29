using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    [InternalBufferCapacity(0)]
    struct Obstacle : IBufferElementData
    {
        internal int Next;
        internal int Previous;
        internal float2 Direction;
        internal float2 Point;
        internal int Id;
        internal bool Convex;

        public static bool operator ==(Obstacle a, Obstacle b) => a.Id == b.Id;
        public static bool operator !=(Obstacle a, Obstacle b) => !(a == b);

        public override string ToString() => $"Obst: {Point} => {Direction}";
    }
}