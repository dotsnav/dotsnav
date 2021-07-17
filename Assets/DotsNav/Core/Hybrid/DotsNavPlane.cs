using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Hybrid
{
    class PlaneConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavPlane localAvoidance) =>
            {
                var entity = GetPrimaryEntity(localAvoidance);
                localAvoidance.Entity = entity;
                localAvoidance.World = DstEntityManager.World;
            });
        }
    }

    public class DotsNavPlane : EntityLifetimeBehaviour
    {
        public Vector3 DirectionToWorldSpace(float2 dir)
        {
            return transform.InverseTransformDirection(dir.ToXxY());
        }
    }
}