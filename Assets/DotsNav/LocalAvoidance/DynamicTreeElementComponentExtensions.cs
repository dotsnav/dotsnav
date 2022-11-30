using DotsNav.BVH;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    public static class DynamicTreeElementComponentExtensions
    {
        public static NativeList<Entity> ComputeNeighbours(this DynamicTreeElementComponent treeElement, float neighbourDist,
            float2 pos, ComponentLookup<VelocityObstacleComponent> velocityObstacleLookup)
        {
            var neighbours = new NativeList<Entity>(Allocator.Temp);
            var ext = neighbourDist / 2;
            var aabb = new AABB {LowerBound = pos - ext, UpperBound = pos + ext};
            treeElement.Query(new DistanceCollector(pos, neighbourDist, neighbours, velocityObstacleLookup), aabb);
            return neighbours;
        }

        struct DistanceCollector : IQueryResultCollector<Entity>
        {
            readonly float2 _position;
            NativeList<Entity> _neighbours;
            readonly ComponentLookup<VelocityObstacleComponent> _velocityObstacleLookup;
            readonly float _rangeSq;

            public DistanceCollector(float2 position, float range, NativeList<Entity> neighbours,
                                     ComponentLookup<VelocityObstacleComponent> velocityObstacleLookup)
            {
                _position = position;
                _neighbours = neighbours;
                _velocityObstacleLookup = velocityObstacleLookup;
                _rangeSq = Math.Square(range);
            }

            public bool QueryCallback(Entity node)
            {
                var velocityObstacle = _velocityObstacleLookup[node];
                var distSq = math.lengthsq(_position - velocityObstacle.Position);
                if (distSq < _rangeSq) 
                    _neighbours.Add(node);

                return true;
            }
        }
    }
}