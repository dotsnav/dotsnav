using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    class NavmeshHybridWriteSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((Hybrid.DotsNavNavmesh hybrid, Navmesh.Navmesh navmesh) =>
                {
                    hybrid.Vertices = navmesh.Vertices;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(hybrid);
#endif
                })
                .Run();
        }
    }
}