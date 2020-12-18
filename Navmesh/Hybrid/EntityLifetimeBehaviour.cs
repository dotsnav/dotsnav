using Unity.Entities;
using UnityEngine;

namespace DotsNav.Hybrid
{
    /// <summary>
    /// Base class for MonoBehaviours controlling an entity's lifetime
    /// </summary>
    public class EntityLifetimeBehaviour : MonoBehaviour
    {
        public Entity Entity;
        public World World;

        protected bool Injected;

        protected virtual void Awake()
        {
            var t = transform;
            ConvertToEntity c;
            while ((c = t.GetComponent<ConvertToEntity>()) == null && t.parent != null)
                t = t.parent;
            if (c == null)
            {
                Debug.LogError($"No Convert to Entity attached to {GetType().Name} or one of its parents", gameObject);
                return;
            }
            Injected = c.ConversionMode == ConvertToEntity.Mode.ConvertAndInjectGameObject;
        }

        protected virtual void OnDestroy()
        {
            if (Injected && World != null && World.IsCreated && World.EntityManager.Exists(Entity))
                World.EntityManager.DestroyEntity(Entity);
        }
    }
}