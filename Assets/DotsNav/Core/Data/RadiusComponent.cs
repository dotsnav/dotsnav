using Unity.Entities;

namespace DotsNav.Core
{
    public struct RadiusComponent : IComponentData
    {
        public float Value;

        public RadiusComponent(float f)
        {
            Value = f;
        }

        public static implicit operator float(RadiusComponent e) => e.Value;
        public static implicit operator RadiusComponent(float v) => new RadiusComponent(v);
    }
}