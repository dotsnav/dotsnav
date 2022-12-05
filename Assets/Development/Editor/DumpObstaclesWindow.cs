// #if UNITY_EDITOR
// using System.Collections.Generic;
// using DotsNav;
// using DotsNav.Hybrid;
// using DotsNav.PathFinding.Hybrid;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEditor;
// using UnityEngine;
//
// class DumpObstaclesWindow : EditorWindow
// {
//     public List<List<Vector2>> ToDump;
//     public float2 Start;
//     public float2 Goal;
//     public float Scale;
//
//     void OnGUI()
//     {
//         if (GUILayout.Button("Go"))
//         {
//             var root  = new GameObject("Dump").transform;
//
//             var agentgo = new GameObject("Agent");
//             var a = agentgo.transform;
//             var agent = agentgo.AddComponent<DotsNavPathFindingAgent>();
//             agent.DrawCorners = true;
//             agent.DrawGizmos = false;
//             agentgo.AddComponent<ConvertToEntity>().ConversionMode = ConvertToEntity.Mode.ConvertAndInjectGameObject;
//             a.parent = root;
//             var s = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
//             s.GetComponent<MeshRenderer>().material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Assets/start.mat");
//             s.name = "Start";
//             s.parent = a;
//             s.position = Start.ToXxY();
//             s.localScale = (float3) Scale;
//
//             var g = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
//             g.GetComponent<MeshRenderer>().material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Assets/goal.mat");
//             g.name = "Goal";
//             g.parent = a;
//             g.position = Goal.ToXxY();
//             g.localScale = (float3) Scale;
//
//             foreach (var obstacle in ToDump)
//             {
//                 var go = new GameObject("Obstacle");
//                 go.AddComponent<ConvertToEntity>().ConversionMode = ConvertToEntity.Mode.ConvertAndInjectGameObject;
//                 go.transform.parent = root;
//                 var o = go.AddComponent<DotsNavObstacle>();
//                 o.Vertices = obstacle.ToArray();
//             }
//
//             Close();
//         }
//     }
// }
// #endif