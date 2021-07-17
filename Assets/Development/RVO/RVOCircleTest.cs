using DotsNav;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Hybrid;
using DotsNav.LocalAvoidance.Systems;
using DotsNav.PathFinding.Data;
using DotsNav.PathFinding.Hybrid;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RVOCircleTest : MonoBehaviour
{
    public int AgentAmount;
    public float SpawnRadius;
    public DotsNavLocalAvoidanceAgent Prefab;

    void Start()
    {
        var plane = GetComponent<DotsNavPlane>();

        for (int i = 0; i < AgentAmount; i++)
        {
            var prefab = Instantiate(Prefab);

            var pos = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
            prefab.transform.position = transform.TransformPoint(pos.ToXxY());

            var pathFinding = prefab.GetComponent<DotsNavPathFindingAgent>();
            pathFinding.FindPath(transform.TransformPoint(-pos.ToXxY()));

            prefab.GetComponent<DotsNavAgent>().Plane = plane;
        }
    }
}

[UpdateInGroup(typeof(DotsNavSystemGroup))]
[UpdateBefore(typeof(RVOSystem))]
class DirectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithBurst()
            .ForEach((DirectionComponent direction, SettingsComponent agent, NavmeshAgentComponent navmesh, ref PreferredVelocityComponent preferredVelocity) =>
            {
                const float preferredSpeed = 8;
                preferredVelocity.Value = direction.Value * preferredSpeed;
            })
            .ScheduleParallel();
    }
}

[UpdateInGroup(typeof(DotsNavSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(LocalAvoidanceHybridWriteSystem))]
class MoveSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithoutBurst()
            .ForEach((DotsNavLocalAvoidanceAgent agent) =>
            {
                agent.transform.position += (Vector3) (agent.Velocity * dt);
            })
            .Run();
    }
}