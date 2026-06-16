using System.Collections.Generic;

public class DefaultRunMapPathCatalog : IRunMapPathCatalog
{
    public List<RunMapPathDefinition> BuildPaths(string selectedRouteChoiceId)
    {
        string routeChoice = string.IsNullOrEmpty(selectedRouteChoiceId) ? "route-balanced-frontier" : selectedRouteChoiceId;
        return new List<RunMapPathDefinition>
        {
            Path("path-pressure", routeChoice, "Pressure Path", "More battles, stronger reward hints.",
                Node("node-pressure-1", "path-pressure", RunMapNodeType.Battle, 1, "Border Clash", "Mass or Skill", "Uncertain: medium danger", "enc-iron-border-clash", "node-pressure-2"),
                Node("node-pressure-2", "path-pressure", RunMapNodeType.Battle, 2, "Hill Ambush", "Quality or Recovery", "Uncertain: high losses possible", "enc-iron-hill-ambush", "node-final")),
            Path("path-recovery", routeChoice, "Recovery Path", "Safer route with shop timing.",
                Node("node-recovery-1", "path-recovery", RunMapNodeType.Battle, 1, "Scavenger Guard", "Recovery or RUN GOLD", "Uncertain: low danger", "enc-iron-border-clash", "node-recovery-2"),
                Node("node-recovery-2", "path-recovery", RunMapNodeType.Shop, 2, "Run Shop", "Shop offers", "No battle risk", string.Empty, "node-final")),
            Path("path-pivot", routeChoice, "Pivot Path", "Recruit and reward bias for new roles.",
                Node("node-pivot-1", "path-pivot", RunMapNodeType.RecruitReward, 1, "Recruit Signal", "Width or Skill", "No battle risk", string.Empty, "node-pivot-2"),
                Node("node-pivot-2", "path-pivot", RunMapNodeType.Battle, 2, "Proving Fight", "Pivot reward", "Uncertain: medium danger", "enc-iron-hill-ambush", "node-final")),
            Path("path-final", routeChoice, "Final Proof", "Final node shared by all routes.",
                Node("node-final", "path-final", RunMapNodeType.FinalBoss, 3, "Final Proof", "Saved army eligibility", "Uncertain: boss danger", "enc-final-proof", string.Empty))
        };
    }

    private static RunMapPathDefinition Path(string pathId, string routeChoiceId, string displayName, string bias, params RunMapNodeDefinition[] nodes)
    {
        return new RunMapPathDefinition(pathId, routeChoiceId, displayName, bias, new List<RunMapNodeDefinition>(nodes));
    }

    private static RunMapNodeDefinition Node(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        int stage,
        string displayName,
        string rewardHint,
        string riskHint,
        string encounterId,
        string nextNodeId)
    {
        return new RunMapNodeDefinition(nodeId, pathId, nodeType, stage, displayName, rewardHint, riskHint, encounterId, nextNodeId);
    }
}
