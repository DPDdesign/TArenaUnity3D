using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;

public class PRD45RewardOpportunityMaterializationTests
{
    [Test]
    public void StartRun_PersistsUnresolvedRewardOpportunitiesBeforeBattle()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            StartRunResult startRun = BeginRun(databasePath, units);

            Assert.That(startRun.Success, Is.True);

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                int rewardNodeCount = ScalarInt(
                    connection,
                    "SELECT COUNT(*) FROM map_nodes WHERE node_type_id IN (" + (int)DBNodeTypeId.Battle + ", " + (int)DBNodeTypeId.RecruitReward + ");");
                int expectedOpportunityCount = rewardNodeCount * RewardMapMaterializedGenerator.RewardSlotCount;

                Assert.That(rewardNodeCount, Is.GreaterThan(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities;"), Is.EqualTo(expectedOpportunityCount));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE opportunity_state_id = " + (int)DBRewardOpportunityStateId.Unresolved + ";"), Is.EqualTo(expectedOpportunityCount));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE reward_choice_id <> 0;"), Is.EqualTo(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE resolved_reward_card_id IS NOT NULL;"), Is.EqualTo(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE planned_operation_type = '' OR catalog_entry_id = '';"), Is.EqualTo(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities opportunities INNER JOIN offline_runs runs ON runs.run_id = opportunities.run_id WHERE opportunities.run_seed <> runs.run_seed OR opportunities.seed_version <> runs.run_seed_version;"), Is.EqualTo(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM (SELECT node_id FROM reward_opportunities GROUP BY node_id HAVING COUNT(*) = 3) grouped;"), Is.EqualTo(rewardNodeCount));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void BattleCompletion_ResolvesPersistedPlanWithoutChangingSlotsOrOperationTypes()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            StartRunResult startRun = BeginRun(databasePath, units);
            int nodeId = ReadNodeId(databasePath, "node-pressure-1");
            List<string> planned = LoadOpportunitySequence(databasePath, nodeId);

            RunBattleCompletionResult completion = CompleteFirstBattle(databasePath, units, startRun);

            Assert.That(completion.Success, Is.True);
            Assert.That(completion.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.Reward));

            int rewardChoiceId;
            List<string> resolved = LoadResolvedOpportunitySequence(databasePath, nodeId, out rewardChoiceId);
            List<string> cards = LoadCardSequence(databasePath, rewardChoiceId);

            Assert.That(resolved, Is.EqualTo(planned));
            Assert.That(cards, Is.EqualTo(planned));

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE node_id = " + nodeId + " AND opportunity_state_id = " + (int)DBRewardOpportunityStateId.Resolved + ";"), Is.EqualTo(RewardMapMaterializedGenerator.RewardSlotCount));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_opportunities WHERE node_id = " + nodeId + " AND resolved_card_reward_id = '';"), Is.EqualTo(0));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_cards WHERE reward_choice_id = " + rewardChoiceId + " AND is_fallback = 1;"), Is.EqualTo(0));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RewardMapReload_UsesResolvedRowsAndDoesNotRerollOpportunities()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            StartRunResult startRun = BeginRun(databasePath, units);
            int nodeId = ReadNodeId(databasePath, "node-pressure-1");
            List<string> planned = LoadOpportunitySequence(databasePath, nodeId);
            RunBattleCompletionResult completion = CompleteFirstBattle(databasePath, units, startRun);
            RewardMapService rewardService = new RewardMapService(
                new DefaultRewardMapTemplateCatalog(),
                units,
                new OfflineRewardMapDbStore(databasePath, units));

            int opportunitiesBeforeLoad = ScalarInt(databasePath, "SELECT COUNT(*) FROM reward_opportunities;");
            int rewardChoicesBeforeLoad = ScalarInt(databasePath, "SELECT COUNT(*) FROM reward_choices;");

            RewardMapChoiceViewData firstLoad = rewardService.BuildChoice(
                new RewardMapChoiceRequest(
                    startRun.CreatedRun.RunId,
                    1,
                    startRun.CreatedRun.StartingCurrency + completion.CompletionRecord.RunGoldGained,
                    ToRewardArmy(completion.CompletionRecord.ArmyAfterBattle),
                    new RewardMapBattleResultSummary(
                        completion.CompletionRecord.RunBattleId,
                        "Victory",
                        completion.CompletionRecord.TotalLosses,
                        completion.CompletionRecord.RunGoldGained)),
                string.Empty);
            RewardMapChoiceViewData secondLoad = rewardService.BuildChoice(
                new RewardMapChoiceRequest(
                    startRun.CreatedRun.RunId,
                    1,
                    firstLoad.RunGoldBeforeReward,
                    firstLoad.ArmyBeforeReward,
                    firstLoad.BattleResultSummary),
                firstLoad.Cards[1].RewardId);

            Assert.That(firstLoad.ChoiceId, Is.EqualTo(secondLoad.ChoiceId));
            Assert.That(ToCardSequence(firstLoad), Is.EqualTo(planned));
            Assert.That(ToCardSequence(secondLoad), Is.EqualTo(planned));
            Assert.That(secondLoad.FocusedCard.RewardId, Is.EqualTo(firstLoad.Cards[1].RewardId));
            Assert.That(ScalarInt(databasePath, "SELECT COUNT(*) FROM reward_opportunities;"), Is.EqualTo(opportunitiesBeforeLoad));
            Assert.That(ScalarInt(databasePath, "SELECT COUNT(*) FROM reward_choices;"), Is.EqualTo(rewardChoicesBeforeLoad));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static StartRunResult BeginRun(string databasePath, TestUnitCatalog units)
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        StartRunService service = new StartRunService(
            catalog,
            catalog,
            units,
            new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog(), units));

        return service.BeginRun(new StartRunCommand(
            "offline-player",
            "barbarian-starter",
            "barbarian-starter-v1",
            "barbarian-starter",
            "iron-line"));
    }

    private static RunBattleCompletionResult CompleteFirstBattle(string databasePath, TestUnitCatalog units, StartRunResult startRun)
    {
        RunBattleService battleService = new RunBattleService(
            new DefaultRunBattleEncounterCatalog(),
            new OfflineRunBattleLaunchAdapter(),
            new OfflineRunBattleDbStore(databasePath, units, new DefaultRunBattleEncounterCatalog(), units));

        RunBattleLaunchViewData prepared = battleService.PrepareBattle(new RunBattlePrepareRequest(
            startRun.CreatedRun.RunId,
            "node-pressure-1",
            "enc-iron-border-clash",
            1,
            startRun.CreatedRun.StartingCurrency,
            CreateBattleArmy()));

        Assert.That(prepared.CanLaunch, Is.True);

        return battleService.CompleteBattle(new RunBattleCompletionPayload(
            prepared.RunBattleId,
            RunBattleOutcome.Win,
            CreateAfterBattleArmy(prepared.CurrentArmy),
            45,
            "completion-prd45",
            "prd45-test"));
    }

    private static RunBattleArmySnapshot CreateBattleArmy()
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>
        {
            new RunBattleStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RunBattleSkillState> { new RunBattleSkillState("Chope", true), new RunBattleSkillState("Rush", false) }),
            new RunBattleStackSnapshot("stack-thrower", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Range_Stance_Barb", true), new RunBattleSkillState("Double_Throw", true) }),
            new RunBattleStackSnapshot("stack-healer", "Healer", "Healer", "I", 1, 5, 0, 5 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Tough_Skin", true) }),
            new RunBattleStackSnapshot("stack-wisp", "Wisp", "Wisp", "I", 1, 22, 0, 22 * 6, new List<RunBattleSkillState> { new RunBattleSkillState("Blind_by_light", true) })
        };

        return new RunBattleArmySnapshot("army-before", SumBattleValue(stacks), stacks);
    }

    private static RunBattleArmySnapshot CreateAfterBattleArmy(RunBattleArmySnapshot before)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        for (int i = 0; before != null && before.Stacks != null && i < before.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = before.Stacks[i];
            int amount = stack.StackId == "stack-rusher" ? Math.Max(0, stack.Amount - 4) : stack.Amount;
            int lost = stack.StackId == "stack-rusher" ? 4 : 0;
            stacks.Add(new RunBattleStackSnapshot(
                stack.StackId,
                stack.UnitId,
                stack.DisplayName,
                stack.Tier,
                stack.Level,
                amount,
                lost,
                amount * Math.Max(1, stack.CombatValue / Math.Max(1, stack.Amount)),
                CloneBattleSkills(stack.Skills)));
        }

        return new RunBattleArmySnapshot("army-after", SumBattleValue(stacks), stacks);
    }

    private static RewardMapArmySnapshot ToRewardArmy(RunBattleArmySnapshot battleArmy)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        for (int i = 0; battleArmy != null && battleArmy.Stacks != null && i < battleArmy.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = battleArmy.Stacks[i];
            stacks.Add(new RewardMapStackSnapshot(
                stack.StackId,
                stack.UnitId,
                stack.DisplayName,
                stack.Tier,
                stack.Level,
                stack.Amount,
                stack.Lost,
                stack.CombatValue,
                CloneRewardSkills(stack.Skills)));
        }

        return new RewardMapArmySnapshot(battleArmy == null ? string.Empty : battleArmy.SnapshotId, battleArmy == null ? 0 : battleArmy.TotalArmyValue, stacks);
    }

    private static List<string> LoadOpportunitySequence(string databasePath, int nodeId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return OfflineDatabaseSql.Query(
                connection,
                @"
SELECT reward_slot_index, planned_operation_type
FROM reward_opportunities
WHERE node_id = @nodeId
  AND is_active = 1
ORDER BY reward_slot_index;",
                delegate(IDataRecord row)
                {
                    return OfflineDatabaseSql.ReadInt(row["reward_slot_index"], -1) + ":" + OfflineDatabaseSql.ReadText(row["planned_operation_type"]);
                },
                null,
                new OfflineDatabaseSqlParameter("@nodeId", nodeId));
        }
    }

    private static List<string> LoadResolvedOpportunitySequence(string databasePath, int nodeId, out int rewardChoiceId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<ResolvedOpportunityRow> rows = OfflineDatabaseSql.Query(
                connection,
                @"
SELECT reward_slot_index, planned_operation_type, reward_choice_id
FROM reward_opportunities
WHERE node_id = @nodeId
  AND is_active = 1
ORDER BY reward_slot_index;",
                delegate(IDataRecord row)
                {
                    return new ResolvedOpportunityRow
                    {
                        Sequence = OfflineDatabaseSql.ReadInt(row["reward_slot_index"], -1) + ":" + OfflineDatabaseSql.ReadText(row["planned_operation_type"]),
                        RewardChoiceId = OfflineDatabaseSql.ReadInt(row["reward_choice_id"])
                    };
                },
                null,
                new OfflineDatabaseSqlParameter("@nodeId", nodeId));

            rewardChoiceId = rows.Count == 0 ? 0 : rows[0].RewardChoiceId;
            List<string> result = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.That(rows[i].RewardChoiceId, Is.EqualTo(rewardChoiceId));
                result.Add(rows[i].Sequence);
            }

            return result;
        }
    }

    private static List<string> LoadCardSequence(string databasePath, int rewardChoiceId)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return OfflineDatabaseSql.Query(
                connection,
                @"
SELECT reward_slot_index, operation_type
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND is_fallback = 0
  AND is_active = 1
ORDER BY reward_slot_index;",
                delegate(IDataRecord row)
                {
                    return OfflineDatabaseSql.ReadInt(row["reward_slot_index"], -1) + ":" + OfflineDatabaseSql.ReadText(row["operation_type"]);
                },
                null,
                new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId));
        }
    }

    private static List<string> ToCardSequence(RewardMapChoiceViewData choice)
    {
        List<string> result = new List<string>();
        for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card == null || card.IsFallback || card.Operation == null)
            {
                continue;
            }

            result.Add(card.RewardSlotIndex + ":" + card.Operation.Type);
        }

        return result;
    }

    private static int ReadNodeId(string databasePath, string catalogNodeId)
    {
        return ScalarInt(databasePath, "SELECT node_id FROM map_nodes WHERE catalog_entry_id = '" + catalogNodeId + "' LIMIT 1;");
    }

    private static int ScalarInt(string databasePath, string sql)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return ScalarInt(connection, sql);
        }
    }

    private static int ScalarInt(IDbConnection connection, string sql)
    {
        return OfflineDatabaseSql.ReadInt(OfflineDatabaseSql.ExecuteScalar(connection, sql));
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

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_PRD45_" + Guid.NewGuid().ToString("N") + ".db");
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

    private sealed class ResolvedOpportunityRow
    {
        public string Sequence;
        public int RewardChoiceId;
    }

    private sealed class TestUnitCatalog : IStartRunUnitDefinitionSource, IRewardMapUnitPoolSource, IOfflineArmySnapshotCatalogResolver
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

        RunShopUnitDefinition IRewardMapUnitDefinitionSource.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return rewardUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<RunShopUnitDefinition> ListUnits()
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
            return new StartRunUnitDefinition(unitId, unitId, tier, cost, new List<string>(skills));
        }

        private static RunShopUnitDefinition RewardUnit(string unitId, string tier, int cost, params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, unitId, tier, cost, new List<string>(skills));
        }
    }
}
