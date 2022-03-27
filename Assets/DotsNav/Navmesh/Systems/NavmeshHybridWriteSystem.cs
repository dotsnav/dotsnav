using DotsNav.Navmesh.Data;
using DotsNav.Systems;
using Unity.Entities;

namespace DotsNav.Navmesh.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
    partial class NavmeshHybridWriteSystem : SystemBase
    {
        protected override unsafe void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((Hybrid.DotsNavNavmesh hybrid, NavmeshComponent navmesh) =>
                {
                    hybrid.Vertices = navmesh.Navmesh->Vertices;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(hybrid);
#endif
                })
                .Run();
        }
    }
}