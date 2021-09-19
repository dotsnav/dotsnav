using System;
using System.Collections.Generic;
using System.Linq;
using DotsNav.Core.Hybrid;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

namespace DotsNav.Samples.Code
{
    class StressTest : MonoBehaviour
    {
        public int Amount;
        public int Seed;
        public float ScaleOffset;
        public DotsNavPlane Plane;
        DotsNavNavmesh _navmesh;
        public DotsNavObstacle[] Prefabs;
        public Text Output;
        public Text Output1;
        public RectTransform Help;

        Random _r;
        readonly List<ObstacleReference> _ids = new List<ObstacleReference>();
        Mode _mode = Mode.Inserting;
        readonly List<Vector2> _points = new List<Vector2>();
        float _startTime;

        void Awake()
        {
            // Ensure gameobject conversion when loading a scene
            World.All[0].GetOrCreateSystem<InitializationSystemGroup>().Update();

            _navmesh = Plane.GetComponent<DotsNavNavmesh>();
            FindObjectOfType<CameraController>().Initialize(Plane.Size);
            _startTime = Time.time;
            _r = new Random((uint) Seed);
            UpdateSeedText();
            Help.gameObject.SetActive(!Application.isEditor);
        }

        void UpdateSeedText() => Output.text = $"Seed: {Seed}";

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                SceneManager.LoadScene("menu");
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
                Help.gameObject.SetActive(!Help.gameObject.activeSelf);

            if (Input.GetKeyDown(KeyCode.E))
                _navmesh.DrawMode = _navmesh.DrawMode == DrawMode.Constrained ? DrawMode.Both : DrawMode.Constrained;

            if (!Application.isEditor && Time.time - _startTime < 1)
                return;

            switch (_mode)
            {
                case Mode.Inserting:
                    Insert();
                    if (_ids.Count == Amount)
                        _mode = Mode.Removing;
                    break;
                case Mode.Removing:
                    Plane.RemoveObstacle(_ids.Last());
                    _ids.RemoveAt(_ids.Count - 1);

                    if (_ids.Count == 0)
                    {
                        _mode = Mode.Inserting;
                        Seed++;
                        UpdateSeedText();
                        _r = new Random((uint) Seed);
                    }

                    break;
                default:
                    throw new Exception();
            }

            Output1.text = $"Vertices: {_navmesh.Vertices}";
        }

        void Insert()
        {
            var p = Prefabs[_r.NextInt(Prefabs.Length)];
            var scale = 1 - ScaleOffset + _r.NextFloat() * 2 * ScaleOffset;
            var rot = _r.NextFloat(2 * math.PI);

            _points.Clear();
            foreach (var f in p.Vertices)
                _points.Add(DemoMath.Rotate(scale * f, rot));

            var min = new float2(float.MaxValue);
            var max = new float2(float.MinValue);

            for (var i = 0; i < _points.Count; i++)
            {
                min = math.min(_points[i], min);
                max = math.max(_points[i], max);
            }

            var size = max - min;
            var range = (float2) Plane.Size - size;
            var offset = _r.NextFloat2(range);

            for (var i = 0; i < _points.Count; i++)
                _points[i] += (Vector2) (offset - min - (float2) Plane.Size / 2);

            _ids.Add(Plane.InsertObstacle(_points));
        }

        enum Mode
        {
            Inserting,
            Removing,
            Done
        }
    }
}