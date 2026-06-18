using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public static class RunBattleTacticalResultBridge
{
    private const string TacticalResultSource = "legacy-tactical-battle-scene";

    private static bool resultReported;

    public static bool ReportBattleFinished(bool playerWon, HexMap hexMap)
    {
        if (resultReported)
        {
            return true;
        }

        RunBattleLaunchViewData preparedBattle = LoadLatestPreparedBattle();
        if (preparedBattle == null)
        {
            Debug.LogWarning("[RunBattleTacticalResultBridge] No prepared run battle was found for tactical result.");
            return false;
        }

        RunBattleArmySnapshot armyAfterBattle = BuildPlayerArmyAfterBattle(preparedBattle.CurrentArmy, hexMap, playerWon);
        RunBattleCompletionResult completion = OfflineModeDatabaseComposition.CreateRunBattleAdapter().CompleteBattle(
            new RunBattleCompletionPayload(
                preparedBattle.RunBattleId,
                playerWon ? RunBattleOutcome.Win : RunBattleOutcome.Loss,
                armyAfterBattle,
                0,
                "tactical-completion-" + Guid.NewGuid().ToString("N"),
                TacticalResultSource));

        if (completion == null || !completion.Success)
        {
            Debug.LogWarning("[RunBattleTacticalResultBridge] Run battle completion failed: " + (completion == null ? "null result" : completion.Message));
            return false;
        }

        resultReported = true;
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("[RunBattleTacticalResultBridge] GameSceneManager is missing; tactical result was persisted but screen routing could not run.");
            return true;
        }

        if (playerWon)
        {
            GameSceneManager.Instance.ReturnFromBattleWon();
        }
        else
        {
            GameSceneManager.Instance.ReturnFromBattleLost();
        }

        return true;
    }

    public static void ResetRuntimeResultLatch()
    {
        resultReported = false;
    }

    private static RunBattleLaunchViewData LoadLatestPreparedBattle()
    {
        string runBattleId = LoadLatestPreparedBattleId();
        if (string.IsNullOrEmpty(runBattleId))
        {
            return null;
        }

        IRunBattleEncounterSource encounterSource = new OfflineRunBattleEncounterCatalog(null, new DefaultRunBattleEncounterCatalog());
        IOfflineArmySnapshotCatalogResolver resolver = new DataMapperOfflineArmySnapshotCatalogResolver(DataMapper.Instance);
        OfflineRunBattleDbStore store = new OfflineRunBattleDbStore(null, resolver, encounterSource, new RewardMapDataMapperUnitSource(DataMapper.Instance));
        return store.FindPreparedBattle(runBattleId);
    }

    private static string LoadLatestPreparedBattleId()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection())
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT run_battle_id
FROM run_battles
WHERE is_active = 1
  AND battle_status_id = @preparedStatus
ORDER BY prepared_at_utc DESC, run_battle_id DESC
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@preparedStatus", (int)DBBattleStatusId.Prepared));
            int runBattleId = OfflineDatabaseSql.ReadInt(result);
            return runBattleId <= 0 ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(runBattleId);
        }
    }

    private static RunBattleArmySnapshot BuildPlayerArmyAfterBattle(RunBattleArmySnapshot beforeBattle, HexMap hexMap, bool playerWon)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        List<TosterHexUnit> playerUnits = GetPlayerUnits(hexMap);
        bool[] usedUnits = new bool[playerUnits.Count];
        int totalValue = 0;

        for (int i = 0; beforeBattle != null && beforeBattle.Stacks != null && i < beforeBattle.Stacks.Count; i++)
        {
            RunBattleStackSnapshot beforeStack = beforeBattle.Stacks[i];
            if (beforeStack == null)
            {
                continue;
            }

            int amountAfter = playerWon ? FindRemainingAmount(beforeStack.UnitId, playerUnits, usedUnits) : 0;
            int unitValue = beforeStack.Amount <= 0 ? beforeStack.CombatValue : beforeStack.CombatValue / Math.Max(1, beforeStack.Amount);
            int combatValue = Math.Max(0, amountAfter * Math.Max(0, unitValue));
            totalValue += combatValue;
            stacks.Add(new RunBattleStackSnapshot(
                beforeStack.StackId,
                beforeStack.UnitId,
                beforeStack.DisplayName,
                beforeStack.Tier,
                beforeStack.Level,
                amountAfter,
                Math.Max(0, beforeStack.Amount - amountAfter),
                combatValue,
                CloneSkills(beforeStack.Skills)));
        }

        return new RunBattleArmySnapshot(beforeBattle == null ? string.Empty : beforeBattle.SnapshotId, totalValue, stacks);
    }

    private static List<TosterHexUnit> GetPlayerUnits(HexMap hexMap)
    {
        List<TosterHexUnit> result = new List<TosterHexUnit>();
        if (hexMap == null || hexMap.Teams == null || hexMap.Teams.Count == 0 || hexMap.Teams[0] == null || hexMap.Teams[0].Tosters == null)
        {
            return result;
        }

        for (int i = 0; i < hexMap.Teams[0].Tosters.Count; i++)
        {
            TosterHexUnit unit = hexMap.Teams[0].Tosters[i];
            if (unit != null)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private static int FindRemainingAmount(string unitId, List<TosterHexUnit> playerUnits, bool[] usedUnits)
    {
        for (int i = 0; i < playerUnits.Count; i++)
        {
            TosterHexUnit unit = playerUnits[i];
            if (usedUnits[i] || unit == null || unit.Name != unitId)
            {
                continue;
            }

            usedUnits[i] = true;
            return unit.isDead ? 0 : Math.Max(0, unit.Amount);
        }

        return 0;
    }

    private static List<RunBattleSkillState> CloneSkills(List<RunBattleSkillState> skills)
    {
        List<RunBattleSkillState> result = new List<RunBattleSkillState>();
        if (skills == null)
        {
            return result;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            RunBattleSkillState skill = skills[i];
            if (skill != null)
            {
                result.Add(new RunBattleSkillState(skill.SkillId, skill.Unlocked));
            }
        }

        return result;
    }
}
