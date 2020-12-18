using Unity.Entities;
using UnityEngine;

namespace DotsNav.Data
{
    struct NavmeshDrawComponent : IComponentData
    {
        public DrawMode DrawMode;
        public Color ConstrainedColor;
        public Color UnconstrainedColor;
    }
}