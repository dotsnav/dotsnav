using System;
using DotsNav;
using DotsNav.Collections;
using DotsNav.Navmesh;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using IntPtr = DotsNav.IntPtr;
using Math = DotsNav.Math;

unsafe struct LctValidator
{
    HashSet<int> _triangles;
    NativeArray<int> _previousTriangles;
    NativeList<int> _removedTriangles;
    HashSet<IntPtr> _closed;
    NativeList<Disturbance> _disturbances;
    NativeList<IntPtr> _unconstrained;
    NativeArray<int> _clearanceCalculated;
    public int ClearanceCalculated => _clearanceCalculated[0];

    [Serializable]
    public struct Profile
    {
        public bool CDT;
        public bool LCT;
        public bool Triangles;
        public bool Clearance;
    }

    public LctValidator(Navmesh* navmesh, Allocator allocator)
    {
        _triangles = new HashSet<int>(1024, allocator);
        _removedTriangles = new NativeList<int>(1024, allocator);
        _closed = new HashSet<IntPtr>(1024, allocator);
        _disturbances = new NativeList<Disturbance>(allocator);
        _unconstrained = new NativeList<IntPtr>(allocator);
        _clearanceCalculated = new NativeArray<int>(1, allocator);
        _previousTriangles = default;
        InitTriangles(navmesh);
    }

    public void Validate(Navmesh* navmesh, Profile profile)
    {
        if (profile.CDT)
            IsCdt(navmesh);
        if (profile.LCT)
            TestDisturbances(navmesh);
        if (profile.Triangles)
            TestTriangleIds(navmesh);
        if (profile.Clearance)
            _clearanceCalculated[0] += TestLocalClearance(navmesh);
    }

    void TestDisturbances(Navmesh* lct)
    {
        _disturbances.Clear();

        var e = lct->GetEdgeEnumerator(true);
        while (e.MoveNext())
        {
            if (e.Current->Constrained)
                continue;

            lct->CheckEdgeForDisturbances(e.Current, _disturbances);

            for (int j = 0; j < _disturbances.Length; j++)
            {
                var p = _disturbances[j].Vertex->Point;
                var o = _disturbances[j].Edge->Org->Point;
                var d = _disturbances[j].Edge->Dest->Point;
                Debug.Log($"Edge {o} => {d} disturbed by {p}");
            }

            Assert.IsTrue(_disturbances.Length == 0);
        }
    }

    int TestLocalClearance(Navmesh* lct)
    {
        var clearanceCalculated = 0;

        var e = lct->GetEdgeEnumerator(true);
        while (e.MoveNext())
        {
            if (e.Current->Constrained)
                continue;

            var edge = e.Current;
            var b = edge->Org->Point;
            var c = edge->Dest->Point;

            if (!Math.Contains(b, -lct->Extent, lct->Extent) || !Math.Contains(c, -lct->Extent, lct->Extent))
                continue;

            var entrance = edge->ONext;
            if (!entrance->Constrained)
            {
                var clearance = GetLocalClearance(entrance->Dest->Point, b, c, edge, false);
                if (edge->ClearanceRight == -1)
                {
                    edge->ClearanceRight = clearance;
                    ++clearanceCalculated;
                }
                else if (!Approx(edge->ClearanceRight, clearance, 1e-6f))
                {
                    var a = entrance->Dest->Point;
                    var was = edge->ClearanceRight;
                    Debug.Log($"TestLocalClearance failed. a: {a}, b: {b}, c: {c}, rhs, was: {was}, is: {clearance}");
                    Assert.IsTrue(false);
                }
            }

            entrance = edge->OPrev;
            if (!entrance->Constrained)
            {
                var clearance = GetLocalClearance(entrance->Dest->Point, b, c, edge, true);
                if (edge->ClearanceLeft == -1)
                {
                    edge->ClearanceLeft = clearance;
                    ++clearanceCalculated;
                }
                else if (!Approx(edge->ClearanceLeft, clearance, 1e-6f))
                {
                    var a = entrance->Dest->Point;
                    var was = edge->ClearanceLeft;
                    Debug.Log($"TestLocalClearance failed. a: {a}, b: {b}, c: {c}, lhs, was: {was}, is: {clearance}");
                    Assert.IsTrue(false);
                }
            }
        }

        return clearanceCalculated;
    }


    static bool Approx(float p0, float p1, float epsilon)
        => math.abs(p1 - p0) < epsilon;

