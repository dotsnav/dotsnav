using System;
using DotsNav.BVH;
using DotsNav.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    unsafe struct ObstacleTree : IEquatable<ObstacleTree>
    {
        struct Control
        {
            public Tree<IntPtr> Tree;
            public UnsafeHashMap<Entity, IntPtr> Map;
            public BlockPool<Obstacle> ObstaclePool;
            public readonly Allocator Allocator;

            public Control(Allocator allocator)
            {
                Allocator = allocator;
                Tree = new Tree<IntPtr>(allocator);
                ObstaclePool = new BlockPool<Obstacle>(128, 1, allocator);
                Map = new UnsafeHashMap<Entity, IntPtr>(128, allocator);
            }
        }

        Control* _control;
        ref Tree<IntPtr> Tree => ref _control->Tree;
        ref UnsafeHashMap<Entity, IntPtr> Map => ref _control->Map;
        ref BlockPool<Obstacle> ObstaclePool => ref _control->ObstaclePool;

        public bool IsCreated => _control != null;

        public ObstacleTree(Allocator allocator)
        {
            _control = (Control*) Mem.Malloc<Control>(allocator);
            *_control = new Control(allocator);
        }

        public void Dispose()
        {
            Tree.Dispose();
            ObstaclePool.Dispose();
            Map.Dispose();
            UnsafeUtility.Free(_control, _control->Allocator);
            _control = null;
        }

        public void InsertObstacle(Entity key, float4x4 ltw, float2* vertices, int amount)
        {
            Assert.IsTrue(amount > 1);
            Assert.IsTrue(key != Entity.Null);
            Assert.IsTrue(!Map.ContainsKey(key));
            Assert.IsTrue(math.all(vertices[0] == vertices[amount - 1]));

            Obstacle* first = default;
            Obstacle* previous = default;

            var prevPos = Math.Mul2D(ltw, vertices[0]);
            var prevPos2 = Math.Mul2D(ltw, vertices[amount - 2]);
            for (var i = 0; i < amount - 1; ++i)
            {
                var obstacle = ObstaclePool.GetElementPointer();
                obstacle->Point = prevPos;
                var pos = Math.Mul2D(ltw, vertices[i + 1]);
                var aabb = AABB.FromOpposingCorners(prevPos, pos);
                obstacle->Id = Tree.Insert(aabb, (IntPtr) obstacle);
                obstacle->Direction = math.normalize(pos - prevPos);

                if (amount == 2)
                    obstacle->Convex = true;
                else
                    obstacle->Convex = LeftOf(prevPos2, prevPos, pos) >= 0;

                prevPos2 = prevPos;
                prevPos = pos;

                if (i == 0)
                {
                    first = obstacle;
                    previous = obstacle;
                }
                else if (i < amount - 2)
                {
                    obstacle->Previous = previous;
                    previous->Next = obstacle;
                    previous = obstacle;
                }
                else
                {
                    obstacle->Previous = previous;
                    previous->Next = obstacle;
                    first->Previous = obstacle;
                    obstacle->Next = first;
                }
            }

            Map.Add(key, (IntPtr) first);

            static float LeftOf(float2 a, float2 b, float2 c) => Determinant(a - c, b - a);
            static float Determinant(float2 vector1, float2 vector2) => vector1.x * vector2.y - vector1.y * vector2.x;
        }

        public void RemoveObstacle(Entity key)
        {
            var first = (Obstacle*) Map[key];
            Map.Remove(key);
            var current = first;
            do
            {
                Tree.Remove(current->Id);
                current = current->Next;
            } while (current != first);
        }

        public void Query<T>(T collector, AABB aabb) where T : IQueryResultCollector<IntPtr>
        {
            Tree.Query(collector, aabb);
        }

        public bool Equals(ObstacleTree other) => _control == other._control;

        public override int GetHashCode() => _control == null ? 0 : ((ulong) _control).GetHashCode();
    }
}