using System;

namespace DotsNav.PathFinding
{
    /// <summary>
    /// Indicates current agent state, set to Pending to trigger a path query
    /// </summary>
    [Flags]
    public enum AgentState
    {
        Inactive = 1,
        Pending = 2,
        StartInvalid = 4,
        GoalInvalid = 8,
        NoPath = 16,
        PathFound = 32,
        Invalidated = 64
    }

    /// <summary>
    /// Indicates which agent states, in addition to Pending, should trigger a path query
    /// </summary>
    [Flags]
    public enum RecalculateFlags
    {
        // Inactive = 1,
        // Pending = 2,
        StartInvalid = 4,
        GoalInvalid = 8,
        NoPath = 16,
        PathFound = 32,
        Invalidated = 64
    }
}