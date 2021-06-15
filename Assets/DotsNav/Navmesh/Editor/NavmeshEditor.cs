#if UNITY_EDITOR
using DotsNav.Navmesh.Hybrid;
using UnityEditor;
using UnityEngine;

namespace DotsNav.Navmesh
{
    [CustomEditor(typeof(DotsNavNavmesh))]
    class NavmeshEditor : UnityEditor.Editor
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