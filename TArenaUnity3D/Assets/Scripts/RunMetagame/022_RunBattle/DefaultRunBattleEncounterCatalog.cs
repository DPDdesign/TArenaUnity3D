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

        return null;
    }
}
