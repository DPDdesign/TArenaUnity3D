#if UNITY_EDITOR
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;

public class OfflineDatabaseSchemaTests
{
    [Test]
    public void BuildStatements_IncludesCoreTablesAndSoftDeleteColumns()
    {
        List<string> statements = OfflineDatabaseSchemaV1.BuildStatements();

        Assert.That(statements.Count, Is.GreaterThanOrEqualTo(20));
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS offline_runs"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS army_snapshots"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS map_nodes"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS route_nodes"), Is.False);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS reward_opportunities"), Is.True);
        Assert.That(ContainsSql(statements, "planned_operation_type TEXT NOT NULL"), Is.True);
        Assert.That(ContainsSql(statements, "seed_version INTEGER NOT NULL DEFAULT 1"), Is.True);
        Assert.That(ContainsSql(statements, "opportunity_state_id INTEGER NOT NULL DEFAULT 1"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS run_events"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS shop_offers"), Is.True);
        Assert.That(ContainsSql(statements, "offer_id TEXT NOT NULL"), Is.True);
        Assert.That(ContainsSql(statements, "reward_id TEXT NOT NULL DEFAULT ''"), Is.True);
        Assert.That(ContainsSql(statements, "reward_slot_index INTEGER NOT NULL DEFAULT 0"), Is.True);
        Assert.That(ContainsSql(statements, "focused_reward_slot_index INTEGER NOT NULL DEFAULT -1"), Is.True);
        Assert.That(ContainsSql(statements, "card_reward_id TEXT NOT NULL DEFAULT ''"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS saved_armies"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS async_battle_results"), Is.True);
        Assert.That(ContainsSql(statements, "is_active INTEGER NOT NULL DEFAULT 1"), Is.True);
    }

    [Test]
    public void BuildStatements_UsesIntegerPrimaryKeysForLocalRuntimeRecords()
    {
        List<string> statements = OfflineDatabaseSchemaV1.BuildStatements();

        Assert.That(ContainsSql(statements, "account_id INTEGER PRIMARY KEY"), Is.True);
        Assert.That(ContainsSql(statements, "run_id INTEGER PRIMARY KEY"), Is.True);
        Assert.That(ContainsSql(statements, "snapshot_id INTEGER PRIMARY KEY"), Is.True);
        Assert.That(ContainsSql(statements, "node_id INTEGER PRIMARY KEY"), Is.True);
        Assert.That(ContainsSql(statements, "event_id INTEGER PRIMARY KEY"), Is.True);
        Assert.That(ContainsSql(statements, "saved_army_id INTEGER PRIMARY KEY"), Is.True);
    }

    [Test]
    public void DbEnums_UseStableManualIds()
    {
        Assert.That((int)DBGameModeId.Offline, Is.EqualTo(1));
        Assert.That((int)DBRunStatusId.Created, Is.EqualTo(1));
        Assert.That((int)DBNodeTypeId.FinalBoss, Is.EqualTo(5));
        Assert.That((int)DBEventTypeId.Purchase, Is.EqualTo(5));
        Assert.That((int)DBRewardOpportunityStateId.Unresolved, Is.EqualTo(1));
        Assert.That((int)DBRewardOpportunityStateId.Resolved, Is.EqualTo(2));
        Assert.That((int)DBResultKindId.DefenceLoss, Is.EqualTo(4));
    }

    [Test]
    public void OpenOrCreate_AddsPrd37ColumnsToExistingVersionOneDatabase()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), "TArenaOffline_LegacyV1_" + System.Guid.NewGuid().ToString("N") + ".db");
        try
        {
            CreateLegacyVersionOneDatabase(databasePath);

            OfflineDatabaseOpenResult result = new OfflineDatabaseModule(databasePath).OpenOrCreate();

            Assert.That(result.Success, Is.True);
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ColumnExists(connection, "offline_runs", "run_seed"), Is.True);
                Assert.That(ColumnExists(connection, "offline_runs", "run_seed_version"), Is.True);
                Assert.That(TableExists(connection, "map_nodes"), Is.True);
                Assert.That(TableExists(connection, "map_node_rewards"), Is.True);
                Assert.That(TableExists(connection, "reward_opportunities"), Is.True);
                Assert.That(ColumnExists(connection, "reward_opportunities", "planned_operation_type"), Is.True);
                Assert.That(ColumnExists(connection, "reward_opportunities", "seed_version"), Is.True);
                Assert.That(ColumnExists(connection, "reward_opportunities", "opportunity_state_id"), Is.True);
                Assert.That(ColumnExists(connection, "map_nodes", "catalog_path_id"), Is.True);
                Assert.That(ColumnExists(connection, "reward_choices", "focused_reward_slot_index"), Is.True);
                Assert.That(ColumnExists(connection, "reward_choices", "selected_reward_slot_index"), Is.True);
                Assert.That(ColumnExists(connection, "reward_cards", "reward_id"), Is.True);
                Assert.That(ColumnExists(connection, "reward_cards", "reward_slot_index"), Is.True);
                Assert.That(ColumnExists(connection, "reward_cards", "legal"), Is.True);
                Assert.That(ColumnExists(connection, "reward_cards", "is_fallback"), Is.True);
                Assert.That(ColumnExists(connection, "map_node_rewards", "reward_choice_id"), Is.True);
                Assert.That(ColumnExists(connection, "map_node_rewards", "card_reward_id"), Is.True);
                Assert.That(ColumnExists(connection, "map_node_rewards", "is_fallback"), Is.True);
                Assert.That(TableExists(connection, "route_nodes"), Is.False);
            }
        }
        finally
        {
            try
            {
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
            catch
            {
            }
        }
    }

    private static bool ContainsSql(List<string> statements, string fragment)
    {
        for (int i = 0; i < statements.Count; i++)
        {
            if (statements[i] != null && statements[i].Contains(fragment))
            {
                return true;
            }
        }

        return false;
    }

    private static void CreateLegacyVersionOneDatabase(string databasePath)
    {
        OfflineDatabaseProviderResolution provider = OfflineDatabaseProvider.Resolve();
        Assert.That(provider.Success, Is.True, provider.ErrorMessage);
        string initializationError = OfflineDatabaseProvider.Initialize(provider);
        Assert.That(initializationError, Is.Empty);

        using (IDbConnection connection = OfflineDatabaseProvider.CreateConnection(provider, databasePath))
        {
            connection.Open();
            ExecuteNonQuery(connection, @"
CREATE TABLE schema_version (
    id INTEGER PRIMARY KEY,
    version INTEGER NOT NULL,
    applied_at_utc TEXT NOT NULL,
    notes TEXT
);");
            ExecuteNonQuery(connection, @"
INSERT INTO schema_version (id, version, applied_at_utc, notes)
VALUES (1, 1, '2026-06-17T00:00:00.0000000Z', 'Legacy v1 before PRD37');");
            ExecuteNonQuery(connection, @"
CREATE TABLE offline_runs (
    run_id INTEGER PRIMARY KEY,
    account_id INTEGER NOT NULL,
    game_mode_id INTEGER NOT NULL,
    authority_source_id INTEGER NOT NULL,
    run_status_id INTEGER NOT NULL,
    starting_army_template_id TEXT NOT NULL,
    starting_army_variant_id TEXT NOT NULL,
    selected_starting_army_id TEXT NOT NULL,
    selected_route_choice_id TEXT NOT NULL,
    route_map_id INTEGER,
    current_node_id INTEGER,
    current_army_snapshot_id INTEGER,
    start_army_snapshot_id INTEGER,
    pre_final_army_snapshot_id INTEGER,
    current_run_gold INTEGER NOT NULL DEFAULT 0,
    stage_progress INTEGER NOT NULL DEFAULT 0,
    route_progress INTEGER NOT NULL DEFAULT 0,
    next_screen TEXT,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1
);");
            ExecuteNonQuery(connection, @"
CREATE TABLE map_node_rewards (
    reward_id INTEGER PRIMARY KEY,
    node_id INTEGER NOT NULL,
    reward_slot_index INTEGER NOT NULL,
    catalog_entry_id TEXT NOT NULL,
    base_snapshot_id INTEGER NOT NULL,
    target_snapshot_stack_id INTEGER,
    reward_type TEXT NOT NULL,
    unit_id TEXT,
    to_unit_id TEXT,
    amount INTEGER NOT NULL DEFAULT 0,
    currency_delta INTEGER NOT NULL DEFAULT 0,
    operation_json TEXT NOT NULL,
    is_selected INTEGER NOT NULL DEFAULT 0,
    applied_snapshot_id INTEGER,
    is_active INTEGER NOT NULL DEFAULT 1
);");
        }
    }

    private static bool ColumnExists(IDbConnection connection, string tableName, string columnName)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(" + tableName + ");";
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader["name"].ToString() == columnName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TableExists(IDbConnection connection, string tableName)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1;";
            IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            object result = command.ExecuteScalar();
            return result != null && result != System.DBNull.Value;
        }
    }

    private static void ExecuteNonQuery(IDbConnection connection, string sql)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
#endif
