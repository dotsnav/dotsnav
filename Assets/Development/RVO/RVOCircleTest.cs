using DotsNav.Core.Data;
using DotsNav.Core.Systems;
using DotsNav.LocalAvoidance.Data;
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
    int _i;
    Entity _agentTree1;
    Entity _agent;

    void Start()
    {
        _store = new BlobAssetStore();
        var world = World.All[0];
        var convSettings = GameObjectConversionSettings.FromWorld(world, _store);
        var entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab.gameObject, convSettings);
        var entityManager = world.EntityManager;

        SpawnMoveAgentTest();

        void SpawnMoveAgentTest()
        {
            var agentTree0 = entityManager.CreateEntity(typeof(AgentTreeComponent));
            _agentTree1 = entityManager.CreateEntity(typeof(AgentTreeComponent));
            _agent = entityManager.Instantiate(entityPrefab);
            entityManager.AddComponentData(_agent, new TargetComponent());
            entityManager.AddSharedComponentData(_agent, new AgentTreeSharedComponent(agentTree0));
        }

        // SpawnCircleTest();

        void SpawnCircleTest()
        {
            SpawnTree(0);
            SpawnTree(new float2(SpawnRadius * 1f, 0));

            void SpawnTree(float2 origin)
            {
                var agentTree = entityManager.CreateEntity(typeof(AgentTreeComponent));

                for (int i = 0; i < AgentAmount; i++)
                {
                    var entity = entityManager.Instantiate(entityPrefab);
                    var p = SpawnRadius * new float2(math.cos(i * 2 * math.PI / AgentAmount), math.sin(i * 2 * math.PI / AgentAmount));
                    entityManager.SetComponentData(entity, new Translation {Value = (origin + p).ToXxY()});
                    entityManager.AddComponentData(entity, new TargetComponent {Value = origin - p});
                    entityManager.AddSharedComponentData(entity, new AgentTreeSharedComponent(agentTree));
                }
            }
        }
    }

    void Update()
    {
        if (++_i == 50)
        {
            var em = World.All[0].EntityManager;
            em.SetSharedComponentData(_agent, new AgentTreeSharedComponent(_agentTree1));

            // em.DestroyEntity(em.CreateEntityQuery(typeof(AgentTreeSharedComponent)));
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
            .ForEach((RVOComponent agent, ref Translation translation) =>
            {
                translation.Value += (agent.Velocity * dt).ToXxY();
            })
            .ScheduleParallel();
    }
}