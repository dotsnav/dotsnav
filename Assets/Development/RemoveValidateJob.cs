using DotsNav.Navmesh.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
struct RemoveValidateJob : IJob
{
    public NativeArray<NavmeshComponent> Navmesh;
    public DynamicBuffer<DestroyedTriangleElement> Destroyed;
    public NativeList<Entity> ConstraintIds;
    public LctValidator.Profile Validation;
    public NativeQueue<Entity> ToRemove;

    public unsafe void Execute()
    {
        var navmesh = Navmesh[0];
        var validator = new LctValidator(navmesh.Navmesh, Allocator.Temp);

        var vertices = new NativeList<float2>(Allocator.Temp);
        var amounts = new NativeList<int>(Allocator.Temp);
        var entities = new NativeList<Entity>(Allocator.Temp);
        var bufferEntities = new NativeArray<Entity>(0, Allocator.Temp);
        var blobEntities = new NativeArray<Entity>(0, Allocator.Temp);

        for (int i = 0; i < ConstraintIds.Length; i++)
        {
            ToRemove.Enqueue(ConstraintIds[i]);
            // todo fix test
            // navmesh.Navmesh->Update(vertices, amounts, entities, ToRemove, Destroyed, default, default, bufferEntities, default, blobEntities);
            validator.Validate(navmesh.Navmesh, Validation);
        }

        Navmesh[0] = navmesh;

        if (validator.ClearanceCalculated > 0)
            Debug.Log($"Clearance calculated: {validator.ClearanceCalculated}");
        validator.Dispose();
    }
}