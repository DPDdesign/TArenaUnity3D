using System.Collections.Generic;
using NUnit.Framework;

public class RunBattleServiceTests
{
    [Test]
    public void PrepareBattle_ReturnsOfflineLaunchPayloadAndRuntimeInputRecord()
    {
        InMemoryRunBattleStore store = new InMemoryRunBattleStore();
        RunBattleService service = CreateService(store);

        RunBattleLaunchViewData launch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-battle-1",
            "enc-iron-border-clash",
            2,
            120,
            CreateArmy("army-before", 28, 10, 5, 22)));

        Assert.That(launch.CanLaunch, Is.True);
        Assert.That(launch.GameMode, Is.EqualTo(RunBattleGameMode.Offline));
        Assert.That(launch.AuthoritySource, Is.EqualTo(RunBattleAuthoritySource.LocalOfflineAdapter));
        Assert.That(launch.Encounter.EnemyGoal, Is.EqualTo(RunBattleEnemyGoal.TryToWin));
        Assert.That(launch.LaunchPayload.RunId, Is.EqualTo("run-1"));
        Assert.That(launch.LaunchPayload.RouteNodeId, Is.EqualTo("node-battle-1"));
        Assert.That(launch.LaunchPayload.CurrentArmySnapshotId, Is.EqualTo("army-before"));
        Assert.That(launch.LaunchRecord.AdapterSurface, Does.Contain("runtime snapshot battle input"));
        Assert.That(store.PreparedBattles.Count, Is.EqualTo(1));
    }

    [Test]
    public void CompleteBattle_RecordsWinLossesAndRoutesToReward()
    {
        InMemoryRunBattleStore store = new InMemoryRunBattleStore();
        RunBattleService service = CreateService(store);
        RunBattleLaunchViewData launch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-battle-1",
            "enc-iron-border-clash",
            2,
            120,
            CreateArmy("army-before", 28, 10, 5, 22)));

        RunBattleCompletionResult result = service.CompleteBattle(new RunBattleCompletionPayload(
            launch.RunBattleId,
            RunBattleOutcome.Win,
            CreateArmy("army-after", 21, 10, 2, 22),
            45,
            "completion-1",
            "test-local-completion"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.CompletionRecord.Outcome, Is.EqualTo(RunBattleOutcome.Win));
        Assert.That(result.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.RunMap));
        Assert.That(result.CompletionRecord.TotalLosses, Is.EqualTo(10));
        Assert.That(result.CompletionRecord.RunGoldGained, Is.EqualTo(45));
        Assert.That(FindLoss(result.CompletionRecord.Losses, "stack-rusher").LostAmount, Is.EqualTo(7));
        Assert.That(FindLoss(result.CompletionRecord.Losses, "stack-healer").LostAmount, Is.EqualTo(3));
        Assert.That(store.Completions.Count, Is.EqualTo(1));
    }

    [Test]
    public void CompleteBattle_RoutesWinToRunMapAndLossToRunLoss()
    {
        InMemoryRunBattleStore store = new InMemoryRunBattleStore();
        RunBattleService service = CreateService(store);
        RunBattleLaunchViewData finalLaunch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-final",
            "enc-final-proof",
            5,
            220,
            CreateArmy("army-final-before", 24, 8, 5, 18)));
        RunBattleLaunchViewData battleLaunch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-battle-1",
            "enc-iron-border-clash",
            2,
            120,
            CreateArmy("army-before", 28, 10, 5, 22)));

        RunBattleCompletionResult finalResult = service.CompleteBattle(new RunBattleCompletionPayload(
            finalLaunch.RunBattleId,
            RunBattleOutcome.Win,
            CreateArmy("army-final-after", 20, 8, 4, 18),
            0,
            "completion-final",
            "test-local-completion"));
        RunBattleCompletionResult lossResult = service.CompleteBattle(new RunBattleCompletionPayload(
            battleLaunch.RunBattleId,
            RunBattleOutcome.Loss,
            CreateArmy("army-loss-after", 0, 0, 0, 0),
            0,
            "completion-loss",
            "test-local-completion"));

        Assert.That(finalResult.Success, Is.True);
        Assert.That(finalResult.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.RunMap));
        Assert.That(lossResult.Success, Is.True);
        Assert.That(lossResult.CompletionRecord.NextScreen, Is.EqualTo(RunBattleNextScreen.RunLoss));
    }

    [Test]
    public void PrepareBattle_RejectsMissingArmy()
    {
        RunBattleService service = CreateService(new InMemoryRunBattleStore());

        RunBattleLaunchViewData launch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-battle-1",
            "enc-iron-border-clash",
            2,
            120,
            null));

        Assert.That(launch.CanLaunch, Is.False);
        Assert.That(launch.Error, Is.EqualTo(RunBattleError.MissingCurrentArmy));
        Assert.That(launch.LaunchPayload, Is.Null);
    }

    [Test]
    public void PrepareBattle_RejectsMissingEncounterId()
    {
        RunBattleService service = CreateService(new InMemoryRunBattleStore());

        RunBattleLaunchViewData launch = service.PrepareBattle(new RunBattlePrepareRequest(
            "run-1",
            "node-battle-1",
            string.Empty,
            2,
            120,
            CreateArmy("army-before", 28, 10, 5, 22)));

        Assert.That(launch.CanLaunch, Is.False);
        Assert.That(launch.Error, Is.EqualTo(RunBattleError.MissingEncounter));
        Assert.That(launch.LaunchPayload, Is.Null);
    }

    private static RunBattleService CreateService(InMemoryRunBattleStore store)
    {
        return new RunBattleService(
            new FakeEncounterSource(),
            new OfflineRunBattleLaunchAdapter(),
            store);
    }

    private static RunBattleArmySnapshot CreateArmy(
        string snapshotId,
        int rushers,
        int throwers,
        int healers,
        int wisps)
    {
        List<RunBattleStackSnapshot> stacks = new List<RunBattleStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", "Rusher", "I", rushers, rushers * 31, Skill("Chope", true), Skill("Rush", false)),
            Stack("stack-thrower", "Thrower", "Thrower", "I", throwers, throwers * 60, Skill("Range_Stance_Barb", true), Skill("Double_Throw", true)),
            Stack("stack-healer", "Healer", "Healer", "I", healers, healers * 60, Skill("Tough_Skin", true)),
            Stack("stack-wisp", "Wisp", "Wisp", "I", wisps, wisps * 6, Skill("Blind_by_light", true))
        };

        int totalValue = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            totalValue += stacks[i].CombatValue;
        }

        return new RunBattleArmySnapshot(snapshotId, totalValue, stacks);
    }

    private static RunBattleStackSnapshot Stack(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int amount,
        int value,
        params RunBattleSkillState[] skills)
    {
        return new RunBattleStackSnapshot(
            stackId,
            unitId,
            displayName,
            tier,
            1,
            amount,
            0,
            value,
            new List<RunBattleSkillState>(skills));
    }

    private static RunBattleSkillState Skill(string skillId, bool unlocked)
    {
        return new RunBattleSkillState(skillId, unlocked);
    }

    private static RunBattleStackLossRecord FindLoss(List<RunBattleStackLossRecord> losses, string stackId)
    {
        for (int i = 0; i < losses.Count; i++)
        {
            if (losses[i].StackId == stackId)
            {
                return losses[i];
            }
        }

        return null;
    }

    private class FakeEncounterSource : IRunBattleEncounterSource
    {
        public RunBattleEncounterDefinition FindEncounter(string routeNodeId, string encounterId)
        {
            if (encounterId == "enc-final-proof")
            {
                return new RunBattleEncounterDefinition(
                    "enc-final-proof",
                    routeNodeId,
                    RunBattleNodeType.Final,
                    "Final Proof",
                    "High",
                    2650,
                    "enemy-final-proof",
                    RunBattleEnemyGoal.TryToWin);
            }

            if (encounterId == "enc-iron-border-clash")
            {
                return new RunBattleEncounterDefinition(
                    "enc-iron-border-clash",
                    routeNodeId,
                    RunBattleNodeType.Battle,
                    "Border Clash",
                    "Low",
                    1450,
                    "enemy-border-clash",
                    RunBattleEnemyGoal.TryToWin);
            }

            return null;
        }
    }
}
