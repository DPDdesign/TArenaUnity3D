using System;
using System.Data;
using System.Globalization;

public sealed class OfflineRunContextDbWriter
{
    public int InsertStartRun(
        IDbConnection connection,
        IDbTransaction transaction,
        CreatedRunRecord record,
        int accountId)
    {
        string now = OfflineDatabaseSql.UtcNowText();
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO offline_runs (
    account_id,
    game_mode_id,
    authority_source_id,
    run_status_id,
    starting_army_template_id,
    starting_army_variant_id,
    selected_starting_army_id,
    selected_route_choice_id,
    current_run_gold,
    stage_progress,
    route_progress,
    run_seed,
    run_seed_version,
    next_screen,
    created_at_utc,
    updated_at_utc,
    is_active
) VALUES (
    @accountId,
    @gameModeId,
    @authoritySourceId,
    @runStatusId,
    @startingArmyTemplateId,
    @startingArmyVariantId,
    @selectedStartingArmyId,
    @selectedRouteChoiceId,
    @currentRunGold,
    0,
    0,
    @runSeed,
    @runSeedVersion,
    @nextScreen,
    @createdAtUtc,
    @updatedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@gameModeId", ToDbGameModeId(record == null ? StartRunGameMode.Offline : record.GameMode)),
            new OfflineDatabaseSqlParameter("@authoritySourceId", ToDbAuthoritySourceId(record == null ? StartRunAuthoritySource.LocalOfflineAdapter : record.AuthoritySource)),
            new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.InProgress),
            new OfflineDatabaseSqlParameter("@startingArmyTemplateId", record == null ? string.Empty : record.StartingArmyTemplateId),
            new OfflineDatabaseSqlParameter("@startingArmyVariantId", record == null ? string.Empty : record.StartingArmyVariantId),
            new OfflineDatabaseSqlParameter("@selectedStartingArmyId", record == null ? string.Empty : record.SelectedStartingArmyId),
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", record == null ? string.Empty : record.RoutePreviewOptionId),
            new OfflineDatabaseSqlParameter("@currentRunGold", record == null ? 0 : record.StartingCurrency),
            new OfflineDatabaseSqlParameter("@runSeed", ParseRunSeed(record == null ? string.Empty : record.RoutePreviewOptionId)),
            new OfflineDatabaseSqlParameter("@runSeedVersion", 1),
            new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));

        return (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
    }

    public void AttachStartRunRouteAndArmy(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int routeMapId,
        int startArmySnapshotId,
        int currentRunGold)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET route_map_id = @routeMapId,
    start_army_snapshot_id = @startArmySnapshotId,
    current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    run_status_id = @runStatusId,
    next_screen = @nextScreen,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId),
            new OfflineDatabaseSqlParameter("@startArmySnapshotId", PositiveIntOrNull(startArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", PositiveIntOrNull(startArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.InProgress),
            new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    public void UpdateRunMapState(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int routeMapId,
        int currentNodeId,
        int currentRunGold,
        int stageProgress,
        int routeProgress,
        int runStatusId,
        string nextScreen)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET route_map_id = @routeMapId,
    current_node_id = @currentNodeId,
    current_run_gold = @currentRunGold,
    stage_progress = @stageProgress,
    route_progress = @routeProgress,
    run_status_id = @runStatusId,
    next_screen = @nextScreen,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId),
            new OfflineDatabaseSqlParameter("@currentNodeId", PositiveIntOrNull(currentNodeId)),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@stageProgress", stageProgress),
            new OfflineDatabaseSqlParameter("@routeProgress", routeProgress),
            new OfflineDatabaseSqlParameter("@runStatusId", runStatusId),
            new OfflineDatabaseSqlParameter("@nextScreen", nextScreen),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    public void UpdateNodeArmyGoldScreen(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int currentNodeId,
        int currentArmySnapshotId,
        int currentRunGold,
        int runStatusId,
        string nextScreen)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET current_node_id = @currentNodeId,
    current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    run_status_id = @runStatusId,
    next_screen = @nextScreen,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@currentNodeId", PositiveIntOrNull(currentNodeId)),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", PositiveIntOrNull(currentArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@runStatusId", runStatusId),
            new OfflineDatabaseSqlParameter("@nextScreen", nextScreen),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    public void UpdateArmyGoldScreen(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int currentArmySnapshotId,
        int currentRunGold,
        int runStatusId,
        string nextScreen)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    run_status_id = @runStatusId,
    next_screen = @nextScreen,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", PositiveIntOrNull(currentArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@runStatusId", runStatusId),
            new OfflineDatabaseSqlParameter("@nextScreen", nextScreen),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    public void UpdateSummarySnapshots(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int preFinalArmySnapshotId,
        int currentArmySnapshotId)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET pre_final_army_snapshot_id = @preFinalArmySnapshotId,
    current_army_snapshot_id = @currentArmySnapshotId,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@preFinalArmySnapshotId", PositiveIntOrNull(preFinalArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", PositiveIntOrNull(currentArmySnapshotId)),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    private static object PositiveIntOrNull(int value)
    {
        return value > 0 ? (object)value : DBNull.Value;
    }

    private static int ToDbGameModeId(StartRunGameMode gameMode)
    {
        return gameMode == StartRunGameMode.Online ? (int)DBGameModeId.Online : (int)DBGameModeId.Offline;
    }

    private static int ToDbAuthoritySourceId(StartRunAuthoritySource authoritySource)
    {
        return authoritySource == StartRunAuthoritySource.BackendAdapter
            ? (int)DBAuthoritySourceId.BackendAdapter
            : (int)DBAuthoritySourceId.LocalOfflineAdapter;
    }

    private static int ParseRunSeed(string routeChoiceId)
    {
        if (string.IsNullOrEmpty(routeChoiceId))
        {
            return 35035;
        }

        string[] parts = routeChoiceId.Split('-');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] != "seed")
            {
                continue;
            }

            int parsed;
            if (int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed == 0 ? 35035 : parsed;
            }
        }

        return 35035;
    }
}
