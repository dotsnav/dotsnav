using DotsNav.Core.Data;
using DotsNav.Core.Extensions;
using DotsNav.LocalAvoidance;
using DotsNav.Navmesh.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RVOCircleTest : MonoBehaviour
{
    public int AgentAmount;
    public float SpawnRadius;
    public RVOAgent Prefab;
    BlobAssetStore _store;

    void Start()
    {
        _store = new BlobAssetStore();
        var world = World.All[0];
        var convSettings = GameObjectConversionSettings.FromWorld(world, _store);
        var entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab.gameObject, convSettings);
        var entityManager = world.EntityManager;

        for (int i = 0; i < AgentAmount; i++)
        {
            var entity = entityManager.Instantiate(entityPrefab);
            var p = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
            entityManager.SetComponentData(entity, new Translation {Value = p.ToXxY()});
            entityManager.AddComponentData(entity, new TargetComponent {Value = -p});
        }
    }

    void OnDestroy()
    {
        _store.Dispose();
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
                direction.Value = math.normalize(target.Value - translation.Value.xz);
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
            .ForEach((RVOComponent agent, ref Translation translation) =>
            {
                translation.Value += (agent.Velocity * dt).ToXxY();
            })
            .ScheduleParallel();
    }
}