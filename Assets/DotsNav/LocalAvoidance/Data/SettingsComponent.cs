using Unity.Entities;

namespace DotsNav.LocalAvoidance.Data
{
    public struct SettingsComponent : IComponentData
    {
        public float NeighbourDist;
        public float TimeHorizon;
        public float TimeHorizonObst;
        public int MaxNeighbours;
    }
}