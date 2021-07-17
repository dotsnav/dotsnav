using DotsNav.LocalAvoidance.Hybrid;
using UnityEngine;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Systems;
using DotsNav.PathFinding.Systems;
using DotsNav.Systems;
using Unity.Entities;

class DemoAgent : MonoBehaviour, IConvertGameObjectToEntity
{
    DotsNavLocalAvoidanceAgent _agent;
    public float PreferredSpeed;

    void Awake()
    {
        _agent = GetComponent<DotsNavLocalAvoidanceAgent>();
    }

    void Update()
    {
        transform.position += _agent.Velocity * Time.deltaTime;
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PreferredSpeedComponent {Value = PreferredSpeed});
    }
}

[UpdateInGroup(typeof(DotsNavSystemGroup))]
[UpdateAfter(typeof(PathFinderSystem))]
[UpdateBefore(typeof(RVOSystem))]
class PreferredVelocitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithBurst()
            .ForEach((DirectionComponent direction, PreferredSpeedComponent preferredSpeed, ref PreferredVelocityComponent preferredVelocity) =>
            {
                preferredVelocity.Value = direction.Value * preferredSpeed.Value;
            })
            .ScheduleParallel();
    }
}

struct PreferredSpeedComponent : IComponentData
{
    public float Value;
}
