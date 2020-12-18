#if UNITY_EDITOR
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DotsNav.Hybrid
{
    [CustomEditor(typeof(DotsNavObstacle), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    class ObstacleEditor : Editor
    {
        const int Width = 50;
        int _vertexIndex;
        bool _initialized;
        bool _pivot;
        static Material _lineMat;

        static bool Key => Event.current.type == EventType.KeyDown;
        static bool KeyUp => Key && Event.current.keyCode == KeyCode.M;
        static bool KeyDown => Key && Event.current.keyCode == KeyCode.N;

        void OnSceneGUI()
        {
            if (Application.isPlaying)
                return;

            if (!_initialized)
            {
                // todo intercept keys here? this breaks arrow keys in hierarchy
                // SceneView.lastActiveSceneView.Focus();
                _initialized = true;
            }

            var obstacle = (DotsNavObstacle)target;
            var scale = obstacle.Scale;
            var rot = obstacle.Rotation;
            var offset = obstacle.Offset;

            var cam = SceneView.lastActiveSceneView.camera;
            if (_lineMat == null)
                _lineMat = new Material(Shader.Find("Lines/Colored Blended"));
            _lineMat.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);

            for (var i = 0; i < obstacle.Vertices.Length; i++)
            {
                var c = Color.Lerp(DotsNavPrefs.EditColor, DotsNavPrefs.FadeColor, i * (.8f / (obstacle.Vertices.Length - 1)));
                GL.Color(c);
                var vertex = obstacle.GetVertex(i, scale, rot, offset);
                var screenPoint = (float3)cam.WorldToScreenPoint(vertex.ToXxY());
                DrawPoint(screenPoint.xy);
            }

            GL.End();
            GL.PopMatrix();


            Handles.BeginGUI();

            _pivot = GUILayout.Toggle(_pivot, "Pivot");
            if (_pivot)
            {
                if (GUILayout.Button("P ↔", GUILayout.Width(Width)))
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    var min = new float2(float.MaxValue);
                    var max = new float2(float.MinValue);
                    foreach (var vertex in obstacle.Vertices)
                    {
                        min = math.min(min, vertex);
                        max = math.max(max, vertex);
                    }

                    Vector2 p = min + (max - min) / 2;
                    for (var i = 0; i < obstacle.Vertices.Length; i++)
                        obstacle.Vertices[i] -= p;
                }
                if (GUILayout.Button("P ↙", GUILayout.Width(Width)))
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    Vector2 p = new float2(float.MaxValue);
                    foreach (var vertex in obstacle.Vertices)
                        p = math.min(p, vertex);
                    for (var i = 0; i < obstacle.Vertices.Length; i++)
                        obstacle.Vertices[i] -= p;
                }
                if (GUILayout.Button("P ↖", GUILayout.Width(Width)))
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    Vector2 p = new float2(float.MaxValue, float.MinValue);
                    foreach (var vertex in obstacle.Vertices)
                        p = new float2(math.min(p.x, vertex.x), math.max(p.y, vertex.y));
                    for (var i = 0; i < obstacle.Vertices.Length; i++)
                        obstacle.Vertices[i] -= p;
                }
                if (GUILayout.Button("P ↗", GUILayout.Width(Width)))
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    Vector2 p = new float2(float.MinValue);
                    foreach (var vertex in obstacle.Vertices)
                        p = math.max(p, vertex);
                    for (var i = 0; i < obstacle.Vertices.Length; i++)
                        obstacle.Vertices[i] -= p;
                }
                if (GUILayout.Button("P ↘", GUILayout.Width(Width)))
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    Vector2 p = new float2(float.MinValue, float.MaxValue);
                    foreach (var vertex in obstacle.Vertices)
                        p = new float2(math.max(p.x, vertex.x), math.min(p.y, vertex.y));
                    for (var i = 0; i < obstacle.Vertices.Length; i++)
                        obstacle.Vertices[i] -= p;
                }

                {
                    var min = new float2(float.MaxValue);
                    var max = new float2(float.MinValue);
                    foreach (var vertex in obstacle.Vertices)
                    {
                        min = math.min(min, vertex);
                        max = math.max(max, vertex);
                    }

                    var p = offset + scale * Rotate(min + (max - min) / 2, -rot);
                    Handles.EndGUI();

                    EditorGUI.BeginChangeCheck();
                    var newTargetPosition = Handles.PositionHandle(p.ToXxY(), quaternion.RotateY(rot)).xz();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(obstacle, "Edit obstacle");
                        for (int i = 0; i < obstacle.Vertices.Length; i++)
                            obstacle.Vertices[i] += (Vector2)Rotate((newTargetPosition - p) / scale, rot);
                    }
                }
            }
            else
            {
                GUILayout.Label($"{_vertexIndex}/{obstacle.Vertices.Length - 1}");
                if (GUILayout.Button("↑ (M)", GUILayout.Width(Width)) || KeyUp)
                    if (++_vertexIndex >= obstacle.Vertices.Length)
                        _vertexIndex = 0;

                if (GUILayout.Button("↓ (N)", GUILayout.Width(Width)) || KeyDown)
                    if (--_vertexIndex == -1)
                        _vertexIndex = math.max(0, obstacle.Vertices.Length - 1);

                if (GUILayout.Button("+ ↑", GUILayout.Width(Width))) CreateUp();
                if (GUILayout.Button("+ ↓", GUILayout.Width(Width))) CreateDown();
                if (GUILayout.Button(" X", GUILayout.Width(Width)) && obstacle.Vertices.Length > 2) Destroy();
                Handles.EndGUI();

                EditorGUI.BeginChangeCheck();
                var position = GetPos(_vertexIndex);
                var newTargetPosition = Handles.PositionHandle(position, quaternion.RotateY(rot)).xz();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    obstacle.Vertices[_vertexIndex] = Rotate((newTargetPosition - offset) / scale, rot);
                }
            }

            float2 Rotate(double2 p, double degrees) => (float2) new double2(p.x * math.cos(degrees) - p.y * math.sin(degrees), p.x * math.sin(degrees) + p.y * math.cos(degrees));

            if (!obstacle.gameObject.activeInHierarchy || obstacle.Vertices == null || obstacle.Vertices.Length < 2)
                return;

            var t = Handles.color;
            Handles.color = DotsNavPrefs.EditColor;
            for (int i = 0; i < obstacle.Vertices.Length - 1; i++)
                Handles.DrawLine(GetPos(i), GetPos(i + 1));
            if (obstacle.Closed)
                Handles.DrawLine(GetPos(obstacle.Vertices.Length - 1), GetPos(0));
            Handles.color = t;

            float3 GetPos(int i) => obstacle.GetVertex(i, scale, rot, offset).ToXxY();

            void CreateUp()
            {
                Undo.RecordObject(obstacle, "Edit obstacle");
                var l = obstacle.Vertices.ToList();

                if (obstacle.Vertices.Length == 2 && _vertexIndex == 1)
                {
                    l.Add(l[1]);
                    ++_vertexIndex;
                }
                else
                {
                    l.Insert(_vertexIndex, l[_vertexIndex]);
                    ++_vertexIndex;
                }

                obstacle.Vertices = l.ToArray();
            }

            void CreateDown()
            {
                Undo.RecordObject(obstacle, "Edit obstacle");
                var l = obstacle.Vertices.ToList();

                if (obstacle.Vertices.Length == 2 && _vertexIndex == 0)
                    l.Insert(0, l[0]);
                else
                    l.Insert(_vertexIndex, l[_vertexIndex]);

                obstacle.Vertices = l.ToArray();
            }

            void Destroy()
            {
                if (obstacle.Vertices.Length > 0)
                {
                    Undo.RecordObject(obstacle, "Edit obstacle");
                    var l = obstacle.Vertices.ToList();
                    l.RemoveAt(_vertexIndex);
                    obstacle.Vertices = l.ToArray();
                    if (_vertexIndex == obstacle.Vertices.Length)
                        _vertexIndex = 0;
                }
            }
        }

        void DrawPoint(float2 pos)
        {
            const int size = 8;
            const float diag = size / 2.8284f;
            const int hor = size / 2;
            DrawLine(pos + new float2(-diag, -diag), pos + new float2(diag, diag));
            DrawLine(pos + new float2(-diag, diag), pos + new float2(diag, -diag));
            DrawLine(pos + new float2(-hor, 0), pos + new float2(hor, 0));
            DrawLine(pos + new float2(0, -hor), pos + new float2(0, hor));
        }

        static void DrawLine(float2 from, float2 to)
        {
            GL.Vertex(new float3(from, 1));
            GL.Vertex(new float3(to, 1));
        }
    }
}
#endif