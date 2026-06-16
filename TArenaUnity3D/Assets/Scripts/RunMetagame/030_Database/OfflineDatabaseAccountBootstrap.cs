using System;
using System.Data;

public static class OfflineDatabaseAccountBootstrap
{
    public const int DefaultAccountId = 1;
    private const string DefaultDisplayName = "Offline Account";

    public static int EnsureDefaultAccount(IDbConnection connection, IDbTransaction transaction, string externalAccountId)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT account_id FROM offline_accounts WHERE account_id = @accountId LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", DefaultAccountId));

        if (existing != null && existing != DBNull.Value)
        {
            return DefaultAccountId;
        }

        string now = OfflineDatabaseSql.UtcNowText();
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO offline_accounts (
    account_id,
    external_account_id,
    display_name,
    created_at_utc,
    updated_at_utc,
    unlocked_saved_army_slots,
    is_active
) VALUES (
    @accountId,
    @externalAccountId,
    @displayName,
    @createdAtUtc,
    @updatedAtUtc,
    @unlockedSavedArmySlots,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", DefaultAccountId),
            new OfflineDatabaseSqlParameter("@externalAccountId", string.IsNullOrEmpty(externalAccountId) ? "offline-player" : externalAccountId),
            new OfflineDatabaseSqlParameter("@displayName", DefaultDisplayName),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
            new OfflineDatabaseSqlParameter("@unlockedSavedArmySlots", 2));

        return DefaultAccountId;
    }
}
