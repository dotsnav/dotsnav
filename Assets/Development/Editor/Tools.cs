#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

static class Tools
{
    [MenuItem("Tools/Create Scriptable Object")]
    public static void CreateScriptableObject()
    {
        EditorWindow.GetWindow<CreateScriptableObjectWindow>();
    }

    [MenuItem("Tools/Open Persistent Data")]
    public static void OpenPersistentData()
    {
        Process.Start(Application.persistentDataPath);
    }
}
#endif