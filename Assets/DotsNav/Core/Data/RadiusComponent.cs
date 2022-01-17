using Unity.Entities;

namespace DotsNav.Data
{
    public struct RadiusComponent : IComponentData
    {
        public float Value;
        public int Priority;
    }
}