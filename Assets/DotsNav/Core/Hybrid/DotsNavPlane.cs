using System.Collections.Generic;
using DotsNav.Core.Hybrid;
using DotsNav.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.Hybrid
{
    public class DotsNavPlane : ToEntity
    {
        /// <summary>
        /// Size of the navmesh to be created. Changing this value after initialization has no effect
        /// </summary>
        public Vector2 Size = new(1000, 1000);
        public Color ConstrainedColor = Color.red;
        public Color UnconstrainedColor = Color.white;

        IPlaneComponent[] _components;
        World _world;
        
        void Awake()
        {
            _components = GetComponents<IPlaneComponent>();
        }
        
        protected sealed override void Convert(EntityManager entityManager, Entity entity)
        {
            _world = entityManager.World;
            entityManager.AddComponentObject(entity, this);
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
            var em = _world.EntityManager;
            var obstacle = em.CreateEntity();
            em.AddComponentData(obstacle, new LocalToWorld { Value = float4x4.identity });
            em.AddSharedComponent(obstacle, new PlaneComponent { Entity = Entity });
            var input = em.AddBuffer<VertexElement>(obstacle);
            foreach (float2 vertex in vertices)
                input.Add(vertex);
            foreach (var component in _components)
                component.InsertObstacle(em, Entity, obstacle);
            return new ObstacleReference(obstacle);
        }

        /// <summary>
        /// Queue bulk insertion of permanant obstacles in world space. Once inserted, these obstacles can not be removed.
        /// </summary>
        /// <param name="amount">Amount of obstacles to insert</param>
        /// <param name="adder">A Burst compatible struct implementing IObstacleAdder</param>
        public void InsertObstacleBulk<T>(int amount, T adder) where T : struct, IObstacleAdder
        {
            var em = _world.EntityManager;
            var obstacle = em.CreateEntity();
            em.AddComponentData(obstacle, new LocalToWorld { Value = float4x4.identity });
            em.AddSharedComponent(obstacle, new PlaneComponent { Entity = Entity });
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
                component.InsertObstacle(em, Entity, obstacle);
        }

        public void RemoveObstacle(ObstacleReference toRemove)
        {
            _world.EntityManager.DestroyEntity(toRemove.Value);
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

        void OnValidate()
        {
            Size = math.abs(Size);
        }

        /// <summary>
        /// Returns true when point p is contained within the navmesh
        /// </summary>
        public bool Contains(Vector2 p) => Math.Contains(p, -Size / 2, Size / 2);

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            float2 hs = Size / 2;
            var color = ConstrainedColor;
            DrawLine(-hs, hs * new float2(1, -1));
            DrawLine(hs * new float2(1, -1), hs);
            DrawLine(hs, hs * new float2(-1, 1));
            DrawLine(hs * new float2(-1, 1), -hs);
            void DrawLine(float2 a, float2 b) => Debug.DrawLine(a.ToXxY(), b.ToXxY(), color);
        }
    }
}