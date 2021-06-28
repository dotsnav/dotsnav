using DotsNav;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Hybrid;
using DotsNav.LocalAvoidance.Systems;
using DotsNav.Navmesh.Hybrid;
using DotsNav.PathFinding.Hybrid;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RVOCircleTest : MonoBehaviour
{
    public int AgentAmount;
    public float SpawnRadius;
    public DotsNavLocalAvoidanceAgent Prefab;

    void Start()
    {
        var navmesh = GetComponent<DotsNavNavmesh>();
        var localAvoidance = GetComponent<DotsNavLocalAvoidance>();

        for (int i = 0; i < AgentAmount; i++)
        {
            var prefab = Instantiate(Prefab);

            var pos = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
            prefab.transform.position = transform.TransformPoint(pos.ToXxY());

            var pathFinding = prefab.GetComponent<DotsNavPathFindingAgent>();
            pathFinding.Navmesh = navmesh;
            pathFinding.FindPath(((float3) transform.TransformPoint(-pos.ToXxY())).xz);

            prefab.GetComponent<DotsNavLocalAvoidanceAgent>().LocalAvoidance = localAvoidance;
        }
    }
}

// struct TargetComponent : IComponentData
// {
//     public float3 Value;
// }

[UpdateInGroup(typeof(DotsNavSystemGroup))]
[UpdateBefore(typeof(RVOSystem))]
class DirectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.fixedDeltaTime;

        Entities
            .WithBurst()
            .ForEach((DirectionComponent direction, SettingsComponent agent, ref PreferredVelocityComponent preferredVelocity) =>
            {
                // var toTarget = target.Value - translation.Value;
                // var length = math.length(toTarget);
                const float preferredSpeed = 8;
                // if (length >= preferredSpeed * dt)
                //     preferredVelocity.Value = toTarget / length * preferredSpeed;
                // else if (length >= -1e3f)
                //     preferredVelocity.Value = toTarget;
                // else
                //     preferredVelocity.Value = 0;

                preferredVelocity.Value = (direction.Value * preferredSpeed).ToXxY();
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

// class TargetSystem : SystemBase
// {
//     protected override void OnUpdate()
//     {
//         Entities
//             .WithoutBurst()
//             .ForEach((TargetableAgent agent, ref TargetComponent target) =>
//             {
//                 target.Value = agent.Target;
//             })
//             .Run();
//     }
// }