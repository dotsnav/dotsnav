using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.Hybrid
{
    public interface IToEntity
    {
        void Convert(EntityManager entityManager, Entity entity);
    }

    public class ToEntity : MonoBehaviour
    {
        World _world;
        public Entity Entity { get; private set; }

        void Start()
        {
            _world = World.All[0];
            var em = _world.EntityManager;
            Entity = em.CreateEntity();
            var tr = transform;
            em.AddComponentData(Entity, new LocalToWorld { Value = float4x4.TRS(tr.position, tr.rotation, tr.lossyScale) });
            Convert(em, Entity);
            foreach (var c in GetComponentsInChildren<IToEntity>()) 
                c.Convert(em, Entity);
        }

        protected virtual void Convert(EntityManager entityManager, Entity entity)
        {
        }

        void OnDestroy()
        {
            if (_world.IsCreated)
                _world.EntityManager.DestroyEntity(Entity);
        }
    }
}