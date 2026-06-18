using System;
using System.Collections.Generic;
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
            Assert.That(completion.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.RunMap));
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
