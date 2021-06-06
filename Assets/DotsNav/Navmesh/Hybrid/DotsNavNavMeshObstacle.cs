using System.Linq;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Navmesh.Editor;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DotsNav.Navmesh.Hybrid
{
    [UpdateAfter(typeof(NavmeshConversionSystem))]
    class NavMeshObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavNavMeshObstacle obstacle) =>
            {
                var entity = GetPrimaryEntity(obstacle);
                DstEntityManager.AddComponentData(entity, new ObstacleComponent());
                var values = DstEntityManager.AddBuffer<VertexElement>(entity);

                for (int i = 0; i < obstacle.Vertices.Length; i++)
                    values.Add((float2)obstacle.Vertices[i]);

                if (obstacle.Closed)
                    values.Add((float2)obstacle.Vertices[0]);
            });
        }
    }

    /// <summary>
    /// Create to triggers insertion of a navmesh obstacle. Destroy to trigger removal of a navmesh obstacle.
    /// </summary>
    [RequireComponent(typeof(DotsNavObstacle))]
    public class DotsNavNavMeshObstacle : MonoBehaviour
    {
        [SerializeField]
        bool Close = true;

        /// <summary>
        /// Indicates wether this obstacle should be closed by duplicating the first vertex as the last vertex
        /// </summary>
        public bool Closed => Close && Vertices.Length > 2;

        /// <summary>
        /// The vertices of this obstacle in object space
        /// </summary>
        public Vector2[] Vertices;

        internal float2 Scale => transform.lossyScale.xz();
        internal float2 Offset => transform.position.xz();
        internal Angle Rotation => math.radians(transform.rotation.eulerAngles.y);

        /// <summary>
        /// Gets a vertex in world space
        /// </summary>
        internal float2 GetVertex(int index) => GetVertex(index, Scale, Rotation, Offset);

        /// <summary>
        /// Gets a vertex in world space
        /// </summary>
        internal float2 GetVertex(int index, float2 scale, float rotation, float2 offset) => offset + Rotate(scale * (float2)Vertices[index], -rotation);

        static float2 Rotate(double2 p, double degrees) => (float2) new double2(p.x * math.cos(degrees) - p.y * math.sin(degrees), p.x * math.sin(degrees) + p.y * math.cos(degrees));

        /// <summary>
        /// Draw this obstacle through the DebugDisplay
        /// </summary>
        [Header("Debug")]
        public bool DrawGizmos = true;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Application.isPlaying || !DrawGizmos || Selection.gameObjects.Contains(gameObject))
                return;

            var t = Handles.color;
            Handles.color = DotsNavPrefs.GizmoColor;
            var scale = Scale;
            var rot = Rotation;
            var offset = Offset;
            for (int i = 0; i < Vertices.Length - 1; i++)
                Handles.DrawLine(GetVertex(i, scale, rot, offset).ToXxY(), GetVertex(i + 1, scale, rot, offset).ToXxY());
            if (Closed)
                Handles.DrawLine(GetVertex(Vertices.Length - 1, scale, rot, offset).ToXxY(), GetVertex(0, scale, rot, offset).ToXxY());
            Handles.color = t;
        }

        void OnValidate()
        {
            if (Vertices == null || Vertices.Length == 0)
                Vertices = new[] {new Vector2(-1, 0), new Vector2(1, 0)};
            else if (Vertices.Length == 1)
                Vertices = new[] {Vertices[0], new Vector2(1, 0)};
        }
#endif
    }
}