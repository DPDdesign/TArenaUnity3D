using System.Collections.Generic;
using System.Data;

public sealed class OfflineRunBattleEncounterCatalog : IRunBattleEncounterSource
{
    private readonly string databasePath;
    private readonly IRunBattleEncounterSource fallback;

    public OfflineRunBattleEncounterCatalog(string databasePath, IRunBattleEncounterSource fallback)
    {
        this.databasePath = databasePath;
        this.fallback = fallback ?? new DefaultRunBattleEncounterCatalog();
    }

    public RunBattleEncounterDefinition FindEncounter(string routeNodeId, string encounterId)
    {
        MaterializedEnemyRow enemy = FindMaterializedEnemy(routeNodeId, encounterId);
        RunBattleEncounterDefinition baseEncounter = fallback.FindEncounter(routeNodeId, encounterId);
        if (enemy == null)
        {
            return baseEncounter;
        }

        RunBattleNodeType nodeType = enemy.NodeTypeId == (int)DBNodeTypeId.FinalBoss
            ? RunBattleNodeType.Final
            : RunBattleNodeType.Battle;
        string enemyArmySourceId = enemy.ArmySnapshotId > 0
            ? OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(enemy.ArmySnapshotId)
            : string.Empty;

        return new RunBattleEncounterDefinition(
            string.IsNullOrEmpty(enemy.EncounterId) ? encounterId : enemy.EncounterId,
            OfflineDatabaseLegacyIdentity.ToLegacyRouteNodeId(enemy.NodeId),
            nodeType,
            baseEncounter == null ? "Materialized Battle" : baseEncounter.DisplayName,
            string.IsNullOrEmpty(enemy.RiskBand) ? (baseEncounter == null ? string.Empty : baseEncounter.ExpectedRisk) : enemy.RiskBand,
            baseEncounter == null ? 0 : baseEncounter.RecommendedArmyValue,
            string.IsNullOrEmpty(enemyArmySourceId) ? (baseEncounter == null ? string.Empty : baseEncounter.EnemyArmySourceId) : enemyArmySourceId,
            baseEncounter == null ? RunBattleEnemyGoal.TryToWin : baseEncounter.EnemyGoal);
    }

    private MaterializedEnemyRow FindMaterializedEnemy(string routeNodeId, string encounterId)
    {
        int parsedNodeId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(routeNodeId);
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<MaterializedEnemyRow> byNode = parsedNodeId <= 0
                ? new List<MaterializedEnemyRow>()
                : QueryEnemy(
                    connection,
                    @"
SELECT enemies.node_id, nodes.node_type_id, enemies.army_snapshot_id, enemies.encounter_id, enemies.risk_band
FROM map_node_enemies enemies
INNER JOIN map_nodes nodes ON nodes.node_id = enemies.node_id
WHERE enemies.node_id = @nodeId
  AND enemies.is_active = 1
LIMIT 1;",
                    new OfflineDatabaseSqlParameter("@nodeId", parsedNodeId));
            if (byNode.Count > 0)
            {
                return byNode[0];
            }

            if (string.IsNullOrEmpty(encounterId))
            {
                return null;
            }

            List<MaterializedEnemyRow> byEncounter = QueryEnemy(
                connection,
                @"
SELECT enemies.node_id, nodes.node_type_id, enemies.army_snapshot_id, enemies.encounter_id, enemies.risk_band
FROM map_node_enemies enemies
INNER JOIN map_nodes nodes ON nodes.node_id = enemies.node_id
WHERE enemies.encounter_id = @encounterId
  AND enemies.is_active = 1
ORDER BY enemies.enemy_id
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@encounterId", encounterId));
            return byEncounter.Count == 0 ? null : byEncounter[0];
        }
    }

    private static List<MaterializedEnemyRow> QueryEnemy(
        IDbConnection connection,
        string sql,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        return OfflineDatabaseSql.Query(
            connection,
            sql,
            delegate(IDataRecord row)
            {
                return new MaterializedEnemyRow
                {
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    NodeTypeId = OfflineDatabaseSql.ReadInt(row["node_type_id"]),
                    ArmySnapshotId = OfflineDatabaseSql.ReadInt(row["army_snapshot_id"]),
                    EncounterId = OfflineDatabaseSql.ReadText(row["encounter_id"]),
                    RiskBand = OfflineDatabaseSql.ReadText(row["risk_band"])
                };
            },
            null,
            parameters);
    }

    private sealed class MaterializedEnemyRow
    {
        public int NodeId;
        public int NodeTypeId;
        public int ArmySnapshotId;
        public string EncounterId;
        public string RiskBand;
    }
}
