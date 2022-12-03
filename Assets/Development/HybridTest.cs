using DotsNav.Core.Hybrid;
using DotsNav.Hybrid;
using DotsNav.PathFinding.Hybrid;
using UnityEngine;

class HybridTest : MonoBehaviour
{
    public DotsNavRunner UpdateMode;
    public DotsNavPlane Plane;
    public DotsNavPathFindingAgent Agent;
    ObstacleReference _id;
    public DotsNavObstacle ObstaclePrefab;

    void Update()
    {
        UpdateMode.Step();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Add"))
        {
            // _id = Navmesh.InsertObstacle(new[] {new Vector2(1, 1), new Vector2(1, 5)}, Vector2.zero, Vector2.one, 0);
            _id = Plane.InsertObstacle(new[] {new Vector2(3, 0), new Vector2(3, -1)});
        }
        if (GUILayout.Button("Remove"))
            Plane.RemoveObstacle(_id);
        if (GUILayout.Button("Path"))
        {
            Agent.transform.position = new Vector2(-5, 1).ToXxY();
            Agent.FindPath(new Vector2(5, 1));
        }

        if (GUILayout.Button("Spawn"))
            Instantiate(ObstaclePrefab);
    }
}