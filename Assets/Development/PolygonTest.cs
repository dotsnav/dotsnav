using System.Collections.Generic;
using System.Linq;
using DotsNav;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Navmesh.Hybrid;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Navmesh = DotsNav.Navmesh.Navmesh;
using Random = Unity.Mathematics.Random;

unsafe class PolygonTest : MonoBehaviour
{
    public float Size;
    public int Amount;
    public DotsNavObstacle[] Prefabs;
    public float ScaleOffset;

    public int Seed;
    public bool BurstInsert = true;
    public bool Remove;
    public bool BurstRemove = true;
    public bool BurstSafetyChecks = true;
    public DotsNavRunner Runner;
    public DotsNavNavmesh Navmesh;
    int _i;
    List<Entity> _ids;
    public bool Track;
    public bool Select;
    public Vector2 SelectPos = new Vector2(3.985278f, 7.360839f);
    public float SelectRadius = .1f;

    public Transform P;
    float2 _mousePos;
    int _points;
    public LctValidator.Profile Validation;
    string _newConstraint = "-1";
    NativeList<Entity> _entities;
    bool _runTest;

    void Start()
    {
        BurstCompiler.Options.EnableBurstSafetyChecks = BurstSafetyChecks;
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
    }

    void RunTest()
    {
        var em = World.All[0].EntityManager;

        if (!em.HasComponent<Navmesh>(Navmesh.Entity))
            return;
        _runTest = true;

        BurstCompiler.Options.EnableBurstCompilation = BurstInsert;
        var r = new Random((uint) Seed);
        var l = new NativeList<float2>(Allocator.Persistent);
        var a = new NativeList<int>(Allocator.Persistent);
        _entities = new NativeList<Entity>(Amount, Allocator.Persistent);

        if (Select && P)
            P.position = ((float2) SelectPos).ToXxY();

        for (int i = 0; i < Amount; i++)
        {
            var p = Prefabs[r.NextInt(Prefabs.Length)];
            var scale = (1 - ScaleOffset + r.NextFloat() * 2 * ScaleOffset);
            var rot = r.NextFloat(2 * math.PI);

            var vertices = p.Vertices.Select(f => Math.Rotate((float2)(scale * f), rot)).ToList();

            var min = new float2(float.MaxValue);
            var max = new float2(float.MinValue);

            foreach (var f in vertices)
            {
                min = math.min(f, min);
                max = math.max(f, max);
            }

            var size = max - min;
            var range = Size - size;
            var offset = r.NextFloat2(range);

            if (Select)
            {
                var add = false;
                for (int j = 0; j < vertices.Count - 1; j++)
                    if (i < 10 || IntersectSegCircle(vertices[j] - min + offset, vertices[j + 1] - min + offset, SelectPos, SelectRadius) > 0)
                    {
                        add = true;
                        break;
                    }

                if (!add)
                {
                    DestroyImmediate(p.gameObject);
                    continue;
                }
            }

            foreach (var f in vertices)
                l.Add(f - min + offset);
            a.Add(vertices.Count);
        }

        _points = l.Length;

        Warmup();


        for (int i = 0; i < Amount; i++)
            _entities.Add(em.CreateEntity());

        var navmeshes = new NativeArray<Navmesh>(1, Allocator.TempJob);
        navmeshes[0] = em.GetComponentData<Navmesh>(Navmesh.Entity);
        var toRemove = new NativeQueue<Entity>(Allocator.TempJob);

        new InsertValidateJob
            {
                Navmesh = navmeshes,
                Destroyed = em.GetBuffer<DestroyedTriangleElement>(Navmesh.Entity),
                Points = l,
                Amounts = a,
                ObstacleEntities = _entities,
                Validation = Validation,
                ToRemove = toRemove
            }
            .RunTimed($"Insert and refine {_points} points");

        em.SetComponentData(Navmesh.Entity, navmeshes[0]);

        if (Remove)
            RunRemove();

        navmeshes.Dispose();
        l.Dispose();
        a.Dispose();
        toRemove.Dispose();
    }

