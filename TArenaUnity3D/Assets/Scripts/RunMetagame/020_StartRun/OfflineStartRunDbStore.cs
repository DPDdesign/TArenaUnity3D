using System;
using System.Collections.Generic;
using System.Data;

public class OfflineStartRunDbStore : IStartRunRecordStore
{
    private readonly string databasePath;
    private readonly IRunMapPathCatalog pathCatalog;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
    private readonly OfflineRunContextDbWriter runContextWriter = new OfflineRunContextDbWriter();

    public OfflineStartRunDbStore()
        : this(null, new DefaultRunMapPathCatalog())
    {
    }

    public OfflineStartRunDbStore(string databasePath, IRunMapPathCatalog pathCatalog)
        : this(databasePath, pathCatalog, null)
    {
    }

    public OfflineStartRunDbStore(string databasePath, IRunMapPathCatalog pathCatalog, IOfflineArmySnapshotCatalogResolver resolver)
    {
        this.databasePath = databasePath;
        this.pathCatalog = pathCatalog ?? new DefaultRunMapPathCatalog();
        this.resolver = resolver;
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
            int runId = runContextWriter.InsertStartRun(connection, transaction, record, accountId);
            int routeMapId = SeedRouteMap(connection, transaction, runId, record.RoutePreviewOptionId);
            int snapshotId = snapshotRepository.SaveSnapshot(connection, transaction, OfflineArmySnapshotMapper.FromStartRun(record.InitialArmySnapshot, accountId, runId));
            runContextWriter.AttachStartRunRouteAndArmy(connection, transaction, runId, routeMapId, snapshotId, record.StartingCurrency);

            transaction.Commit();

            OfflineRunContextDbReader reader = new OfflineRunContextDbReader(databasePath, resolver);
            CreatedRunRecord persisted = reader.ToStartRunCreatedRecord(reader.LoadRun(OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId)));
            if (persisted == null)
            {
                throw new InvalidOperationException("Start Run DB store could not reload the persisted run context.");
            }

            return persisted;
        }
    }

    private int SeedRouteMap(IDbConnection connection, IDbTransaction transaction, int runId, string selectedRouteChoiceId)
    {
        string now = OfflineDatabaseSql.UtcNowText();
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
        OfflineMaterializedRunMapDbStore.SaveMaterializedMap(connection, transaction, seed);

        return routeMapId;
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

}
