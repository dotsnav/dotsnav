using System.Linq;
using System.Text;
using DotsNav;
using DotsNav.Collections;
using DotsNav.PathFinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class FunnelTest : MonoBehaviour
{
    public Transform Begin;
    public Transform End;
    public Transform V0;
    public int Amount;
    public Transform[] Portals;
    public float Radius = 1;
    int _waypoints;
    string _path;
    int _unique;
    Funnel _f;

    void Update()
    {
        if (!_f.IsCreated)
        {
            Debug.Log("creating");
            _f = new Funnel(64, Allocator.Persistent);
        }

        var portals = Portals.Take(Amount == 0 ? Portals.Length : math.min(Amount, Portals.Length)).ToArray();
        foreach (var portal in portals)
            DebugUtil.DrawCircle(portal.position, Radius);

        for (int i = 2; i < portals.Length; i++)
        {
            DebugUtil.DrawLine(portals[i - 2].position, portals[i - 1].position, Color.green);
            DebugUtil.DrawLine(portals[i - 2].position, portals[i].position);
            DebugUtil.DrawLine(portals[i - 1].position, portals[i].position);

            // if (i == 3 || i == 4)
            //     DebugUtil.DrawCircle(portals[i - 2].position.TakeXZ(), portals[i - 1].position.TakeXZ(), portals[i].position.TakeXZ(), i == 3 ? Color.black : Color.blue);
        }
        
        // DebugUtil.DrawLine(V0.position, Portals[0].position);
        // DebugUtil.DrawLine(V0.position, Portals[1].position);
        
        var l1 = portals.Select(t => t.position.xz()).ToList();
        var start = Begin.position.xz();
        var l = new System.Collections.Generic.List<Gate>();
        var b = true;
        for (int j = 0; j < l1.Count - 1; j++)
        {
            l.Add(b ? new Gate {Left = l1[j], Right = l1[j + 1]} : new Gate {Left = l1[j + 1], Right = l1[j]});
            b = !b;
        }

        var end = End.position.xz();
        var ll = new List<Gate>(64, Allocator.Persistent);
        foreach (var portal in l) 
            ll.Add(portal);

        var result = new Deque<Funnel.Node>(10, Allocator.Persistent);
        _f.GetPath(ll, start, end, Radius, result);
        _waypoints = result.Count;

        var ra = new Funnel.Node[result.Count];
        for (int i = 0; i < result.Count; i++) 
            ra[i] = result.FromFront(i);

        _unique = ra.Distinct().Count();
        _path = new StringBuilder().Concat(ra.SelectMany(r => new[] {r.From, r.To}));

        for (int i = 0; i < ra.Length; ++i)
            DebugUtil.DrawLine(ra[i].From.ToXxY(), ra[i].To.ToXxY(), Color.red);
        ll.Dispose();
        result.Dispose();
    }

    void OnGUI()
    {
        GUILayout.Label($"Waypoints: {_waypoints}, Unique {_unique}");
        GUILayout.Label($"Path: {_path}");
    }

    void OnDestroy()
    {
        if (_f.IsCreated)
            _f.Dispose();
    }
}