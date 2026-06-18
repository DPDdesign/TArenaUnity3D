using System.Collections.Generic;
using System.Data;

public class OfflineStartRunUnlockContextSource
{
    private readonly string databasePath;

    public OfflineStartRunUnlockContextSource()
        : this(null)
    {
    }

    public OfflineStartRunUnlockContextSource(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public StartRunGenerationUnlockContext LoadUnlockContext(string accountPlayerId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, null, accountPlayerId);
            return new StartRunGenerationUnlockContext(
                ReadUnlocks(connection, accountId, DBUnlockTypeId.Unit),
                ReadUnlocks(connection, accountId, DBUnlockTypeId.Skill));
        }
    }

    private static List<string> ReadUnlocks(IDbConnection connection, int accountId, DBUnlockTypeId unlockType)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT target_id
FROM account_unlocks
WHERE account_id = @accountId
  AND unlock_type_id = @unlockTypeId
  AND is_active = 1
ORDER BY unlock_id;",
            row => OfflineDatabaseSql.ReadText(row["target_id"]),
            null,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@unlockTypeId", (int)unlockType));
    }
}
