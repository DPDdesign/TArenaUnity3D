using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class OfflineRunBattleLaunchAdapter : IRunBattleLaunchAdapter
{
    public const int RuntimePlayerBuildSlot = 9001;
    public const int RuntimeEnemyBuildSlot = 9002;

    private readonly string databasePath;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository;
    private readonly IOfflineArmySnapshotCatalogResolver snapshotCatalogResolver;
    private readonly DataMapper dataMapper;
    private readonly bool writeLegacyBattleInputs;

    public OfflineRunBattleLaunchAdapter()
        : this(
            false,
            null,
            new OfflineArmySnapshotDbRepository(),
            new DataMapperOfflineArmySnapshotCatalogResolver(DataMapper.Instance),
            DataMapper.Instance)
    {
    }

    public OfflineRunBattleLaunchAdapter(
        string databasePath,
        OfflineArmySnapshotDbRepository snapshotRepository,
        IOfflineArmySnapshotCatalogResolver snapshotCatalogResolver,
        DataMapper dataMapper)
        : this(true, databasePath, snapshotRepository, snapshotCatalogResolver, dataMapper)
    {
    }

    private OfflineRunBattleLaunchAdapter(
        bool writeLegacyBattleInputs,
        string databasePath,
        OfflineArmySnapshotDbRepository snapshotRepository,
        IOfflineArmySnapshotCatalogResolver snapshotCatalogResolver,
        DataMapper dataMapper)
    {
        this.writeLegacyBattleInputs = writeLegacyBattleInputs;
        this.databasePath = databasePath;
        this.snapshotRepository = snapshotRepository ?? new OfflineArmySnapshotDbRepository();
        this.snapshotCatalogResolver = snapshotCatalogResolver;
        this.dataMapper = dataMapper == null ? DataMapper.Instance : dataMapper;
    }

    public RunBattleLaunchRecord CreateLaunchRecord(RunBattleLaunchPayload payload)
    {
        if (payload == null)
        {
            return null;
        }

        if (!writeLegacyBattleInputs)
        {
            return new RunBattleLaunchRecord(
                "runtime-launch-" + Guid.NewGuid().ToString("N"),
                payload.RunBattleId,
                "player-snapshot:" + payload.CurrentArmySnapshotId,
                "enemy-source:" + payload.EnemyArmySourceId,
                "runtime snapshot battle input",
                "offline-runtime-run-battle-launch-adapter");
        }

        RunBattleArmySnapshot playerArmy = LoadSnapshot(payload.CurrentArmySnapshotId, "player");
        RunBattleArmySnapshot enemyArmy = LoadSnapshot(payload.EnemyArmySourceId, "enemy");

        WriteLegacyBuild(RuntimePlayerBuildSlot, "Run Player Army", playerArmy);
        WriteLegacyBuild(RuntimeEnemyBuildSlot, "Run Enemy Army", enemyArmy);
        PlayerPrefs.SetInt("YourArmy", RuntimePlayerBuildSlot);
        PlayerPrefs.SetInt("EnemyArmy", RuntimeEnemyBuildSlot);
        PlayerPrefs.SetInt("AI", 1);
        LocalGameSession.ForceLocalMode();
        PlayerPrefs.Save();

        return new RunBattleLaunchRecord(
            "runtime-launch-" + Guid.NewGuid().ToString("N"),
            payload.RunBattleId,
            "legacy-build:" + RuntimePlayerBuildSlot + ":" + payload.CurrentArmySnapshotId,
            "legacy-build:" + RuntimeEnemyBuildSlot + ":" + payload.EnemyArmySourceId,
            "runtime snapshot to legacy build bridge",
            "offline-runtime-run-battle-launch-adapter");
    }

    private RunBattleArmySnapshot LoadSnapshot(string snapshotIdText, string owner)
    {
        int snapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshotIdText);
        if (snapshotId <= 0)
        {
            throw new InvalidOperationException("Run battle " + owner + " snapshot id is missing: " + snapshotIdText);
        }

        OfflineArmySnapshotRecord snapshot;
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            snapshot = snapshotRepository.LoadSnapshot(connection, snapshotId);
        }

        if (snapshot == null)
        {
            throw new InvalidOperationException("Run battle " + owner + " snapshot was not found: " + snapshotIdText);
        }

        return OfflineArmySnapshotMapper.ToRunBattle(snapshot, snapshotCatalogResolver);
    }

    private void WriteLegacyBuild(int buildSlot, string displayName, RunBattleArmySnapshot army)
    {
        PanelArmii.BuildG build = ToLegacyBuild(displayName, army);
        string path = dataMapper.GetBuildFilePath(buildSlot);
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Create(path);
        try
        {
            formatter.Serialize(file, build);
        }
        finally
        {
            file.Close();
        }
    }

    private static PanelArmii.BuildG ToLegacyBuild(string displayName, RunBattleArmySnapshot army)
    {
        if (army == null || army.Stacks == null || army.Stacks.Count == 0)
        {
            throw new InvalidOperationException("Run battle army snapshot has no stacks: " + displayName);
        }

        PanelArmii.BuildG build = new PanelArmii.BuildG();
        build.hero = 0;
        build.NazwaBohatera = displayName;

        List<RunBattleStackSnapshot> stacks = RunBattleTacticalStackReconciler.BuildPreparedStacksInBattleInputOrder(army);

        for (int i = 0; i < stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = stacks[i];
            string unitId = RunBattleTacticalStackReconciler.ResolveBattleInputUnitId(stack);

            build.Units.Add(unitId);
            build.NoUnits.Add(stack.Amount);
            build.Costs.Add(Math.Max(0, stack.CombatValue));
        }

        if (build.Units.Count == 0)
        {
            throw new InvalidOperationException("Run battle army snapshot has no playable stacks: " + displayName);
        }

        return build;
    }
}
