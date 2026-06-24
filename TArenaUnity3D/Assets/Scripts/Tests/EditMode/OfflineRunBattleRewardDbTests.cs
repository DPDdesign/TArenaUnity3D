using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;

public class OfflineRunBattleRewardDbTests
{
    [Test]
    public void RunBattleAndReward_PersistAcrossOfflineDatabase()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));

            Assert.That(startRun.Success, Is.True);

            RunBattleService runBattleService = new RunBattleService(
                new DefaultRunBattleEncounterCatalog(),
                new OfflineRunBattleLaunchAdapter(),
                new OfflineRunBattleDbStore(databasePath, units, new DefaultRunBattleEncounterCatalog()));

            RunBattleLaunchViewData preparedBattle = runBattleService.PrepareBattle(new RunBattlePrepareRequest(
                startRun.CreatedRun.RunId,
                "node-pressure-1",
                "enc-iron-border-clash",
                1,
                startRun.CreatedRun.StartingCurrency,
                CreateRunBattleArmy()));

            Assert.That(preparedBattle.CanLaunch, Is.True);
            Assert.That(preparedBattle.RunBattleId, Does.StartWith("run-battle-"));
            Assert.That(preparedBattle.CurrentArmy.SnapshotId, Does.StartWith("snapshot-"));

            RunBattleCompletionResult completion = runBattleService.CompleteBattle(new RunBattleCompletionPayload(
                preparedBattle.RunBattleId,
                RunBattleOutcome.Win,
                CreateAfterBattleArmy(preparedBattle.CurrentArmy),
                45,
                "completion-db-1",
                "db-test"));

            Assert.That(completion.Success, Is.True);
            Assert.That(completion.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.Reward));
            Assert.That(completion.CompletionRecord.ArmyAfterBattle.SnapshotId, Does.StartWith("snapshot-"));

            RewardMapService rewardService = new RewardMapService(
                new DefaultRewardMapTemplateCatalog(),
                units,
                new OfflineRewardMapDbStore(databasePath, units));

            RewardMapChoiceViewData choice = rewardService.BuildChoice(
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
                "reward-reward-grow-rusher");

            Assert.That(choice.ChoiceId, Does.StartWith("reward-choice-"));
            Assert.That(choice.ArmyBeforeReward.SnapshotId, Does.StartWith("snapshot-"));
            Assert.That(choice.FocusedCard, Is.Not.Null);

            RewardMapApplyResult firstApply = rewardService.Apply(new RewardMapApplyCommand(
                choice.ChoiceId,
                choice.FocusedCard.RewardId,
                choice.RunGoldBeforeReward,
                choice.ArmyBeforeReward));

            Assert.That(firstApply.Success, Is.True);
            Assert.That(firstApply.ArmyAfterReward.SnapshotId, Does.StartWith("snapshot-"));

            RewardMapApplyResult secondApply = rewardService.Apply(new RewardMapApplyCommand(
                choice.ChoiceId,
                choice.FocusedCard.RewardId,
                choice.RunGoldBeforeReward,
                choice.ArmyBeforeReward));

            Assert.That(secondApply.Success, Is.False);
            Assert.That(secondApply.Error, Is.EqualTo(RewardMapError.AlreadyApplied));

            RewardMapChoiceViewData reloadedChoice = new OfflineRewardMapDbStore(databasePath, units).FindChoice(choice.ChoiceId);
            Assert.That(reloadedChoice.SelectedRewardId, Is.EqualTo(choice.FocusedCard.RewardId));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RewardChoicePersistence_KeepsZeroBasedSlotTarget()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));

            RewardMapArmySnapshot army = new RewardMapArmySnapshot(
                "army-slot-target-test",
                28 * 31 + 10 * 60,
                new List<RewardMapStackSnapshot>
                {
                    new RewardMapStackSnapshot("slot-0", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RewardMapSkillState> { new RewardMapSkillState("Chope", true) }),
                    new RewardMapStackSnapshot("slot-1", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RewardMapSkillState> { new RewardMapSkillState("Range_Stance_Barb", true) })
                });
            RewardMapCardViewData card = new RewardMapCardViewData(
                "reward-prd37-downgrade-unit-v1-0",
                "prd37-downgrade-unit-v1",
                RewardMapFamily.Mass,
                RewardMapIntention.Stabilize,
                RewardMapRarity.Common,
                "Downgrade",
                "Thrower to Rusher",
                "Move one tier down in the same faction for more bodies.",
                "Thrower x10",
                "Rusher x23",
                "slot-1",
                true,
                RewardMapError.None,
                new RewardMapOperation(RewardMapOperationType.DowngradeStack, "slot-1", "Thrower", "Rusher", string.Empty, string.Empty, 23, 0));
            RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
                "reward-choice-materialized-test",
                startRun.CreatedRun.RunId,
                RewardMapGameMode.Offline,
                RewardMapAuthoritySource.LocalOfflineAdapter,
                new RewardMapBattleResultSummary(string.Empty, "Victory", 0, 0),
                "Gained: 0 RUN GOLD",
                startRun.CreatedRun.StartingCurrency,
                army,
                new List<RewardMapCardViewData> { card },
                card,
                null,
                "Materialized rewards loaded.");

            OfflineRewardMapDbStore store = new OfflineRewardMapDbStore(databasePath, units);
            RewardMapChoiceViewData saved = store.SaveChoice(choice);
            RewardMapCardViewData savedCard = saved.Cards[0];

            Assert.That(savedCard.Operation.StackId, Is.EqualTo("stack-thrower"));
            Assert.That(savedCard.AffectedStackId, Is.EqualTo("stack-thrower"));

            RewardMapService service = new RewardMapService(new DefaultRewardMapTemplateCatalog(), units, store);
            RewardMapChoiceViewData loaded = service.BuildChoice(
                new RewardMapChoiceRequest(
                    startRun.CreatedRun.RunId,
                    1,
                    choice.RunGoldBeforeReward,
                    army,
                    choice.BattleResultSummary),
                savedCard.RewardId);

            Assert.That(loaded.FocusedCard.BeforeStackPreview.UnitId, Is.EqualTo("Thrower"));
            Assert.That(loaded.FocusedCard.AfterStackPreview.UnitId, Is.EqualTo("Rusher"));
            Assert.That(loaded.FocusedCard.AffectedSlotIndex, Is.EqualTo(1));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RewardChoicePersistence_KeepsSemanticStackTarget()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));

            RewardMapArmySnapshot army = new RewardMapArmySnapshot(
                "army-semantic-target-test",
                28 * 31 + 10 * 60,
                new List<RewardMapStackSnapshot>
                {
                    new RewardMapStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RewardMapSkillState> { new RewardMapSkillState("Chope", true) }),
                    new RewardMapStackSnapshot("stack-thrower", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RewardMapSkillState> { new RewardMapSkillState("Range_Stance_Barb", true) })
                });
            RewardMapCardViewData card = new RewardMapCardViewData(
                "reward-prd37-downgrade-unit-v1-0",
                "prd37-downgrade-unit-v1",
                RewardMapFamily.Mass,
                RewardMapIntention.Stabilize,
                RewardMapRarity.Common,
                "Downgrade",
                "Thrower to Rusher",
                "Move one tier down in the same faction for more bodies.",
                "Thrower x10",
                "Rusher x23",
                "stack-thrower",
                true,
                RewardMapError.None,
                new RewardMapOperation(RewardMapOperationType.DowngradeStack, "stack-thrower", "Thrower", "Rusher", string.Empty, string.Empty, 23, 0));
            RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
                "reward-choice-materialized-test",
                startRun.CreatedRun.RunId,
                RewardMapGameMode.Offline,
                RewardMapAuthoritySource.LocalOfflineAdapter,
                new RewardMapBattleResultSummary(string.Empty, "Victory", 0, 0),
                "Gained: 0 RUN GOLD",
                startRun.CreatedRun.StartingCurrency,
                army,
                new List<RewardMapCardViewData> { card },
                card,
                null,
                "Materialized rewards loaded.");

            OfflineRewardMapDbStore store = new OfflineRewardMapDbStore(databasePath, units);
            RewardMapChoiceViewData saved = store.SaveChoice(choice);
            RewardMapCardViewData savedCard = saved.Cards[0];

            Assert.That(savedCard.Operation.StackId, Is.EqualTo("stack-thrower"));
            Assert.That(savedCard.AffectedStackId, Is.EqualTo("stack-thrower"));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RewardChoicePersistence_ReloadsDisabledNormalsAndEmergencyFallbackFromCardState()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            StartRunResult startRun = CreateStartedRun(databasePath, units);
            RewardMapArmySnapshot army = CreateTwoStackRewardArmy();

            RewardMapCardViewData disabledAdd = CreateRewardCard(
                "reward-prd37-add-new-stack-v1-0",
                "prd37-add-new-stack-v1",
                0,
                RewardMapOperationType.AddStack,
                string.Empty,
                "Rusher",
                string.Empty,
                0,
                false,
                RewardMapError.NoLegalTarget,
                "Disabled persisted state",
                false);
            RewardMapCardViewData disabledGrow = CreateRewardCard(
                "reward-prd37-increase-stack-v1-1",
                "prd37-increase-stack-v1",
                1,
                RewardMapOperationType.AddUnits,
                string.Empty,
                "Rusher",
                string.Empty,
                0,
                false,
                RewardMapError.NoLegalTarget,
                "Disabled persisted state",
                false);
            RewardMapCardViewData disabledPromote = CreateRewardCard(
                "reward-prd37-promote-unit-v1-2",
                "prd37-promote-unit-v1",
                2,
                RewardMapOperationType.PromoteStack,
                string.Empty,
                "Rusher",
                "Axeman",
                0,
                false,
                RewardMapError.NoLegalTarget,
                "Disabled persisted state",
                false);
            RewardMapCardViewData fallback = CreateRewardCard(
                "reward-prd37-run-gold-fallback-v1-3",
                "prd37-run-gold-fallback-v1",
                3,
                RewardMapOperationType.GainCurrency,
                string.Empty,
                string.Empty,
                string.Empty,
                60,
                true,
                RewardMapError.None,
                "+60 RUN GOLD",
                true);

            RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
                "reward-choice-prd43-disabled",
                startRun.CreatedRun.RunId,
                RewardMapGameMode.Offline,
                RewardMapAuthoritySource.LocalOfflineAdapter,
                new RewardMapBattleResultSummary(string.Empty, "Victory", 0, 0),
                "Gained: 0 RUN GOLD",
                startRun.CreatedRun.StartingCurrency,
                army,
                new List<RewardMapCardViewData> { disabledAdd, disabledGrow, disabledPromote, fallback },
                fallback,
                null,
                "Materialized rewards loaded.");

            OfflineRewardMapDbStore store = new OfflineRewardMapDbStore(databasePath, units);
            RewardMapChoiceViewData saved = store.SaveChoice(choice);
            RewardMapChoiceViewData reloaded = store.FindChoice(saved.ChoiceId);

            Assert.That(reloaded.Cards.Count, Is.EqualTo(4));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(reloaded.Cards[i].RewardSlotIndex, Is.EqualTo(i));
                Assert.That(reloaded.Cards[i].Legal, Is.False);
                Assert.That(reloaded.Cards[i].Error, Is.EqualTo(RewardMapError.NoLegalTarget));
                Assert.That(reloaded.Cards[i].AfterText, Is.EqualTo("Disabled persisted state"));
                Assert.That(reloaded.Cards[i].Operation.StackId, Is.Empty);
                Assert.That(reloaded.Cards[i].IsFallback, Is.False);
            }

            Assert.That(reloaded.Cards[3].RewardSlotIndex, Is.EqualTo(3));
            Assert.That(reloaded.Cards[3].Legal, Is.True);
            Assert.That(reloaded.Cards[3].Operation.Type, Is.EqualTo(RewardMapOperationType.GainCurrency));
            Assert.That(reloaded.Cards[3].IsFallback, Is.True);
            Assert.That(reloaded.FocusedCard.RewardId, Is.EqualTo(fallback.RewardId));

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_cards WHERE legal = 0;"), Is.EqualTo(3));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_cards WHERE is_fallback = 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards WHERE legal = 0;"), Is.EqualTo(3));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards WHERE is_fallback = 1;"), Is.EqualTo(1));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RewardChoicePersistence_DuplicateTemplateIdsFocusAndApplyByRewardSlot()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            StartRunResult startRun = CreateStartedRun(databasePath, units);
            RewardMapArmySnapshot army = CreateTwoStackRewardArmy();

            RewardMapCardViewData first = CreateRewardCard(
                "reward-duplicate-template-0",
                "duplicate-template",
                0,
                RewardMapOperationType.AddUnits,
                "stack-rusher",
                "Rusher",
                string.Empty,
                0,
                true,
                RewardMapError.None,
                "Rusher +3",
                false);
            first.Operation.Amount = 3;

            RewardMapCardViewData second = CreateRewardCard(
                "reward-duplicate-template-1",
                "duplicate-template",
                1,
                RewardMapOperationType.AddUnits,
                "stack-thrower",
                "Thrower",
                string.Empty,
                0,
                true,
                RewardMapError.None,
                "Thrower +2",
                false);
            second.Operation.Amount = 2;

            RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
                "reward-choice-prd43-duplicate",
                startRun.CreatedRun.RunId,
                RewardMapGameMode.Offline,
                RewardMapAuthoritySource.LocalOfflineAdapter,
                new RewardMapBattleResultSummary(string.Empty, "Victory", 0, 0),
                "Gained: 0 RUN GOLD",
                startRun.CreatedRun.StartingCurrency,
                army,
                new List<RewardMapCardViewData> { first, second },
                first,
                null,
                "Materialized rewards loaded.");

            OfflineRewardMapDbStore store = new OfflineRewardMapDbStore(databasePath, units);
            RewardMapChoiceViewData saved = store.SaveChoice(choice);
            RewardMapService service = new RewardMapService(new DefaultRewardMapTemplateCatalog(), units, store);

            RewardMapChoiceViewData focusedSecond = service.BuildChoice(
                new RewardMapChoiceRequest(
                    startRun.CreatedRun.RunId,
                    1,
                    choice.RunGoldBeforeReward,
                    army,
                    choice.BattleResultSummary),
                second.RewardId);

            Assert.That(focusedSecond.FocusedCard.RewardId, Is.EqualTo(second.RewardId));
            Assert.That(focusedSecond.FocusedCard.TemplateId, Is.EqualTo(first.TemplateId));
            Assert.That(focusedSecond.FocusedCard.RewardSlotIndex, Is.EqualTo(1));
            Assert.That(focusedSecond.FocusedCard.Operation.StackId, Is.EqualTo("stack-thrower"));

            RewardMapApplyResult apply = service.Apply(new RewardMapApplyCommand(
                saved.ChoiceId,
                second.RewardId,
                choice.RunGoldBeforeReward,
                army));

            Assert.That(apply.Success, Is.True);
            Assert.That(apply.Reward.RewardId, Is.EqualTo(second.RewardId));
            Assert.That(apply.Reward.RewardSlotIndex, Is.EqualTo(1));

            RewardMapChoiceViewData reloaded = store.FindChoice(saved.ChoiceId);
            Assert.That(reloaded.SelectedRewardId, Is.EqualTo(second.RewardId));

            using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
            {
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_cards WHERE is_selected = 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT reward_slot_index FROM reward_cards WHERE is_selected = 1 LIMIT 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM reward_cards WHERE applied_snapshot_id IS NOT NULL;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT COUNT(*) FROM map_node_rewards WHERE is_selected = 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT reward_slot_index FROM map_node_rewards WHERE is_selected = 1 LIMIT 1;"), Is.EqualTo(1));
                Assert.That(ScalarInt(connection, "SELECT selected_reward_slot_index FROM reward_choices WHERE selected_reward_id = 'reward-duplicate-template-1' LIMIT 1;"), Is.EqualTo(1));
            }
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static StartRunResult CreateStartedRun(string databasePath, TestUnitCatalog units)
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        StartRunService startRunService = new StartRunService(
            catalog,
            catalog,
            units,
            new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

        StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
            "offline-player",
            "barbarian-starter",
            "barbarian-starter-v1",
            "barbarian-starter",
            "iron-line"));

        Assert.That(startRun.Success, Is.True);
        return startRun;
    }

    private static RewardMapArmySnapshot CreateTwoStackRewardArmy()
    {
        return new RewardMapArmySnapshot(
            "army-prd43",
            28 * 31 + 10 * 60,
            new List<RewardMapStackSnapshot>
            {
                new RewardMapStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RewardMapSkillState> { new RewardMapSkillState("Chope", true) }),
                new RewardMapStackSnapshot("stack-thrower", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RewardMapSkillState> { new RewardMapSkillState("Range_Stance_Barb", true) })
            });
    }

    private static RewardMapCardViewData CreateRewardCard(
        string rewardId,
        string templateId,
        int rewardSlotIndex,
        RewardMapOperationType operationType,
        string stackId,
        string unitId,
        string toUnitId,
        int currencyDelta,
        bool legal,
        RewardMapError error,
        string afterText,
        bool isFallback)
    {
        RewardMapOperation operation = new RewardMapOperation(
            operationType,
            stackId,
            unitId,
            toUnitId,
            string.Empty,
            string.Empty,
            1,
            currencyDelta);
        RewardMapCardViewData card = new RewardMapCardViewData(
            rewardId,
            templateId,
            RewardMapFamily.Mass,
            RewardMapIntention.Strengthen,
            RewardMapRarity.Common,
            "Test",
            "Test Reward",
            "Test reward card.",
            "Before",
            afterText,
            stackId,
            legal,
            error,
            operation);
        card.RewardSlotIndex = rewardSlotIndex;
        card.IsFallback = isFallback;
        return card;
    }

    private static int ScalarInt(IDbConnection connection, string sql)
    {
        return OfflineDatabaseSql.ReadInt(OfflineDatabaseSql.ExecuteScalar(connection, sql));
    }

    private static RunBattleArmySnapshot CreateRunBattleArmy()
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>
        {
            new RunBattleStackSnapshot("stack-rusher", "Rusher", "Rusher", "I", 1, 28, 0, 28 * 31, new List<RunBattleSkillState> { new RunBattleSkillState("Chope", true), new RunBattleSkillState("Rush", false) }),
            new RunBattleStackSnapshot("stack-thrower", "Thrower", "Thrower", "I", 1, 10, 0, 10 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Range_Stance_Barb", true), new RunBattleSkillState("Double_Throw", true) }),
            new RunBattleStackSnapshot("stack-healer", "Healer", "Healer", "I", 1, 5, 0, 5 * 60, new List<RunBattleSkillState> { new RunBattleSkillState("Tough_Skin", true) }),
            new RunBattleStackSnapshot("stack-wisp", "Wisp", "Wisp", "I", 1, 22, 0, 22 * 6, new List<RunBattleSkillState> { new RunBattleSkillState("Blind_by_light", true) })
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RunBattleArmySnapshot("army-before", total, stacks);
    }

    private static RunBattleArmySnapshot CreateAfterBattleArmy(RunBattleArmySnapshot preparedArmy)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>();
        for (int i = 0; i < preparedArmy.Stacks.Count; i++)
        {
            RunBattleStackSnapshot stack = preparedArmy.Stacks[i];
            int amount = stack.Amount;
            int lost = 0;
            if (stack.StackId == "stack-rusher")
            {
                amount = Math.Max(0, amount - 7);
                lost = 7;
            }
            else if (stack.StackId == "stack-healer")
            {
                amount = Math.Max(0, amount - 3);
                lost = 3;
            }

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

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RunBattleArmySnapshot("army-after", total, stacks);
    }

    private static RewardMapArmySnapshot ToRewardArmy(RunBattleArmySnapshot battleArmy)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        for (int i = 0; i < battleArmy.Stacks.Count; i++)
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

        return new RewardMapArmySnapshot(battleArmy.SnapshotId, battleArmy.TotalArmyValue, stacks);
    }

    private static List<RunBattleSkillState> CloneBattleSkills(List<RunBattleSkillState> skills)
    {
        List<RunBattleSkillState> copy = new List<RunBattleSkillState>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                copy.Add(new RunBattleSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return copy;
    }

    private static List<RewardMapSkillState> CloneRewardSkills(List<RunBattleSkillState> skills)
    {
        List<RewardMapSkillState> copy = new List<RewardMapSkillState>();
        if (skills == null)
        {
            return copy;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                copy.Add(new RewardMapSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return copy;
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_BattleReward_" + Guid.NewGuid().ToString("N") + ".db");
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

    private sealed class TestUnitCatalog : IStartRunUnitDefinitionSource, IRewardMapUnitDefinitionSource, IOfflineArmySnapshotCatalogResolver
    {
        private readonly Dictionary<string, StartRunUnitDefinition> startRunUnits = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", StartRunUnit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", StartRunUnit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", StartRunUnit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", StartRunUnit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", StartRunUnit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", StartRunUnit("Axeman", "Axeman", "II", 97, "Slash") },
            { "StoneGolem", StartRunUnit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw") }
        };

        private readonly Dictionary<string, RunShopUnitDefinition> rewardUnits = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", RewardUnit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", RewardUnit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", RewardUnit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", RewardUnit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", RewardUnit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", RewardUnit("Axeman", "Axeman", "II", 97, "Slash") },
            { "StoneGolem", RewardUnit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw") }
        };

        StartRunUnitDefinition IStartRunUnitDefinitionSource.FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return startRunUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        RunShopUnitDefinition IRewardMapUnitDefinitionSource.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return rewardUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        OfflineArmySnapshotUnitCatalogEntry IOfflineArmySnapshotCatalogResolver.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            if (!rewardUnits.TryGetValue(unitId, out unit))
            {
                return null;
            }

            return new OfflineArmySnapshotUnitCatalogEntry(unit.UnitId, unit.DisplayName, unit.Tier, unit.Cost, new List<string>(unit.SkillIds));
        }

        private static StartRunUnitDefinition StartRunUnit(string unitId, string displayName, string tier, int cost, params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }

        private static RunShopUnitDefinition RewardUnit(string unitId, string displayName, string tier, int cost, params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
