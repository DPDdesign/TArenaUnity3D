using System;
using System.Data;
using UnityEngine;

public static class OfflinePlayerPreferences
{
    public static bool IsSmartCastEnabled()
    {
        return ReadBool(OfflineDatabaseAccountBootstrap.SmartCastPreferenceKey, false);
    }

    public static void SetSmartCastEnabled(bool enabled)
    {
        WriteBool(OfflineDatabaseAccountBootstrap.SmartCastPreferenceKey, enabled);
    }

    static bool ReadBool(string preferenceKey, bool fallback)
    {
        try
        {
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection())
            using (IDbTransaction transaction = connection.BeginTransaction())
            {
                int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
                object result = OfflineDatabaseSql.ExecuteScalar(
                    connection,
                    @"
SELECT bool_value
FROM player_preferences
WHERE account_id = @accountId
  AND preference_key = @preferenceKey
LIMIT 1;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@accountId", accountId),
                    new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));
                transaction.Commit();
                return OfflineDatabaseSql.ReadBool(result, fallback);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Offline player preference read failed: " + ex.Message);
            return fallback;
        }
    }

    static void WriteBool(string preferenceKey, bool value)
    {
        try
        {
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection())
            using (IDbTransaction transaction = connection.BeginTransaction())
            {
                int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
UPDATE player_preferences
SET bool_value = @boolValue,
    updated_at_utc = @updatedAtUtc
WHERE account_id = @accountId
  AND preference_key = @preferenceKey;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@boolValue", value ? 1 : 0),
                    new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
                    new OfflineDatabaseSqlParameter("@accountId", accountId),
                    new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));
                transaction.Commit();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Offline player preference write failed: " + ex.Message);
        }
    }
}
