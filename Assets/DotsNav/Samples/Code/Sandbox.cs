using System;
using System.Collections.Generic;
using System.Linq;
using DotsNav.CollisionDetection.Hybrid;
using DotsNav.Drawing;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Hybrid;
using DotsNav.PathFinding.Hybrid;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DotsNav.Samples.Code
{
    class Sandbox : MonoBehaviour
    {
        public DotsNavRunner Runner;
        public DotsNavNavmesh Navmesh;
        public float AgentSizeZoomSpeed;
        public float MinAgentSize;
        public float MaxAgentSize;
        public RectTransform Help;
        public DotsNavObstacle[] Prefabs;
        public float PrefabMinSize = .1f;
        public float PrefabMaxSize = 3f;
        public float PrefabSizeSpeed = 1;
        public float PrefabRotationSpeed = 1;
        public Color CastHitColor = Color.yellow;
        public Color CastColor = Color.white;

        readonly List<List<Vector2>> _points = new List<List<Vector2>> {new List<Vector2>()};
        readonly List<ObstacleReference> _obstacles = new List<ObstacleReference>();

        GameObject _target;
        int _placingPrefab = -1;
        float _prefabSize = 1;
        float _prefabRotation;
        Camera _camera;
        Vector2 _previousMouse;
        DotsNavPathFindingAgent _agent;
        Transform _start;
        Transform _goal;
        LineDrawer _lineDrawer;
        CameraController _cameraController;

        void Awake()
        {
            _cameraController = FindObjectOfType<CameraController>();
            _cameraController.Initialize(Navmesh.Size, .5f);
            _lineDrawer = GetComponent<LineDrawer>();
            _camera = Camera.main;
            Help.gameObject.SetActive(!Application.isEditor);
            _agent = FindObjectOfType<DotsNavPathFindingAgent>();
            var tr = _agent.transform;
            _start = tr.Find("Start");
            _goal = tr.Find("Goal");
            var size = _start.localScale.x;
            var s = new Vector3(size, size, size);
            _goal.localScale = s;
            _agent.GetComponent<DotsNavAgent>().Radius = size / 2;
        }

        protected void Update()
        {
            ProcessInputAndUpdateUi();

            // Enqueue a path queries using FindPath. Checking start or goal changed
            // is omitted for clarity. Use Pathfinder recalculate flags to indicate which
            // states trigger automatic recalculation of an agent's path.
            _agent.FindPath(_start.position, _goal.position);

            // Manually trigger navmesh and pathfinder update to ensure the visuals line up
            Runner.ProcessModifications();

            // A ray cast, make sure to dispose of the RayCastResult returned from
            // Navmesh.CastSegment or Navmesh.CastRay
            var rbs = FindObjectsOfType<RaycastBehaviour>();
            foreach (var rb in rbs)
            {
                var from = rb.GetStart();
                var to = rb.GetGoal();

                if (!Navmesh.Contains(from) || !Navmesh.Contains(to))
                    continue;

                var pointSize = .1f * _cameraController.Zoom;

                using (var result = Navmesh.CastSegment(from, to, true))
                {
                    _lineDrawer.DrawLine(from, to, result.CollisionDetected ? CastHitColor : CastColor);
                    var hits = result.Hits;
                    for (int i = 0; i < hits.Length; i++)
                        _lineDrawer.DrawPoint(hits[i].Position, i == 0 ? CastHitColor : CastColor, pointSize);
                }
            }

            // A disc cast, make sure to dispose of the DiscCastResult returned from
            // Navmesh.CastDisc
            var dbs = FindObjectsOfType<DiscCastBehaviour>();
            foreach (var db in dbs)
            {
                if (!Navmesh.Contains(db.Centre))
                    continue;
                using (var result = Navmesh.CastDisc(db.Centre, db.Radius, false))
                    _lineDrawer.DrawCircle(db.Centre, new Vector2(0, db.Radius), 2 * Mathf.PI, result.CollisionDetected ? CastHitColor : CastColor, res:200);
            }

        }

        // Obstacles can be inserted by spawing prefabs, but when they are based on
        // user input or generated procedurally they can be inserted by supplying a
        // list of vertices to Navmesh.InsertObstacle
        void Insert()
        {
            foreach (var points in _points)
            {
                // Note that generally speaking it is recommended to use a larger navmesh
                // with an inner bounding box to avoid having to adjust each obstacle
                ClampObstacle(points, Navmesh.Size);

                if (points.Count == 0)
                    continue;
                var obstacleReference = Navmesh.InsertObstacle(points);
                _obstacles.Add(obstacleReference);
            }
            _points.Clear();
            _points.Add(new List<Vector2>());
        }

        static unsafe void ClampObstacle(List<Vector2> vertices, Vector2 size)
        {
            var buffer = new List<Vector2>(vertices);
            vertices.Clear();
            var hs = size / 2;
            var br = new Vector2(hs.x, -hs.y);
            var tl = new Vector2(-hs.x, hs.y);
            var l = stackalloc Vector2[2];

            for (var i = 0; i < buffer.Count; i++)
            {
                var f = buffer[i];

                if (DemoMath.Contains(f, -hs, hs))
                    vertices.Add(f);

                if (i < buffer.Count - 1)
                {
                    var next = buffer[i + 1];
                    var li = 0;
                    if (DemoMath.IntersectSegSeg(f, next, -hs, br, out var r))
                        l[li++] = r;
                    if (DemoMath.IntersectSegSeg(f, next, br, hs, out r))
                        l[li++] = r;
                    if (DemoMath.IntersectSegSeg(f, next, hs, tl, out r))
                        l[li++] = r;
                    if (DemoMath.IntersectSegSeg(f, next, tl, -hs, out r))
                        l[li++] = r;

                    switch (li)
                    {
                        case 1:
                            AddCorner(l[0]);
                            vertices.Add(l[0]);
                            break;
                        case 2 when math.lengthsq(l[0] - f) < math.lengthsq(l[1] - f):
                            vertices.Add(l[0]);
                            vertices.Add(l[1]);
                            break;
                        case 2:
                            vertices.Add(l[1]);
                            vertices.Add(l[0]);
                            break;
                    }
                }
            }

            if (vertices.Count == 1)
                vertices.Clear();

            void AddCorner(Vector2 point)
            {
                Side a, b;
                if (vertices.Count == 0 || (a = GetSide(point)) == Side.None || (b = GetSide(vertices.Last())) == Side.None || a == b)
                    return;

                var aIsHor = a == Side.Bottom || a == Side.Top;

                switch (aIsHor ? a : b)
                {
                    case Side.Top:
                        switch (aIsHor ? b : a)
                        {
                            case Side.Left:
                                vertices.Add(tl);
                                return;
                            case Side.Right:
                                vertices.Add(hs);
                                return;
                        }
                        break;
                    case Side.Bottom:
                        switch (aIsHor ? b : a)
                        {
                            case Side.Left:
                                vertices.Add(-hs);
                                return;
                            case Side.Right:
                                vertices.Add(br);
                                return;
                        }
                        break;
                }
                throw new ArgumentOutOfRangeException();
            }

            Side GetSide(Vector2 p)
            {
                if (p.x == -hs.x)
                    return Side.Left;
                if (p.x == hs.x)
                    return Side.Right;
                if (p.y == -hs.y)
                    return Side.Bottom;
                if (p.y == hs.y)
                    return Side.Top;
                return Side.None;
            }
        }

        enum Side { None, Left, Right, Top, Bottom }

        void ProcessInputAndUpdateUi()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                SceneManager.LoadScene("menu");
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _points.Clear();
                _points.Add(new List<Vector2>());
                _placingPrefab = -1;
            }

            var prev = _placingPrefab;
            if (Input.GetKeyDown(KeyCode.Alpha1))
                _placingPrefab = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2))
                _placingPrefab = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3))
                _placingPrefab = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4))
                _placingPrefab = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5))
                _placingPrefab = 4;

            var mousePosRaw = Input.mousePosition;
            mousePosRaw.z = _camera.nearClipPlane;
            var mousePos = (Vector2) _camera.ScreenToWorldPoint(mousePosRaw).xz();
            var scrollDelta = Input.mouseScrollDelta.y;
            var mouseDelta = mousePos - _previousMouse;
            _previousMouse = mousePos;

            if (_placingPrefab != -1)
            {
                if (prev != _placingPrefab)
                    _prefabSize = _placingPrefab < 4
                        ? _cameraController.Zoom
                        : Mathf.Min(1, _cameraController.Zoom * 2);

                if (scrollDelta != 0 && Input.GetKey(KeyCode.LeftShift))
                    _prefabRotation += scrollDelta * PrefabRotationSpeed;

                if (scrollDelta != 0 && Input.GetKey(KeyCode.LeftControl))
                    _prefabSize = math.clamp(_prefabSize + scrollDelta * PrefabSizeSpeed * _prefabSize, PrefabMinSize, PrefabMaxSize);


                _points.Clear();

                var start = _placingPrefab;
                var end = _placingPrefab == 4 ? 14 : _placingPrefab + 1;

                for (int i = start; i < end; i++)
                {
                    var obstacle = Prefabs[i];
                    var verts = obstacle.Vertices;
                    var points = new List<Vector2>();
                    foreach (var vert in verts)
                        points.Add(DemoMath.Rotate(vert * _prefabSize, _prefabRotation) + mousePos);
                    if (obstacle.Closed)
                        points.Add(points[0]);
                    _points.Add(points);
                }

                if (Input.GetMouseButtonDown(0))
                    Insert();
            }
            else
            {
                if (Input.GetMouseButtonDown(1) && _points[0].Count > 0)
                {
                    if (_points[0].Count > 1)
                    {
                        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && _points[0].Count > 2)
                            _points[0].Add(_points[0][0]);
                        Insert();
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    var ray = _camera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray.origin, ray.direction * 10, out var hit))
                        _target = hit.collider.gameObject;
                    if (_target == null || _points[0].Count > 0)
                        _points[0].Add(mousePos);
                }

                if (Input.GetMouseButtonUp(0))
                    _target = null;

                if (scrollDelta != 0 && Input.GetKey(KeyCode.LeftControl))
                {
                    var size = _start.localScale.x;
                    size = math.clamp(size + scrollDelta * AgentSizeZoomSpeed * size, MinAgentSize, MaxAgentSize);
                    var s = new Vector3(size, size, size);
                    _start.localScale = s;
                    _goal.localScale = s;
                    _agent.GetComponent<DotsNavAgent>().Radius = size / 2;
                }

                if (_target != null && Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && mouseDelta != Vector2.zero)
                    _target.transform.position += mouseDelta.ToXxY();
            }

            if (Input.GetKeyDown(KeyCode.E))
                Navmesh.DrawMode = Navmesh.DrawMode == DrawMode.Constrained ? DrawMode.Both : DrawMode.Constrained;
            if (Input.GetKeyDown(KeyCode.H))
                Help.gameObject.SetActive(!Help.gameObject.activeSelf);

            if ((Input.GetKey(KeyCode.T) || Input.GetKeyDown(KeyCode.R)) && _obstacles.Count > 0)
            {
                Navmesh.RemoveObstacle(_obstacles.Last());
                _obstacles.RemoveAt(_obstacles.Count - 1);
            }

            if (Input.GetKeyDown(KeyCode.C))
                _agent.DrawCorners = !_agent.DrawCorners;

            foreach (var points in _points)
                for (int i = 1; i < points.Count; i++)
                    _lineDrawer.DrawLine(points[i - 1], points[i], Color.cyan);
            if (_placingPrefab == -1 && _points[0].Count > 0)
                _lineDrawer.DrawLine(_points[0][_points[0].Count - 1], mousePos, Color.cyan);
        }
    }
}