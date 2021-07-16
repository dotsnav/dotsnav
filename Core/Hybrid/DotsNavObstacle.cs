using System.Linq;
using DotsNav.Data;
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

                var values = DstEntityManager.AddBuffer<VertexElement>(entity);

                for (int i = 0; i < obstacle.Vertices.Length; i++)
                    values.Add((float2) obstacle.Vertices[i]);

                if (obstacle.Closed)
                    values.Add((float2) obstacle.Vertices[0]);
            });
        }
    }

    public class DotsNavObstacle : EntityLifetimeBehaviour
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

        [Header("Debug")]
        public bool DrawGizmos = true;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Application.isPlaying || !DrawGizmos || Selection.gameObjects.Contains(gameObject))
                return;

            var t = Handles.color;
            Handles.color = DotsNavPrefs.GizmoColor;

            for (int i = 0; i < Vertices.Length - 1; i++)
            {
                var a = math.transform(transform.localToWorldMatrix, Vertices[i].ToXxY());
                var b = math.transform(transform.localToWorldMatrix, Vertices[i + 1].ToXxY());
                Handles.DrawLine(a, b);
            }

            if (Closed)
                Handles.DrawLine(GetVertexWorldSpace(Vertices.Length - 1), GetVertexWorldSpace(0));
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
        public float3 GetVertexWorldSpace(int i) => math.transform(transform.localToWorldMatrix, Vertices[i].ToXxY());
    }
}