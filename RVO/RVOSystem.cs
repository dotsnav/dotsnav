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
            .ForEach((Translation translation, AgentDirectionComponent agentDirectionComponent, ref Agent agent) =>
            {
                agent.Position = translation.Value.xz;
                agent.PrefVelocity = agentDirectionComponent.Value * agent.PrefSpeed;
            })
            .ScheduleParallel();

        Dependency = JobHandle.CombineDependencies(Dependency, _treeSystem.OutputDependecy);
        var tree = _treeSystem.Tree;
        // var agentLookup = GetComponentDataFromEntity<Agent>(true);
        var agentLookup = GetComponentDataFromEntity<Agent>();

        // todo fix parallel execution
        Entities
            .WithBurst()
            .WithAll<Agent>()
            .WithReadOnly(tree)
            // .WithReadOnly(agentLookup)
            .ForEach((Entity entity) =>
            {
                // todo expose
                const int maxNeighbours = 10;
                var neighbours = new NativeList<VelocityObstacle>(maxNeighbours, Allocator.Temp);
                var agent = agentLookup[entity];
                var ext = agent.NeighbourDist / 2;
                var aabb = new AABB {LowerBound = agent.Position - ext, UpperBound = agent.Position + ext};
                tree.Query(new NearestCollector(agent.Position, agent.NeighbourDist, maxNeighbours, neighbours, agentLookup), aabb);
                var obstacleNeighbours = new NativeList<ObstacleDistance>(0, Allocator.Temp);
                var allObstacles = new NativeList<Obstacle>(0, Allocator.Temp);
                RVO.CalculateNewVelocity(ref agent, neighbours, obstacleNeighbours, allObstacles, invTimeStep, maxNeighbours);
                agentLookup[entity] = agent;
            })
            // .ScheduleParallel();
            .Schedule();

        Entities
            .WithBurst()
            .ForEach((ref Agent agent) =>
            {
                agent.Velocity = agent.NewVelocity;
            })
            .ScheduleParallel();
    }

    struct NearestCollector : IQueryResultCollector
    {
        readonly float2 _position;
        readonly int _maxResults;
        NativeList<VelocityObstacle> _neighbours;
        readonly ComponentDataFromEntity<Agent> _agentLookup;
        float _rangeSq;

        public NearestCollector(float2 position, float range, int maxResults, NativeList<VelocityObstacle> neighbours, ComponentDataFromEntity<Agent> agentLookup)
        {
            _position = position;
            _maxResults = maxResults;
            _neighbours = neighbours;
            _agentLookup = agentLookup;
            _rangeSq = Math.Square(range);
        }

        public bool QueryCallback(Entity node)
        {
            var agent = _agentLookup[node];
            var neighbour = new VelocityObstacle(node, agent.Position, agent.Velocity, agent.Radius);
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