using System.Collections.Generic;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.Navmesh.Data;
using DotsNav.Navmesh.Hybrid;
using DotsNav.PathFinding.Data;
using DotsNav.PathFinding.Systems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.PathFinding.Hybrid
{
    [UpdateAfter(typeof(PlaneConversionSystem))]
    class AgentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavAgent agent, DotsNavPathFindingAgent pathFindingAgent) =>
            {
                var entity = GetPrimaryEntity(pathFindingAgent);
                DstEntityManager.AddComponentData(entity, new PathQueryComponent {State = PathQueryState.Inactive});
                DstEntityManager.AddComponentData(entity, new DirectionComponent());
                DstEntityManager.AddBuffer<PathSegmentElement>(entity);
                DstEntityManager.AddBuffer<TriangleElement>(entity);
                DstEntityManager.AddComponentData(entity, new AgentDrawComponent {Draw = true});
                DstEntityManager.AddComponentData(entity, new NavmeshAgentComponent {Navmesh = agent.Plane.Entity});
            });
        }
    }

    /// <summary>
    /// Triggers path queries and provides access to their results
    /// </summary>
    [RequireComponent(typeof(DotsNavAgent))]
    public class DotsNavPathFindingAgent : MonoBehaviour
    {
        /// <summary>
        /// The path segments making up the current path
        /// </summary>
        public readonly List<PathSegmentElement> Segments = new List<PathSegmentElement>();

        /// <summary>
        /// The direction needed to follow the path
        /// </summary>
        public Vector2 Direction;

        /// <summary>
        /// The state of the agent. Use FindPath and Deactivate to update.
        /// </summary>
        public PathQueryState State { get; internal set; } = PathQueryState.Inactive;

        /// <summary>
        /// Increased each time a path is computed successfully. The path found can be identical to the previous path
        /// </summary>
        public int Version { get; internal set; }

        /// <summary>
        /// The starting position used when finding paths. Use FindPath to update.
        /// </summary>
        public Vector3 Start { get; private set; }

        /// <summary>
        /// The destination used when finding paths. Use FindPath to update.
        /// </summary>
        public Vector3 Goal { get; private set; }

        /// <summary>
        /// DebugDisplay will draw this agent's path when set to true
        /// </summary>
        [Header("Debug")]
        public bool DrawGizmos = true;
        public bool DrawPath = true;

        /// <summary>
        /// DebugDisplay draws circle sectors at corners when drawing this agent's path
        /// </summary>
        public bool DrawCorners;

        /// <summary>
        /// Color used by DebugDisplay when drawing this agent's path
        /// </summary>
        public Color DrawColor = Color.black;

        /// <summary>
        /// No more paths will be computed for this agent until FindPath is called
        /// </summary>
        public void Deactivate() => State = PathQueryState.Inactive;

        /// <summary>
        /// Set goal and activate agent. Start is set to the agent's current position.
        /// </summary>
        public void FindPath(Vector3 goal) => FindPath(transform.position, goal);

        /// <summary>
        /// Set start and goal and activate agent
        /// </summary>
        public void FindPath(Vector3 start, Vector3 goal)
        {
            Start = start;
            Goal = goal;
            State = PathQueryState.Pending;
        }

        /// <summary>
        /// Recalculate path using agent's current position and goal
        /// </summary>
        void FindPath() => FindPath(Goal);

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Application.isPlaying || !DrawGizmos)
                return;
            var center = transform.position;
            center.y = 0;
            UnityEditor.Handles.color = DrawColor;
            const int res = 24;
            var q = Quaternion.AngleAxis(360f / res, Vector3.up);
            var currentArm = new Vector3(0, 0, GetComponent<DotsNavAgent>().Radius);

            for (var i = 0; i < res; i++)
            {
                var nextArm = q * currentArm;
                UnityEditor.Handles.DrawLine(center + currentArm, center + nextArm);
                currentArm = nextArm;
            }
        }
#endif
    }
}

