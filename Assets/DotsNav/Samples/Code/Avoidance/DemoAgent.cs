using DotsNav.LocalAvoidance.Hybrid;
using UnityEngine;
using Unity.Entities;

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