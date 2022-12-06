using DotsNav.BVH;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNav.LocalAvoidance.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateAfter(typeof(DynamicTreeSystem))]
    [UpdateAfter(typeof(ObstacleTreeSystem))]
    public partial class RVOSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var invTimeStep = 1 / World.Time.DeltaTime;
            var localToWorldLookup = GetComponentLookup<LocalToWorld>(true);

            Entities
                .WithBurst()
                .WithReadOnly(localToWorldLookup)
                .ForEach((LocalTransform translation, RadiusComponent radius, VelocityComponent velocity, DynamicTreeElementComponent dynamicTree, ref VelocityObstacleComponent obstacle) =>
                {
                    var transform = math.inverse(localToWorldLookup[dynamicTree.Tree].Value);
                    obstacle.Position = math.transform(transform, translation.Position).xz;
                    obstacle.Velocity = velocity.Value;
                    obstacle.Radius = radius;
                })
                .ScheduleParallel();

            var velocityObstacleLookup = GetComponentLookup<VelocityObstacleComponent>(true);
            var obstacleTreeLookup = GetComponentLookup<ObstacleTreeComponent>(true);

            Entities
                .WithBurst()
                .WithReadOnly(velocityObstacleLookup)
                .WithReadOnly(obstacleTreeLookup)
                .WithReadOnly(localToWorldLookup)
                .ForEach((LocalTransform translation, RadiusComponent radius, DynamicTreeElementComponent agentTree, ObstacleTreeAgentComponent obstacleTree,
                          RVOSettingsComponent agent, PreferredVelocityComponent preferredVelocity, MaxSpeedComponent maxSpeed, ref VelocityComponent velocity) =>
                {
                    Assert.IsTrue(agentTree.Tree == obstacleTree.Tree);
                    var ltw = localToWorldLookup[agentTree.Tree].Value;
                    var inv = math.inverse(ltw);
                    var pos = math.transform(inv, translation.Position).xz;
                    var neighbours = ComputeNeighbours(agent, agentTree, pos, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(16, Allocator.Temp);
                    var obstacleDist = agent.TimeHorizonObst * maxSpeed.Value + radius;
                    var ext = obstacleDist / 2;
                    var aabb = new AABB {LowerBound = pos - ext, UpperBound = pos + ext};
                    obstacleTreeLookup[obstacleTree.Tree].TreeRef.Query(new ObstacleCollector(pos, obstacleDist, obstacleNeighbours), aabb);
                    RVO.ComputeNewVelocity(agent, pos, radius, neighbours, obstacleNeighbours, invTimeStep, preferredVelocity.Value, velocity.Value, maxSpeed.Value, ref velocity.Value);
                    velocity.WorldSpace = math.rotate(ltw, velocity.Value.ToXxY());
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ObstacleTreeAgentComponent>()
                .WithReadOnly(velocityObstacleLookup)
                .WithReadOnly(localToWorldLookup)
                .ForEach((LocalTransform translation, RadiusComponent radius, DynamicTreeElementComponent agentTree, RVOSettingsComponent agent,
                          PreferredVelocityComponent preferredVelocity, MaxSpeedComponent maxSpeed, ref VelocityComponent velocity) =>
                {
                    var ltw = localToWorldLookup[agentTree.Tree].Value;
                    var inv = math.inverse(ltw);
                    var pos = math.transform(inv, translation.Position).xz;
                    var neighbours = ComputeNeighbours(agent, agentTree, pos, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(0, Allocator.Temp);
                    RVO.ComputeNewVelocity(agent, pos, radius, neighbours, obstacleNeighbours, invTimeStep, preferredVelocity.Value, velocity.Value, maxSpeed.Value, ref velocity.Value);
                    velocity.WorldSpace = math.rotate(ltw, velocity.Value.ToXxY());
                })
                .ScheduleParallel();
        }

        static NativeList<VelocityObstacle> ComputeNeighbours(RVOSettingsComponent agent, DynamicTreeElementComponent agentTree, float2 pos, ComponentLookup<VelocityObstacleComponent> velocityObstacleLookup)
        {
            var neighbours = new NativeList<VelocityObstacle>(agent.MaxNeighbours, Allocator.Temp);
            var ext = agent.NeighbourDist / 2;
            var aabb = new AABB {LowerBound = pos - ext, UpperBound = pos + ext};
            agentTree.Query(new VelocityObstacleCollector(pos, agent.NeighbourDist, agent.MaxNeighbours, neighbours, velocityObstacleLookup), aabb);
            return neighbours;
        }

        unsafe struct ObstacleCollector : IQueryResultCollector<IntPtr>
        {
            readonly float2 _position;
            readonly float _rangeSq;
            NativeList<ObstacleDistance> _neighbours;

            public ObstacleCollector(float2 position, float range, NativeList<ObstacleDistance> neighbours)
            {
                _position = position;
                _rangeSq = range * range;
                _neighbours = neighbours;
            }

            public bool QueryCallback(IntPtr node)
            {
                var obstacle = (Obstacle*) node;
                var nextObstacle = obstacle->Next;
                var distSq = DistSqPointLineSegment(obstacle->Point, nextObstacle->Point, _position);

                if (distSq < _rangeSq)
                {
                    _neighbours.Add(new ObstacleDistance(distSq, obstacle));

                    var i = _neighbours.Length - 1;

                    while (i != 0 && distSq < _neighbours[i - 1].Dist)
                    {
                        _neighbours[i] = _neighbours[i - 1];
                        --i;
                    }
                    _neighbours[i] = new ObstacleDistance(distSq, obstacle);
                }

                return true;
            }

            static float DistSqPointLineSegment(float2 vector1, float2 vector2, float2 vector3)
            {
                var v1 = vector3 - vector1;
                var v2 = vector2 - vector1;
                var r = (v1.x * v2.x + v1.y * v2.y) / math.lengthsq(vector2 - vector1);

                if (r < 0.0f)
                    return math.lengthsq(vector3 - vector1);

                if (r > 1.0f)
                    return math.lengthsq(vector3 - vector2);

                return math.lengthsq(vector3 - (vector1 + r * (vector2 - vector1)));
            }
        }

        struct VelocityObstacleCollector : IQueryResultCollector<Entity>
        {
            readonly float2 _position;
            readonly int _maxResults;
            NativeList<VelocityObstacle> _neighbours;
            readonly ComponentLookup<VelocityObstacleComponent> _velocityObstacleLookup;
            float _rangeSq;

            public VelocityObstacleCollector(float2 position, float range, int maxResults, NativeList<VelocityObstacle> neighbours, ComponentLookup<VelocityObstacleComponent> velocityObstacleLookup)
            {
                _position = position;
                _maxResults = maxResults;
                _neighbours = neighbours;
                _velocityObstacleLookup = velocityObstacleLookup;
                _rangeSq = Math.Square(range);
            }

            public bool QueryCallback(Entity node)
            {
                var velocityObstacle = _velocityObstacleLookup[node];
                var neighbour = new VelocityObstacle(velocityObstacle);
                // todo should probably take in to account neighbour radius here, it could be very large
                var distSq = math.lengthsq(_position - neighbour.Position);

                if (distSq < _rangeSq)
                {
                    neighbour.Dist = distSq;

                    if (_neighbours.Length < _maxResults)
                        _neighbours.Add(neighbour);

                    var i = _neighbours.Length - 1;

                    while (i != 0 && distSq < _neighbours[i - 1].Dist)
                        _neighbours[i] = _neighbours[--i];

                    _neighbours[i] = neighbour;

                    if (_neighbours.Length == _maxResults)
                        _rangeSq = _neighbours[^1].Dist;
                }

                return true;
            }
        }
    }
}