using System;
using System.Collections.Generic;
using System.Linq;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Hybrid;
using DotsNav.PathFinding.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

namespace DotsNav.Samples.Code
{
    class Avoidance : MonoBehaviour
    {
        public DotsNavPlane Plane;

        [Header("Agents")]
        public int AgentAmount;
        public float AgentMinRadius;
        public float AgentRadiusRange;
        public float SpawnRectHeight;
        public DotsNavPathFindingAgent AgentPrefab;

        [Header("Obstacles")]
        public int ObstacleAmount;
        public float ObstacleMinScale;
        public float ObstacleScaleRange;
        public DotsNavObstacle[] ObstaclePrefabs;

        [Header("UI")]
        public RectTransform Help;

        float2 _size;
        Random _r;
        public int Seed;

        void Start()
        {
            // Ensure gameobject conversion when loading a scene
            World.All[0].GetOrCreateSystem<InitializationSystemGroup>().Update();

            _size = Plane.GetComponent<DotsNavPlane>().Size;
            FindObjectOfType<CameraController>().Initialize(_size);
            // _r = new Random((uint) DateTime.Now.Ticks);
            _r = new Random((uint) Seed);

            var placedStarts = new List<Circle>();
            var placedGoals = new List<Circle>();

            for (int i = 0; i < AgentAmount; i++)
            {
                var agent = Instantiate(AgentPrefab);
                var r = AgentMinRadius + i * AgentRadiusRange / AgentAmount;
                var dotsNavAgent = agent.GetComponent<DotsNavAgent>();
                dotsNavAgent.Radius = r;
                dotsNavAgent.Plane = Plane;
                agent.transform.localScale = new Vector3(r, r, r) * 2;

                var cycles = 0;
                float2 pos;
                do
                {
                    pos = -_size / 2 + new float2(r + _r.NextFloat() * (_size.x - 2 * r), r + _r.NextFloat() * (SpawnRectHeight - 2 * r));
                } while (placedStarts.Any(p => math.length(p.Position - pos) < r + p.Radius) && ++cycles < 1000);

                agent.transform.position = pos.ToXxY();
                placedStarts.Add(new Circle{Position = pos, Radius = r});

                cycles = 0;

                do
                {
                    pos = _size / 2 - new float2(r + _r.NextFloat() * (_size.x - 2 * r), r + _r.NextFloat() * (SpawnRectHeight - 2 * r));
                } while (placedGoals.Any(p => math.length(p.Position - pos) < r + p.Radius) && ++cycles < 1000);

                placedGoals.Add(new Circle{Position = pos, Radius = r});

                agent.FindPath(pos.ToXxY());
            }

            Help.gameObject.SetActive(!Application.isEditor);

            for (int i = 0; i < ObstacleAmount; i++)
                Insert();
        }

        void Insert()
        {
            var obstacle = ObstaclePrefabs[_r.NextInt(ObstaclePrefabs.Length)];
            var scale = ObstacleMinScale + _r.NextFloat() * ObstacleScaleRange;
            var rot = _r.NextFloat(2 * math.PI);
            var vertices = obstacle.Vertices.Select(f => DemoMath.Rotate(scale * f, rot)).ToList();
            if (obstacle.Closed)
                vertices.Add(vertices[0]);

            var min = new float2(float.MaxValue);
            var max = new float2(float.MinValue);

            for (var i = 0; i < vertices.Count; i++)
            {
                min = math.min(vertices[i], min);
                max = math.max(vertices[i], max);
            }

            var size = max - min;
            var range = _size - new float2(0, 2 * SpawnRectHeight) - size;
            var offset = _r.NextFloat2(range) + new float2(0, SpawnRectHeight) - _size / 2;

            for (var i = 0; i < vertices.Count; i++)
                vertices[i] += (Vector2) (offset - min);

            Plane.InsertObstacle(vertices);
        }

        struct Circle
        {
            public float2 Position;
            public float Radius;
        }
    }
}