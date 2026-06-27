using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using UnityEngine;

public class OfflineDatabaseModule
{
    public const string DefaultDatabaseFileName = "TArenaOffline.db";
    private const string InitialSchemaNotes = "Initial Offline Mode schema";

    private readonly string databasePath;

    public OfflineDatabaseModule()
        : this(GetDefaultDatabasePath())
    {
    }

    public OfflineDatabaseModule(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public string DatabasePath
    {
        get { return databasePath; }
    }

    public OfflineDatabaseOpenResult OpenOrCreate()
    {
        if (string.IsNullOrEmpty(databasePath))
        {
            return Fail(OfflineDatabaseError.InvalidPath, "Database path is empty.", string.Empty, string.Empty, 0);
        }

        string fullPath = Path.GetFullPath(databasePath);
        string directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            return Fail(OfflineDatabaseError.InvalidPath, "Database path directory is invalid.", fullPath, string.Empty, 0);
        }

        Directory.CreateDirectory(directory);

        OfflineDatabaseProviderResolution provider = OfflineDatabaseProvider.Resolve();
        if (!provider.Success)
        {
            return Fail(OfflineDatabaseError.ProviderNotFound, provider.ErrorMessage, fullPath, string.Empty, 0);
        }

        string initializationError = OfflineDatabaseProvider.Initialize(provider);
        if (!string.IsNullOrEmpty(initializationError))
        {
            return Fail(
                OfflineDatabaseError.ProviderInitializationFailed,
                initializationError,
                fullPath,
                provider.ProviderName,
                0);
        }

        try
        {
            using (IDbConnection connection = OfflineDatabaseProvider.CreateConnection(provider, fullPath))
            {
                if (connection == null)
                {
                    return Fail(OfflineDatabaseError.OpenFailed, "Could not create SQLite connection instance.", fullPath, provider.ProviderName, 0);
                }

                connection.Open();
                ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
                EnsureBootstrapSchema(connection);
                int currentVersion = ReadCurrentVersion(connection);

                if (currentVersion < OfflineDatabaseSchemaV1.Version)
                {
                    ApplySchemaVersion1(connection);
                    currentVersion = ReadCurrentVersion(connection);
                }

                EnsureSchemaVersion1Compatibility(connection);

                return new OfflineDatabaseOpenResult(
                    true,
                    OfflineDatabaseError.None,
                    "Offline database is ready.",
                    fullPath,
                    provider.ProviderName,
                    currentVersion);
            }
        }
        catch (Exception ex)
        {
            return Fail(OfflineDatabaseError.MigrationFailed, ex.Message, fullPath, provider.ProviderName, 0);
        }
    }

    public static string GetDefaultDatabasePath()
    {
        return Path.Combine(Application.persistentDataPath, DefaultDatabaseFileName);
    }

