using DotsNav;
using DotsNav.Collections;
using DotsNav.Data;
using DotsNav.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(DotsNavSystemGroup))]
[UpdateAfter(typeof(AgentTreeSystem))]
class RVOSystem : SystemBase
{
    AgentTreeSystem _treeSystem;

    protected override void OnCreate()
    {
        _treeSystem = World.GetOrCreateSystem<AgentTreeSystem>();
    }

    protected override void OnUpdate()
    {
       var invTimeStep = 1 / Time.DeltaTime;

       Entities
            .WithBurst()
            .ForEach((Translation translation, AgentDirectionComponent agentDirectionComponent, ref Agent agent, ref VelocityObstacleComponent obstacle) =>
            {
                agent.Position = translation.Value.xz;
                agent.PrefVelocity = agentDirectionComponent.Value * agent.PrefSpeed;
                obstacle.Position = agent.Position;
                obstacle.Velocity = agent.Velocity;
                obstacle.Radius = agent.Radius;
            })
            .ScheduleParallel();

        Dependency = JobHandle.CombineDependencies(Dependency, _treeSystem.OutputDependecy);
        var tree = _treeSystem.Tree;
        var velocityObstacleLookup = GetComponentDataFromEntity<VelocityObstacleComponent>(true);

        Entities
            .WithBurst()
            .WithAll<Agent>()
            .WithReadOnly(tree)
            .WithReadOnly(velocityObstacleLookup)
            .ForEach((ref Agent agent) =>
            {
                var neighbours = new NativeList<VelocityObstacle>(agent.MaxNeighbours, Allocator.Temp);
                var ext = agent.NeighbourDist / 2;
                var aabb = new AABB {LowerBound = agent.Position - ext, UpperBound = agent.Position + ext};
                tree.Query(new VelocityObstacleCollector(agent.Position, agent.NeighbourDist, agent.MaxNeighbours, neighbours, velocityObstacleLookup), aabb);
                var obstacleNeighbours = new NativeList<ObstacleDistance>(0, Allocator.Temp);
                var allObstacles = new NativeList<Obstacle>(0, Allocator.Temp);
                agent.Velocity = RVO.CalculateNewVelocity(agent, neighbours, obstacleNeighbours, allObstacles, invTimeStep);
            })
            .ScheduleParallel();
    }

    struct VelocityObstacleCollector : IQueryResultCollector
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

            // neighbour.Dist = distSq;
            //
            // if (neighbourAmount < MaxNeighbours)
            //     neighbours[neighbourAmount++] = neighbour;
            //
            // var i1 = neighbourAmount - 1;
            //
            // while (i1 != 0 && distSq < neighbours[i1 - 1].Dist)
            // {
            //     neighbours[i1] = neighbours[i1 - 1];
            //     --i1;
            // }
            //
            // neighbours[i1] = neighbour;
            //
            // if (neighbourAmount == MaxNeighbours)
            //     _rangeSq = neighbours[neighbourAmount - 1].Dist;
        }
    }
}