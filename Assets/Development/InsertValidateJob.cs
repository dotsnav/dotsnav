using DotsNav;
using DotsNav.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct InsertValidateJob : IJob
{
    public NativeArray<Navmesh> Navmesh;
    public DynamicBuffer<DestroyedTriangleElement> Destroyed;
    public NativeList<float2> Points;
    public NativeList<int> Amounts;
    public NativeList<Entity> ObstacleEntities;
    public LctValidator.Profile Validation;
    public NativeQueue<Entity> ToRemove;

    public void Execute()
    {
        var navmesh = Navmesh[0];
        var validator = new LctValidator(ref navmesh, Allocator.Temp);

        var vertices = new NativeList<float2>(Allocator.Temp);
        var amounts = new NativeList<int>(Allocator.Temp);
        var entities = new NativeList<Entity>(Allocator.Temp);
        var bufferEntities = new NativeArray<Entity>(0, Allocator.Temp);
        var blobEntities = new NativeArray<Entity>(0, Allocator.Temp);

        var start = 0;
        for (int i = 0; i < Amounts.Length; i++)
        {
            var amount = Amounts[i];
            var end = start + amount;
            for (int j = start; j < end; j++)
                vertices.Add(Points[j]);
            amounts.Add(amount);
            entities.Add(ObstacleEntities[i]);
            start += amount;

            navmesh.Update(vertices, amounts, entities, ToRemove, Destroyed, default, default, bufferEntities, default, blobEntities);
            validator.Validate(ref navmesh, Validation);

            vertices.Clear();
            amounts.Clear();
            entities.Clear();
        }

        Navmesh[0] = navmesh;

        if (validator.ClearanceCalculated > 0)
            Debug.Log($"Clearance calculated: {validator.ClearanceCalculated}");
        validator.Dispose();
    }
}