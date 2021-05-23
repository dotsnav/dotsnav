#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DotsNav.Hybrid
{
    [CustomEditor(typeof(DotsNavNavmesh))]
    class NavmeshEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            GUILayout.Label($"Vertices: {((DotsNavNavmesh)target).Vertices}");
            GUI.enabled = true;
            DrawDefaultInspector();
        }
    }
}
#endif