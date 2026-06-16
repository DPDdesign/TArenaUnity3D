using System.Collections.Generic;
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
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS route_nodes"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS run_events"), Is.True);
        Assert.That(ContainsSql(statements, "CREATE TABLE IF NOT EXISTS shop_offers"), Is.True);
        Assert.That(ContainsSql(statements, "offer_id TEXT NOT NULL"), Is.True);
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
        Assert.That((int)DBResultKindId.DefenceLoss, Is.EqualTo(4));
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
}
