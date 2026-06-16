using System;
using System.Collections.Generic;
using System.Data;

public class OfflineStartRunDbStore : IStartRunRecordStore
{
    private readonly string databasePath;
    private readonly IRunMapPathCatalog pathCatalog;

    public OfflineStartRunDbStore()
        : this(null, new DefaultRunMapPathCatalog())
    {
    }

    public OfflineStartRunDbStore(string databasePath, IRunMapPathCatalog pathCatalog)
    {
        this.databasePath = databasePath;
        this.pathCatalog = pathCatalog ?? new DefaultRunMapPathCatalog();
    }

    public CreatedRunRecord SaveCreatedRun(CreatedRunRecord record)
    {
        if (record == null)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, record.AccountPlayerId);
            string now = OfflineDatabaseSql.UtcNowText();
            int runId = InsertRun(connection, transaction, record, accountId, now);
            int routeMapId = SeedRouteMap(connection, transaction, runId, record.RoutePreviewOptionId, now);
            int snapshotId = SaveSnapshot(connection, transaction, OfflineArmySnapshotMapper.FromStartRun(record.InitialArmySnapshot, accountId, runId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE offline_runs
SET route_map_id = @routeMapId,
    start_army_snapshot_id = @startArmySnapshotId,
    current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    updated_at_utc = @updatedAtUtc,
    next_screen = @nextScreen
WHERE run_id = @runId;",
                transaction,
                new OfflineDatabaseSqlParameter("@routeMapId", routeMapId),
                new OfflineDatabaseSqlParameter("@startArmySnapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@currentArmySnapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@currentRunGold", record.StartingCurrency),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
                new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
                new OfflineDatabaseSqlParameter("@runId", runId));

            transaction.Commit();
            return BuildPersistedRecord(record, runId, snapshotId);
        }
    }

    private int InsertRun(IDbConnection connection, IDbTransaction transaction, CreatedRunRecord record, int accountId, string now)
    {
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
    @nextScreen,
    @createdAtUtc,
    @updatedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@gameModeId", (int)DBGameModeId.Offline),
            new OfflineDatabaseSqlParameter("@authoritySourceId", (int)DBAuthoritySourceId.LocalOfflineAdapter),
            new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.InProgress),
            new OfflineDatabaseSqlParameter("@startingArmyTemplateId", record.StartingArmyTemplateId),
            new OfflineDatabaseSqlParameter("@startingArmyVariantId", record.StartingArmyVariantId),
            new OfflineDatabaseSqlParameter("@selectedStartingArmyId", record.SelectedStartingArmyId),
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", record.RoutePreviewOptionId),
            new OfflineDatabaseSqlParameter("@currentRunGold", record.StartingCurrency),
            new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));

        return (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
    }

    private int SeedRouteMap(IDbConnection connection, IDbTransaction transaction, int runId, string selectedRouteChoiceId, string now)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO route_maps (
    run_id,
    selected_route_choice_id,
    created_from_catalog_id,
    created_at_utc,
    updated_at_utc,
    is_active
) VALUES (
    @runId,
    @selectedRouteChoiceId,
    @createdFromCatalogId,
    @createdAtUtc,
    @updatedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", selectedRouteChoiceId),
            new OfflineDatabaseSqlParameter("@createdFromCatalogId", selectedRouteChoiceId),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));

        int routeMapId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
        int nextRoutePathId = ReadNextId(connection, "route_paths", "route_path_id", transaction);
        int nextNodeId = ReadNextId(connection, "route_nodes", "node_id", transaction);
        List<RunMapPathDefinition> paths = pathCatalog.BuildPaths(selectedRouteChoiceId);
        OfflineRouteMapSeedRecord seed = OfflineRouteMapSeedFactory.Create(runId, routeMapId, selectedRouteChoiceId, paths, nextRoutePathId, nextNodeId);

        for (int pathIndex = 0; pathIndex < seed.Paths.Count; pathIndex++)
        {
            OfflineRoutePathSeedRecord path = seed.Paths[pathIndex];
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO route_paths (
    route_path_id,
    route_map_id,
    path_id,
    display_name,
    bias_description,
    sort_order,
    is_active
) VALUES (
    @routePathId,
    @routeMapId,
    @pathId,
    @displayName,
    @biasDescription,
    @sortOrder,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@routePathId", path.RoutePathId),
                new OfflineDatabaseSqlParameter("@routeMapId", path.RouteMapId),
                new OfflineDatabaseSqlParameter("@pathId", path.PathId),
                new OfflineDatabaseSqlParameter("@displayName", path.DisplayName),
                new OfflineDatabaseSqlParameter("@biasDescription", path.BiasDescription),
                new OfflineDatabaseSqlParameter("@sortOrder", path.SortOrder));
        }

        InsertRouteNodes(connection, transaction, seed);
        UpdateRouteNodeLinks(connection, transaction, seed);

        return routeMapId;
    }

    private int SaveSnapshot(IDbConnection connection, IDbTransaction transaction, OfflineArmySnapshotRecord snapshot)
    {
        if (snapshot == null)
        {
            return 0;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO army_snapshots (
    account_id,
    run_id,
    saved_army_id,
    node_id,
    created_at_utc,
    is_active
) VALUES (
    @accountId,
    @runId,
    NULL,
    NULL,
    @createdAtUtc,
    @isActive
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", snapshot.AccountId),
            new OfflineDatabaseSqlParameter("@runId", snapshot.RunId),
            new OfflineDatabaseSqlParameter("@createdAtUtc", snapshot.CreatedAtUtc),
            new OfflineDatabaseSqlParameter("@isActive", snapshot.IsActive ? 1 : 0));
        int snapshotId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

        for (int stackIndex = 0; stackIndex < snapshot.Stacks.Count; stackIndex++)
        {
            OfflineArmySnapshotStackRecord stack = snapshot.Stacks[stackIndex];
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO army_snapshot_stacks (
    snapshot_id,
    unit_id,
    amount,
    formation_slot,
    is_active
) VALUES (
    @snapshotId,
    @unitId,
    @amount,
    @formationSlot,
    @isActive
);",
                transaction,
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@unitId", stack.UnitId),
                new OfflineDatabaseSqlParameter("@amount", stack.Amount),
                new OfflineDatabaseSqlParameter("@formationSlot", stack.FormationSlot),
                new OfflineDatabaseSqlParameter("@isActive", stack.IsActive ? 1 : 0));
            int snapshotStackId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

            for (int skillIndex = 0; skillIndex < stack.Skills.Count; skillIndex++)
            {
                OfflineArmySnapshotStackSkillRecord skill = stack.Skills[skillIndex];
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
INSERT INTO army_snapshot_stack_skills (
    snapshot_stack_id,
    skill_id,
    acquired_at_run_node_id,
    is_active
) VALUES (
    @snapshotStackId,
    @skillId,
    @acquiredAtRunNodeId,
    @isActive
);",
                    transaction,
                    new OfflineDatabaseSqlParameter("@snapshotStackId", snapshotStackId),
                    new OfflineDatabaseSqlParameter("@skillId", skill.SkillId),
                    new OfflineDatabaseSqlParameter("@acquiredAtRunNodeId", skill.AcquiredAtRunNodeId > 0 ? (object)skill.AcquiredAtRunNodeId : DBNull.Value),
                    new OfflineDatabaseSqlParameter("@isActive", skill.IsActive ? 1 : 0));
            }
        }

        return snapshotId;
    }

    private static int ReadNextId(IDbConnection connection, string tableName, string keyColumn, IDbTransaction transaction)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT COALESCE(MAX(" + keyColumn + "), 0) + 1 FROM " + tableName + ";",
            transaction);
        return OfflineDatabaseSql.ReadInt(result, 1);
    }

    private static string EmptyAsNull(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static void InsertRouteNodes(IDbConnection connection, IDbTransaction transaction, OfflineRouteMapSeedRecord seed)
    {
        for (int pathIndex = 0; pathIndex < seed.Paths.Count; pathIndex++)
        {
            OfflineRoutePathSeedRecord path = seed.Paths[pathIndex];
            for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
            {
                OfflineRouteNodeSeedRecord node = path.Nodes[nodeIndex];
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
INSERT INTO route_nodes (
    node_id,
    route_map_id,
    route_path_id,
    node_type_id,
    node_state_id,
    stage_index,
    display_name,
    possible_reward_hint,
    expected_risk_hint,
    encounter_id,
    next_node_id,
    is_active
) VALUES (
    @nodeId,
    @routeMapId,
    @routePathId,
    @nodeTypeId,
    @nodeStateId,
    @stageIndex,
    @displayName,
    @possibleRewardHint,
    @expectedRiskHint,
    @encounterId,
    NULL,
    1
);",
                    transaction,
                    new OfflineDatabaseSqlParameter("@nodeId", node.NodeId),
                    new OfflineDatabaseSqlParameter("@routeMapId", node.RouteMapId),
                    new OfflineDatabaseSqlParameter("@routePathId", node.RoutePathId),
                    new OfflineDatabaseSqlParameter("@nodeTypeId", node.NodeTypeId),
                    new OfflineDatabaseSqlParameter("@nodeStateId", node.NodeStateId),
                    new OfflineDatabaseSqlParameter("@stageIndex", node.StageIndex),
                    new OfflineDatabaseSqlParameter("@displayName", node.DisplayName),
                    new OfflineDatabaseSqlParameter("@possibleRewardHint", node.PossibleRewardHint),
                    new OfflineDatabaseSqlParameter("@expectedRiskHint", node.ExpectedRiskHint),
                    new OfflineDatabaseSqlParameter("@encounterId", EmptyAsNull(node.EncounterId)));
            }
        }
    }

    private static void UpdateRouteNodeLinks(IDbConnection connection, IDbTransaction transaction, OfflineRouteMapSeedRecord seed)
    {
        for (int pathIndex = 0; pathIndex < seed.Paths.Count; pathIndex++)
        {
            OfflineRoutePathSeedRecord path = seed.Paths[pathIndex];
            for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
            {
                OfflineRouteNodeSeedRecord node = path.Nodes[nodeIndex];
                if (node.NextNodeId <= 0)
                {
                    continue;
                }

                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
UPDATE route_nodes
SET next_node_id = @nextNodeId
WHERE node_id = @nodeId;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@nextNodeId", node.NextNodeId),
                    new OfflineDatabaseSqlParameter("@nodeId", node.NodeId));
            }
        }
    }

    private static CreatedRunRecord BuildPersistedRecord(CreatedRunRecord source, int runId, int snapshotId)
    {
        RunArmySnapshot sourceSnapshot = source.InitialArmySnapshot;
        RunArmySnapshot persistedSnapshot = new RunArmySnapshot(
            OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(snapshotId),
            sourceSnapshot == null ? 0 : sourceSnapshot.TotalArmyValue,
            sourceSnapshot == null ? new List<RunArmyStackSnapshot>() : CloneStacks(sourceSnapshot.Stacks));

        return new CreatedRunRecord(
            OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId),
            source.GameMode,
            source.AuthoritySource,
            source.AccountPlayerId,
            source.StartingArmyTemplateId,
            source.StartingArmyVariantId,
            source.SelectedStartingArmyId,
            source.RoutePreviewOptionId,
            source.StartingCurrency,
            source.RunStatus,
            persistedSnapshot);
    }

    private static List<RunArmyStackSnapshot> CloneStacks(List<RunArmyStackSnapshot> stacks)
    {
        List<RunArmyStackSnapshot> copy = new List<RunArmyStackSnapshot>();
        if (stacks == null)
        {
            return copy;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            RunArmyStackSnapshot stack = stacks[i];
            if (stack == null)
            {
                continue;
            }

            copy.Add(new RunArmyStackSnapshot(
                stack.UnitId,
                stack.Tier,
                stack.Level,
                stack.Amount,
                stack.CombatValue,
                CloneSkills(stack.Skills)));
        }

        return copy;
    }

    private static List<StartRunSkillViewData> CloneSkills(List<StartRunSkillViewData> skills)
    {
        List<StartRunSkillViewData> copy = new List<StartRunSkillViewData>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            StartRunSkillViewData skill = skills[i];
            if (skill != null)
            {
                copy.Add(new StartRunSkillViewData(skill.SkillId, skill.Unlocked));
            }
        }

        return copy;
    }
}
