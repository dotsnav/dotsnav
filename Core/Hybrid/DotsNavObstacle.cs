using System.Linq;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DotsNav.Hybrid
{
    class ObstacleConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavObstacle obstacle) =>
            {
                var entity = GetPrimaryEntity(obstacle);
                obstacle.Entity = entity;
                obstacle.World = DstEntityManager.World;
            });
        }
    }

    public class DotsNavObstacle : EntityLifetimeBehaviour
    {
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