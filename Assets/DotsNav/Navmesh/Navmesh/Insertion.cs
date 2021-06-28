using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Navmesh
{
    public unsafe partial struct Navmesh
    {
        internal readonly struct Insertion
        {
            public readonly InsertionType Type;
            public readonly Entity Obstacle;
            public readonly float4x4 Ltw;
            public readonly float2* Vertices;
            public readonly int* Amounts;
            public readonly int Amount;

            /// <summary>
            /// Insert
            /// </summary>
            public Insertion(Entity obstacle, float4x4 ltw, float2* vertices, int amount)
            {
                Debug.Log($"insert");
                Type = InsertionType.Insert;
                Obstacle = obstacle;
                Ltw = ltw;
                Vertices = vertices;
                Amount = amount;
                Amounts = default;
            }

            /// <summary>
            /// Bulk Insert
            /// </summary>
            public Insertion(float4x4 ltw, float2* verts, int* amounts, int length)
            {
                Debug.Log($"bulk");

                Type = InsertionType.BulkInsert;
                Obstacle = default;
                Ltw = ltw;
                Vertices = verts;
                Amount = length;
                Amounts = amounts;
            }
        }

        internal enum InsertionType
        {
            Insert,
            BulkInsert,
        }
    }
}