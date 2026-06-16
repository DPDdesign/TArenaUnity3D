using System;
using System.Collections.Generic;
using System.Data;

public class OfflineRunMapDbStore : IRunMapStore
{
    private sealed class PersistedPathRow
    {
        public int RoutePathId;
        public string PathId;
        public string DisplayName;
        public string BiasDescription;
        public int SortOrder;
    }

    private sealed class PersistedNodeRow
    {
        public int NodeId;
        public int RoutePathId;
        public int NodeTypeId;
        public int NodeStateId;
        public int StageIndex;
        public string DisplayName;
        public string PossibleRewardHint;
        public string ExpectedRiskHint;
        public string EncounterId;
        public int NextNodeId;
        public string CatalogNodeId;
    }

    private readonly string databasePath;
    private readonly IRunMapPathCatalog pathCatalog;

    public OfflineRunMapDbStore()
        : this(null, new DefaultRunMapPathCatalog())
    {
    }

    public OfflineRunMapDbStore(string databasePath, IRunMapPathCatalog pathCatalog)
    {
        this.databasePath = databasePath;
        this.pathCatalog = pathCatalog ?? new DefaultRunMapPathCatalog();
    }

    public RunMapStateRecord Save(RunMapStateRecord state)
    {
        if (state == null)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            int runId = EnsureRun(connection, transaction, state, accountId);
            int routeMapId = EnsureRouteMap(connection, transaction, runId, state);
            UpdateNodeStates(connection, transaction, routeMapId, state);

            int currentNodeId = ResolveCurrentNodeIntId(connection, transaction, routeMapId, state.CurrentNodeId);
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
                new OfflineDatabaseSqlParameter("@currentNodeId", currentNodeId > 0 ? (object)currentNodeId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@currentRunGold", state.RunGold),
                new OfflineDatabaseSqlParameter("@stageProgress", state.StageProgress),
                new OfflineDatabaseSqlParameter("@routeProgress", state.RouteProgress),
                new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.InProgress),
                new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
                new OfflineDatabaseSqlParameter("@runId", runId));

