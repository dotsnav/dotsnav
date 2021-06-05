using DotsNav.BVH;
using DotsNav.Data;
using DotsNav.LocalAvoidance.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DotsNav.LocalAvoidance
{
    [UpdateInGroup(typeof(DotsNavSystemGroup))]
    [UpdateAfter(typeof(DynamicTreeSystem))]
    class RVOSystem : SystemBase
    {
        DynamicTreeSystem _treeSystem;

        protected override void OnCreate()
        {
            _treeSystem = World.GetOrCreateSystem<DynamicTreeSystem>();
        }

        protected override void OnUpdate()
        {
            var invTimeStep = 1 / Time.DeltaTime;

            Entities
                .WithBurst()
                .ForEach((Translation translation, DirectionComponent direction, RadiusComponent radius, ref AgentComponent agent, ref VelocityObstacleComponent obstacle) =>
                {
                    agent.PrefVelocity = direction.Value * agent.PrefSpeed;
                    obstacle.Position = translation.Value.xz;
                    obstacle.Velocity = agent.Velocity;
                    obstacle.Radius = radius;
                })
                .ScheduleParallel();

            Dependency = JobHandle.CombineDependencies(Dependency, _treeSystem.OutputDependecy);
            var velocityObstacleLookup = GetComponentDataFromEntity<VelocityObstacleComponent>(true);
            var obstacleTreeLookup = GetComponentDataFromEntity<ObstacleTreeComponent>(true);

            Entities
                .WithBurst()
                .WithReadOnly(velocityObstacleLookup)
                .WithReadOnly(obstacleTreeLookup)
                .ForEach((Translation translation, RadiusComponent radius, DynamicTreeElementComponent agentTree, ObstacleTreeAgentComponent obstacleTree, ref AgentComponent agent) =>
                {
                    var pos = translation.Value.xz;
                    var neighbours = GetNeighbours(agent, agentTree, pos, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(16, Allocator.Temp);
                    var obstacleDist = agent.TimeHorizonObst * agent.MaxSpeed + radius;
                    var ext = obstacleDist / 2;
                    var aabb = new AABB {LowerBound = pos - ext, UpperBound = pos + ext};
                    obstacleTreeLookup[obstacleTree.Tree].TreeRef.Query(new ObstacleCollector(pos, obstacleDist, obstacleNeighbours), aabb);
                    agent.Velocity = RVO.CalculateNewVelocity(agent, pos, radius, neighbours, obstacleNeighbours, invTimeStep);
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ObstacleTreeAgentComponent>()
                .WithReadOnly(velocityObstacleLookup)
                .ForEach((Translation translation, RadiusComponent radius, DynamicTreeElementComponent agentTree, ref AgentComponent agent) =>
                {
                    var pos = translation.Value.xz;
                    var neighbours = GetNeighbours(agent, agentTree, pos, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(0, Allocator.Temp);
                    agent.Velocity = RVO.CalculateNewVelocity(agent, pos, radius, neighbours, obstacleNeighbours, invTimeStep);
                })
                .ScheduleParallel();
        }

        static NativeList<VelocityObstacle> GetNeighbours(AgentComponent agent, DynamicTreeElementComponent agentTree, float2 pos, ComponentDataFromEntity<VelocityObstacleComponent> velocityObstacleLookup)
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
            readonly ComponentDataFromEntity<VelocityObstacleComponent> _velocityObstacleLookup;
            float _rangeSq;

            public VelocityObstacleCollector(float2 position, float range, int maxResults, NativeList<VelocityObstacle> neighbours, ComponentDataFromEntity<VelocityObstacleComponent> velocityObstacleLookup)
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
                        _rangeSq = _neighbours[_neighbours.Length - 1].Dist;
                }

                return true;
            }
        }
    }
}