using System;
using System.Collections.Generic;
using System.Data;

public static class OfflineMaterializedRunMapDbStore
{
    public static void SaveMaterializedMap(
        IDbConnection connection,
        IDbTransaction transaction,
        OfflineRouteMapSeedRecord seed)
    {
        SaveMaterializedMap(connection, transaction, seed, null, null, 0, 35035);
    }

    public static void SaveMaterializedMap(
        IDbConnection connection,
        IDbTransaction transaction,
        OfflineRouteMapSeedRecord seed,
        IStartRunUnitPoolSource enemyUnitSource,
        EnemyEncounterRuleCatalog enemyRuleCatalog,
        int accountId,
        int runSeed)
    {
        if (connection == null || transaction == null || seed == null)
        {
            return;
        }

        for (int pathIndex = 0; pathIndex < seed.Paths.Count; pathIndex++)
        {
            OfflineRoutePathSeedRecord path = seed.Paths[pathIndex];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
            {
                InsertNode(connection, transaction, seed.RunId, seed.RouteMapId, path.RoutePathId, path.Nodes[nodeIndex]);
            }
        }

        if (enemyUnitSource != null && enemyRuleCatalog == null)
        {
            throw new InvalidOperationException("Enemy army materialization requires an EnemyEncounterRuleCatalog.");
        }

        EnemyEncounterArmyMaterializer enemyMaterializer = enemyUnitSource == null || enemyRuleCatalog == null
            ? null
            : new EnemyEncounterArmyMaterializer(enemyUnitSource, enemyRuleCatalog);
        OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();

        for (int pathIndex = 0; pathIndex < seed.Paths.Count; pathIndex++)
        {
            OfflineRoutePathSeedRecord path = seed.Paths[pathIndex];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
            {
                InsertConnections(connection, transaction, seed.RunId, path.Nodes[nodeIndex]);
                InsertEnemy(connection, transaction, snapshotRepository, enemyMaterializer, accountId, seed.RunId, runSeed, path.Nodes[nodeIndex]);
            }
        }
    }

    private static void InsertNode(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int routeMapId,
        int routePathId,
        OfflineRouteNodeSeedRecord node)
    {
        if (node == null || node.NodeId <= 0)
        {
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO map_nodes (
    node_id,
    run_id,
    route_map_id,
    route_path_id,
    catalog_entry_id,
    catalog_path_id,
    node_type_id,
    node_state_id,
    stage_index,
    display_name,
    possible_reward_hint,
    expected_risk_hint,
    encounter_id,
    completed_at_utc,
    is_active
) VALUES (
    @nodeId,
    @runId,
    @routeMapId,
    @routePathId,
    @catalogEntryId,
    @catalogPathId,
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
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId),
            new OfflineDatabaseSqlParameter("@routePathId", routePathId),
            new OfflineDatabaseSqlParameter("@catalogEntryId", node.CatalogNodeId),
            new OfflineDatabaseSqlParameter("@catalogPathId", node.CatalogPathId),
            new OfflineDatabaseSqlParameter("@nodeTypeId", node.NodeTypeId),
            new OfflineDatabaseSqlParameter("@nodeStateId", node.NodeStateId),
            new OfflineDatabaseSqlParameter("@stageIndex", node.StageIndex),
            new OfflineDatabaseSqlParameter("@displayName", node.DisplayName),
            new OfflineDatabaseSqlParameter("@possibleRewardHint", node.PossibleRewardHint),
            new OfflineDatabaseSqlParameter("@expectedRiskHint", node.ExpectedRiskHint),
            new OfflineDatabaseSqlParameter("@encounterId", EmptyAsNull(node.EncounterId)));
    }

    private static void InsertConnections(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        OfflineRouteNodeSeedRecord node)
    {
        if (node == null || node.NodeId <= 0 || node.NextNodeIds == null)
        {
            return;
        }

        for (int i = 0; i < node.NextNodeIds.Count; i++)
        {
            int nextNodeId = node.NextNodeIds[i];
            if (nextNodeId <= 0)
            {
                continue;
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO map_node_connections (
    run_id,
    from_node_id,
    to_node_id,
    is_active
) VALUES (
    @runId,
    @fromNodeId,
    @toNodeId,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@fromNodeId", node.NodeId),
                new OfflineDatabaseSqlParameter("@toNodeId", nextNodeId));
        }
    }

    private static void InsertEnemy(
        IDbConnection connection,
        IDbTransaction transaction,
        OfflineArmySnapshotDbRepository snapshotRepository,
        EnemyEncounterArmyMaterializer enemyMaterializer,
        int accountId,
        int runId,
        int runSeed,
        OfflineRouteNodeSeedRecord node)
    {
        if (node == null || node.NodeId <= 0 || string.IsNullOrEmpty(node.EncounterId) || !IsBattleNode(node))
        {
            return;
        }

        int snapshotId = 0;
        if (enemyMaterializer != null && snapshotRepository != null)
        {
            OfflineArmySnapshotRecord snapshot = enemyMaterializer.BuildSnapshot(node, accountId, runId, runSeed);
            snapshotId = snapshotRepository.SaveSnapshot(connection, transaction, snapshot);
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO map_node_enemies (
    node_id,
    catalog_entry_id,
    army_snapshot_id,
    encounter_id,
    enemy_rule_id,
    risk_band,
    is_active
) VALUES (
    @nodeId,
    @catalogEntryId,
    @armySnapshotId,
    @encounterId,
    @enemyRuleId,
    @riskBand,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@nodeId", node.NodeId),
            new OfflineDatabaseSqlParameter("@catalogEntryId", node.EncounterId),
            new OfflineDatabaseSqlParameter("@armySnapshotId", snapshotId > 0 ? (object)snapshotId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@encounterId", node.EncounterId),
            new OfflineDatabaseSqlParameter("@enemyRuleId", node.EncounterDifficulty.ToString()),
            new OfflineDatabaseSqlParameter("@riskBand", node.EncounterDifficulty.ToString()));
    }

    private static bool IsBattleNode(OfflineRouteNodeSeedRecord node)
    {
        return node != null &&
            (node.NodeTypeId == (int)DBNodeTypeId.Battle || node.NodeTypeId == (int)DBNodeTypeId.FinalBoss);
    }

    private static string EmptyAsNull(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
