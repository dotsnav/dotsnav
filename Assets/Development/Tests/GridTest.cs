using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

class GridTest : MonoBehaviour
{
    void Start()
    {
        var g = new Grid<Foo>(5, 3, Allocator.Persistent);
        g.Add(new Foo {Position = 2});
        g.Add(new Foo {Position = 2.5f});
        var r = new NativeList<Grid<Foo>.Result>(Allocator.Persistent);
        g.GetClosest(3.5f, 5, r, 10);
        for (int i = 0; i < r.Length; i++) 
            Debug.Log(r[i].Item.Position);

        g.Dispose();
        r.Dispose();
    }

    struct Foo : Grid<Foo>.IElement<Foo>
    {
        public float2 Position { get; set; }
        public bool Equals(Foo other) => math.all(Position == other.Position);
    }
}