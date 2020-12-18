#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DotsNav.PathFinding.Hybrid
{
    [CustomEditor(typeof(DotsNavAgent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    class AgentEditor : Editor
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