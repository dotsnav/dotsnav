using System.Collections.Generic;
using DotsNav.Core;
using DotsNav.Data;
using DotsNav.Hybrid;
using DotsNav.PathFinding.Data;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace DotsNav.PathFinding.Hybrid
{
    class AgentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((DotsNavAgent agent) =>
            {
                var entity = GetPrimaryEntity(agent);
                agent.World = DstEntityManager.World;
                agent.Entity = entity;
                Assert.IsTrue(agent.Radius > 0, "Radius must be larger than 0");

                DstEntityManager.AddComponentData(entity, new LCTPathFindingComponent {State = AgentState.Inactive});
                DstEntityManager.AddComponentData(entity, new RadiusComponent {Value = agent.Radius});
                DstEntityManager.AddComponentData(entity, new DirectionComponent());
                DstEntityManager.AddBuffer<PathSegmentElement>(entity);
                DstEntityManager.AddBuffer<TriangleElement>(entity);
                DstEntityManager.AddComponentData(entity, new AgentDrawComponent {Draw = true});
            });
        }
    }

    /// <summary>
    /// Triggers path queries and provides access to their results
    /// </summary>
    public class DotsNavAgent : EntityLifetimeBehaviour
    {
        /// <summary>
        /// The radius used when finding paths. Call FindPath to trigger calculating a new path
        /// </summary>
        public float Radius = .5f;

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
        public AgentState State { get; internal set; } = AgentState.Inactive;

        /// <summary>
        /// Increased each time a path is computed successfully. The path found can be identical to the previous path
        /// </summary>
        public int Version { get; internal set; }

        /// <summary>
        /// The starting position used when finding paths. Use FindPath to update.
        /// </summary>
        public Vector2 Start { get; private set; }

        /// <summary>
        /// The destination used when finding paths. Use FindPath to update.
        /// </summary>
        public Vector2 Goal { get; private set; }

        /// <summary>
        /// DebugDisplay will draw this agent's path when set to true
        /// </summary>
        [Header("Debug")]
        public bool DrawGizmos = true;
        public bool DrawPath = true;

        /// <summary>
        /// DebugDisplay draws circle sectors at corners when drawing this agent's path
        /// </summary>
        [FormerlySerializedAs("Delimit")]
        public bool DrawCorners;

        /// <summary>
        /// Color used by DebugDisplay when drawing this agent's path
        /// </summary>
        public Color DrawColor = Color.black;

        /// <summary>
        /// No more paths will be computed for this agent until FindPath is called
        /// </summary>
        public void Deactivate() => State = AgentState.Inactive;

        /// <summary>
        /// Set goal and activate agent. Start is set to the agent's current position.
        /// </summary>
        public void FindPath(Vector2 goal) => FindPath(((float3) transform.position).xz, goal);

        /// <summary>
        /// Set start and goal and activate agent
        /// </summary>
        public void FindPath(Vector2 start, Vector2 goal)
        {
            Start = start;
            Goal = goal;
            State = AgentState.Pending;
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
            var currentArm = new Vector3(0, 0, Radius);

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

