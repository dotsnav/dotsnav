using System;
using DotsNav.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Math = DotsNav.Core.Math;

// todo recycle lists when empty
public readonly unsafe struct Grid<T> where T : struct, Grid<T>.IElement<T>
{
    readonly int2 _gridSize;
    readonly float2 _cellSize;
    readonly Allocator _allocator;
    readonly byte* _lists;
    readonly int _sizeOf;

    public Grid(int2 gridSize, float2 cellSize, Allocator allocator)
    {
        _gridSize = gridSize;
        _cellSize = cellSize;
        _allocator = allocator;
        Assert.IsTrue(math.all(gridSize > 0));
        Assert.IsTrue(math.all(cellSize > 0));
        var cells = gridSize.x * gridSize.y;
        _sizeOf = UnsafeUtility.SizeOf<NativeList<T>>();
        var total = cells * _sizeOf;
        _lists = (byte*) UnsafeUtility.Malloc(total, UnsafeUtility.AlignOf<NativeList<T>>(), allocator);
        UnsafeUtility.MemClear(_lists, total);
    }

    public void Add(T t) => GetOrCreate(t.Position).Add(t);

    NativeList<T> GetOrCreate(float2 pos)
    {
        var id = GetId(pos);
        if (!Get(id, out var l))
        {
            l = new NativeList<T>(_allocator);
            UnsafeUtility.CopyStructureToPtr(ref l, _lists + id * _sizeOf);
        }
        return l;
    }

    int GetId(float2 pos) => GetId((int2) (pos / _cellSize));
    int GetId(int2 cell) => cell.y * _gridSize.x + cell.x;
    bool Get(float2 position, out NativeList<T> items) => Get(GetId(position), out items);
    bool Get(int2 cell, out NativeList<T> items) => Get(GetId(cell), out items);

    bool Get(int id, out NativeList<T> items)
    {
        UnsafeUtility.CopyPtrToStructure(_lists + id * _sizeOf, out items);
        return items.IsCreated;
    }

    public void Remove(T t)
    {
        Get(t.Position, out var l);
        for (int i = 0; i < l.Length; i++)
            if (t.Equals(l[i]))
            {
                l.RemoveAtSwapBack(i);
                return;
            }

        throw new Exception("element not found");
    }

    public void GetClosest(float2 pos, float range, NativeList<Result> items, int maxResults)
    {
        var min = math.max(0, (int2) ((pos - range) / _cellSize));
        var max = math.min(_gridSize - 1, (int2) ((pos + range) / _cellSize));
        var rangeSq = Math.Square(range);

        for (int y = min.y; y <= max.y; y++)
        for (int x = min.x; x <= max.x; x++)
        {
            if (Get(new int2(x, y), out var l))
                for (int i = 0; i < l.Length; i++)
                {
                    var item = new Result {Item = l[i], Dist = math.lengthsq(l[i].Position - pos)};

                    if (item.Dist < rangeSq)
                    {
                        if (items.Length < maxResults)
                            items.Add(item);

                        var i1 = items.Length - 1;

                        while (i1 != 0 && item.Dist < items[i1 - 1].Dist)
                        {
                            items[i1] = items[i1 - 1];
                            --i1;
                        }

                        items[i1] = item;

                        if (items.Length == maxResults)
                            rangeSq = items[items.Length - 1].Dist;
                    }
                }
        }
    }

    public struct Result
    {
        public T Item;
        public float Dist;
    }

    public void Dispose()
    {
        var cells = _gridSize.x * _gridSize.y;
        for (int i = 0; i < cells; i++)
            if (Get(i, out var list))
                list.Dispose();
        UnsafeUtility.Free(_lists, _allocator);
    }

    public interface IElement<T> : IEquatable<T> where T : struct, IElement<T>
    {
        float2 Position { get; }
    }
}