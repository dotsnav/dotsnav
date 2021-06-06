using DotsNav.Data;
using DotsNav.PathFinding.Data;
using DotsNav.PathFinding.Hybrid;
using DotsNav.Systems;
using Unity.Entities;
using Unity.Transforms;

namespace DotsNav.PathFinding.Systems
{
    [UpdateInGroup(typeof(DotsNavSystemGroup), OrderFirst = true)]
    class PathFinderHybridReadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((DotsNavPathFinder pathFinder, ref PathFinderComponent d) =>
                {
                    d.RecalculateFlags = pathFinder.GetRecalculateFlags();
                })
                .Run();

            Entities
                .WithoutBurst()
                .ForEach((DotsNavPathFindingAgent hybrid, ref PathQueryComponent agent, ref RadiusComponent radius, ref Translation translation, ref AgentDrawComponent drawData) =>
                {
                    agent.State = hybrid.State;
                    agent.From = hybrid.Start;
                    agent.To = hybrid.Goal;
                    radius.Value = hybrid.Radius;
                    var pos = hybrid.transform.position;
                    pos.y = 0;
                    translation.Value = pos;

                    drawData.Draw = hybrid.DrawPath;
                    drawData.Delimit = hybrid.DrawCorners;
                    drawData.Color = hybrid.DrawColor;
                })
                .Run();
         }
    }
}