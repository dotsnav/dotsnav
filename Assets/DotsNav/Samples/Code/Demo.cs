using System;
using System.Collections.Generic;
using System.Linq;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Hybrid;
using DotsNav.PathFinding.Hybrid;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

namespace DotsNav.Samples.Code
{
    class Demo : MonoBehaviour
    {
        public DotsNavNavmesh Navmesh;

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
        public float PlacementDelay;
        public DotsNavObstacle[] ObstaclePrefabs;

        [Header("UI")]
        public RectTransform Help;
        public Slider SpeedSlider;
        public Color RemovedObstacleColor;
        public Color AddedObstacleColor;
        public Color OldPathColor;
        public Color NewPathColor;
        public Color HighlightColor;
        Color _restoreColor;

        readonly Dictionary<ObstacleReference, List<Vector2>> _obstacles = new Dictionary<ObstacleReference, List<Vector2>>();
        readonly List<Vector2> _added = new List<Vector2>();
        readonly List<DotsNavPathFindingAgent> _found = new List<DotsNavPathFindingAgent>();
        DotsNavPathFindingAgent[] _agents;
        int[] _versions;
        float _lastPlacement;
        bool _paused;
        bool _step;
        bool _add;
        bool _lockHighlight;
        float _previousSpeed;
        float2 _size;
        Random _r;
        List<Vector2> _removed;
        LineDrawer _lineDrawer;

        DotsNavPathFindingAgent _highlight;
        DotsNavPathFindingAgent Highlight
        {
            get => _highlight;
            set
            {
                if (_highlight != null)
                {
                    _highlight.DrawColor = _restoreColor;
                    _highlight.DrawCorners = false;
                    _highlight.DrawPath = Navmesh.DrawMode == DrawMode.Constrained;
                }
                _highlight = value;
                if (_highlight != null)
                {
                    _restoreColor = _highlight.DrawColor;
                    _highlight.DrawColor = HighlightColor;
                    _highlight.DrawColor.a += 40;
                    _highlight.DrawCorners = true;
                    _highlight.DrawPath = true;
                }
            }
        }

        void Start()
        {
            _lineDrawer = GetComponent<LineDrawer>();
            _size = Navmesh.Size;
            FindObjectOfType<CameraController>().Initialize(_size);
            _r = new Random((uint) DateTime.Now.Ticks);
            _agents = new DotsNavPathFindingAgent[AgentAmount];
            _versions = new int[AgentAmount];
            _previousSpeed = SpeedSlider.value;

            var placedStarts = new List<Circle>();
            var placedGoals = new List<Circle>();

            for (int i = 0; i < AgentAmount; i++)
            {
                var agent = Instantiate(AgentPrefab);
                _agents[i] = agent;
                var r = AgentMinRadius + i * AgentRadiusRange / AgentAmount;
                agent.GetComponent<DotsNavAgent>().Radius = r;
                agent.GetComponent<DotsNavPathFindingAgent>().Navmesh = Navmesh;
                agent.transform.localScale = new Vector3(r, r, r) * 2;

                var cycles = 0;
                float2 pos;
                do
                {
                    pos = -_size / 2 + new float2(r + _r.NextFloat() * (_size.x - 2 * r), r + _r.NextFloat() * (SpawnRectHeight - 2 * r));
                } while (placedStarts.Any(p => math.length(p.Position - pos) < r + p.Radius) && ++cycles < 1000);

                agent.transform.position = pos.ToXxY();
                placedStarts.Add(new Circle{Position = pos, Radius = r});

                var goal = agent.transform.Find("Goal");
                cycles = 0;

                do
                {
                    pos = _size / 2 - new float2(r + _r.NextFloat() * (_size.x - 2 * r), r + _r.NextFloat() * (SpawnRectHeight - 2 * r));
                } while (placedGoals.Any(p => math.length(p.Position - pos) < r + p.Radius) && ++cycles < 1000);

                goal.position = pos.ToXxY();
                placedGoals.Add(new Circle{Position = pos, Radius = r});

                agent.FindPath(pos.ToXxY());
            }

            Help.gameObject.SetActive(!Application.isEditor);

            for (int i = 0; i < ObstacleAmount; i++)
                Insert();
        }

        public void PausedButton()
        {
            _paused = !_paused;
        }

        public void StepButton()
        {
            _paused = true;
            _step = true;
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

            var id = Navmesh.InsertObstacle(vertices);
            _obstacles.Add(id, vertices);

            _added.Clear();
            _added.AddRange(vertices);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                SceneManager.LoadScene("menu");
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Navmesh.DrawMode = Navmesh.DrawMode == DrawMode.Constrained ? DrawMode.Both : DrawMode.Constrained;
                var drawAgents = Navmesh.DrawMode == DrawMode.Constrained;
                foreach (var agent in _agents)
                    agent.DrawPath = drawAgents || agent == Highlight;
            }

            if (Input.GetKeyDown(KeyCode.H))
                Help.gameObject.SetActive(!Help.gameObject.activeSelf);

            if (SpeedSlider.value != _previousSpeed)
            {
                _previousSpeed = SpeedSlider.value;
                _paused = false;
            }

            if (!_lockHighlight)
                Highlight = null;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var over = Physics.Raycast(ray.origin, ray.direction * 10, out var hit);
            DotsNavPathFindingAgent highlight = null;
            if (over)
                highlight = hit.transform.parent.GetComponentInChildren<DotsNavPathFindingAgent>();

            var mouseDown = Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject();

            if (_lockHighlight)
            {
                if (mouseDown)
                {
                    if (highlight == null || highlight == Highlight)
                        _lockHighlight = false;
                    else
                        Highlight = highlight;
                }
            }
            else
            {
                if (mouseDown && over)
                    _lockHighlight = true;
                Highlight = highlight;
            }

            if (!_paused && Time.time - _lastPlacement > SpeedSlider.value * PlacementDelay || _step)
            {
                _lastPlacement = Time.time;
                _step = false;

                if (_add)
                {
                    Insert();
                    _removed = null;
                }
                else
                {
                    var ids = _obstacles.Keys.ToArray();
                    var toRemove = ids[_r.NextInt(ids.Length)];
                    Navmesh.RemoveObstacle(toRemove);
                    _removed = _obstacles[toRemove];
                    _obstacles.Remove(toRemove);
                    _added.Clear();
                }

                _add = !_add;
                foreach (var agent in _found)
                {
                    if (agent == _highlight)
                    {
                        _restoreColor = OldPathColor;
                        continue;
                    }
                    agent.DrawColor = OldPathColor;
                }
                _found.Clear();
            }

            if (_found.Count == 0)
            {
                for (int i = 0; i < _agents.Length; i++)
                {
                    var agent = _agents[i];
                    if (_versions[i] < agent.Version)
                    {
                        _found.Add(agent);
                        _versions[i] = agent.Version;
                        if (agent == _highlight)
                        {
                            _restoreColor = NewPathColor;
                            continue;
                        }
                        agent.DrawColor = NewPathColor;
                        agent.DrawColor.a += 20;
                    }
                }
            }

            if (Navmesh.IsInitialized)
            {
                _lineDrawer.DrawPoly(_added, AddedObstacleColor);
                _lineDrawer.DrawPoly(_removed, RemovedObstacleColor);
            }
        }

        struct Circle
        {
            public float2 Position;
            public float Radius;
        }
    }
}