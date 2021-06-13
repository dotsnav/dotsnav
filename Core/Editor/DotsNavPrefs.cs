#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DotsNav.Editor
{
    class DotsNavPrefs
    {
        const string PrefName = "DotsNavPreferences";
        public static Color GizmoColor;
        public static Color EditColor;
        public static Color FadeColor;
        static bool _loaded;
        static StringBuilder _sb;

        [PreferenceItem("DotsNav")]
        static void DotsNavPreferences()
        {
            Init();

            EditorGUILayout.LabelField("Colors");
            GizmoColor = EditorGUILayout.ColorField("Gizmo", GizmoColor);
            EditColor = EditorGUILayout.ColorField("Edit", EditColor);
            FadeColor = EditorGUILayout.ColorField("Fade", FadeColor);

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
        }

        static void Store()
        {
            _sb.Clear();
            Append(GizmoColor, _sb);
            Append(EditColor, _sb);
            Append(FadeColor, _sb);
            EditorPrefs.SetString(PrefName, _sb.ToString());
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