using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Hybrid;
using UnityEngine;
using Unity.Entities;

[RequireComponent(typeof(DotsNavLocalAvoidanceAgent))]
class DemoAgent : MonoBehaviour, IToEntity
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
    
    void IToEntity.Convert(EntityManager entityManager, Entity entity)
    {
        entityManager.AddComponentData(entity, new SteeringComponent
        {
            PreferredSpeed = PreferredSpeed,
            BrakeSpeed = BrakeSpeed
        });
    }
}