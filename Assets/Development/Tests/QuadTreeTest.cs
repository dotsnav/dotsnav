// using Unity.Collections;
// using Unity.Mathematics;
// using UnityEngine;
// using Random = Unity.Mathematics.Random;
//
//  class QuadTreeTest : MonoBehaviour
// {
//     public int Amount;
//     public int Size;
//     QuadTree<Foo> _qt;
//     ChunkedStore<Foo> _storage;
//     public Transform Target;
//     public Transform Marker;
//
//     void Start()
//     {
//         var r = new Random(1);
//         _qt = new QuadTree<Foo>(Size, 10, 10, Allocator.Persistent);
//         _storage = new ChunkedStore<Foo>(100, Allocator.Persistent);
//         for (int i = 0; i < Amount; i++)
//         {
//             unsafe
//             {
//                 var go = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
//                 go.localScale = new Vector3(.1f, .1f, .1f);
//                 var p = r.NextFloat2(Size);
//                 go.position = p.ToX0Y();
//                 var foo = _storage.Set(new Foo{Point = p});
//                 _qt.Insert(foo);
//             }
//         }
//     }
//
//     void Update()
//     {
//         unsafe
//         {
//             var p = _qt.FindClosest(Target.position.TakeXZ())->Point;
//             Marker.position = p.ToX0Y();
//         }
//     }
//
//     void OnDestroy()
//     {
//         _qt.Dispose();
//         _storage.Dispose();
//     }
// }
//
// struct Foo : IPoint, IRecyclable
// {
//     public float2 Point { get; set; }
//     public int RecycleId { get; set; }
// }