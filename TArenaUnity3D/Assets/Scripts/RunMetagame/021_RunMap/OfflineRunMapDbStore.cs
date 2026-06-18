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
        public string CatalogPathId;
        public int NodeTypeId;
        public int NodeStateId;
        public int StageIndex;
        public string DisplayName;
        public string PossibleRewardHint;
        public string ExpectedRiskHint;
        public string EncounterId;
        public List<int> NextNodeIds = new List<int>();
        public string CatalogNodeId;
    }

    private readonly string databasePath;
    private readonly IRunMapPathCatalog pathCatalog;
    private readonly OfflineRunContextDbWriter runContextWriter = new OfflineRunContextDbWriter();

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
            int runId = EnsureRun(connection, transaction, state);
            int routeMapId = EnsureRouteMap(connection, transaction, runId, state);
            UpdateNodeStates(connection, transaction, routeMapId, state);

            int currentNodeId = ResolveCurrentNodeIntId(connection, transaction, routeMapId, state.SelectedRouteChoiceId, state.CurrentNodeId);
            runContextWriter.UpdateRunMapState(
                connection,
                transaction,
                runId,
                routeMapId,
                currentNodeId,
                state.RunGold,
                state.StageProgress,
                state.RouteProgress,
                (int)DBRunStatusId.InProgress,
                ResolveNextScreen(state));

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

    private int EnsureRun(IDbConnection connection, IDbTransaction transaction, RunMapStateRecord state)
    {
        int parsedRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(state.RunId);
        if (parsedRunId <= 0)
        {
            throw new InvalidOperationException("Run Map DB store requires a persisted run id from Start Run.");
        }

        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT run_id FROM offline_runs WHERE run_id = @runId AND is_active = 1 LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", parsedRunId));
        if (existing == null || existing == DBNull.Value)
        {
            throw new InvalidOperationException("Run Map DB store could not find a persisted run. Start Run must create the run before Run Map saves route state.");
        }

        return parsedRunId;
    }

    private int EnsureRouteMap(IDbConnection connection, IDbTransaction transaction, int runId, RunMapStateRecord state)
    {
        int routeMapId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(state.RouteMapId);
        if (routeMapId > 0)
        {
            object existing = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT node_id FROM map_nodes WHERE route_map_id = @routeMapId AND is_active = 1 LIMIT 1;",
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

        routeMapId = runId;
        int nextRoutePathId = 1;
        int nextNodeId = ReadNextId(connection, "map_nodes", "node_id", transaction);
        OfflineRouteMapSeedRecord seed = OfflineRouteMapSeedFactory.Create(runId, routeMapId, state.SelectedRouteChoiceId, state.Paths, nextRoutePathId, nextNodeId);
        OfflineMaterializedRunMapDbStore.SaveMaterializedMap(connection, transaction, seed);

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
UPDATE map_nodes
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

    private int ResolveCurrentNodeIntId(IDbConnection connection, IDbTransaction transaction, int routeMapId, string selectedRouteChoiceId, string currentNodeId)
    {
        if (string.IsNullOrEmpty(currentNodeId) || currentNodeId == "run-start")
        {
            return 0;
        }

        List<RunMapPathDefinition> catalogPaths = pathCatalog.BuildPaths(selectedRouteChoiceId);
        List<PersistedPathRow> persistedPaths = LoadPaths(connection, routeMapId, transaction);
        List<PersistedNodeRow> persistedNodes = LoadNodes(connection, routeMapId, transaction);
        Dictionary<string, PersistedNodeRow> byCatalogNodeId = BuildNodeLookup(catalogPaths, persistedPaths, persistedNodes);
        PersistedNodeRow node;
        return byCatalogNodeId.TryGetValue(currentNodeId, out node) ? node.NodeId : 0;
    }

    private static string ResolveNextScreen(RunMapStateRecord state)
    {
        RunMapNodeDefinition current = state == null ? null : FindNode(state.Paths, state.CurrentNodeId);
        if (current == null)
        {
            return "RunMap";
        }

        switch (current.NodeType)
        {
            case RunMapNodeType.Battle:
            case RunMapNodeType.FinalBoss:
                return "RunBattle";
            case RunMapNodeType.Shop:
                return "RunShop";
            case RunMapNodeType.RecruitReward:
                return "Reward";
            default:
                return "RunMap";
        }
    }

    private static List<PersistedPathRow> LoadPaths(IDbConnection connection, int routeMapId, IDbTransaction transaction = null)
    {
        List<PersistedPathRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT route_path_id, catalog_path_id, MIN(route_path_id) AS sort_order
FROM map_nodes
WHERE route_map_id = @routeMapId AND is_active = 1
GROUP BY route_path_id, catalog_path_id
ORDER BY route_path_id;",
            delegate(IDataRecord row)
            {
                string pathId = OfflineDatabaseSql.ReadText(row["catalog_path_id"]);
                return new PersistedPathRow
                {
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    PathId = string.IsNullOrEmpty(pathId) ? "path-" + OfflineDatabaseSql.ReadInt(row["route_path_id"]) : pathId,
                    DisplayName = string.Empty,
                    BiasDescription = string.Empty,
                    SortOrder = OfflineDatabaseSql.ReadInt(row["sort_order"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
        return rows;
    }

    private static List<PersistedNodeRow> LoadNodes(IDbConnection connection, int routeMapId, IDbTransaction transaction = null)
    {
        List<PersistedNodeRow> nodes = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT node_id, route_path_id, catalog_path_id, catalog_entry_id, node_type_id, node_state_id, stage_index, display_name, possible_reward_hint, expected_risk_hint, encounter_id
FROM map_nodes
WHERE route_map_id = @routeMapId AND is_active = 1
ORDER BY route_path_id, stage_index, node_id;",
            delegate(IDataRecord row)
            {
                return new PersistedNodeRow
                {
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    CatalogPathId = OfflineDatabaseSql.ReadText(row["catalog_path_id"]),
                    CatalogNodeId = OfflineDatabaseSql.ReadText(row["catalog_entry_id"]),
                    NodeTypeId = OfflineDatabaseSql.ReadInt(row["node_type_id"]),
                    NodeStateId = OfflineDatabaseSql.ReadInt(row["node_state_id"]),
                    StageIndex = OfflineDatabaseSql.ReadInt(row["stage_index"]),
                    DisplayName = OfflineDatabaseSql.ReadText(row["display_name"]),
                    PossibleRewardHint = OfflineDatabaseSql.ReadText(row["possible_reward_hint"]),
                    ExpectedRiskHint = OfflineDatabaseSql.ReadText(row["expected_risk_hint"]),
                    EncounterId = OfflineDatabaseSql.ReadText(row["encounter_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));

        Dictionary<int, List<int>> connections = LoadConnections(connection, routeMapId, transaction);
        for (int i = 0; i < nodes.Count; i++)
        {
            List<int> nextNodeIds;
            if (connections.TryGetValue(nodes[i].NodeId, out nextNodeIds))
            {
                nodes[i].NextNodeIds = nextNodeIds;
            }
        }

        return nodes;
    }

    private static Dictionary<int, List<int>> LoadConnections(IDbConnection connection, int routeMapId, IDbTransaction transaction = null)
    {
        Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();
        List<int[]> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT from_node_id, to_node_id
FROM map_node_connections
WHERE run_id = @runId AND is_active = 1
ORDER BY connection_id;",
            delegate(IDataRecord row)
            {
                return new[]
                {
                    OfflineDatabaseSql.ReadInt(row["from_node_id"]),
                    OfflineDatabaseSql.ReadInt(row["to_node_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", routeMapId));

        for (int i = 0; i < rows.Count; i++)
        {
            int fromNodeId = rows[i][0];
            int toNodeId = rows[i][1];
            if (fromNodeId <= 0 || toNodeId <= 0)
            {
                continue;
            }

            List<int> nextNodeIds;
            if (!result.TryGetValue(fromNodeId, out nextNodeIds))
            {
                nextNodeIds = new List<int>();
                result.Add(fromNodeId, nextNodeIds);
            }

            if (!nextNodeIds.Contains(toNodeId))
            {
                nextNodeIds.Add(toNodeId);
            }
        }

        return result;
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
            if (catalogPath != null)
            {
                path.DisplayName = catalogPath.DisplayName;
                path.BiasDescription = catalogPath.BiasDescription;
            }

            List<PersistedNodeRow> nodes = FilterNodesForPath(persistedNodes, path.RoutePathId);
            List<RunMapNodeDefinition> definitions = new List<RunMapNodeDefinition>();
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                PersistedNodeRow node = nodes[nodeIndex];
                RunMapNodeDefinition catalogNode = catalogPath != null && catalogPath.Nodes != null && nodeIndex < catalogPath.Nodes.Count
                    ? catalogPath.Nodes[nodeIndex]
                    : null;
                if (string.IsNullOrEmpty(node.CatalogNodeId))
                {
                    node.CatalogNodeId = catalogNode == null ? "node-" + node.NodeId.ToString() : catalogNode.NodeId;
                }

                catalogNodeByDb[node.NodeId] = node.CatalogNodeId;
                catalogNodeIdByDbNodeId[node.NodeId] = node.CatalogNodeId;
            }

            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                PersistedNodeRow node = nodes[nodeIndex];
                RunMapNodeDefinition catalogNode = catalogPath != null && catalogPath.Nodes != null && nodeIndex < catalogPath.Nodes.Count
                    ? catalogPath.Nodes[nodeIndex]
                    : null;
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
                        BuildNextNodeIds(node, catalogNode, catalogNodeByDb)));
            }

            result.Add(new RunMapPathDefinition(path.PathId, string.Empty, string.IsNullOrEmpty(path.DisplayName) ? path.PathId : path.DisplayName, path.BiasDescription, definitions));
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
                string catalogNodeId = string.IsNullOrEmpty(node.CatalogNodeId)
                    ? (catalogNode == null ? "node-" + node.NodeId.ToString() : catalogNode.NodeId)
                    : node.CatalogNodeId;
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

    private static List<string> BuildNextNodeIds(
        PersistedNodeRow node,
        RunMapNodeDefinition catalogNode,
        Dictionary<int, string> catalogNodeByDb)
    {
        List<string> result = new List<string>();
        if (node != null && node.NextNodeIds != null)
        {
            for (int i = 0; i < node.NextNodeIds.Count; i++)
            {
                int nextNodeId = node.NextNodeIds[i];
                if (nextNodeId > 0 && catalogNodeByDb.ContainsKey(nextNodeId) && !result.Contains(catalogNodeByDb[nextNodeId]))
                {
                    result.Add(catalogNodeByDb[nextNodeId]);
                }
            }
        }

        if (catalogNode != null && catalogNode.NextNodeIds != null)
        {
            for (int i = 0; i < catalogNode.NextNodeIds.Count; i++)
            {
                string nextNodeId = catalogNode.NextNodeIds[i];
                if (!string.IsNullOrEmpty(nextNodeId) && !result.Contains(nextNodeId))
                {
                    result.Add(nextNodeId);
                }
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
            case (int)DBNodeTypeId.RandomEvent:
                return RunMapNodeType.RandomEvent;
            case (int)DBNodeTypeId.Empty:
                return RunMapNodeType.Empty;
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
            return state.RouteProgress >= 2 && IsReachableFromCurrent(state, node)
                ? RunMapNodeState.Available
                : RunMapNodeState.Locked;
        }

        if (IsReachableFromCurrent(state, node))
        {
            return RunMapNodeState.Available;
        }

        return RunMapNodeState.Locked;
    }

    private static bool IsReachableFromCurrent(RunMapStateRecord state, RunMapNodeDefinition node)
    {
        if (state == null || node == null || string.IsNullOrEmpty(state.CurrentNodeId) || state.CurrentNodeId == "run-start")
        {
            return false;
        }

        if (!string.IsNullOrEmpty(state.CurrentNodeId) && state.CurrentNodeId != "run-start")
        {
            RunMapNodeDefinition current = FindNode(state.Paths, state.CurrentNodeId);
            if (current != null && HasNextNode(current, node.NodeId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNextNode(RunMapNodeDefinition current, string nodeId)
    {
        if (current == null || string.IsNullOrEmpty(nodeId))
        {
            return false;
        }

        if (current.NextNodeId == nodeId)
        {
            return true;
        }

        if (current.NextNodeIds == null)
        {
            return false;
        }

        for (int i = 0; i < current.NextNodeIds.Count; i++)
        {
            if (current.NextNodeIds[i] == nodeId)
            {
                return true;
            }
        }

        return false;
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
