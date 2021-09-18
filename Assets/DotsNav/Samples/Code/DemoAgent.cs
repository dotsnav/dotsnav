using DotsNav.LocalAvoidance.Hybrid;
using UnityEngine;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.LocalAvoidance.Systems;
using DotsNav.PathFinding.Data;
using DotsNav.PathFinding.Systems;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

class DemoAgent : MonoBehaviour, IConvertGameObjectToEntity
{
    DotsNavLocalAvoidanceAgent _agent;
    public float PreferredSpeed;
    public float BrakeSpeed;

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
        dstManager.AddComponentData(entity, new SteeringComponent
        {
            PreferredSpeed = PreferredSpeed,
            BrakeSpeed = BrakeSpeed
        });
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
            .ForEach((Translation translation, DirectionComponent direction, SteeringComponent steering, PathQueryComponent query, ref PreferredVelocityComponent preferredVelocity) =>
            {
                var dist = math.length(query.To - translation.Value);
                var speed = math.min(dist * steering.BrakeSpeed, steering.PreferredSpeed);
                preferredVelocity.Value = direction.Value * speed;
            })
            .ScheduleParallel();
    }
}

struct SteeringComponent : IComponentData
{
    public float PreferredSpeed;
    public float BrakeSpeed;
}
