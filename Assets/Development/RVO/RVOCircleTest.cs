using DotsNav;
using DotsNav.Hybrid;
using DotsNav.PathFinding.Hybrid;
using Unity.Mathematics;
using UnityEngine;

public class RVOCircleTest : MonoBehaviour
{
    public int AgentAmount;
    public float SpawnRadius;
    public DotsNavAgent Prefab;

    void Start()
    {
        var plane = GetComponent<DotsNavPlane>();

        for (int i = 0; i < AgentAmount; i++)
        {
            var prefab = Instantiate(Prefab);
            prefab.Plane = plane;

            var pos = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
            prefab.transform.position = transform.TransformPoint(pos.ToXxY());

            var pathFinding = prefab.GetComponent<DotsNavPathFindingAgent>();
            pathFinding.FindPath(transform.TransformPoint(-pos.ToXxY()));
        }
    }
}