    private static void EnsureBootstrapSchema(IDbConnection connection)
    {
        ExecuteNonQuery(connection, @"
CREATE TABLE IF NOT EXISTS schema_version (
    id INTEGER PRIMARY KEY,
    version INTEGER NOT NULL,
    applied_at_utc TEXT NOT NULL,
    notes TEXT
);");
    }

    private static void ApplySchemaVersion1(IDbConnection connection)
    {
        IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            List<string> statements = OfflineDatabaseSchemaV1.BuildStatements();
            for (int i = 0; i < statements.Count; i++)
            {
                ExecuteNonQuery(connection, statements[i], transaction);
            }

            ExecuteNonQuery(connection, "DELETE FROM schema_version;", transaction);
            ExecuteNonQuery(
                connection,
                "INSERT INTO schema_version (id, version, applied_at_utc, notes) VALUES (1, " +
                OfflineDatabaseSchemaV1.Version.ToString(CultureInfo.InvariantCulture) +
                ", '" + UtcNowText() + "', '" + InitialSchemaNotes + "');",
                transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void EnsureSchemaVersion1Compatibility(IDbConnection connection)
    {
        IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            AddColumnIfMissing(
                connection,
                transaction,
                "offline_runs",
                "run_seed",
                "ALTER TABLE offline_runs ADD COLUMN run_seed INTEGER NOT NULL DEFAULT 35035;");
            AddColumnIfMissing(
                connection,
                transaction,
                "offline_runs",
                "run_seed_version",
                "ALTER TABLE offline_runs ADD COLUMN run_seed_version INTEGER NOT NULL DEFAULT 1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "player_preferences",
                "float_value",
                "ALTER TABLE player_preferences ADD COLUMN float_value REAL NOT NULL DEFAULT 0;");

            List<string> statements = OfflineDatabaseSchemaV1.BuildStatements();
            for (int i = 0; i < statements.Count; i++)
            {
                ExecuteNonQuery(connection, statements[i], transaction);
            }

            AddColumnIfMissing(
                connection,
                transaction,
                "map_nodes",
                "catalog_path_id",
                "ALTER TABLE map_nodes ADD COLUMN catalog_path_id TEXT;");
            AddColumnIfMissing(
                connection,
                transaction,
                "map_node_rewards",
                "reward_choice_id",
                "ALTER TABLE map_node_rewards ADD COLUMN reward_choice_id INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "map_node_rewards",
                "card_reward_id",
                "ALTER TABLE map_node_rewards ADD COLUMN card_reward_id TEXT NOT NULL DEFAULT '';");
            AddColumnIfMissing(
                connection,
                transaction,
                "map_node_rewards",
                "legal",
                "ALTER TABLE map_node_rewards ADD COLUMN legal INTEGER NOT NULL DEFAULT 1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "map_node_rewards",
                "error_id",
                "ALTER TABLE map_node_rewards ADD COLUMN error_id INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "map_node_rewards",
                "is_fallback",
                "ALTER TABLE map_node_rewards ADD COLUMN is_fallback INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_choices",
                "focused_reward_slot_index",
                "ALTER TABLE reward_choices ADD COLUMN focused_reward_slot_index INTEGER NOT NULL DEFAULT -1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_choices",
                "selected_reward_slot_index",
                "ALTER TABLE reward_choices ADD COLUMN selected_reward_slot_index INTEGER NOT NULL DEFAULT -1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "reward_id",
                "ALTER TABLE reward_cards ADD COLUMN reward_id TEXT NOT NULL DEFAULT '';");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "reward_slot_index",
                "ALTER TABLE reward_cards ADD COLUMN reward_slot_index INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "affected_stack_id",
                "ALTER TABLE reward_cards ADD COLUMN affected_stack_id TEXT;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "affected_slot_index",
                "ALTER TABLE reward_cards ADD COLUMN affected_slot_index INTEGER NOT NULL DEFAULT -1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "operation_type",
                "ALTER TABLE reward_cards ADD COLUMN operation_type TEXT NOT NULL DEFAULT '';");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "legal",
                "ALTER TABLE reward_cards ADD COLUMN legal INTEGER NOT NULL DEFAULT 1;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "error_id",
                "ALTER TABLE reward_cards ADD COLUMN error_id INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "is_selected",
                "ALTER TABLE reward_cards ADD COLUMN is_selected INTEGER NOT NULL DEFAULT 0;");
            AddColumnIfMissing(
                connection,
                transaction,
                "reward_cards",
                "is_fallback",
                "ALTER TABLE reward_cards ADD COLUMN is_fallback INTEGER NOT NULL DEFAULT 0;");

            DropTableIfExists(connection, transaction, "route_nodes");
            DropTableIfExists(connection, transaction, "route_paths");
            DropTableIfExists(connection, transaction, "route_maps");

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void DropTableIfExists(IDbConnection connection, IDbTransaction transaction, string tableName)
    {
        ExecuteNonQuery(connection, "DROP TABLE IF EXISTS " + tableName + ";", transaction);
    }

    private static void AddColumnIfMissing(
        IDbConnection connection,
        IDbTransaction transaction,
        string tableName,
        string columnName,
        string alterSql)
    {
        if (ColumnExists(connection, transaction, tableName, columnName))
        {
            return;
        }

        ExecuteNonQuery(connection, alterSql, transaction);
    }

    private static bool ColumnExists(
        IDbConnection connection,
        IDbTransaction transaction,
        string tableName,
        string columnName)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(" + tableName + ");";
            command.Transaction = transaction;
            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(Convert.ToString(reader["name"], CultureInfo.InvariantCulture), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static int ReadCurrentVersion(IDbConnection connection)
    {
        object result = ExecuteScalar(connection, "SELECT version FROM schema_version WHERE id = 1 LIMIT 1;");
        if (result == null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static void ExecuteNonQuery(IDbConnection connection, string sql, IDbTransaction transaction = null)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            command.ExecuteNonQuery();
        }
    }

    private static object ExecuteScalar(IDbConnection connection, string sql)
    {
        using (IDbCommand command = connection.CreateCommand())
        {
            command.CommandText = sql;
            return command.ExecuteScalar();
        }
    }

    private static string UtcNowText()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    private static OfflineDatabaseOpenResult Fail(
        OfflineDatabaseError error,
        string message,
        string path,
        string providerName,
        int schemaVersion)
    {
        return new OfflineDatabaseOpenResult(false, error, message, path, providerName, schemaVersion);
    }
}
