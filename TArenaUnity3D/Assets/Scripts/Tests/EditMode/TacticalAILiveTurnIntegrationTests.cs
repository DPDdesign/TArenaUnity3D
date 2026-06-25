#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class TacticalAILiveTurnIntegrationTests
{
    [Test]
    public void TryStartTurn_ReturnsStarted_WhenBridgeStartsAction()
    {
        BattleSnapshot snapshot = CreateSnapshot();
        TacticalAISearchPlan plan = CreatePlan(TacticalAIActionType.Move);
        TacticalAIExecutionResult executionResult = new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.Started,
            ExecutedIntent = plan.BestIntent
        };

        TacticalAILiveTurnIntegrator integrator = new TacticalAILiveTurnIntegrator(
            () => snapshot,
            () => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null),
            (plannedSnapshot, profile) => plan,
            (orderedIntents, plannedSnapshot) => executionResult);

        TacticalAILiveTurnIntegrationResult result = integrator.TryStartTurn();

        Assert.That(result.Started, Is.True);
        Assert.That(result.Status, Is.EqualTo(TacticalAILiveTurnStatus.Started));
        Assert.That(result.FallbackReason, Is.Empty);
    }

    [Test]
    public void TryStartTurn_ReturnsFallback_WhenPlanIsEmpty()
    {
        BattleSnapshot snapshot = CreateSnapshot();
        TacticalAILiveTurnIntegrator integrator = new TacticalAILiveTurnIntegrator(
            () => snapshot,
            () => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null),
            (plannedSnapshot, profile) => new TacticalAISearchPlan(),
            (orderedIntents, plannedSnapshot) => new TacticalAIExecutionResult
            {
                Status = TacticalAIExecutionStatus.Started
            });

        TacticalAILiveTurnIntegrationResult result = integrator.TryStartTurn();

        Assert.That(result.Started, Is.False);
        Assert.That(result.FallbackReason, Is.EqualTo("EmptyPlan"));
        Assert.That(result.ExecutionResult, Is.Null);
    }

    [Test]
    public void TryStartTurn_ReturnsFallback_WhenBridgeReportsNoLegalAction()
    {
        BattleSnapshot snapshot = CreateSnapshot();
        TacticalAISearchPlan plan = CreatePlan(TacticalAIActionType.Skill, skillId: "BattleCry", targetUnitId: "team-1-slot-0");
        TacticalAIExecutionResult executionResult = new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.NoLegalAction,
            Attempts = new List<TacticalAIExecutionAttempt>
            {
                new TacticalAIExecutionAttempt
                {
                    Intent = plan.BestIntent,
                    Started = false,
                    FailureReason = "Skill rejected."
                }
            }
        };

        TacticalAILiveTurnIntegrator integrator = new TacticalAILiveTurnIntegrator(
            () => snapshot,
            () => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null),
            (plannedSnapshot, profile) => plan,
            (orderedIntents, plannedSnapshot) => executionResult);

        TacticalAILiveTurnIntegrationResult result = integrator.TryStartTurn();

        Assert.That(result.Started, Is.False);
        Assert.That(result.FallbackReason, Is.EqualTo("NoLegalAction"));
        Assert.That(result.ExecutionResult, Is.SameAs(executionResult));
    }

    [Test]
    public void BuildFallbackLog_IncludesSelectedActionAndFailureStatus()
    {
        TacticalAISearchPlan plan = CreatePlan(TacticalAIActionType.Skill, skillId: "BattleCry", targetUnitId: "team-1-slot-0");
        TacticalAIExecutionResult executionResult = new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.NoLegalAction,
            Attempts = new List<TacticalAIExecutionAttempt>
            {
                new TacticalAIExecutionAttempt()
            }
        };

        string log = TacticalAILiveTurnIntegrator.BuildFallbackLog(
            "team-0-slot-0",
            plan,
            executionResult,
            "NoLegalAction");

        Assert.That(log, Does.Contain("actor=team-0-slot-0"));
        Assert.That(log, Does.Contain("reason=NoLegalAction"));
        Assert.That(log, Does.Contain("status=NoLegalAction"));
        Assert.That(log, Does.Contain("best=Skill skill=BattleCry target=team-1-slot-0"));
    }

    static TacticalAISearchPlan CreatePlan(
        TacticalAIActionType actionType,
        string skillId = "",
        string targetUnitId = "")
    {
        TacticalAIActionIntent intent = new TacticalAIActionIntent
        {
            ActionType = actionType,
            ActorUnitId = "team-0-slot-0",
            SourceHex = new TacticalAIHexCoordinate(0, 0),
            DestinationHex = new TacticalAIHexCoordinate(1, 0),
            TargetUnitId = targetUnitId,
            TargetHex = new TacticalAIHexCoordinate(2, 0),
            SkillSlot = skillId.Length > 0 ? 0 : -1,
            SkillId = skillId,
            StableOrderKey = actionType + "-test"
        };

        return new TacticalAISearchPlan
        {
            BestIntent = intent,
            OrderedActionIntents = new List<TacticalAIActionIntent> { intent },
            BestScore = 42.5f,
            CompletedDepth = 3,
            CoveredOpponentResponse = true
        };
    }

    static BattleSnapshot CreateSnapshot()
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
            Amount = 10,
            TempHP = 0,
            BaseHP = 10,
            Attack = 5,
            Defense = 5,
            MovementSpeed = 3,
            Initiative = 5,
            MinDamage = 1,
            MaxDamage = 2,
            IsAlive = true,
            IsRange = false
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
            Amount = 10,
            TempHP = 0,
            BaseHP = 10,
            Attack = 5,
            Defense = 5,
            MovementSpeed = 3,
            Initiative = 4,
            MinDamage = 1,
            MaxDamage = 2,
            IsAlive = true,
            IsRange = false
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
}
#endif