            transaction.Commit();
            return Find(OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId));
        }
    }

    public RunMapStateRecord Find(string runId)
    {
        int parsedRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runId);
        if (parsedRunId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<object[]> runRows = OfflineDatabaseSql.Query(
                connection,
                @"
SELECT run_id, authority_source_id, selected_route_choice_id, route_map_id, current_node_id, route_progress, stage_progress, current_run_gold
FROM offline_runs
WHERE run_id = @runId AND is_active = 1
LIMIT 1;",
                delegate(IDataRecord row)
                {
                    return new object[]
                    {
                        row["run_id"],
                        row["authority_source_id"],
                        row["selected_route_choice_id"],
                        row["route_map_id"],
                        row["current_node_id"],
                        row["route_progress"],
                        row["stage_progress"],
                        row["current_run_gold"]
                    };
                },
                null,
                new OfflineDatabaseSqlParameter("@runId", parsedRunId));

            if (runRows.Count == 0)
            {
                return null;
            }

            object[] runRow = runRows[0];
            int routeMapId = OfflineDatabaseSql.ReadInt(runRow[3]);
            if (routeMapId <= 0)
            {
                return null;
            }

            string selectedRouteChoiceId = OfflineDatabaseSql.ReadText(runRow[2]);
            List<RunMapPathDefinition> catalogPaths = pathCatalog.BuildPaths(selectedRouteChoiceId);
            List<PersistedPathRow> persistedPaths = LoadPaths(connection, routeMapId);
            List<PersistedNodeRow> persistedNodes = LoadNodes(connection, routeMapId);
            Dictionary<int, string> catalogNodeIdByDbNodeId = new Dictionary<int, string>();
            List<RunMapPathDefinition> paths = BuildDefinitionsFromPersistence(catalogPaths, persistedPaths, persistedNodes, catalogNodeIdByDbNodeId);
            List<string> completedNodeIds = BuildCompletedNodeIds(persistedNodes);
            int currentNodeIntId = OfflineDatabaseSql.ReadInt(runRow[4]);
            string currentNodeId = currentNodeIntId <= 0 || !catalogNodeIdByDbNodeId.ContainsKey(currentNodeIntId)
                ? "run-start"
                : catalogNodeIdByDbNodeId[currentNodeIntId];

            return new RunMapStateRecord(
                OfflineDatabaseLegacyIdentity.ToLegacyRunId(OfflineDatabaseSql.ReadInt(runRow[0])),
                RunMapGameMode.Offline,
                ToAuthoritySource(OfflineDatabaseSql.ReadInt(runRow[1], (int)DBAuthoritySourceId.LocalOfflineAdapter)),
                selectedRouteChoiceId,
                OfflineDatabaseLegacyIdentity.ToLegacyRouteMapId(routeMapId),
                currentNodeId,
                OfflineDatabaseSql.ReadInt(runRow[5]),
                OfflineDatabaseSql.ReadInt(runRow[6]),
                OfflineDatabaseSql.ReadInt(runRow[7]),
                completedNodeIds,
                paths);
        }
    }

    private int EnsureRun(IDbConnection connection, IDbTransaction transaction, RunMapStateRecord state, int accountId)
    {
        int parsedRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(state.RunId);
        if (parsedRunId > 0)
        {
            object existing = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT run_id FROM offline_runs WHERE run_id = @runId LIMIT 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", parsedRunId));
            if (existing != null && existing != DBNull.Value)
            {
                return parsedRunId;
            }
        }

        string now = OfflineDatabaseSql.UtcNowText();
        if (parsedRunId > 0)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO offline_runs (
    run_id,
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
    @runId,
    @accountId,
    @gameModeId,
    @authoritySourceId,
    @runStatusId,
    @startingArmyTemplateId,
    @startingArmyVariantId,
    @selectedStartingArmyId,
    @selectedRouteChoiceId,
    @currentRunGold,
    @stageProgress,
    @routeProgress,
    @nextScreen,
    @createdAtUtc,
    @updatedAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", parsedRunId),
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@gameModeId", (int)DBGameModeId.Offline),
                new OfflineDatabaseSqlParameter("@authoritySourceId", (int)DBAuthoritySourceId.LocalOfflineAdapter),
                new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.InProgress),
                new OfflineDatabaseSqlParameter("@startingArmyTemplateId", "mock-start-army"),
                new OfflineDatabaseSqlParameter("@startingArmyVariantId", "mock-start-army-v1"),
                new OfflineDatabaseSqlParameter("@selectedStartingArmyId", "mock-start-army"),
                new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", state.SelectedRouteChoiceId),
                new OfflineDatabaseSqlParameter("@currentRunGold", state.RunGold),
                new OfflineDatabaseSqlParameter("@stageProgress", state.StageProgress),
                new OfflineDatabaseSqlParameter("@routeProgress", state.RouteProgress),
                new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
            return parsedRunId;
        }

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
    @stageProgress,
    @routeProgress,
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
            new OfflineDatabaseSqlParameter("@startingArmyTemplateId", "mock-start-army"),
            new OfflineDatabaseSqlParameter("@startingArmyVariantId", "mock-start-army-v1"),
            new OfflineDatabaseSqlParameter("@selectedStartingArmyId", "mock-start-army"),
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", state.SelectedRouteChoiceId),
            new OfflineDatabaseSqlParameter("@currentRunGold", state.RunGold),
            new OfflineDatabaseSqlParameter("@stageProgress", state.StageProgress),
            new OfflineDatabaseSqlParameter("@routeProgress", state.RouteProgress),
            new OfflineDatabaseSqlParameter("@nextScreen", "RunMap"),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
        return (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
    }

    private int EnsureRouteMap(IDbConnection connection, IDbTransaction transaction, int runId, RunMapStateRecord state)
    {
        int routeMapId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(state.RouteMapId);
        if (routeMapId > 0)
        {
            object existing = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT route_map_id FROM route_maps WHERE route_map_id = @routeMapId LIMIT 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
            if (existing != null && existing != DBNull.Value)
            {
                return routeMapId;
            }
        }

        object current = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT route_map_id FROM offline_runs WHERE run_id = @runId LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));
        routeMapId = OfflineDatabaseSql.ReadInt(current);
        if (routeMapId > 0)
        {
            return routeMapId;
        }

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
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", state.SelectedRouteChoiceId),
            new OfflineDatabaseSqlParameter("@createdFromCatalogId", state.SelectedRouteChoiceId),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
        routeMapId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

        int nextRoutePathId = ReadNextId(connection, "route_paths", "route_path_id", transaction);
        int nextNodeId = ReadNextId(connection, "route_nodes", "node_id", transaction);
        OfflineRouteMapSeedRecord seed = OfflineRouteMapSeedFactory.Create(runId, routeMapId, state.SelectedRouteChoiceId, state.Paths, nextRoutePathId, nextNodeId);
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

    private void UpdateNodeStates(IDbConnection connection, IDbTransaction transaction, int routeMapId, RunMapStateRecord state)
    {
        List<RunMapPathDefinition> catalogPaths = pathCatalog.BuildPaths(state.SelectedRouteChoiceId);
        List<PersistedPathRow> persistedPaths = LoadPaths(connection, routeMapId, transaction);
        List<PersistedNodeRow> persistedNodes = LoadNodes(connection, routeMapId, transaction);
        Dictionary<string, PersistedNodeRow> byCatalogNodeId = BuildNodeLookup(catalogPaths, persistedPaths, persistedNodes);
        string now = OfflineDatabaseSql.UtcNowText();

        for (int pathIndex = 0; pathIndex < state.Paths.Count; pathIndex++)
        {
            RunMapPathDefinition path = state.Paths[pathIndex];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
            {
                RunMapNodeDefinition node = path.Nodes[nodeIndex];
                PersistedNodeRow persistedNode;
                if (node == null || !byCatalogNodeId.TryGetValue(node.NodeId, out persistedNode))
                {
                    continue;
                }

                int nodeStateId = ToDbNodeStateId(DetermineNodeState(state, node));
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
UPDATE route_nodes
SET node_state_id = @nodeStateId,
    completed_at_utc = @completedAtUtc
WHERE node_id = @nodeId;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@nodeStateId", nodeStateId),
                    new OfflineDatabaseSqlParameter("@completedAtUtc", nodeStateId == (int)DBNodeStateId.Completed ? (object)now : DBNull.Value),
                    new OfflineDatabaseSqlParameter("@nodeId", persistedNode.NodeId));
            }
        }
    }

    private int ResolveCurrentNodeIntId(IDbConnection connection, IDbTransaction transaction, int routeMapId, string currentNodeId)
    {
        if (string.IsNullOrEmpty(currentNodeId) || currentNodeId == "run-start")
        {
            return 0;
        }

        List<RunMapPathDefinition> catalogPaths = pathCatalog.BuildPaths(string.Empty);
        List<PersistedPathRow> persistedPaths = LoadPaths(connection, routeMapId, transaction);
        List<PersistedNodeRow> persistedNodes = LoadNodes(connection, routeMapId, transaction);
        Dictionary<string, PersistedNodeRow> byCatalogNodeId = BuildNodeLookup(catalogPaths, persistedPaths, persistedNodes);
        PersistedNodeRow node;
        return byCatalogNodeId.TryGetValue(currentNodeId, out node) ? node.NodeId : 0;
    }

    private static List<PersistedPathRow> LoadPaths(IDbConnection connection, int routeMapId, IDbTransaction transaction = null)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT route_path_id, path_id, display_name, bias_description, sort_order
