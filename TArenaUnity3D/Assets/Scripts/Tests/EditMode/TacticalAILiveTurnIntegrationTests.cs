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
            ExecutedAction = plan.BestAction
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
                    Action = plan.BestAction,
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
        BattleAction action = new BattleAction
        {
            ActorUnitId = "team-0-slot-0",
            ActionKind = TacticalAIPlannedAction.ToBattleActionKind(actionType),
            DestinationHex = new HexCoord(1, 0),
            ImpactHex = new HexCoord(2, 0),
            PrimaryTargetUnitId = targetUnitId,
            SkillSlot = skillId.Length > 0 ? 0 : -1,
            SkillId = skillId,
            StableOrderKey = actionType + "-test"
        };
        action.SelectedHexes.Add(new HexCoord(1, 0));
        if (targetUnitId.Length > 0)
        {
            action.TargetUnitIds.Add(targetUnitId);
            action.AffectedUnitIds.Add(targetUnitId);
        }

        if (actionType == TacticalAIActionType.Skill)
        {
            action.SkillCast = new SkillCast
            {
                ActorUnitId = "team-0-slot-0",
                SkillId = skillId,
                PrimaryTargetUnitId = targetUnitId,
                SelectedHexes = new List<HexCoord> { new HexCoord(2, 0) }
            };
        }

        BattleActionResult result = new BattleActionResult
        {
            ActorUnitId = "team-0-slot-0",
            ActionKind = action.ActionKind
        };
        TacticalAIPlannedAction plannedAction = TacticalAIPlannedAction.FromBattleAction(action, result);

        return new TacticalAISearchPlan
        {
            BestAction = plannedAction,
            OrderedActions = new List<TacticalAIPlannedAction> { plannedAction },
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
            CatalogUnitId = "Actor",
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
            CatalogUnitId = "Enemy",
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
