#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using UnityEngine;

public class PRD40EncounterMaterializationTests
{
    [Test]
    public void StartRun_WithEnemyCatalog_MaterializesEnemySnapshotsForEveryBattleNode()
    {
        string databasePath = BuildTempDatabasePath();
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        RewardGeneratorRuleSet rewardRuleSet = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        EnemyEncounterRuleCatalog enemyCatalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        try
        {
            ruleSet.ConfigureMockDefaults();
            enemyCatalog.Entries = BuildEnemyRules(ruleSet, rewardRuleSet);
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units, ruleSet);

            StartRunResult startRun = BeginGeneratedRun(databasePath, catalog, units, enemyCatalog);

            Assert.That(startRun.Success, Is.True);
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies;"), Is.EqualTo(8));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL;"), Is.EqualTo(8));
                Assert.That(ScalarInt(connection, "SELECT COUNT(DISTINCT army_snapshot_id) FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL;"), Is.EqualTo(8));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies WHERE risk_band = 'Low';"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies WHERE risk_band = 'Medium';"), Is.EqualTo(3));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies WHERE risk_band = 'High';"), Is.EqualTo(3));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies WHERE risk_band = 'Boss';"), Is.EqualTo(1));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyCatalog);
            UnityEngine.Object.DestroyImmediate(rewardRuleSet);
            UnityEngine.Object.DestroyImmediate(ruleSet);
            TryDelete(databasePath);
        }
    }

    [Test]
    public void OfflineRunBattleEncounterCatalog_ReturnsMaterializedSnapshotAsEnemyArmySource()
    {
        string databasePath = BuildTempDatabasePath();
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        RewardGeneratorRuleSet rewardRuleSet = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        EnemyEncounterRuleCatalog enemyCatalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        try
        {
            ruleSet.ConfigureMockDefaults();
            enemyCatalog.Entries = BuildEnemyRules(ruleSet, rewardRuleSet);
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units, ruleSet);
            BeginGeneratedRun(databasePath, catalog, units, enemyCatalog);

            int nodeId;
            int snapshotId;
            string encounterId;
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                nodeId = ScalarInt(connection, "SELECT node_id FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL ORDER BY node_id LIMIT 1;");
                snapshotId = ScalarInt(connection, "SELECT army_snapshot_id FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL ORDER BY node_id LIMIT 1;");
                encounterId = ScalarText(connection, "SELECT encounter_id FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL ORDER BY node_id LIMIT 1;");
            }

            OfflineRunBattleEncounterCatalog encounterCatalog = new OfflineRunBattleEncounterCatalog(databasePath, new DefaultRunBattleEncounterCatalog());
            RunBattleEncounterDefinition encounter = encounterCatalog.FindEncounter(
                OfflineDatabaseLegacyIdentity.ToLegacyRouteNodeId(nodeId),
                encounterId);

            Assert.That(encounter, Is.Not.Null);
            Assert.That(encounter.EnemyArmySourceId, Is.EqualTo(OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(snapshotId)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyCatalog);
            UnityEngine.Object.DestroyImmediate(rewardRuleSet);
            UnityEngine.Object.DestroyImmediate(ruleSet);
            TryDelete(databasePath);
        }
    }

    [Test]
    public void OfflineRunBattleLaunchAdapter_WritesRuntimeSnapshotsToLegacyBattleInputs()
    {
        string databasePath = BuildTempDatabasePath();
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        RewardGeneratorRuleSet rewardRuleSet = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        EnemyEncounterRuleCatalog enemyCatalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        try
        {
            ruleSet.ConfigureMockDefaults();
            enemyCatalog.Entries = BuildEnemyRules(ruleSet, rewardRuleSet);
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units, ruleSet);
            BeginGeneratedRun(databasePath, catalog, units, enemyCatalog);

            int playerSnapshotId;
            int enemySnapshotId;
            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                playerSnapshotId = ScalarInt(connection, "SELECT current_army_snapshot_id FROM offline_runs ORDER BY run_id DESC LIMIT 1;");
                enemySnapshotId = ScalarInt(connection, "SELECT army_snapshot_id FROM map_node_enemies WHERE army_snapshot_id IS NOT NULL ORDER BY node_id LIMIT 1;");
            }

            OfflineRunBattleLaunchAdapter adapter = new OfflineRunBattleLaunchAdapter(
                databasePath,
                new OfflineArmySnapshotDbRepository(),
                units,
                DataMapper.Instance);
            RunBattleLaunchRecord launchRecord = adapter.CreateLaunchRecord(new RunBattleLaunchPayload(
                "run-battle-test",
                "run-1",
                "node-1",
                "encounter-test",
                OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(playerSnapshotId),
                OfflineDatabaseLegacyIdentity.ToLegacySnapshotId(enemySnapshotId),
                RunBattleEnemyGoal.TryToWin,
                "test"));

            PanelArmii.BuildG playerBuild = ReadLegacyBuild(OfflineRunBattleLaunchAdapter.RuntimePlayerBuildSlot);
            PanelArmii.BuildG enemyBuild = ReadLegacyBuild(OfflineRunBattleLaunchAdapter.RuntimeEnemyBuildSlot);

            Assert.That(launchRecord.PlayerArmyInputId, Does.Contain("legacy-build:" + OfflineRunBattleLaunchAdapter.RuntimePlayerBuildSlot));
            Assert.That(launchRecord.EnemyArmyInputId, Does.Contain("legacy-build:" + OfflineRunBattleLaunchAdapter.RuntimeEnemyBuildSlot));
            Assert.That(PlayerPrefs.GetInt("YourArmy"), Is.EqualTo(OfflineRunBattleLaunchAdapter.RuntimePlayerBuildSlot));
            Assert.That(PlayerPrefs.GetInt("EnemyArmy"), Is.EqualTo(OfflineRunBattleLaunchAdapter.RuntimeEnemyBuildSlot));
            Assert.That(PlayerPrefs.GetInt("AI"), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt("Multi"), Is.EqualTo(0));
            Assert.That(playerBuild.Units, Is.EquivalentTo(LoadSnapshotUnitIds(databasePath, playerSnapshotId)));
            Assert.That(enemyBuild.Units, Is.EquivalentTo(LoadSnapshotUnitIds(databasePath, enemySnapshotId)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyCatalog);
            UnityEngine.Object.DestroyImmediate(rewardRuleSet);
            UnityEngine.Object.DestroyImmediate(ruleSet);
            TryDeleteBuild(OfflineRunBattleLaunchAdapter.RuntimePlayerBuildSlot);
            TryDeleteBuild(OfflineRunBattleLaunchAdapter.RuntimeEnemyBuildSlot);
            PlayerPrefs.DeleteKey("YourArmy");
            PlayerPrefs.DeleteKey("EnemyArmy");
            PlayerPrefs.DeleteKey("AI");
            PlayerPrefs.DeleteKey("Multi");
            TryDelete(databasePath);
        }
    }

    [Test]
    public void StartRun_WithEnemyUnitSourceButNoEnemyCatalog_FailsClearly()
    {
        string databasePath = BuildTempDatabasePath();
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        try
        {
            ruleSet.ConfigureMockDefaults();
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units, ruleSet);
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, catalog, units, units, null));
            string routeId = catalog.ListRoutePreviews()[0].RouteId;
            StartingArmyTemplate army = catalog.ListStartingArmies()[0];

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(delegate
            {
                startRunService.BeginRun(new StartRunCommand(
                    "offline-player",
                    army.TemplateId,
                    army.VariantId,
                    army.TemplateId,
                    routeId));
            });

            Assert.That(exception.Message, Does.Contain("EnemyEncounterRuleCatalog"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(ruleSet);
            TryDelete(databasePath);
        }
    }

    private static StartRunResult BeginGeneratedRun(
        string databasePath,
        DeterministicRunGenerationCatalog catalog,
        TestUnitCatalog units,
        EnemyEncounterRuleCatalog enemyCatalog)
    {
        StartRunService startRunService = new StartRunService(
            catalog,
            catalog,
            units,
            new OfflineStartRunDbStore(databasePath, catalog, units, units, enemyCatalog));
        string routeId = catalog.ListRoutePreviews()[0].RouteId;
        StartingArmyTemplate army = catalog.ListStartingArmies()[0];

        return startRunService.BeginRun(new StartRunCommand(
            "offline-player",
            army.TemplateId,
            army.VariantId,
            army.TemplateId,
            routeId));
    }

    private static DeterministicRunGenerationCatalog CreateCatalog(IStartRunUnitPoolSource units, ArmyGeneratorRuleSet ruleSet)
    {
        return new DeterministicRunGenerationCatalog(
            units,
            ruleSet,
            StartingArmyGeneratorConfig.CreateDefault(ruleSet),
            RouteGeneratorConfig.CreateDefault(),
            null);
    }

    private static List<EnemyEncounterRule> BuildEnemyRules(
        ArmyGeneratorRuleSet ruleSet,
        RewardGeneratorRuleSet rewardRuleSet)
    {
        return new List<EnemyEncounterRule>
        {
            new EnemyEncounterRule(EnemyEncounterDifficulty.Low, ruleSet, rewardRuleSet, string.Empty),
            new EnemyEncounterRule(EnemyEncounterDifficulty.Medium, ruleSet, rewardRuleSet, string.Empty),
            new EnemyEncounterRule(EnemyEncounterDifficulty.High, ruleSet, rewardRuleSet, string.Empty),
            new EnemyEncounterRule(EnemyEncounterDifficulty.Boss, ruleSet, string.Empty)
        };
    }

    private static int ScalarInt(IDbConnection connection, string sql)
    {
        return OfflineDatabaseSql.ReadInt(OfflineDatabaseSql.ExecuteScalar(connection, sql));
    }

    private static string ScalarText(IDbConnection connection, string sql)
    {
        return OfflineDatabaseSql.ReadText(OfflineDatabaseSql.ExecuteScalar(connection, sql));
    }

    private static List<string> LoadSnapshotUnitIds(string databasePath, int snapshotId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return OfflineDatabaseSql.Query(
                connection,
                @"
SELECT unit_id
FROM army_snapshot_stacks
WHERE snapshot_id = @snapshotId AND amount > 0 AND is_active = 1
ORDER BY formation_slot, snapshot_stack_id;",
                delegate(IDataRecord row)
                {
                    return OfflineDatabaseSql.ReadText(row["unit_id"]);
                },
                null,
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));
        }
    }

    private static PanelArmii.BuildG ReadLegacyBuild(int buildSlot)
    {
        string path = DataMapper.Instance.GetBuildFilePath(buildSlot);
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.OpenRead(path);
        try
        {
            return (PanelArmii.BuildG)formatter.Deserialize(file);
        }
        finally
        {
            file.Close();
        }
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_PRD40_" + Guid.NewGuid().ToString("N") + ".db");
    }

    private static void TryDeleteBuild(int buildSlot)
    {
        TryDelete(DataMapper.Instance.GetBuildFilePath(buildSlot));
    }

    private static void TryDelete(string databasePath)
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

    private sealed class TestUnitCatalog : IStartRunUnitPoolSource, IOfflineArmySnapshotCatalogResolver
    {
        private readonly Dictionary<string, StartRunUnitDefinition> units = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", Unit("Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", Unit("Axeman", "II", 97, "Slash") },
            { "StoneGolem", Unit("StoneGolem", "II", 67, "Stone_Throw") },
            { "StoneLord", Unit("StoneLord", "III", 120, "Stone_Skin") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<StartRunUnitDefinition> ListUnits()
        {
            return new List<StartRunUnitDefinition>(units.Values);
        }

        OfflineArmySnapshotUnitCatalogEntry IOfflineArmySnapshotCatalogResolver.FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return units.TryGetValue(unitId, out unit)
                ? new OfflineArmySnapshotUnitCatalogEntry(unit.UnitId, unit.DisplayName, unit.Tier, unit.Cost, new List<string>(unit.SkillIds))
                : null;
        }

        private static StartRunUnitDefinition Unit(string unitId, string tier, int cost, params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, unitId, tier, cost, UnitFactionResolver.ResolveFactionId(unitId), UnitRoleCategory.Flexible, new List<string>(skills));
        }
    }
}
#endif
