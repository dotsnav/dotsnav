using System.Collections.Generic;
using DotsNav.Core.Hybrid;
using DotsNav.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
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
        IPlaneComponent[] _components;

        protected override void Awake()
        {
            _components = GetComponents<IPlaneComponent>();
        }

        public Vector3 DirectionToWorldSpace(float2 dir)
        {
            return transform.InverseTransformDirection(dir.ToXxY());
        }

        /// <summary>
        /// Queue insertion of an obstacle in world space
        /// </summary>
        public ObstacleReference InsertObstacle(IEnumerable<Vector2> vertices)
        {
            var em = World.EntityManager;
            Assert.IsTrue(em.Exists(Entity));
            var obstacle = em.CreateEntity();
            var input = em.AddBuffer<VertexElement>(obstacle);
            foreach (float2 vertex in vertices)
                input.Add(vertex);
            foreach (var component in _components)
                component.InsertObstacle(obstacle, em);
            return new ObstacleReference(obstacle);
        }

        /// <summary>
        /// Queue bulk insertion of permanant obstacles. Once inserted, these obstacles can not be removed.
        /// Returns the total amount of inserted vertices excluding intersections
        /// </summary>
        /// <param name="amount">Amount of obstacles to insert</param>
        /// <param name="adder">A Burst compatible struct implementing IObstacleAdder</param>
        public void InsertObstacleBulk<T>(int amount, T adder) where T : struct, IObstacleAdder
        {
            var em = World.EntityManager;
            Assert.IsTrue(em.Exists(Entity));
            var obstacle = em.CreateEntity();
            em.AddBuffer<VertexElement>(obstacle);
            var amounts = em.AddBuffer<VertexAmountElement>(obstacle);
            var input = em.GetBuffer<VertexElement>(obstacle);
            new PopulateBulkJob<T>
                {
                    Amount = amount,
                    Adder = adder,
                    Input = input,
                    Amounts = amounts
                }
                .Run();

            foreach (var component in _components)
                component.InsertObstacle(obstacle, em);
        }

        public void RemoveObstacle(ObstacleReference toRemove)
        {
            World.EntityManager.DestroyEntity(toRemove.Value);
        }

        [BurstCompile]
        struct PopulateBulkJob<T> : IJob where T : struct, IObstacleAdder
        {
            public int Amount;
            public T Adder;
            public DynamicBuffer<VertexElement> Input;
            public DynamicBuffer<VertexAmountElement> Amounts;

            public void Execute()
            {
                for (int i = 0; i < Amount; i++)
                {
                    var s = Input.Length;
                    Adder.Add(i, Input);
                    Amounts.Add(Input.Length - s);
                }
            }
        }
    }
}