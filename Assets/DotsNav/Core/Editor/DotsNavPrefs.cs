#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DotsNav
{
    class DotsNavPrefs
    {
        const string PrefName = "DotsNavPreferences";
        public static Color GizmoColor;
        public static Color EditColor;
        public static Color FadeColor;
        public static KeyCode VertexUp;
        public static KeyCode VertexDown;
        static bool _loaded;
        static StringBuilder _sb;

        [PreferenceItem("DotsNav")]
        static void DotsNavPreferences()
        {
            Init();

            VertexUp = (KeyCode) EditorGUILayout.EnumPopup("Vertex Up", VertexUp);
            VertexDown = (KeyCode) EditorGUILayout.EnumPopup("Vertex Down", VertexDown);
            GizmoColor = EditorGUILayout.ColorField("Gizmo Color", GizmoColor);
            EditColor = EditorGUILayout.ColorField("Edit Color", EditColor);
            FadeColor = EditorGUILayout.ColorField("Fade Color", FadeColor);

            if (GUI.changed)
                Store();
        }

        static DotsNavPrefs()
        {
            Init();
        }

        static void Init()
        {
            if (!_loaded)
            {
                _loaded = true;
                _sb = new StringBuilder();

                if (EditorPrefs.HasKey(PrefName))
                {
                    try
                    {
                        var data = EditorPrefs.GetString(PrefName).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                        GizmoColor = GetColor(data[0]);
                        EditColor = GetColor(data[1]);
                        FadeColor = GetColor(data[2]);
                        VertexUp = GetKeyCode(data[3]);
                        VertexDown = GetKeyCode(data[4]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load dotsnav preferences: {e}");
                        SetDefaults();
                        Store();
                    }
                }
                else
                {
                    SetDefaults();
                }
            }
        }


        static void SetDefaults()
        {
            GizmoColor = Color.red;
            EditColor = Color.red;
            FadeColor = Color.white;
            VertexUp = KeyCode.M;
            VertexDown = KeyCode.N;
        }

        static void Store()
        {
            _sb.Clear();
            Append(GizmoColor, _sb);
            Append(EditColor, _sb);
            Append(FadeColor, _sb);
            Append(VertexUp, _sb);
            Append(VertexDown, _sb);
            EditorPrefs.SetString(PrefName, _sb.ToString());
        }

        static void Append(KeyCode kc, StringBuilder sb)
        {
            sb.Append($"{kc.ToString()};");
        }

        static KeyCode GetKeyCode(string s)
        {
            return (KeyCode) Enum.Parse(typeof(KeyCode), s);
        }

        static void Append(Color color, StringBuilder sb)
        {
            sb.Append($"{Convert.ToString(color.r)},{Convert.ToString(color.g)},{Convert.ToString(color.b)},{Convert.ToString(color.a)};");
        }

        static Color GetColor(string s)
        {
            var d = s.Split(',');
            return new Color(Convert.ToSingle(d[0]), Convert.ToSingle(d[1]), Convert.ToSingle(d[2]), Convert.ToSingle(d[3]));
        }
    }
}
#endif