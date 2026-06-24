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

        if (completion.CompletionRecord != null)
        {
            GameSceneManager.Instance.ReturnFromBattle(completion.CompletionRecord.NextScreen);
        }
        else if (playerWon)
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
        return RunBattleTacticalStackReconciler.BuildPlayerArmyAfterBattle(
            beforeBattle,
            BuildTacticalPlayerStackStates(hexMap),
            playerWon);
    }

    private static List<RunBattleTacticalStackState> BuildTacticalPlayerStackStates(HexMap hexMap)
    {
        List<RunBattleTacticalStackState> result = new List<RunBattleTacticalStackState>();
        List<TosterHexUnit> playerUnits = GetPlayerUnits(hexMap);
        for (int i = 0; i < playerUnits.Count; i++)
        {
            TosterHexUnit unit = playerUnits[i];
            if (unit == null)
            {
                continue;
            }

            result.Add(new RunBattleTacticalStackState(
                string.Empty,
                unit.Name,
                i,
                unit.Amount,
                unit.isDead));
        }

        return result;
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

}
