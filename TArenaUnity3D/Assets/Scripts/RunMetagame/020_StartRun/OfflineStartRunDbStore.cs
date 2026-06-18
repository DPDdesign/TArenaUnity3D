using System;
using System.Collections.Generic;
using System.Data;

public class OfflineStartRunDbStore : IStartRunRecordStore
{
    private readonly string databasePath;
    private readonly IRunMapPathCatalog pathCatalog;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly IStartRunUnitPoolSource enemyUnitSource;
    private readonly EnemyEncounterRuleCatalog enemyRuleCatalog;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
    private readonly OfflineRunContextDbWriter runContextWriter = new OfflineRunContextDbWriter();

    public OfflineStartRunDbStore()
        : this(null, new DefaultRunMapPathCatalog())
    {
    }

    public OfflineStartRunDbStore(string databasePath, IRunMapPathCatalog pathCatalog)
        : this(databasePath, pathCatalog, null)
    {
    }

    public OfflineStartRunDbStore(string databasePath, IRunMapPathCatalog pathCatalog, IOfflineArmySnapshotCatalogResolver resolver)
        : this(databasePath, pathCatalog, resolver, null, null)
    {
    }

    public OfflineStartRunDbStore(
        string databasePath,
        IRunMapPathCatalog pathCatalog,
        IOfflineArmySnapshotCatalogResolver resolver,
        IStartRunUnitPoolSource enemyUnitSource,
        EnemyEncounterRuleCatalog enemyRuleCatalog)
    {
        this.databasePath = databasePath;
        this.pathCatalog = pathCatalog ?? new DefaultRunMapPathCatalog();
        this.resolver = resolver;
        this.enemyUnitSource = enemyUnitSource;
        this.enemyRuleCatalog = enemyRuleCatalog;
    }

    public CreatedRunRecord SaveCreatedRun(CreatedRunRecord record)
    {
        if (record == null)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, record.AccountPlayerId);
            int runId = runContextWriter.InsertStartRun(connection, transaction, record, accountId);
            int runSeed = ParseRunSeed(record.RoutePreviewOptionId);
            int routeMapId = SeedRouteMap(connection, transaction, runId, record.RoutePreviewOptionId, accountId, runSeed);
            int snapshotId = snapshotRepository.SaveSnapshot(connection, transaction, OfflineArmySnapshotMapper.FromStartRun(record.InitialArmySnapshot, accountId, runId));
            runContextWriter.AttachStartRunRouteAndArmy(connection, transaction, runId, routeMapId, snapshotId, record.StartingCurrency);

            transaction.Commit();

            OfflineRunContextDbReader reader = new OfflineRunContextDbReader(databasePath, resolver);
            CreatedRunRecord persisted = reader.ToStartRunCreatedRecord(reader.LoadRun(OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId)));
            if (persisted == null)
            {
                throw new InvalidOperationException("Start Run DB store could not reload the persisted run context.");
            }

            return persisted;
        }
    }

    private int SeedRouteMap(IDbConnection connection, IDbTransaction transaction, int runId, string selectedRouteChoiceId, int accountId, int runSeed)
    {
        int routeMapId = runId;
        int nextRoutePathId = ReadNextId(connection, "map_nodes", "route_path_id", transaction);
        int nextNodeId = ReadNextId(connection, "map_nodes", "node_id", transaction);
        List<RunMapPathDefinition> paths = pathCatalog.BuildPaths(selectedRouteChoiceId);
        OfflineRouteMapSeedRecord seed = OfflineRouteMapSeedFactory.Create(runId, routeMapId, selectedRouteChoiceId, paths, nextRoutePathId, nextNodeId);

        OfflineMaterializedRunMapDbStore.SaveMaterializedMap(connection, transaction, seed, enemyUnitSource, enemyRuleCatalog, accountId, runSeed);

        return routeMapId;
    }

    private static int ReadNextId(IDbConnection connection, string tableName, string keyColumn, IDbTransaction transaction)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT COALESCE(MAX(" + keyColumn + "), 0) + 1 FROM " + tableName + ";",
            transaction);
        return OfflineDatabaseSql.ReadInt(result, 1);
    }

    private static int ParseRunSeed(string routeChoiceId)
    {
        if (string.IsNullOrEmpty(routeChoiceId))
        {
            return 35035;
        }

        string[] parts = routeChoiceId.Split('-');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] != "seed")
            {
                continue;
            }

            int parsed;
            if (int.TryParse(parts[i + 1], out parsed))
            {
                return parsed == 0 ? 35035 : parsed;
            }
        }

        return 35035;
    }

}
