using System.Collections.Generic;
using System.Data;
using System.Globalization;

public sealed class OfflineRunContext
{
    public int RunId;
    public int AccountId;
    public int GameModeId;
    public int AuthoritySourceId;
    public int RunStatusId;
    public int RouteMapId;
    public int CurrentNodeId;
    public int CurrentArmySnapshotId;
    public int StartArmySnapshotId;
    public int PreFinalArmySnapshotId;
    public int CurrentRunGold;
    public int StageProgress;
    public int AccountXp;
    public int UnlockedSavedArmySlots;
    public string AccountPlayerId;
    public string StartingArmyTemplateId;
    public string StartingArmyVariantId;
    public string SelectedStartingArmyId;
    public string SelectedRouteChoiceId;
    public string NextScreen;
    public OfflineArmySnapshotRecord CurrentArmySnapshot;
    public OfflineArmySnapshotRecord StartArmySnapshot;
    public OfflineArmySnapshotRecord PreFinalArmySnapshot;

    public string RunIdText
    {
        get { return OfflineDatabaseLegacyIdentity.ToLegacyRunId(RunId); }
    }

    public string CurrentNodeIdText
    {
        get { return OfflineDatabaseLegacyIdentity.ToLegacyRouteNodeId(CurrentNodeId); }
    }

    public string CurrentNodeDatabaseIdText
    {
        get { return CurrentNodeId.ToString(CultureInfo.InvariantCulture); }
    }
}

public sealed class OfflineRunContextDbReader
{
    private readonly string databasePath;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();

    public OfflineRunContextDbReader(string databasePath, IOfflineArmySnapshotCatalogResolver resolver)
    {
        this.databasePath = databasePath;
        this.resolver = resolver;
    }

    public OfflineRunContext LoadLatestActiveRun()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<OfflineRunContext> rows = QueryRunContexts(
                connection,
                @"
SELECT runs.run_id, runs.account_id, runs.route_map_id, runs.current_node_id,
       runs.game_mode_id, runs.authority_source_id, runs.run_status_id,
       runs.starting_army_template_id, runs.starting_army_variant_id, runs.selected_starting_army_id,
       runs.current_army_snapshot_id, runs.start_army_snapshot_id,
       runs.pre_final_army_snapshot_id, runs.current_run_gold, runs.stage_progress,
       runs.selected_route_choice_id, runs.next_screen, accounts.external_account_id, accounts.account_xp, accounts.unlocked_saved_army_slots
FROM offline_runs runs
INNER JOIN offline_accounts accounts ON accounts.account_id = runs.account_id
WHERE runs.is_active = 1
ORDER BY runs.updated_at_utc DESC, runs.run_id DESC
LIMIT 1;");
            return rows.Count == 0 ? null : HydrateSnapshots(connection, rows[0]);
        }
    }

    public OfflineRunContext LoadLatestRunForNextScreen(string nextScreen)
    {
        if (string.IsNullOrEmpty(nextScreen))
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<OfflineRunContext> rows = QueryRunContexts(
                connection,
                @"
SELECT runs.run_id, runs.account_id, runs.route_map_id, runs.current_node_id,
       runs.game_mode_id, runs.authority_source_id, runs.run_status_id,
       runs.starting_army_template_id, runs.starting_army_variant_id, runs.selected_starting_army_id,
       runs.current_army_snapshot_id, runs.start_army_snapshot_id,
       runs.pre_final_army_snapshot_id, runs.current_run_gold, runs.stage_progress,
       runs.selected_route_choice_id, runs.next_screen, accounts.external_account_id, accounts.account_xp, accounts.unlocked_saved_army_slots
FROM offline_runs runs
INNER JOIN offline_accounts accounts ON accounts.account_id = runs.account_id
WHERE runs.is_active = 1
  AND runs.next_screen = @nextScreen
ORDER BY runs.updated_at_utc DESC, runs.run_id DESC
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@nextScreen", nextScreen));
            return rows.Count == 0 ? null : HydrateSnapshots(connection, rows[0]);
        }
    }

    public OfflineRunContext LoadRun(string runIdText)
    {
        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runIdText);
        if (runId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<OfflineRunContext> rows = QueryRunContexts(
                connection,
                @"
SELECT runs.run_id, runs.account_id, runs.route_map_id, runs.current_node_id,
       runs.game_mode_id, runs.authority_source_id, runs.run_status_id,
       runs.starting_army_template_id, runs.starting_army_variant_id, runs.selected_starting_army_id,
       runs.current_army_snapshot_id, runs.start_army_snapshot_id,
       runs.pre_final_army_snapshot_id, runs.current_run_gold, runs.stage_progress,
       runs.selected_route_choice_id, runs.next_screen, accounts.external_account_id, accounts.account_xp, accounts.unlocked_saved_army_slots
FROM offline_runs runs
INNER JOIN offline_accounts accounts ON accounts.account_id = runs.account_id
WHERE runs.run_id = @runId
  AND runs.is_active = 1
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@runId", runId));
            return rows.Count == 0 ? null : HydrateSnapshots(connection, rows[0]);
        }
    }

    public OfflineRunContext LoadLatestRunWithSummary()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<OfflineRunContext> rows = QueryRunContexts(
                connection,
                @"
SELECT runs.run_id, runs.account_id, runs.route_map_id, runs.current_node_id,
       runs.game_mode_id, runs.authority_source_id, runs.run_status_id,
       runs.starting_army_template_id, runs.starting_army_variant_id, runs.selected_starting_army_id,
       runs.current_army_snapshot_id, runs.start_army_snapshot_id,
       runs.pre_final_army_snapshot_id, runs.current_run_gold, runs.stage_progress,
       runs.selected_route_choice_id, runs.next_screen, accounts.external_account_id, accounts.account_xp, accounts.unlocked_saved_army_slots
FROM run_summaries summaries
INNER JOIN offline_runs runs ON runs.run_id = summaries.run_id
INNER JOIN offline_accounts accounts ON accounts.account_id = runs.account_id
WHERE summaries.is_active = 1
  AND runs.is_active = 1
ORDER BY summaries.created_at_utc DESC, summaries.run_summary_id DESC
LIMIT 1;");
            return rows.Count == 0 ? null : HydrateSnapshots(connection, rows[0]);
        }
    }


    public string LoadLatestBattleResultId()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT async_battle_result_id
