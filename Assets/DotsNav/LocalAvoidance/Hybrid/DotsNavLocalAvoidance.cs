﻿using DotsNav.Core.Hybrid;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.LocalAvoidance.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.LocalAvoidance.Hybrid
{
    class LocalAvoidanceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavPlane plane, DotsNavLocalAvoidance localAvoidance) =>
            {
                var entity = GetPrimaryEntity(localAvoidance);
                localAvoidance.Entity = entity;
                localAvoidance.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new ObstacleTreeComponent());

                var boxEntity = CreateAdditionalEntity(localAvoidance);
                DstEntityManager.AddComponentData(boxEntity, new ObstacleTreeElementComponent{Tree = entity});
                var box = DstEntityManager.AddBuffer<VertexElement>(boxEntity);
                float2 extent = plane.Size / 2;
                box.Add(-extent);
                box.Add(new float2(-extent.x, extent.y));
                box.Add(extent);
                box.Add(new float2(extent.x, -extent.y));
                box.Add(-extent);

                DstEntityManager.AddComponentData(entity, new DynamicTreeComponent());
                if (localAvoidance.DrawObstacleTree)
                    DstEntityManager.AddComponentData(entity, new DrawComponent {Color = plane.ConstrainedColor});
            });
        }
    }

    [RequireComponent(typeof(DotsNavPlane))]
    public class DotsNavLocalAvoidance : EntityLifetimeBehaviour, IPlaneComponent
    {
        public bool DrawObstacleTree;

        void IPlaneComponent.InsertObstacle(Entity obstacle, EntityManager em)
        {
            em.AddComponentData(obstacle, new ObstacleTreeElementComponent {Tree = Entity});
        }
    }
}