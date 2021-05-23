using Unity.Mathematics;

namespace DotsNav
{
    unsafe struct Disturbance
    {
        public Edge* Edge;
        public Vertex* Vertex;
        public float2 PRef;

        public Disturbance(Vertex* vertex, float2 pRef, Edge* edge)
        {
            Edge = edge;
            Vertex = vertex;
            PRef = pRef;
        }
    }
}