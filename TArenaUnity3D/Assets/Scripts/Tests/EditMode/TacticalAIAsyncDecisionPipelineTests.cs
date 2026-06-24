using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

public class TacticalAIAsyncDecisionPipelineTests
{
    [Test]
    public void CopiedSkillMetadataProvider_CapturesMetadataWithoutLiveReadsAfterCapture()
    {
        CountingSkillMetadataProvider liveProvider = new CountingSkillMetadataProvider();
        liveProvider.Metadata.CanUseAfterMove = true;
        liveProvider.Metadata.CanMoveAfterSkill = true;

        BattleSnapshot snapshot = CreateSnapshot("BattleCry");
        TacticalAICopiedSkillMetadataProvider copiedProvider =
            TacticalAICopiedSkillMetadataProvider.Capture(snapshot, liveProvider);

        liveProvider.Metadata.CanUseAfterMove = false;
        liveProvider.Metadata.CanMoveAfterSkill = false;

        TacticalAISkillMetadata copiedMetadata;
        bool found = copiedProvider.TryGetSkillMetadata("BattleCry", out copiedMetadata);

        Assert.That(found, Is.True);
        Assert.That(copiedMetadata.CanUseAfterMove, Is.True);
        Assert.That(copiedMetadata.CanMoveAfterSkill, Is.True);
        Assert.That(liveProvider.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void AsyncTurnIntegrator_DoesNotCompleteWhileWorkerPlanIsStillRunning()
    {
        TaskCompletionSource<TacticalAISearchPlan> planSource = new TaskCompletionSource<TacticalAISearchPlan>();
        TacticalAIAsyncTurnIntegrator integrator = new TacticalAIAsyncTurnIntegrator(
            () => CreateSnapshot(),
            () => CreateProfile("running"),
            (snapshot, profile, skillMetadataProvider) => planSource.Task.GetAwaiter().GetResult(),
            snapshot => TacticalAICopiedSkillMetadataProvider.Capture(snapshot, new CountingSkillMetadataProvider()),
            (orderedIntents, plannedSnapshot) => new TacticalAIExecutionResult
            {
                Status = TacticalAIExecutionStatus.Started,
                ExecutedIntent = FirstIntent(orderedIntents)
            });

        TacticalAILiveTurnIntegrationResult beginResult;
        bool started = integrator.TryBeginTurn(out beginResult);
        bool completed = integrator.TryCompleteTurn(out beginResult);

        Assert.That(started, Is.True);
        Assert.That(completed, Is.False);

        planSource.SetResult(CreatePlan());

        TacticalAILiveTurnIntegrationResult finalResult = WaitForTerminalResult(integrator);
        Assert.That(finalResult.Started, Is.True);
        Assert.That(finalResult.ExecutionResult, Is.Not.Null);
        Assert.That(finalResult.ExecutionResult.Status, Is.EqualTo(TacticalAIExecutionStatus.Started));
    }

    [Test]
    public void AsyncTurnIntegrator_FallsBackWhenCurrentSnapshotHashChangesBeforeConsume()
    {
        BattleSnapshot plannedSnapshot = CreateSnapshot();
        BattleSnapshot staleSnapshot = CreateSnapshot(actorAmount: 4);
        int snapshotCallCount = 0;
        int executeCallCount = 0;

        TacticalAIAsyncTurnIntegrator integrator = new TacticalAIAsyncTurnIntegrator(
            () =>
            {
                snapshotCallCount++;
                return snapshotCallCount == 1 ? plannedSnapshot : staleSnapshot;
            },
            () => CreateProfile("stale"),
            (snapshot, profile, skillMetadataProvider) => CreatePlan(),
            snapshot => TacticalAICopiedSkillMetadataProvider.Capture(snapshot, new CountingSkillMetadataProvider()),
            (orderedIntents, snapshot) =>
            {
                executeCallCount++;
                return new TacticalAIExecutionResult
                {
                    Status = TacticalAIExecutionStatus.Started,
                    ExecutedIntent = FirstIntent(orderedIntents)
                };
            });

        TacticalAILiveTurnIntegrationResult beginResult;
        bool started = integrator.TryBeginTurn(out beginResult);
        TacticalAILiveTurnIntegrationResult finalResult = WaitForTerminalResult(integrator);

        Assert.That(started, Is.True);
        Assert.That(finalResult.Started, Is.False);
        Assert.That(finalResult.FallbackReason, Is.EqualTo("SnapshotHashChanged"));
        Assert.That(executeCallCount, Is.EqualTo(0));
    }

    [Test]
    public void AsyncTurnIntegrator_ConvertsWorkerFaultToFallbackResult()
    {
        TacticalAIAsyncTurnIntegrator integrator = new TacticalAIAsyncTurnIntegrator(
            () => CreateSnapshot(),
            () => CreateProfile("fault"),
            (snapshot, profile, skillMetadataProvider) => throw new InvalidOperationException("planner failed"),
            snapshot => TacticalAICopiedSkillMetadataProvider.Capture(snapshot, new CountingSkillMetadataProvider()),
            (orderedIntents, plannedSnapshot) => new TacticalAIExecutionResult
            {
                Status = TacticalAIExecutionStatus.Started,
                ExecutedIntent = FirstIntent(orderedIntents)
            });

        TacticalAILiveTurnIntegrationResult beginResult;
        bool started = integrator.TryBeginTurn(out beginResult);
        TacticalAILiveTurnIntegrationResult finalResult = WaitForTerminalResult(integrator);

        Assert.That(started, Is.True);
        Assert.That(finalResult.Started, Is.False);
        Assert.That(finalResult.FallbackReason, Is.EqualTo("AsyncFaulted"));
        Assert.That(finalResult.ExecutionResult, Is.Null);
    }

    static TacticalAILiveTurnIntegrationResult WaitForTerminalResult(TacticalAIAsyncTurnIntegrator integrator)
    {
        TacticalAILiveTurnIntegrationResult result;
        for (int i = 0; i < 2000; i++)
        {
            if (integrator.TryCompleteTurn(out result))
            {
                return result;
            }
        }

        Assert.Fail("Async integrator did not reach a terminal result in the expected polling window.");
        return null;
    }

    static TacticalAISearchPlan CreatePlan()
    {
        TacticalAIActionIntent intent = new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Wait,
            ActorUnitId = "team-0-slot-0",
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            StableOrderKey = "wait"
        };

        return new TacticalAISearchPlan
        {
            BestIntent = intent,
            OrderedActionIntents = new List<TacticalAIActionIntent> { intent },
            BestScore = 12f,
            CompletedDepth = 2,
            PlannedSnapshotHash = string.Empty
        };
    }

    static TacticalAIActionIntent FirstIntent(IEnumerable<TacticalAIActionIntent> orderedIntents)
    {
        if (orderedIntents == null)
        {
            return null;
        }

        foreach (TacticalAIActionIntent intent in orderedIntents)
        {
            return intent;
        }

        return null;
    }

    static TacticalAIResolvedProfile CreateProfile(string difficultyName)
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        profile.DifficultyName = difficultyName;
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        return profile;
    }