    void Warmup()
    {
        var em = World.All[0].EntityManager;
        var navmeshes = new NativeArray<Navmesh>(1, Allocator.TempJob);
        var l = new NativeList<float2>(Allocator.TempJob);
        var a = new NativeList<int>(Allocator.TempJob);
        var e = new NativeList<Entity>(Allocator.TempJob);
        var toRemove = new NativeQueue<Entity>(Allocator.TempJob);

        new InsertValidateJob
            {
                Navmesh = navmeshes,
                Destroyed = em.GetBuffer<DestroyedTriangleElement>(Navmesh.Entity),
                Points = l,
                Amounts = a,
                ObstacleEntities = _entities,
                Validation = Validation,
                ToRemove = toRemove
            }
            .RunTimed(silent: true);

        new RemoveValidateJob
        {
            Navmesh = navmeshes,
            Destroyed = em.GetBuffer<DestroyedTriangleElement>(Navmesh.Entity),
            ConstraintIds = e,
            Validation = new LctValidator.Profile(),
            ToRemove = toRemove
        }.RunTimed(silent: true);

        navmeshes.Dispose();
        l.Dispose();
        a.Dispose();
        e.Dispose();
        toRemove.Dispose();
    }

    void RunRemove()
    {
        BurstCompiler.Options.EnableBurstCompilation = BurstRemove;
        var navmeshes = new NativeArray<Navmesh>(1, Allocator.TempJob);
        var em = World.All[0].EntityManager;
        navmeshes[0] = em.GetComponentData<Navmesh>(Navmesh.Entity);
        var toRemove = new NativeQueue<Entity>(Allocator.TempJob);

        new RemoveValidateJob
            {
                Navmesh = navmeshes,
                Destroyed = em.GetBuffer<DestroyedTriangleElement>(Navmesh.Entity),
                ConstraintIds = _entities,
                Validation = Validation,
                ToRemove = toRemove

            }
            .RunTimed($"Remove and refine {_points} points");

        em.SetComponentData(Navmesh.Entity, navmeshes[0]);
        em.DestroyEntity(_entities);
        navmeshes.Dispose();
        toRemove.Dispose();
    }

    void Update()
    {
        Runner.ProcessModifications();
        if (!_runTest)
            RunTest();

        var mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        _mousePos = ((float3) Camera.main.ScreenToWorldPoint(mousePos)).xz;

        var ids = new HashSet<Entity>();

        if (Track)
        {
            var em = World.All[0].EntityManager;
            var edges = em.GetComponentData<Navmesh>(Navmesh.Entity).GetEdgeEnumerator();
                while (edges.MoveNext())
                    foreach (var c in edges.Current->Constraints)
                        ids.Add(c);
            _ids = ids.OrderBy(i=>i).ToList();
        }
    }

    void OnGUI()
    {
        GUILayout.Label($"{_mousePos.x:F}, {_mousePos.y:F}", GUILayout.MinWidth(80));

        if (GUILayout.Button("Remove"))
            RunRemove();

        if (!Track)
            return;

        var s = (int) GUILayout.HorizontalSlider(_i, -1, _ids.Count - 1, GUILayout.MinWidth(400));

        if (s != _i)
        {
            _i = s;
            _newConstraint = _i.ToString();
        }
        _newConstraint = GUILayout.TextField(_newConstraint);
        if (int.TryParse(_newConstraint, out var parsed))
        {
            _i = math.max(parsed, -1);
            _newConstraint = _i.ToString();
        }
    }

    void OnDestroy()
    {
        _entities.Dispose();
    }

    static int IntersectSegCircle(float2 E, float2 L, float2 C, float r)
    {
        // use epsilon to capture points on circle
        const float epsilon = 1e-6f;
        var d = L - E;
        var f = E - C;
        var a = math.dot(d, d);
        var b = math.dot(2 * f, d);
        var c = math.dot(f, f) - r * r;

        var discriminant = b * b - 4 * a * c;
        if (discriminant >= 0)
        {
            discriminant = math.sqrt(discriminant);

            var t1 = (-b - discriminant) / (2 * a);
            var t2 = (-b + discriminant) / (2 * a);
            if (t1 >= -epsilon && t1 <= 1 + epsilon)
            {
                if (t2 >= -epsilon && t2 <= 1 + epsilon && discriminant > 0)
                    return 2;
                return 1;
            }

            if (t2 >= -epsilon && t2 <= 1 + epsilon)
                return 1;
        }

        var rs = Math.Square(r);
        return (math.distancesq(E, C) <= rs && math.distancesq(E, C) <= rs) ? 2 : 0;
    }
}