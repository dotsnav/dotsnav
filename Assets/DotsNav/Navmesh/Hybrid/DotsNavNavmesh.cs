using DotsNav.Core.Hybrid;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Navmesh.Hybrid
{
    class NavmeshConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavNavmesh navmesh) =>
            {
                var entity = GetPrimaryEntity(navmesh);
                navmesh.Entity = entity;
                navmesh.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new NavmeshComponent
                (
                    navmesh.Size,
                    navmesh.ExpectedVerts,
                    navmesh.MergePointDistance,
                    navmesh.CollinearMargin
                ));
                DstEntityManager.AddBuffer<DestroyedTriangleElement>(entity);
                DstEntityManager.AddComponentData(entity, new NavmeshDrawComponent
                {
                    DrawMode = navmesh.DrawMode,
                    ConstrainedColor = navmesh.ConstrainedColor,
                    UnconstrainedColor = navmesh.UnconstrainedColor
                });
            });
        }
    }

    /// <summary>
    /// Creates a navmesh on startup and can then be used to insert and destroy obstacles. Destroying this object triggers
    /// the destruction of the navmesh releasing its resources.
    /// </summary>
    [RequireComponent(typeof(DotsNavPlane))]
    public class DotsNavNavmesh : EntityLifetimeBehaviour, IPlaneComponent
    {
        /// <summary>
        /// Size of the navmesh to be created. Changing this value after initialization has no effect
        /// </summary>
        public Vector2 Size = new Vector2(1000, 1000);

        /// <summary>
        /// Determines the size of initial allocations. Changing this value after initialization has no effect
        /// </summary>
        [Min(100)]
        public int ExpectedVerts = 1000;

        /// <summary>
        /// Vertices inserted with this range of an existing vertex are merged instead. Changing this value after initialization has no effect
        /// </summary>
        public float MergePointDistance = 1e-3f;

        /// <summary>
        /// Margin for considering points collinear when removing intersections. Changing this value after initialization has no effect
        /// </summary>
        public float CollinearMargin = 1e-6f;

        [Header("Debug")]
        public DrawMode DrawMode = DrawMode.Constrained;
        public Color ConstrainedColor = Color.red;
        public Color UnconstrainedColor = Color.white;

        /// <summary>
        /// The amount of vertices in the current triangulation
        /// </summary>
        public int Vertices { get; internal set; }

        public bool IsInitialized => Vertices > 7;

        void OnValidate()
        {
            Size = math.abs(Size);
        }

        void IPlaneComponent.InsertObstacle(Entity obstacle, EntityManager em)
        {
            em.AddComponentData(obstacle, new NavmeshObstacleComponent {Navmesh = Entity});
        }

        /// <summary>
        /// Returns the native navmesh which exposes the triangulation. This structure is invalidated each update and
        /// the latest version should be obtained each cycle
        /// </summary>
        public unsafe Navmesh GetNativeNavmesh() => *World.EntityManager.GetComponentData<NavmeshComponent>(Entity).Navmesh;

        /// <summary>
        /// Returns true when point p is contained within the navmesh
        /// </summary>
        public bool Contains(Vector2 p) => Math.Contains(p, -Size / 2, Size / 2);

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            float2 hs = Size / 2;
            var color = ConstrainedColor;
            DrawLine(-hs, hs * new float2(1, -1));
            DrawLine(hs * new float2(1, -1), hs);
            DrawLine(hs, hs * new float2(-1, 1));
            DrawLine(hs * new float2(-1, 1), -hs);
            void DrawLine(float2 a, float2 b) => Debug.DrawLine(a.ToXxY(), b.ToXxY(), color);
        }
    }
}