#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

class CreateScriptableObjectWindow : EditorWindow
{
    int _index;
    Type[] _types;
    string[] _names;

    void OnGUI()
    {
        if (_names == null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.StartsWith("Unity"));
            _types = assemblies.SelectMany(a => a.GetTypes().Where(t =>
                                   typeof(ScriptableObject).IsAssignableFrom(t) &&
                                   !typeof(EditorWindow).IsAssignableFrom(t) &&
                                   !typeof(Editor).IsAssignableFrom(t) &&
                                   !t.IsAbstract))
                               .OrderBy(t => t.Name)
                               .ToArray();
            _names = _types.Select(t => t.Name).ToArray();
        }

        _index = EditorGUILayout.Popup(_index, _names);

        var path = "Assets";
        var obj = Selection.activeObject;
        if (obj != null)
        {
            var p = AssetDatabase.GetAssetPath(obj);
            if (p != "" && Directory.Exists(p) && !p.Contains("Assets"))
                path = Path.Combine(path, Path.GetDirectoryName(p));
        }
        path = Path.Combine(path, "New " + _names[_index] + ".asset");

        if (GUILayout.Button("Create " + path))
            AssetDatabase.CreateAsset(CreateInstance(_types[_index]), path);
    }
}
#endif