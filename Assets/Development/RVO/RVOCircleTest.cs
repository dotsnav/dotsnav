using DotsNav;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Hybrid;
using DotsNav.Navmesh.Hybrid;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RVOCircleTest : MonoBehaviour
{
    public int AgentAmount;
    public float SpawnRadius;
    public RVOAgent Prefab;
    public DotsNavDynamicTree DynamicTree;
    public DotsNavObstacleTree Tree;

    void Start()
    {
        for (int i = 0; i < AgentAmount; i++)
        {
            var prefab = Instantiate(Prefab);
            var c = prefab.GetComponent<DotsNavLocalAvoidance>();
            c.ObstacleTree = Tree;
            c.DynamicTree = DynamicTree;
            var pos = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
            prefab.transform.position = pos.ToXxY();
        }
    }
}

struct TargetComponent : IComponentData
{
    public float2 Value;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(DotsNavSystemGroup))]
class DirectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithBurst()
            .ForEach((Translation translation, TargetComponent target, ref DirectionComponent direction) =>
            {
                var toTarget = target.Value - translation.Value.xz;
                direction.Value = math.all(toTarget == 0) ? 0 : math.normalize(toTarget);
            })
            .ScheduleParallel();
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(DotsNavSystemGroup))]
class MoveSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithBurst()
            .ForEach((AgentComponent agent, ref Translation translation) =>
            {
                translation.Value += (agent.Velocity * dt).ToXxY();
            })
            .ScheduleParallel();
    }
}

class TargetSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithoutBurst()
            .WithAll<AgentComponent>()
            .WithNone<TargetComponent>()
            .WithStructuralChanges()
            .ForEach((Entity entity, Translation translation) =>
            {
                EntityManager.AddComponentData(entity, new TargetComponent{Value = -translation.Value.xz});
            })
            .Run();
    }
}