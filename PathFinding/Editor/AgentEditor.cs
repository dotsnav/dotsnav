#if UNITY_EDITOR
using DotsNav.PathFinding.Hybrid;
using UnityEditor;
using UnityEngine;

namespace DotsNav.PathFinding.Editor
{
    [CustomEditor(typeof(DotsNavAgent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    class AgentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            GUILayout.Label($"State: {((DotsNavAgent)target).State}");
            GUI.enabled = true;
            DrawDefaultInspector();
        }
    }
}
#endif