FROM async_battle_results
WHERE is_active = 1
ORDER BY recorded_at_utc DESC, async_battle_result_id DESC
LIMIT 1;");
            int resultId = OfflineDatabaseSql.ReadInt(result);
            return resultId <= 0 ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacyAsyncBattleResultId(resultId);
        }
    }

    public RewardMapArmySnapshot ToRewardMapCurrentArmy(OfflineRunContext context)
    {
        return context == null || context.CurrentArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToRewardMap(context.CurrentArmySnapshot, resolver);
    }

    public RunShopArmySnapshot ToRunShopCurrentArmy(OfflineRunContext context)
    {
        return context == null || context.CurrentArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToRunShop(context.CurrentArmySnapshot, resolver);
    }

    public RunMapArmySummary ToRunMapCurrentArmy(OfflineRunContext context)
    {
        return context == null || context.CurrentArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToRunMap(context.CurrentArmySnapshot, resolver);
    }

    public RunBattleArmySnapshot ToRunBattleCurrentArmy(OfflineRunContext context)
    {
        return context == null || context.CurrentArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToRunBattle(context.CurrentArmySnapshot, resolver);
    }

    public SummaryValueArmySnapshot ToSummaryValueStartArmy(OfflineRunContext context)
    {
        return context == null || context.StartArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToSummaryValue(context.StartArmySnapshot, resolver);
    }

    public SummaryValueArmySnapshot ToSummaryValuePreFinalArmy(OfflineRunContext context)
    {
        return context == null || context.PreFinalArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToSummaryValue(context.PreFinalArmySnapshot, resolver);
    }

    public SummaryValueArmySnapshot ToSummaryValueCurrentArmy(OfflineRunContext context)
    {
        return context == null || context.CurrentArmySnapshot == null
            ? null
            : OfflineArmySnapshotMapper.ToSummaryValue(context.CurrentArmySnapshot, resolver);
    }

    public CreatedRunRecord ToStartRunCreatedRecord(OfflineRunContext context)
    {
        if (context == null)
        {
            return null;
        }

        return new CreatedRunRecord(
            context.RunIdText,
            ToStartRunGameMode(context.GameModeId),
            ToStartRunAuthoritySource(context.AuthoritySourceId),
            string.IsNullOrEmpty(context.AccountPlayerId) ? "offline-player" : context.AccountPlayerId,
            context.StartingArmyTemplateId,
            context.StartingArmyVariantId,
            context.SelectedStartingArmyId,
            context.SelectedRouteChoiceId,
            context.CurrentRunGold,
            ToRunStatusText(context.RunStatusId),
            context.StartArmySnapshot == null ? null : OfflineArmySnapshotMapper.ToStartRun(context.StartArmySnapshot, resolver));
    }

    private OfflineRunContext HydrateSnapshots(IDbConnection connection, OfflineRunContext context)
    {
        if (context == null)
        {
            return null;
        }

        context.CurrentArmySnapshot = snapshotRepository.LoadSnapshot(connection, context.CurrentArmySnapshotId);
        context.StartArmySnapshot = snapshotRepository.LoadSnapshot(connection, context.StartArmySnapshotId);
        context.PreFinalArmySnapshot = snapshotRepository.LoadSnapshot(connection, context.PreFinalArmySnapshotId);
        return context;
    }

    private static List<OfflineRunContext> QueryRunContexts(
        IDbConnection connection,
        string sql,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        return OfflineDatabaseSql.Query(
            connection,
            sql,
            delegate(IDataRecord row)
            {
                return new OfflineRunContext
                {
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    GameModeId = OfflineDatabaseSql.ReadInt(row["game_mode_id"]),
                    AuthoritySourceId = OfflineDatabaseSql.ReadInt(row["authority_source_id"]),
                    RunStatusId = OfflineDatabaseSql.ReadInt(row["run_status_id"]),
                    RouteMapId = OfflineDatabaseSql.ReadInt(row["route_map_id"]),
                    CurrentNodeId = OfflineDatabaseSql.ReadInt(row["current_node_id"]),
                    CurrentArmySnapshotId = OfflineDatabaseSql.ReadInt(row["current_army_snapshot_id"]),
                    StartArmySnapshotId = OfflineDatabaseSql.ReadInt(row["start_army_snapshot_id"]),
                    PreFinalArmySnapshotId = OfflineDatabaseSql.ReadInt(row["pre_final_army_snapshot_id"]),
                    CurrentRunGold = OfflineDatabaseSql.ReadInt(row["current_run_gold"]),
                    StageProgress = OfflineDatabaseSql.ReadInt(row["stage_progress"]),
                    AccountXp = OfflineDatabaseSql.ReadInt(row["account_xp"]),
                    AccountPlayerId = OfflineDatabaseSql.ReadText(row["external_account_id"]),
                    StartingArmyTemplateId = OfflineDatabaseSql.ReadText(row["starting_army_template_id"]),
                    StartingArmyVariantId = OfflineDatabaseSql.ReadText(row["starting_army_variant_id"]),
                    SelectedStartingArmyId = OfflineDatabaseSql.ReadText(row["selected_starting_army_id"]),
                    SelectedRouteChoiceId = OfflineDatabaseSql.ReadText(row["selected_route_choice_id"]),
                    NextScreen = OfflineDatabaseSql.ReadText(row["next_screen"]),
                    UnlockedSavedArmySlots = OfflineDatabaseSql.ReadInt(row["unlocked_saved_army_slots"], 2)
                };
            },
            null,
            parameters);
    }

    private static StartRunGameMode ToStartRunGameMode(int gameModeId)
    {
        return gameModeId == (int)DBGameModeId.Online ? StartRunGameMode.Online : StartRunGameMode.Offline;
    }

    private static StartRunAuthoritySource ToStartRunAuthoritySource(int authoritySourceId)
    {
        return authoritySourceId == (int)DBAuthoritySourceId.BackendAdapter
            ? StartRunAuthoritySource.BackendAdapter
            : StartRunAuthoritySource.LocalOfflineAdapter;
    }

    private static string ToRunStatusText(int runStatusId)
    {
        switch (runStatusId)
        {
            case (int)DBRunStatusId.Won:
                return "Won";
            case (int)DBRunStatusId.Lost:
                return "Lost";
            case (int)DBRunStatusId.AwaitingBattle:
                return "AwaitingBattle";
            case (int)DBRunStatusId.AwaitingReward:
                return "AwaitingReward";
            case (int)DBRunStatusId.InShop:
                return "InShop";
            default:
                return "active";
        }
    }
}
