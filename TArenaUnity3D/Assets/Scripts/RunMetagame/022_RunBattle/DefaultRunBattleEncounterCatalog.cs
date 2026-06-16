using System.Collections.Generic;

public class DefaultRunBattleEncounterCatalog : IRunBattleEncounterSource
{
    private readonly List<RunBattleEncounterDefinition> encounters = new List<RunBattleEncounterDefinition>
    {
        new RunBattleEncounterDefinition(
            "enc-iron-border-clash",
            "n1",
            RunBattleNodeType.Battle,
            "Border Clash",
            "Low",
            1450,
            "enemy-build-iron-border-clash",
            RunBattleEnemyGoal.TryToWin),
        new RunBattleEncounterDefinition(
            "enc-iron-hill-ambush",
            "n4",
            RunBattleNodeType.Battle,
            "Hill Ambush",
            "Medium",
            2050,
            "enemy-build-iron-hill-ambush",
            RunBattleEnemyGoal.DealMaximumLosses),
        new RunBattleEncounterDefinition(
            "enc-final-proof",
            "n5",
            RunBattleNodeType.Final,
            "Final Proof",
            "High",
            2650,
            "enemy-build-final-proof",
            RunBattleEnemyGoal.TryToWin)
    };

    public RunBattleEncounterDefinition FindEncounter(string routeNodeId, string encounterId)
    {
        for (int i = 0; i < encounters.Count; i++)
        {
            RunBattleEncounterDefinition encounter = encounters[i];
            if (encounter == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(encounterId) && encounter.EncounterId == encounterId)
            {
                return encounter;
            }

            if (!string.IsNullOrEmpty(routeNodeId) && encounter.RouteNodeId == routeNodeId)
            {
                return encounter;
            }
        }

        return BuildGeneratedEncounter(routeNodeId, encounterId);
    }

    private static RunBattleEncounterDefinition BuildGeneratedEncounter(string routeNodeId, string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId) || !encounterId.StartsWith("enc-prd35-"))
        {
            return null;
        }

        string riskBand = ResolveRiskBand(encounterId);
        bool isFinal = encounterId.Contains("-final-");
        return new RunBattleEncounterDefinition(
            encounterId,
            string.IsNullOrEmpty(routeNodeId) ? "generated-route-node" : routeNodeId,
            isFinal ? RunBattleNodeType.Final : RunBattleNodeType.Battle,
            isFinal ? "Generated Final Battle" : "Generated Mission Battle",
            riskBand,
            RecommendedValueForRisk(riskBand),
            "enemy-build-" + encounterId,
            riskBand == "High" || riskBand == "Final" ? RunBattleEnemyGoal.DealMaximumLosses : RunBattleEnemyGoal.TryToWin);
    }

    private static string ResolveRiskBand(string encounterId)
    {
        if (encounterId.Contains("-final-"))
        {
            return "Final";
        }

        if (encounterId.Contains("-high-"))
        {
            return "High";
        }

        if (encounterId.Contains("-medium-"))
        {
            return "Medium";
        }

        return "Low";
    }

    private static int RecommendedValueForRisk(string riskBand)
    {
        switch (riskBand)
        {
            case "Final":
                return 2650;
            case "High":
                return 2150;
            case "Medium":
                return 1800;
            default:
                return 1500;
        }
    }
}
