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
    public class RVOSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var invTimeStep = 1 / Time.DeltaTime;
            var localToWorldLookup = GetComponentDataFromEntity<LocalToWorld>(true);

            Entities
                .WithBurst()
                .WithReadOnly(localToWorldLookup)
                .ForEach((Translation translation, RadiusComponent radius, VelocityComponent velocity, DynamicTreeElementComponent dynamicTree, ref VelocityObstacleComponent obstacle) =>
                {
                    var transform = math.inverse(localToWorldLookup[dynamicTree.Tree].Value);
                    obstacle.Position = math.transform(transform, translation.Value).xz;
                    obstacle.Velocity = velocity.Value;
                    obstacle.Radius = radius.Value;
                    obstacle.Priority = radius.Priority;
                })
                .ScheduleParallel();

            var velocityObstacleLookup = GetComponentDataFromEntity<VelocityObstacleComponent>(true);
            var obstacleTreeLookup = GetComponentDataFromEntity<ObstacleTreeComponent>(true);

            Entities
                .WithBurst()
                .WithReadOnly(velocityObstacleLookup)
                .WithReadOnly(obstacleTreeLookup)
                .WithReadOnly(localToWorldLookup)
                .ForEach((
                    Entity entity,
                    ref VelocityComponent velocity,
                    in Translation translation,
                    in RadiusComponent radius,
                    in DynamicTreeElementComponent agentTree,
                    in ObstacleTreeAgentComponent obstacleTree,
                    in RVOSettingsComponent agent,
                    in PreferredVelocityComponent preferredVelocity,
                    in MaxSpeedComponent maxSpeed
                ) =>
                {
                    Assert.IsTrue(agentTree.Tree == obstacleTree.Tree);
                    var ltw = localToWorldLookup[agentTree.Tree].Value;
                    var inv = math.inverse(ltw);
                    var pos = math.transform(inv, translation.Value).xz;
                    var neighbours = ComputeNeighbours(agent, agentTree, pos, entity, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(16, Allocator.Temp);
                    var obstacleDist = agent.TimeHorizonObst * maxSpeed.Value + radius.Value;
                    var ext = obstacleDist / 2;
                    var aabb = new AABB { LowerBound = pos - ext, UpperBound = pos + ext };
                    obstacleTreeLookup[obstacleTree.Tree].TreeRef.Query(new ObstacleCollector(pos, obstacleDist, obstacleNeighbours), aabb);
                    RVO.ComputeNewVelocity(agent, pos, radius.Value, neighbours, obstacleNeighbours, invTimeStep, preferredVelocity.Value, velocity.Value, maxSpeed.Value, ref velocity.Value);
                    velocity.WorldSpace = math.rotate(ltw, velocity.Value.ToXxY());
                })
                .ScheduleParallel();

            Entities
                .WithBurst()
                .WithNone<ObstacleTreeAgentComponent>()
                .WithReadOnly(velocityObstacleLookup)
                .WithReadOnly(localToWorldLookup)
                .ForEach((
                    Entity entity,
                    Translation translation,
                    RadiusComponent radius,
                    DynamicTreeElementComponent agentTree,
                    RVOSettingsComponent agent,
                    PreferredVelocityComponent preferredVelocity,
                    MaxSpeedComponent maxSpeed,
                    ref VelocityComponent velocity
                ) =>
                {
                    var ltw = localToWorldLookup[agentTree.Tree].Value;
                    var inv = math.inverse(ltw);
                    var pos = math.transform(inv, translation.Value).xz;
                    var neighbours = ComputeNeighbours(agent, agentTree, pos, entity, velocityObstacleLookup);
                    var obstacleNeighbours = new NativeList<ObstacleDistance>(0, Allocator.Temp);
                    RVO.ComputeNewVelocity(agent, pos, radius.Value, neighbours, obstacleNeighbours, invTimeStep, preferredVelocity.Value, velocity.Value, maxSpeed.Value, ref velocity.Value);
                    velocity.WorldSpace = math.rotate(ltw, velocity.Value.ToXxY());
                })
                .ScheduleParallel();
        }

        static NativeList<VelocityObstacle> ComputeNeighbours(RVOSettingsComponent agent, DynamicTreeElementComponent agentTree, float2 pos, Entity entity, ComponentDataFromEntity<VelocityObstacleComponent> velocityObstacleLookup)
        {
            var neighbours = new NativeList<VelocityObstacle>(agent.MaxNeighbours, Allocator.Temp);
            var ext = agent.NeighbourDist / 2;
            var aabb = new AABB { LowerBound = pos - ext, UpperBound = pos + ext };
            agentTree.Query(new VelocityObstacleCollector(pos, entity, agent.NeighbourDist, agent.MaxNeighbours, neighbours, velocityObstacleLookup), aabb);
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
                var obstacle = (Obstacle*)node;
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
            Entity _entity;

            public VelocityObstacleCollector(float2 position,Entity entity, float range, int maxResults, NativeList<VelocityObstacle> neighbours, ComponentDataFromEntity<VelocityObstacleComponent> velocityObstacleLookup)
            {
                _position = position;
                _entity = entity;
                _maxResults = maxResults;
                _neighbours = neighbours;
                _velocityObstacleLookup = velocityObstacleLookup;
                _rangeSq = Math.Square(range);
            }

            public bool QueryCallback(Entity node)
            {
                if (_velocityObstacleLookup.HasComponent(_entity) && _velocityObstacleLookup.HasComponent(node))
                {
                    var selfVelocityObstacle = _velocityObstacleLookup[_entity];
                    var velocityObstacle = _velocityObstacleLookup[node];
                    if (selfVelocityObstacle.Priority <= velocityObstacle.Priority)
                    {
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
                    }
                }

                return true;
            }
        }
    }

    static class BringYourOwnDelegate
    {
        // Declare the delegate that takes 12 parameters. T0 is used for the Entity argument
        [Unity.Entities.CodeGeneratedJobForEach.EntitiesForEachCompatible]
        public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5,
             in T6 t6, in T7 t7, in T8 t8);

        // Declare the function overload
        public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8>
            (this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8> codeToRun)
            where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
            LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();
    }
}