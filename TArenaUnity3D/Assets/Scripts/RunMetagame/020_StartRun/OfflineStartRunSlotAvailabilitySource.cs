using System.Data;

public class OfflineStartRunSlotAvailabilitySource : IStartRunSlotAvailabilitySource
{
    private const int XpPerLevel = 250;

    private readonly string databasePath;

    public OfflineStartRunSlotAvailabilitySource()
        : this(null)
    {
    }

    public OfflineStartRunSlotAvailabilitySource(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public StartRunSlotAvailabilityContext LoadAvailabilityContext(string accountPlayerId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, null, accountPlayerId);
            int accountXp = ReadAccountXp(connection, accountId);
            return new StartRunSlotAvailabilityContext(ToLevel(accountXp), HasWonRun(connection, accountId));
        }
    }

    private static int ReadAccountXp(IDbConnection connection, int accountId)
    {
        object xp = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT account_xp FROM offline_accounts WHERE account_id = @accountId AND is_active = 1 LIMIT 1;",
            null,
            new OfflineDatabaseSqlParameter("@accountId", accountId));
        return OfflineDatabaseSql.ReadInt(xp, 0);
    }

    private static bool HasWonRun(IDbConnection connection, int accountId)
    {
        object count = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT COUNT(*)
FROM run_summaries summaries
INNER JOIN offline_runs runs ON runs.run_id = summaries.run_id
WHERE runs.account_id = @accountId
  AND runs.is_active = 1
  AND summaries.is_active = 1
  AND summaries.final_result_id = @wonResultId;",
            null,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@wonResultId", (int)DBFinalResultId.Won));
        return OfflineDatabaseSql.ReadInt(count, 0) > 0;
    }

    private static int ToLevel(int accountXp)
    {
        return (System.Math.Max(0, accountXp) / XpPerLevel) + 1;
    }
}
