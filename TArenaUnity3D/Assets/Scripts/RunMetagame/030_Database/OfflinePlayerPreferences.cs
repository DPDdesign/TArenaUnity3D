using System;
using System.Data;
using UnityEngine;

public static class OfflinePlayerPreferences
{
    static bool smartCastCached;
    static bool smartCastValue;
    static bool animationSpeedCached;
    static float animationSpeedValue = OfflineDatabaseAccountBootstrap.DefaultAnimationSpeedPreferenceValue;

    public static bool IsSmartCastEnabled()
    {
        if (!smartCastCached)
        {
            smartCastValue = ReadBool(OfflineDatabaseAccountBootstrap.SmartCastPreferenceKey, false);
            smartCastCached = true;
        }

        return smartCastValue;
    }

    public static void SetSmartCastEnabled(bool enabled)
    {
        smartCastValue = enabled;
        smartCastCached = true;
        WriteBool(OfflineDatabaseAccountBootstrap.SmartCastPreferenceKey, enabled);
    }

    public static float GetAnimationSpeedMultiplier()
    {
        if (!animationSpeedCached)
        {
            animationSpeedValue = ReadFloat(
                OfflineDatabaseAccountBootstrap.AnimationSpeedPreferenceKey,
                OfflineDatabaseAccountBootstrap.DefaultAnimationSpeedPreferenceValue);
            animationSpeedCached = true;
        }

        return animationSpeedValue;
    }

    public static void SetAnimationSpeedMultiplier(float multiplier)
    {
        animationSpeedValue = Mathf.Max(0f, multiplier);
        animationSpeedCached = true;
        WriteFloat(
            OfflineDatabaseAccountBootstrap.AnimationSpeedPreferenceKey,
            animationSpeedValue);
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

    static float ReadFloat(string preferenceKey, float fallback)
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
SELECT float_value
FROM player_preferences
WHERE account_id = @accountId
  AND preference_key = @preferenceKey
LIMIT 1;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@accountId", accountId),
                    new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));
                transaction.Commit();
                return OfflineDatabaseSql.ReadFloat(result, fallback);
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
                UpsertPreferenceValue(
                    connection,
                    transaction,
                    new OfflineDatabaseSqlParameter("@boolValue", value ? 1 : 0),
                    new OfflineDatabaseSqlParameter("@floatValue", 0f),
                    accountId,
                    preferenceKey,
                    "bool_value = @boolValue");
                transaction.Commit();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Offline player preference write failed: " + ex.Message);
        }
    }

    static void WriteFloat(string preferenceKey, float value)
    {
        try
        {
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection())
            using (IDbTransaction transaction = connection.BeginTransaction())
            {
                int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
                UpsertPreferenceValue(
                    connection,
                    transaction,
                    new OfflineDatabaseSqlParameter("@boolValue", 0),
                    new OfflineDatabaseSqlParameter("@floatValue", value),
                    accountId,
                    preferenceKey,
                    "float_value = @floatValue");
                transaction.Commit();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Offline player preference write failed: " + ex.Message);
        }
    }

    static void UpsertPreferenceValue(
        IDbConnection connection,
        IDbTransaction transaction,
        OfflineDatabaseSqlParameter boolValueParameter,
        OfflineDatabaseSqlParameter floatValueParameter,
        int accountId,
        string preferenceKey,
        string updateAssignment)
    {
        int updatedRows = OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE player_preferences
SET " + updateAssignment + @",
    updated_at_utc = @updatedAtUtc
WHERE account_id = @accountId
  AND preference_key = @preferenceKey;",
            transaction,
            boolValueParameter,
            floatValueParameter,
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));

        if (updatedRows > 0)
        {
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO player_preferences (
    account_id,
    preference_key,
    bool_value,
    float_value,
    updated_at_utc
) VALUES (
    @accountId,
    @preferenceKey,
    @boolValue,
    @floatValue,
    @updatedAtUtc
);",
            transaction,
            boolValueParameter,
            floatValueParameter,
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@preferenceKey", preferenceKey));
    }
}
