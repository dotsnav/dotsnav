using DotsNav.Core.Hybrid;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    [RequireComponent(typeof(DotsNavPlane))]
    public class DotsNavLocalAvoidance : MonoBehaviour, IPlaneComponent
    {
        public bool DrawObstacleTree;
        
        void IPlaneComponent.InsertObstacle(EntityManager entityManager, Entity plane, Entity obstacle)
        {
            entityManager.AddComponentData(obstacle, new ObstacleTreeElementComponent {Tree = plane});
        }

        void IToEntity.Convert(EntityManager entityManager, Entity entity)
        {
            entityManager.AddComponentData(entity, new ObstacleTreeComponent());
            
            var boxEntity = entityManager.CreateEntity();
            entityManager.SetName(boxEntity, "Obstacle Tree Bounds");
            entityManager.AddComponentData(boxEntity, new ObstacleTreeElementComponent{Tree = entity});
            var box = entityManager.AddBuffer<VertexElement>(boxEntity);
            var plane = GetComponent<DotsNavPlane>();
            float2 extent = plane.Size / 2;
            box.Add(-extent);
            box.Add(new float2(-extent.x, extent.y));
            box.Add(extent);
            box.Add(new float2(extent.x, -extent.y));
            box.Add(-extent);
            
            entityManager.AddComponentData(entity, new DynamicTreeComponent());
            if (DrawObstacleTree)
                entityManager.AddComponentData(entity, new DrawComponent {Color = plane.ConstrainedColor});
        }
    }
}