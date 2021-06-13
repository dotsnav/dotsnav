using System.Collections.Generic;
using DotsNav.Data;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.Navmesh.Hybrid
{
    class NavmeshConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavNavmesh navmesh) =>
            {
                var entity = GetPrimaryEntity(navmesh);
                navmesh.Entity = entity;
                navmesh.World = DstEntityManager.World;
                DstEntityManager.AddComponentData(entity, new NavmeshComponent
                (
                    navmesh.Size,
                    navmesh.ExpectedVerts,
                    navmesh.MergePointDistance,
                    navmesh.CollinearMargin
                ));
                DstEntityManager.AddBuffer<DestroyedTriangleElement>(entity);
                DstEntityManager.AddComponentData(entity, new NavmeshDrawComponent
                {
                    DrawMode = navmesh.DrawMode,
                    ConstrainedColor = navmesh.ConstrainedColor,
                    UnconstrainedColor = navmesh.UnconstrainedColor
                });
            });
        }
    }

    /// <summary>
    /// Creates a navmesh on startup and can then be used to insert and destroy obstacles. Destroying this object triggers
    /// the destruction of the navmesh releasing its resources.
    /// </summary>
    public class DotsNavNavmesh : EntityLifetimeBehaviour
    {
        /// <summary>
        /// Size of the navmesh to be created. The navmesh will be centered around the origin.
        /// Changing this value after initialization has no effect
        /// </summary>
        public Vector2 Size = new Vector2(50, 50);

        /// <summary>
        /// Determines the size of initial allocations. Changing this value after initialization has no effect
        /// </summary>
        [Min(100)]
        public int ExpectedVerts = 1000;

        /// <summary>
        /// Vertices inserted with this range of an existing vertex are merged instead. Changing this value after initialization has no effect
        /// </summary>
        public float MergePointDistance = 1e-3f;

        /// <summary>
        /// Margin for considering points collinear when removing intersections. Changing this value after initialization has no effect
        /// </summary>
        public float CollinearMargin = 1e-6f;

        [Header("Debug")]
        public DrawMode DrawMode = DrawMode.Constrained;
        public Color ConstrainedColor = Color.red;
        public Color UnconstrainedColor = Color.white;

        static bool _created;

        /// <summary>
        /// The amount of vertices in the current triangulation
        /// </summary>
        public int Vertices { get; internal set; }

        public bool IsInitialized => Vertices >= 8;

        protected override void Awake()
        {
            if (_created)
            {
                Debug.LogError("Only one navmesh is allowed");
                DestroyImmediate(this);
            }

            _created = true;

            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _created = false;
        }


        void OnValidate()
        {
            Size = math.abs(Size);
        }

        /// <summary>
        /// Queue insertion of an obstacle in world space
        /// </summary>
        public ObstacleReference InsertObstacle(IEnumerable<Vector2> vertices)
        {
            var em = World.All[0].EntityManager;
            var obstacle = em.CreateEntity();
            em.AddComponentData(obstacle, new ObstacleComponent());
            var input = em.AddBuffer<VertexElement>(obstacle);
            foreach (float2 vertex in vertices)
                input.Add(vertex);
            return new ObstacleReference(obstacle);
        }

        /// <summary>
        /// Queue bulk insertion of permanant obstacles. Once inserted, these obstacles can not be removed.
        /// Returns the total amount of inserted vertices excluding intersections
        /// </summary>
        /// <param name="amount">Amount of obstacles to insert</param>
        /// <param name="adder">A Burst compatible struct implementing IObstacleAdder</param>
        public int InsertObstacleBulk<T>(int amount, T adder) where T : struct, IObstacleAdder
        {
            var em = World.All[0].EntityManager;
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
            return input.Length;
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

        /// <summary>
        /// Returns true when point p is contained within the navmesh
        /// </summary>
        public bool Contains(Vector2 p) => Math.Contains(p, -Size / 2, Size / 2);

        /// <summary>
        /// Queue insertion of an obstacle in object space
        /// </summary>
        public ObstacleReference InsertObstacle(IEnumerable<Vector2> vertices, Vector2 position, float rotationDegrees = 0) =>
            InsertObstacle(vertices, position, Vector2.one, rotationDegrees);

        /// <summary>
        /// Queue insertion of an obstacle in object space
        /// </summary>
        public ObstacleReference InsertObstacle(IEnumerable<Vector2> vertices, Vector2 position, Vector2 scale, float rotationDegrees = 0)
        {
            var em = World.All[0].EntityManager;
            var obstacle = em.CreateEntity();
            em.AddComponentData(obstacle, new LocalToWorld {Value = float4x4.TRS(position.ToXxY(), quaternion.RotateY(math.radians(rotationDegrees)), ((float2)scale).xxy)});
            em.AddComponentData(obstacle, new ObstacleComponent());
            var input = em.AddBuffer<VertexElement>(obstacle);
            foreach (float2 vertex in vertices)
                input.Add(vertex);
            return new ObstacleReference(obstacle);
        }

        /// <summary>
        /// Queue removal of an obstacle
        /// </summary>
        public void RemoveObstacle(ObstacleReference toRemove)
        {
            World.All[0].EntityManager.DestroyEntity(toRemove.Value);
        }

        /// <summary>
        /// Returns the native navmesh which exposes the triangulation. This structure is invalidated each update and
        /// the latest version should be obtained each cycle
        /// </summary>
        public Navmesh GetNativeNavmesh() => World.All[0].EntityManager.GetComponentData<Navmesh>(Entity);

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