FROM route_paths
WHERE route_map_id = @routeMapId AND is_active = 1
ORDER BY sort_order, route_path_id;",
            delegate(IDataRecord row)
            {
                return new PersistedPathRow
                {
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    PathId = OfflineDatabaseSql.ReadText(row["path_id"]),
                    DisplayName = OfflineDatabaseSql.ReadText(row["display_name"]),
                    BiasDescription = OfflineDatabaseSql.ReadText(row["bias_description"]),
                    SortOrder = OfflineDatabaseSql.ReadInt(row["sort_order"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
    }

    private static List<PersistedNodeRow> LoadNodes(IDbConnection connection, int routeMapId, IDbTransaction transaction = null)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT node_id, route_path_id, node_type_id, node_state_id, stage_index, display_name, possible_reward_hint, expected_risk_hint, encounter_id, next_node_id
FROM route_nodes
WHERE route_map_id = @routeMapId AND is_active = 1
ORDER BY route_path_id, stage_index, node_id;",
            delegate(IDataRecord row)
            {
                return new PersistedNodeRow
                {
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    NodeTypeId = OfflineDatabaseSql.ReadInt(row["node_type_id"]),
                    NodeStateId = OfflineDatabaseSql.ReadInt(row["node_state_id"]),
                    StageIndex = OfflineDatabaseSql.ReadInt(row["stage_index"]),
                    DisplayName = OfflineDatabaseSql.ReadText(row["display_name"]),
                    PossibleRewardHint = OfflineDatabaseSql.ReadText(row["possible_reward_hint"]),
                    ExpectedRiskHint = OfflineDatabaseSql.ReadText(row["expected_risk_hint"]),
                    EncounterId = OfflineDatabaseSql.ReadText(row["encounter_id"]),
                    NextNodeId = OfflineDatabaseSql.ReadInt(row["next_node_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
    }

    private List<RunMapPathDefinition> BuildDefinitionsFromPersistence(
        List<RunMapPathDefinition> catalogPaths,
        List<PersistedPathRow> persistedPaths,
        List<PersistedNodeRow> persistedNodes,
        Dictionary<int, string> catalogNodeIdByDbNodeId)
    {
        List<RunMapPathDefinition> result = new List<RunMapPathDefinition>();
        Dictionary<int, PersistedPathRow> pathById = new Dictionary<int, PersistedPathRow>();
        for (int pathIndex = 0; pathIndex < persistedPaths.Count; pathIndex++)
        {
            pathById[persistedPaths[pathIndex].RoutePathId] = persistedPaths[pathIndex];
        }

        Dictionary<int, string> catalogNodeByDb = new Dictionary<int, string>();
        for (int pathIndex = 0; pathIndex < persistedPaths.Count; pathIndex++)
        {
            PersistedPathRow path = persistedPaths[pathIndex];
            RunMapPathDefinition catalogPath = FindCatalogPath(catalogPaths, path.PathId);
            List<PersistedNodeRow> nodes = FilterNodesForPath(persistedNodes, path.RoutePathId);
            List<RunMapNodeDefinition> definitions = new List<RunMapNodeDefinition>();
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                PersistedNodeRow node = nodes[nodeIndex];
                RunMapNodeDefinition catalogNode = catalogPath != null && catalogPath.Nodes != null && nodeIndex < catalogPath.Nodes.Count
                    ? catalogPath.Nodes[nodeIndex]
                    : null;
                node.CatalogNodeId = catalogNode == null ? "node-" + node.NodeId.ToString() : catalogNode.NodeId;
                catalogNodeByDb[node.NodeId] = node.CatalogNodeId;
                catalogNodeIdByDbNodeId[node.NodeId] = node.CatalogNodeId;
            }

            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                PersistedNodeRow node = nodes[nodeIndex];
                definitions.Add(
                    new RunMapNodeDefinition(
                        node.CatalogNodeId,
                        path.PathId,
                        ToNodeType(node.NodeTypeId),
                        node.StageIndex,
                        node.DisplayName,
                        node.PossibleRewardHint,
                        node.ExpectedRiskHint,
                        node.EncounterId,
                        node.NextNodeId > 0 && catalogNodeByDb.ContainsKey(node.NextNodeId) ? catalogNodeByDb[node.NextNodeId] : string.Empty));
            }

            result.Add(new RunMapPathDefinition(path.PathId, string.Empty, path.DisplayName, path.BiasDescription, definitions));
        }

        return result;
    }

    private static Dictionary<string, PersistedNodeRow> BuildNodeLookup(
        List<RunMapPathDefinition> catalogPaths,
        List<PersistedPathRow> persistedPaths,
        List<PersistedNodeRow> persistedNodes)
    {
        Dictionary<string, PersistedNodeRow> result = new Dictionary<string, PersistedNodeRow>();
        for (int pathIndex = 0; pathIndex < persistedPaths.Count; pathIndex++)
        {
            PersistedPathRow path = persistedPaths[pathIndex];
            RunMapPathDefinition catalogPath = FindCatalogPath(catalogPaths, path.PathId);
            List<PersistedNodeRow> nodes = FilterNodesForPath(persistedNodes, path.RoutePathId);
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                PersistedNodeRow node = nodes[nodeIndex];
                RunMapNodeDefinition catalogNode = catalogPath != null && catalogPath.Nodes != null && nodeIndex < catalogPath.Nodes.Count
                    ? catalogPath.Nodes[nodeIndex]
                    : null;
                string catalogNodeId = catalogNode == null ? "node-" + node.NodeId.ToString() : catalogNode.NodeId;
                if (!result.ContainsKey(catalogNodeId))
                {
                    result.Add(catalogNodeId, node);
                }
            }
        }

        return result;
    }

    private static List<PersistedNodeRow> FilterNodesForPath(List<PersistedNodeRow> persistedNodes, int routePathId)
    {
        List<PersistedNodeRow> result = new List<PersistedNodeRow>();
        for (int i = 0; i < persistedNodes.Count; i++)
        {
            if (persistedNodes[i].RoutePathId == routePathId)
            {
                result.Add(persistedNodes[i]);
            }
        }

        return result;
    }

    private static List<string> BuildCompletedNodeIds(List<PersistedNodeRow> persistedNodes)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < persistedNodes.Count; i++)
        {
            PersistedNodeRow node = persistedNodes[i];
            if (node != null && node.NodeStateId == (int)DBNodeStateId.Completed && !string.IsNullOrEmpty(node.CatalogNodeId))
            {
                result.Add(node.CatalogNodeId);
            }
        }

        return result;
    }

    private static RunMapPathDefinition FindCatalogPath(List<RunMapPathDefinition> catalogPaths, string pathId)
    {
        if (catalogPaths == null)
        {
            return null;
        }

        for (int i = 0; i < catalogPaths.Count; i++)
        {
            if (catalogPaths[i] != null && catalogPaths[i].PathId == pathId)
            {
                return catalogPaths[i];
            }
        }

        return null;
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

    private static RunMapAuthoritySource ToAuthoritySource(int authoritySourceId)
    {
        return authoritySourceId == (int)DBAuthoritySourceId.BackendAdapter
            ? RunMapAuthoritySource.BackendAdapter
            : RunMapAuthoritySource.LocalOfflineAdapter;
    }

    private static RunMapNodeType ToNodeType(int nodeTypeId)
    {
        switch (nodeTypeId)
        {
            case (int)DBNodeTypeId.Start:
                return RunMapNodeType.Start;
            case (int)DBNodeTypeId.Battle:
                return RunMapNodeType.Battle;
            case (int)DBNodeTypeId.Shop:
                return RunMapNodeType.Shop;
            case (int)DBNodeTypeId.RecruitReward:
                return RunMapNodeType.RecruitReward;
            case (int)DBNodeTypeId.FinalBoss:
                return RunMapNodeType.FinalBoss;
            default:
                return RunMapNodeType.Start;
        }
    }

    private static int ToDbNodeStateId(RunMapNodeState state)
    {
        switch (state)
        {
            case RunMapNodeState.Available:
            case RunMapNodeState.Selected:
                return (int)DBNodeStateId.Available;
            case RunMapNodeState.Completed:
                return (int)DBNodeStateId.Completed;
            default:
                return (int)DBNodeStateId.Locked;
        }
    }

    private static RunMapNodeState DetermineNodeState(RunMapStateRecord state, RunMapNodeDefinition node)
    {
        if (Contains(state.CompletedNodeIds, node.NodeId))
        {
            return RunMapNodeState.Completed;
        }

        if (node.StageIndex == 1 && (string.IsNullOrEmpty(state.CurrentNodeId) || state.CurrentNodeId == "run-start"))
        {
            return RunMapNodeState.Available;
        }

        if (node.NodeType == RunMapNodeType.FinalBoss)
        {
            return state.RouteProgress >= 2 ? RunMapNodeState.Available : RunMapNodeState.Locked;
        }

        if (!string.IsNullOrEmpty(state.CurrentNodeId) && state.CurrentNodeId != "run-start")
        {
            RunMapNodeDefinition current = FindNode(state.Paths, state.CurrentNodeId);
            if (current != null && current.NextNodeId == node.NodeId)
            {
                return RunMapNodeState.Available;
            }
        }

        return RunMapNodeState.Locked;
    }

    private static RunMapNodeDefinition FindNode(List<RunMapPathDefinition> paths, string nodeId)
    {
        if (paths == null || string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i] == null || paths[i].Nodes == null)
            {
                continue;
            }

            for (int j = 0; j < paths[i].Nodes.Count; j++)
            {
                if (paths[i].Nodes[j] != null && paths[i].Nodes[j].NodeId == nodeId)
                {
                    return paths[i].Nodes[j];
                }
            }
        }

        return null;
    }

    private static bool Contains(List<string> values, string value)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return true;
            }
        }

        return false;
    }
}