    static BattleSnapshot CreateSnapshot(string skillId = "BattleCry", int actorAmount = 5)
    {
        BattleUnitSnapshot actor = new BattleUnitSnapshot
        {
            RuntimeUnitId = "team-0-slot-0",
            TeamIndex = 0,
            RosterIndexWithinTeam = 0,
            UnitName = "Actor",
            UnitType = "Actor",
            C = 0,
            R = 0,
            Amount = actorAmount,
            TempHP = 0,
            BaseHP = 10,
            Attack = 5,
            Defense = 5,
            MovementSpeed = 3,
            Initiative = 7,
            MinDamage = 1,
            MaxDamage = 2,
            IsAlive = true,
            IsRange = false,
            SkillIdsBySlot = new List<string> { skillId },
            CooldownsBySlot = new List<int> { 0 },
            UsedSkillIdsThisTurn = new List<string>(),
            Statuses = new List<BattleStatusSnapshot>()
        };

        BattleUnitSnapshot enemy = new BattleUnitSnapshot
        {
            RuntimeUnitId = "team-1-slot-0",
            TeamIndex = 1,
            RosterIndexWithinTeam = 0,
            UnitName = "Enemy",
            UnitType = "Enemy",
            C = 2,
            R = 0,
            Amount = 5,
            TempHP = 0,
            BaseHP = 10,
            Attack = 5,
            Defense = 5,
            MovementSpeed = 3,
            Initiative = 5,
            MinDamage = 1,
            MaxDamage = 2,
            IsAlive = true,
            IsRange = false,
            SkillIdsBySlot = new List<string>(),
            CooldownsBySlot = new List<int>(),
            UsedSkillIdsThisTurn = new List<string>(),
            Statuses = new List<BattleStatusSnapshot>()
        };

        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>();
        for (int c = 0; c < 3; c++)
        {
            for (int r = 0; r < 2; r++)
            {
                hexes.Add(new BattleHexSnapshot
                {
                    C = c,
                    R = r,
                    IsWalkable = true,
                    OccupyingUnitId = ResolveOccupyingUnitId(actor, enemy, c, r)
                });
            }
        }

        return BattleSnapshotBuilder.Build(
            3,
            2,
            hexes,
            new List<BattleUnitSnapshot> { actor, enemy },
            actor.RuntimeUnitId,
            new BattleTurnStateSnapshot());
    }

    static string ResolveOccupyingUnitId(BattleUnitSnapshot actor, BattleUnitSnapshot enemy, int c, int r)
    {
        if (actor.C == c && actor.R == r)
        {
            return actor.RuntimeUnitId;
        }

        if (enemy.C == c && enemy.R == r)
        {
            return enemy.RuntimeUnitId;
        }

        return string.Empty;
    }

    sealed class CountingSkillMetadataProvider : ITacticalAISkillMetadataProvider
    {
        public int CallCount;
        public TacticalAISkillMetadata Metadata = new TacticalAISkillMetadata
        {
            SkillId = "BattleCry",
            IsPassive = false,
            CanUseAfterMove = false,
            CanMoveAfterSkill = false,
            IsRepeatableToggle = false
        };

        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            CallCount++;
            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId ?? string.Empty,
                IsPassive = Metadata.IsPassive,
                CanUseAfterMove = Metadata.CanUseAfterMove,
                CanMoveAfterSkill = Metadata.CanMoveAfterSkill,
                IsRepeatableToggle = Metadata.IsRepeatableToggle
            };
            return true;
        }
    }
}
