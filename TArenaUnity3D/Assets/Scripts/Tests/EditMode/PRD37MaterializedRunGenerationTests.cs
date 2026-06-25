#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class PRD37MaterializedRunGenerationTests
{
    [Test]
    public void StartRun_PersistsMaterializedMapNodesConnectionsAndSeed()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
            StartRunResult startRun = BeginGeneratedRun(databasePath, catalog, units);

            Assert.That(startRun.Success, Is.True);

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_nodes;"), Is.EqualTo(13));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_connections WHERE from_node_id = 3;"), Is.EqualTo(2));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_enemies;"), Is.GreaterThan(0));
                Assert.That(ScalarInt(connection, "SELECT run_seed_version FROM offline_runs LIMIT 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT run_seed FROM offline_runs LIMIT 1;"), Is.EqualTo(35035));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void BattleCompletion_MaterializesRewardRowsAndRewardMapDoesNotReroll()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
            StartRunResult startRun = BeginGeneratedRun(databasePath, catalog, units);
            RunMapNodeDefinition firstBattleNode = catalog.BuildPaths(catalog.ListRoutePreviews()[0].RouteId)[0].Nodes[0];

            RunBattleService battleService = new RunBattleService(
                new DefaultRunBattleEncounterCatalog(),
                new OfflineRunBattleLaunchAdapter(),
                new OfflineRunBattleDbStore(databasePath, units, new DefaultRunBattleEncounterCatalog(), units));

            RunBattleLaunchViewData prepared = battleService.PrepareBattle(new RunBattlePrepareRequest(
                startRun.CreatedRun.RunId,
                firstBattleNode.NodeId,
                firstBattleNode.EncounterId,
                1,
                startRun.CreatedRun.StartingCurrency,
                CreateBattleArmy()));

            Assert.That(prepared.CanLaunch, Is.True);

            RunBattleCompletionResult completion = battleService.CompleteBattle(new RunBattleCompletionPayload(
                prepared.RunBattleId,
                RunBattleOutcome.Win,
                CreateAfterBattleArmy(prepared.CurrentArmy),
                45,
                "completion-prd37",
                "prd37-test"));

            Assert.That(completion.Success, Is.True);
            Assert.That(completion.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.Reward));

            OfflineRewardMapDbStore rewardStore = new OfflineRewardMapDbStore(databasePath, units);
            RewardMapService rewardService = new RewardMapService(new DefaultRewardMapTemplateCatalog(), units, rewardStore);
            RewardMapChoiceViewData firstLoad = rewardService.BuildChoice(
                new RewardMapChoiceRequest(
                    startRun.CreatedRun.RunId,
                    1,
                    startRun.CreatedRun.StartingCurrency + completion.CompletionRecord.RunGoldGained,
                    ToRewardArmy(completion.CompletionRecord.ArmyAfterBattle),
                    new RewardMapBattleResultSummary(completion.CompletionRecord.RunBattleId, "Victory", completion.CompletionRecord.TotalLosses, completion.CompletionRecord.RunGoldGained)),
                string.Empty);
            RewardMapChoiceViewData secondLoad = rewardService.BuildChoice(
                new RewardMapChoiceRequest(startRun.CreatedRun.RunId, 1, firstLoad.RunGoldBeforeReward, firstLoad.ArmyBeforeReward, firstLoad.BattleResultSummary),
                firstLoad.Cards[1].RewardId);

            Assert.That(firstLoad.Cards.Count, Is.EqualTo(3));
            Assert.That(secondLoad.ChoiceId, Is.EqualTo(firstLoad.ChoiceId));
            Assert.That(secondLoad.FocusedCard.RewardId, Is.EqualTo(firstLoad.Cards[1].RewardId));

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_choices;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards;"), Is.EqualTo(3));
            }

            RewardMapApplyResult apply = rewardService.Apply(new RewardMapApplyCommand(
                firstLoad.ChoiceId,
                firstLoad.FocusedCard.RewardId,
                firstLoad.RunGoldBeforeReward,
                firstLoad.ArmyBeforeReward));

            Assert.That(apply.Success, Is.True);

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards WHERE is_selected = 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards WHERE applied_snapshot_id IS NOT NULL;"), Is.EqualTo(1));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static StartRunResult BeginGeneratedRun(string databasePath, DeterministicRunGenerationCatalog catalog, TestUnitCatalog units)
    {
        StartRunService startRunService = new StartRunService(
            catalog,
            catalog,
            units,
            new OfflineStartRunDbStore(databasePath, catalog, units));
        string routeId = catalog.ListRoutePreviews()[0].RouteId;
        StartingArmyTemplate army = catalog.ListStartingArmies()[0];

        return startRunService.BeginRun(new StartRunCommand(
            "offline-player",
            army.TemplateId,
            army.VariantId,
            army.TemplateId,
            routeId));
    }

    private static DeterministicRunGenerationCatalog CreateCatalog(IStartRunUnitPoolSource units)
    {
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        ruleSet.ConfigureMockDefaults();
        return new DeterministicRunGenerationCatalog(
            units,
            ruleSet,
            StartingArmyGeneratorConfig.CreateDefault(ruleSet),
            RouteGeneratorConfig.CreateDefault(),
            null);
    }

    private static RunBattleArmySnapshot CreateBattleArmy()
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>
        {
            new RunBattleStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RunBattleSkillState> { new RunBattleSkillState("Chope", true) }),
            new RunBattleStackSnapshot("stack-thrower", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Range_Stance_Barb", true) }),
            new RunBattleStackSnapshot("stack-healer", "Healer", "Healer", "I", 1, 5, 0, 5 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Tough_Skin", true) }),
            new RunBattleStackSnapshot("stack-wisp", "Wisp", "Wisp", "I", 1, 22, 0, 22 * 6, new List<RunBattleSkillState> { new RunBattleSkillState("Blind_by_light", true) })
        };

        return new RunBattleArmySnapshot("battle-army-before", SumBattleValue(stacks), stacks);
    }

    private static RunBattleArmySnapshot CreateAfterBattleArmy(RunBattleArmySnapshot before)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        for (int i = 0; before != null && before.Stacks != null && i < before.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = before.Stacks[i];
            int amount = stack.StackId == "stack-rusher" ? stack.Amount - 4 : stack.Amount;
            int lost = stack.StackId == "stack-rusher" ? 4 : 0;
            stacks.Add(new RunBattleStackSnapshot(stack.StackId, stack.UnitId, stack.DisplayName, stack.Tier, stack.Level, amount, lost, amount * Math.Max(1, stack.CombatValue / Math.Max(1, stack.Amount)), CloneBattleSkills(stack.Skills)));
        }

        return new RunBattleArmySnapshot("battle-army-after", SumBattleValue(stacks), stacks);
    }

    private static RewardMapArmySnapshot ToRewardArmy(RunBattleArmySnapshot battleArmy)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        for (int i = 0; battleArmy != null && battleArmy.Stacks != null && i < battleArmy.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = battleArmy.Stacks[i];
            stacks.Add(new RewardMapStackSnapshot(stack.StackId, stack.UnitId, stack.DisplayName, stack.Tier, stack.Level, stack.Amount, stack.Lost, stack.CombatValue, CloneRewardSkills(stack.Skills)));
        }

        return new RewardMapArmySnapshot(battleArmy == null ? string.Empty : battleArmy.SnapshotId, battleArmy == null ? 0 : battleArmy.TotalArmyValue, stacks);
    }

    private static int SumBattleValue(List<RunBattleStackSnapshot> stacks)
    {
        int total = 0;
        for (int i = 0; stacks != null && i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return total;
    }

    private static List<RunBattleSkillState> CloneBattleSkills(List<RunBattleSkillState> skills)
    {
        List<RunBattleSkillState> result = new List<RunBattleSkillState>();
        for (int i = 0; skills != null && i < skills.Count; i++)
        {
            result.Add(new RunBattleSkillState(skills[i].SkillId, skills[i].Unlocked));
        }

        return result;
    }

    private static List<RewardMapSkillState> CloneRewardSkills(List<RunBattleSkillState> skills)
    {
        List<RewardMapSkillState> result = new List<RewardMapSkillState>();
        for (int i = 0; skills != null && i < skills.Count; i++)
        {
            result.Add(new RewardMapSkillState(skills[i].SkillId, skills[i].Unlocked));
        }

        return result;
    }

    private static int ScalarInt(IDbConnection connection, string sql)
    {
        return OfflineDatabaseSql.ReadInt(OfflineDatabaseSql.ExecuteScalar(connection, sql));
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_PRD37_" + Guid.NewGuid().ToString("N") + ".db");
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

    private sealed class TestUnitCatalog : IStartRunUnitPoolSource, IRewardMapUnitPoolSource, IOfflineArmySnapshotCatalogResolver
    {
        private readonly Dictionary<string, StartRunUnitDefinition> startRunUnits = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", StartUnit("Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", StartUnit("Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", StartUnit("Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", StartUnit("Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", StartUnit("Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", StartUnit("Axeman", "II", 97, "Slash") },
            { "StoneGolem", StartUnit("StoneGolem", "II", 67, "Stone_Throw") },
            { "StoneLord", StartUnit("StoneLord", "III", 120, "Stone_Skin") }
        };

        private readonly Dictionary<string, RunShopUnitDefinition> rewardUnits = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", RewardUnit("Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", RewardUnit("Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", RewardUnit("Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", RewardUnit("Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", RewardUnit("Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", RewardUnit("Axeman", "II", 97, "Slash") },
            { "StoneGolem", RewardUnit("StoneGolem", "II", 67, "Stone_Throw") },
            { "StoneLord", RewardUnit("StoneLord", "III", 120, "Stone_Skin") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return startRunUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<StartRunUnitDefinition> ListUnits()
        {
            return new List<StartRunUnitDefinition>(startRunUnits.Values);
        }

        RunShopUnitDefinition IRewardMapUnitDefinitionSource.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return rewardUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        List<RunShopUnitDefinition> IRewardMapUnitPoolSource.ListUnits()
        {
            return new List<RunShopUnitDefinition>(rewardUnits.Values);
        }

        OfflineArmySnapshotUnitCatalogEntry IOfflineArmySnapshotCatalogResolver.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return rewardUnits.TryGetValue(unitId, out unit)
                ? new OfflineArmySnapshotUnitCatalogEntry(unit.UnitId, unit.DisplayName, unit.Tier, unit.Cost, new List<string>(unit.SkillIds))
                : null;
        }

        private static StartRunUnitDefinition StartUnit(string unitId, string tier, int cost, params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, unitId, tier, cost, UnitFactionResolver.ResolveFactionId(unitId), UnitRoleCategory.Flexible, new List<string>(skills));
        }

        private static RunShopUnitDefinition RewardUnit(string unitId, string tier, int cost, params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, unitId, tier, cost, new List<string>(skills));
        }
    }
}
#endif
