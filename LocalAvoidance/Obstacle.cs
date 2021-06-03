using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    public unsafe struct Obstacle
    {
        internal int Id;
        internal Obstacle* Next;
        internal Obstacle* Previous;
        internal float2 Direction;
        internal float2 Point;
        internal bool Convex;

        public override string ToString() => $"Obst: {Point} => {Direction}";
    }
}