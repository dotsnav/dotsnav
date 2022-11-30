using System;
using System.Collections.Generic;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.PathFinding.Data;
using Unity.Entities;
using UnityEngine;

namespace DotsNav.PathFinding.Hybrid
{
    /// <summary>
    /// Triggers path queries and provides access to their results
    /// </summary>
    [RequireComponent(typeof(DotsNavAgent))]
    public class DotsNavPathFindingAgent : MonoBehaviour, IToEntity
    {
        /// <summary>
        /// The path segments making up the current path
        /// </summary>
        public readonly List<PathSegmentElement> Segments = new();

        /// <summary>
        /// The direction needed to follow the path
        /// </summary>
        [NonSerialized]
        public Vector3 Direction;

        /// <summary>
        /// The state of the agent. Use FindPath and Deactivate to update.
        /// </summary>
        public PathQueryState State { get; internal set; } = PathQueryState.Inactive;

        /// <summary>
        /// Increased each time a path is computed successfully. The path found can be identical to the previous path
        /// </summary>
        public int Version { get; internal set; }

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
        /// Set goal and activate agent
        /// </summary>
        public void FindPath(Vector3 goal)
        {
            Goal = goal;
            State = PathQueryState.Pending;
        }

        /// <summary>
        /// Recalculate path using agent's current position and goal
        /// </summary>
        public void FindPath() => FindPath(Goal);

        public void Convert(EntityManager entityManager, Entity entity)
        {
            entityManager.AddComponentData(entity, new PathQueryComponent {State = PathQueryState.Inactive});
            entityManager.AddComponentData(entity, new DirectionComponent());
            entityManager.AddBuffer<PathSegmentElement>(entity);
            entityManager.AddBuffer<TriangleElement>(entity);
            entityManager.AddComponentData(entity, new AgentDrawComponent {Draw = true});
            var agent = GetComponent<DotsNavAgent>();
            entityManager.AddComponentData(entity, new NavmeshAgentComponent {Navmesh = agent.Plane.Entity});
            entityManager.AddComponentObject(entity, this);
        }
        
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