    // todo use Navmesh.GetLocalClearance
    static float GetLocalClearance(double2 a, double2 b, double2 c, Edge* exit, bool lhs)
    {
        var lba = math.lengthsq(a - b);
        var lbc = math.lengthsq(c - b);
        var clearance = math.sqrt(math.min(lba, lbc));
        var constraint = TryGetConstraint(exit, clearance, b, lhs);
        if (constraint == null)
            return (float) clearance;
        return (float) math.length(Math.ClosestPointOnLineSegment(b, constraint->Org->Point, constraint->Dest->Point) - b);
    }

    static Edge* TryGetConstraint(Edge* exit, double clearance, double2 corner, bool lhs)
    {
        var edge = lhs ? exit->DNext : exit->DPrev->Sym;
        return Navmesh.TryGetConstraint(clearance, corner, edge);
    }

    void TestTriangleIds(Navmesh* lct)
    {
        if (_previousTriangles.IsCreated)
            _previousTriangles.Dispose();

        _previousTriangles = _triangles.ToNativeArray();
        _triangles.Clear();
        _closed.Clear();
        _removedTriangles.Clear();

        var e = lct->GetEdgeEnumerator(true);
        while (e.MoveNext())
        {
            if (!Math.Contains(e.Current->Org->Point, -lct->Extent, lct->Extent))
                continue;

            if (_closed.Contains((IntPtr) e.Current))
                continue;

            if (e.Current->TriangleId == 0)
            {
                Debug.Log("unset Triangle Id");
                Assert.IsTrue(false);
            }

            if (!_triangles.TryAdd(e.Current->TriangleId))
            {
                Debug.Log("duplicate Triangle Id");
                Assert.IsTrue(false);
            }

            if (e.Current->TriangleId != e.Current->LNext->TriangleId || e.Current->TriangleId != e.Current->LPrev->TriangleId)
            {
                Debug.Log("inconsistent triangle ids");
                Assert.IsTrue(false);
            }

            _closed.TryAdd((IntPtr) e.Current);
            _closed.TryAdd((IntPtr) e.Current->LNext);
            _closed.TryAdd((IntPtr) e.Current->LPrev);
        }

        for (int i = 0; i < _previousTriangles.Length; i++)
        {
            if (!_triangles.Contains(_previousTriangles[i]))
                _removedTriangles.Add(_previousTriangles[i]);
        }

        var destroyed = new HashSet<int>(64, Allocator.Temp);
        var enumerator = lct->DestroyedTriangles.GetEnumerator();
        while (enumerator.MoveNext())
            destroyed.TryAdd(enumerator.Current);

        for (int i = 0; i < _removedTriangles.Length; i++)
        {
            var id = _removedTriangles[i];
            if (!destroyed.Remove(id))
            {
                Debug.Log($"Destroyed triangle {id} not reported");
                Assert.IsTrue(false);
            }
        }

        var remaining = destroyed.GetEnumerator();
        while (remaining.MoveNext())
        {
            if (_triangles.Contains(remaining.Current))
            {
                Debug.Log($"Triangle {remaining.Current} incorrectly reported as destroyed");
                Assert.IsTrue(false);
            }
        }

        destroyed.Dispose();
    }

    void InitTriangles(Navmesh* lct)
    {
        var e = lct->GetEdgeEnumerator(true);
        while (e.MoveNext())
        {
            if (!Math.Contains(e.Current->Org->Point, -lct->Extent, lct->Extent))
                continue;

            _triangles.TryAdd(e.Current->TriangleId);
            _closed.TryAdd((IntPtr) e.Current);
            _closed.TryAdd((IntPtr) e.Current->LNext);
            _closed.TryAdd((IntPtr) e.Current->LPrev);
        }
    }

    void IsCdt(Navmesh* lct, int constraint = -1, int vertex = -1)
    {
        var e = lct->GetEdgeEnumerator(true);
        while (e.MoveNext())
        {
            if (e.Current->Constrained)
                continue;

            var o = e.Current->Org->Point;
            var d = e.Current->Dest->Point;
            if (Math.Contains(o, -lct->Extent, lct->Extent) && Math.Contains(d, -lct->Extent, lct->Extent))
            {
                var on = e.Current->ONext->Dest->Point;
                var dn = e.Current->DNext->Org->Point;
                if (Math.CircumcircleContains(o, d, on, dn) && Math.Ccw(dn, d, on) && Math.Ccw(on, o, dn))
                {
                    Debug.Log($"delaunay fail, constraint: {constraint}, vertex:{vertex}, from: {o}, to: {d}");
                    throw new Exception();
                }
            }
        }
    }

    public void Dispose()
    {
        _triangles.Dispose();
        _removedTriangles.Dispose();
        _closed.Dispose();
        _disturbances.Dispose();
        _unconstrained.Dispose();
        if (_previousTriangles.IsCreated)
            _previousTriangles.Dispose();
        _clearanceCalculated.Dispose();
    }
}