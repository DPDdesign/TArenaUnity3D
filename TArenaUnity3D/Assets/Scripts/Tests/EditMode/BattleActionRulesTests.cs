#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

public class BattleActionRulesTests
{
    [Test]
    public void ValidateMove_RejectsOccupiedDestination()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0),
            AllyUnit("team-0-slot-1", 0, 1));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(0, 1) }
            },
            snapshot);

        Assert.That(validation.IsValid, Is.False);
        Assert.That((validation.RejectReason ?? string.Empty).ToLowerInvariant(), Does.Contain("occupied"));
    }

    [Test]
    public void ValidateMove_EmitsMoveResultForLegalDestination()
    {
        BattleSnapshot snapshot = CreateSnapshot(ActorUnit(0, 0));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(1, 0) }
            },
            snapshot);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(validation.Action, Is.Not.Null);

        BattleActionResult result = BattleActionRules.Apply(snapshot, validation.Action);
        Assert.That(result.Events.Exists(e => e.EventType == BattleActionResultEventType.UnitMoved), Is.True);
    }

    [Test]
    public void WaitAndDefend_AreRejectedAfterMovement()
    {
        BattleSnapshot snapshot = CreateSnapshot(ActorUnit(0, 0, movedThisTurn: true));

        BattleActionValidationResult wait = BattleActionRules.Validate(
            new BattleActionUse { ActorUnitId = "team-0-slot-0", ActionKind = BattleActionKind.Wait },
            snapshot);

        BattleActionValidationResult defend = BattleActionRules.Validate(
            new BattleActionUse { ActorUnitId = "team-0-slot-0", ActionKind = BattleActionKind.Defend },
            snapshot);

        Assert.That(wait.IsValid, Is.False);
        Assert.That(defend.IsValid, Is.False);
    }

    [Test]
    public void ActionsAndAICandidates_AreRejectedDuringNewTurnSequence()
    {
        BattleSnapshot snapshot = CreateSnapshotWithTurnState(
            new BattleTurnStateSnapshot { IsResolvingNewTurnSequence = true },
            ActorUnit(0, 0));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(1, 0) }
            },
            snapshot);

        List<BattleAction> generatedActions = BattleActionRules.GenerateLegalActions(snapshot);
        List<TacticalAIActionIntent> legacyCandidates = TacticalAICandidateGenerator.GenerateCandidates(snapshot);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.RejectReason, Does.Contain("blocking"));
        Assert.That(generatedActions, Is.Empty);
        Assert.That(legacyCandidates, Is.Empty);
    }

    [Test]
    public void BasicRangedAttack_UsesStableDeterministicDamage()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0, isRange: true, minDamage: 2, maxDamage: 6),
            EnemyUnit("team-1-slot-0", 1, 0));

        BattleActionUse use = new BattleActionUse
        {
            ActorUnitId = "team-0-slot-0",
            ActionKind = BattleActionKind.BasicRangedAttack,
            TargetUnitId = "team-1-slot-0",
            SelectedHexes = new List<HexCoord> { new HexCoord(1, 0) },
            ActionIndex = 3
        };

        BattleActionValidationResult firstValidation = BattleActionRules.Validate(use, snapshot);
        BattleActionValidationResult secondValidation = BattleActionRules.Validate(use, snapshot);

        BattleActionResult first = BattleActionRules.Apply(snapshot, firstValidation.Action);
        BattleActionResult second = BattleActionRules.Apply(snapshot, secondValidation.Action);

        int firstDamage = FirstAmount(first, BattleActionResultEventType.DamageApplied);
        int secondDamage = FirstAmount(second, BattleActionResultEventType.DamageApplied);

        Assert.That(firstDamage, Is.InRange(2, 6));
        Assert.That(secondDamage, Is.EqualTo(firstDamage));
    }

    [Test]
    public void PlannedAction_FromBattleAction_CarriesUseActionAndResult()
    {
        BattleSnapshot snapshot = CreateSnapshot(ActorUnit(0, 0));
        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(1, 0) }
            },
            snapshot);

        BattleActionResult result = BattleActionRules.Apply(snapshot, validation.Action);
        TacticalAIPlannedAction planned = TacticalAIPlannedAction.FromBattleAction(validation.Action, result);

        Assert.That(planned.Use, Is.Not.Null);
        Assert.That(planned.Action, Is.Not.Null);
        Assert.That(planned.Result, Is.Not.Null);
        Assert.That(planned.ActionKind, Is.EqualTo(BattleActionKind.Move));
        Assert.That(planned.LegacyIntent, Is.Null);
    }

    static int FirstAmount(BattleActionResult result, BattleActionResultEventType eventType)
    {
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Events, Is.Not.Null);
        for (int i = 0; i < result.Events.Count; i++)
        {
            if (result.Events[i].EventType == eventType)
            {
                return result.Events[i].Amount;
            }
        }

        Assert.Fail("Missing event " + eventType);
        return 0;
    }

    static BattleSnapshot CreateSnapshot(params BattleUnitSnapshot[] units)
    {
        return CreateSnapshotWithTurnState(new BattleTurnStateSnapshot(), units);
    }

    static BattleSnapshot CreateSnapshotWithTurnState(BattleTurnStateSnapshot turnState, params BattleUnitSnapshot[] units)
    {
        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>();
        for (int c = 0; c < 4; c++)
        {
            for (int r = 0; r < 4; r++)
            {
                hexes.Add(new BattleHexSnapshot
                {
                    C = c,
                    R = r,
                    IsWalkable = true,
                    OccupyingUnitId = FindOccupant(units, c, r)
                });
            }
        }

        return BattleSnapshotBuilder.Build(
            4,
            4,
            hexes,
            units,
            "team-0-slot-0",
            turnState,
            gameSeed: 12345,
            battleId: "battle-action-rules-test",
            nextActionIndex: 3);
    }

    static string FindOccupant(BattleUnitSnapshot[] units, int c, int r)
    {
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnitSnapshot unit = units[i];
            if (unit != null && unit.C == c && unit.R == r && unit.IsAlive)
            {
                return unit.RuntimeUnitId;
            }
        }

        return string.Empty;
    }

    static BattleUnitSnapshot ActorUnit(
        int c,
        int r,
        bool movedThisTurn = false,
        bool isRange = false,
        int minDamage = 1,
        int maxDamage = 3)
    {
        return Unit("team-0-slot-0", 0, 0, c, r, movedThisTurn, isRange, minDamage, maxDamage);
    }

    static BattleUnitSnapshot AllyUnit(string unitId, int c, int r)
    {
        return Unit(unitId, 0, 1, c, r);
    }

    static BattleUnitSnapshot EnemyUnit(string unitId, int c, int r)
    {
        return Unit(unitId, 1, 0, c, r);
    }

    static BattleUnitSnapshot Unit(
        string unitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        bool movedThisTurn = false,
        bool isRange = false,
        int minDamage = 1,
        int maxDamage = 3)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = unitId,
            TeamIndex = teamIndex,
            RosterIndexWithinTeam = rosterIndex,
            UnitName = unitId,
            UnitType = unitId,
            C = c,
            R = r,
            Amount = 5,
            TempHP = 10,
            BaseHP = 10,
            Attack = 2,
            Defense = 1,
            MovementSpeed = 3,
            Initiative = 1,
            MinDamage = minDamage,
            MaxDamage = maxDamage,
            IsAlive = true,
            IsRange = isRange,
            MovedThisTurn = movedThisTurn,
            SkillIdsBySlot = new List<string>(),
            CooldownsBySlot = new List<int>(),
            UsedSkillIdsThisTurn = new List<string>()
        };
    }
}
#endif
