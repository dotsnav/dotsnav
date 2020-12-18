using Unity.Entities;
using UnityEngine;

namespace DotsNav.PathFinding.Data
{
    struct AgentDrawComponent : IComponentData
    {
        public bool Draw;
        public bool Delimit;
        public Color Color;
    }
}