using DotsNav.Core;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.Navmesh.Data
{
    struct NavmeshDrawComponent : IComponentData
    {
        public DrawMode DrawMode;
        public Color ConstrainedColor;
        public Color UnconstrainedColor;
    }
}