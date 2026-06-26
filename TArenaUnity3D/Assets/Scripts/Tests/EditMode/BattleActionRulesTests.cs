#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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
    public void ValidateMove_UsesGeneratedOffsetRowNeighbours()
    {
        BattleUnitSnapshot actor = ActorUnit(1, 2);
        actor.MovementSpeed = 1;
        BattleSnapshot snapshot = CreateSnapshot(actor);

        BattleActionValidationResult generatedNeighbour = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(0, 1) }
            },
            snapshot);

        BattleActionValidationResult legacyOnlyNeighbour = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(2, 1) }
            },
            snapshot);

        Assert.That(generatedNeighbour.IsValid, Is.True, generatedNeighbour.RejectReason);
        Assert.That(legacyOnlyNeighbour.IsValid, Is.False);
        Assert.That(legacyOnlyNeighbour.RejectReason, Does.Contain("outside actor movement budget"));
    }

    [Test]
    public void ValidateMove_UsesLegacyNeighboursWhenSnapshotIsLegacy()
    {
        BattleUnitSnapshot actor = ActorUnit(1, 2);
        actor.MovementSpeed = 1;
        BattleSnapshot snapshot = CreateSnapshotWithLayout(true, actor);

        BattleActionValidationResult legacyNeighbour = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(2, 1) }
            },
            snapshot);

        BattleActionValidationResult offsetOnlyNeighbour = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(0, 1) }
            },
            snapshot);

        Assert.That(legacyNeighbour.IsValid, Is.True, legacyNeighbour.RejectReason);
        Assert.That(offsetOnlyNeighbour.IsValid, Is.False);
        Assert.That(offsetOnlyNeighbour.RejectReason, Does.Contain("outside actor movement budget"));
    }

    [Test]
    public void ValidateMove_WithOnlyRepeatableStanceSkill_EndsTurn()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "Range_Stance_Barb" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = CreateSnapshot(actor);

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Move,
                SelectedHexes = new List<HexCoord> { new HexCoord(1, 0) }
            },
            snapshot,
            new TestSkillProvider());

        Assert.That(validation.IsValid, Is.True, validation.RejectReason);
        Assert.That(validation.Action.EndsTurn, Is.True);
        Assert.That(validation.Action.AllowsPostMoveFollowUp, Is.False);
    }

    [Test]
    public void ValidateSkill_SlashUsesMoveDestinationButRemainsSkillTurnCost()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = CreateSnapshot(actor, EnemyUnit("team-1-slot-0", 2, 1));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.Skill,
                SkillSlot = 0,
                SkillId = "Slash",
                SelectedHexes = new List<HexCoord>
                {
                    new HexCoord(1, 0),
                    new HexCoord(2, 0)
                }
            },
            snapshot,
            new TestSkillProvider());

        Assert.That(validation.IsValid, Is.True, validation.RejectReason);
        Assert.That(validation.Action.ActionKind, Is.EqualTo(BattleActionKind.Skill));
        Assert.That(validation.Action.DestinationHex.C, Is.EqualTo(1));
        Assert.That(validation.Action.DestinationHex.R, Is.EqualTo(0));
        Assert.That(validation.Action.EndsTurn, Is.True);
        Assert.That(validation.Action.TurnCost, Is.EqualTo(1));
        Assert.That(validation.Action.AllowsPostMoveFollowUp, Is.False);
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
    public void ActionsAndGeneratedBattleActions_AreRejectedDuringNewTurnSequence()
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

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.RejectReason, Does.Contain("blocking"));
        Assert.That(generatedActions, Is.Empty);
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
    public void MoveAndAttack_ApplyIncludesCounterattackDamageWhenTargetCanCounter()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0, minDamage: 2, maxDamage: 2),
            EnemyUnit("team-1-slot-0", 1, 0, minDamage: 4, maxDamage: 4));

        BattleActionValidationResult validation = BattleActionRules.Validate(
            new BattleActionUse
            {
                ActorUnitId = "team-0-slot-0",
                ActionKind = BattleActionKind.MoveAndAttack,
                TargetUnitId = "team-1-slot-0",
                ActionIndex = 7
            },
            snapshot);

        Assert.That(validation.IsValid, Is.True, validation.RejectReason);

        BattleActionResult result = BattleActionRules.Apply(snapshot, validation.Action);
        List<BattleActionResultEvent> damageEvents =
            result.Events.FindAll(e => e.EventType == BattleActionResultEventType.DamageApplied);

        Assert.That(damageEvents.Count, Is.EqualTo(2));
        Assert.That(damageEvents[0].ActorUnitId, Is.EqualTo("team-0-slot-0"));
        Assert.That(damageEvents[0].TargetUnitId, Is.EqualTo("team-1-slot-0"));
        Assert.That(damageEvents[0].Amount, Is.EqualTo(2));
        Assert.That(damageEvents[1].ActorUnitId, Is.EqualTo("team-1-slot-0"));
        Assert.That(damageEvents[1].TargetUnitId, Is.EqualTo("team-0-slot-0"));
        Assert.That(damageEvents[1].Amount, Is.EqualTo(4));
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
        Assert.That(planned.Use.ActionKind, Is.EqualTo(BattleActionKind.Move));
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

    static BattleSnapshot CreateSnapshotWithLayout(bool usesLegacyHexLayout, params BattleUnitSnapshot[] units)
    {
        return CreateSnapshotWithTurnState(new BattleTurnStateSnapshot(), usesLegacyHexLayout, units);
    }

    static BattleSnapshot CreateSnapshotWithTurnState(BattleTurnStateSnapshot turnState, params BattleUnitSnapshot[] units)
    {
        return CreateSnapshotWithTurnState(turnState, false, units);
    }

    static BattleSnapshot CreateSnapshotWithTurnState(BattleTurnStateSnapshot turnState, bool usesLegacyHexLayout, params BattleUnitSnapshot[] units)
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
            nextActionIndex: 3,
            usesLegacyHexLayout: usesLegacyHexLayout);
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

    static BattleUnitSnapshot EnemyUnit(
        string unitId,
        int c,
        int r,
        int minDamage,
        int maxDamage)
    {
        return Unit(unitId, 1, 0, c, r, minDamage: minDamage, maxDamage: maxDamage);
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
            CounterAttackAvailable = true,
            CounterAttacks = 1,
            TempCounterAttacks = 1,
            SkillIdsBySlot = new List<string>(),
            CooldownsBySlot = new List<int>(),
            UsedSkillIdsThisTurn = new List<string>()
        };
    }

    sealed class TestSkillProvider : ITacticalAISkillMetadataProvider, ITacticalAISkillSpecProvider
    {
        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId ?? string.Empty,
                IsRepeatableToggle = BattleActionSkillUtility.IsRepeatableToggleSkillId(skillId)
            };
            return true;
        }

        public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
        {
            SkillDefinitionAsset skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            skill.Configure(skillId, "Active", string.Empty, string.Empty);
            if (string.Equals(skillId, "Slash", System.StringComparison.Ordinal))
            {
                skill.ConfigureRules(
                    new ActivationRuleData
                    {
                        activationKind = SkillActivationKind.Active,
                        cooldownTurns = 2,
                        consumesTurn = true,
                        canUseAfterMove = false,
                        canMoveAfterUse = false,
                        repeatableInTurn = false
                    },
                    new TargetingRuleData
                    {
                        targetFamily = SkillTargetFamily.Movement,
                        targetRoles = new[] { SkillTargetRole.MovementDestinationHex, SkillTargetRole.DirectionalImpactHex },
                        targetCount = 2,
                        requiresWalkable = true,
                        radius = 1
                    },
                    new ResolutionRuleData
                    {
                        resolutionFamily = SkillResolutionFamily.MoveThenDirectionalAreaAttack,
                        radius = 1
                    },
                    new[]
                    {
                        new SkillEffect
                        {
                            effectType = SkillEffectType.MoveUnit,
                            targetSource = SkillEffectTargetSource.Actor,
                            movementMode = SkillMovementMode.NormalPathMove
                        },
                        new SkillEffect
                        {
                            effectType = SkillEffectType.Damage,
                            targetSource = SkillEffectTargetSource.AffectedUnits,
                            damageMode = SkillDamageMode.BasicAttackDamage,
                            damageScale = 0.4f
                        }
                    });
                spec = SkillDefinitionSpec.FromAsset(skill);
                return spec != null;
            }

            skill.ConfigureRules(
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Stance,
                    consumesTurn = false,
                    canUseAfterMove = true,
                    canMoveAfterUse = true,
                    repeatableInTurn = true
                },
                new TargetingRuleData
                {
                    targetFamily = SkillTargetFamily.Self,
                    targetRoles = new[] { SkillTargetRole.ActorSelf },
                    targetCount = 0
                },
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.None },
                new[] { new SkillEffect { effectType = SkillEffectType.ToggleStance } });
            spec = SkillDefinitionSpec.FromAsset(skill);
            return spec != null;
        }
    }
}
#endif
