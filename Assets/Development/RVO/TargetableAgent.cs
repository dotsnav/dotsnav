// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;
//
// class TargetableAgent : MonoBehaviour, IConvertGameObjectToEntity
// {
//     public float3 Target;
//
//     public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
//     {
//         dstManager.AddComponent<TargetComponent>(entity);
//     }
// }