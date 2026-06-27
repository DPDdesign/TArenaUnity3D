#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using TimeSpells;
using UnityEngine;

public class SkillRulesTests
{
    [Test]
    public void SpikeTrap_EmptyWalkableHex_ValidatesAndPreviewsTrapPlacement()
    {
        SkillDefinitionAsset skill = Skill("Spike_Trap", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Spike_Trap" }, new List<int> { 0 }),
            Enemy("team-1-slot-0", 3, 3));

        SkillContext context = SkillContext.Create(snapshot, "team-0-slot-0", skill, 0);
        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Spike_Trap", new[] { new HexCoord(1, 0) }),
            context);

        Assert.That(result.IsValid, Is.True);
        SkillResult preview = SkillRules.Preview(result.Cast, context);
        Assert.That(preview.Events.Exists(e => e.EventType == SkillResultEventType.TrapPlaced), Is.True);
    }

    [Test]
    public void SpikeTrap_OccupiedEnemyHex_IsRejected()
    {
        SkillDefinitionAsset skill = Skill("Spike_Trap", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Spike_Trap" }, new List<int> { 0 }),
            Enemy("team-1-slot-0", 1, 0));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Spike_Trap", new[] { new HexCoord(1, 0) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(SkillRejectReason.TargetHexNotLegal));
    }

    [Test]
    public void DoubleThrow_AllowsDuplicateEnemyTargetAndKeepsTwoOrderedHits()
    {
        SkillDefinitionAsset skill = Skill("Double_Throw", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Double_Throw" }, new List<int> { 0 }),
            Enemy("team-1-slot-0", 2, 0));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Double_Throw", new[] { new HexCoord(2, 0), new HexCoord(2, 0) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.TargetUnitIds, Is.EqualTo(new[] { "team-1-slot-0", "team-1-slot-0" }));
        SkillResult preview = SkillRules.Preview(result.Cast, SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));
        Assert.That(preview.Events.FindAll(e => e.EventType == SkillResultEventType.DamageApplied).Count, Is.EqualTo(2));
    }

    [Test]
    public void DoubleThrow_AfterFirstTarget_KeepsAllEnemyTargetsLegal()
    {
        SkillDefinitionAsset skill = Skill("Double_Throw", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Double_Throw" }, new List<int> { 0 }),
            Enemy("team-1-slot-0", 2, 0),
            Enemy("team-1-slot-1", 3, 0),
            Enemy("team-1-slot-2", 4, 0));

        SkillContext context = SkillContext.Create(snapshot, "team-0-slot-0", skill, 0);
        List<SkillTarget> targets = SkillRules.GetTargets(context, new List<HexCoord> { new HexCoord(2, 0) });

        Assert.That(targets.Count, Is.EqualTo(3));
        Assert.That(HasTarget(targets, 2, 0), Is.True);
        Assert.That(HasTarget(targets, 3, 0), Is.True);
        Assert.That(HasTarget(targets, 4, 0), Is.True);
    }

    [Test]
    public void ForcePull_RejectsOccupiedDestination()
    {
        SkillDefinitionAsset skill = Skill("Force_Pull", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Force_Pull" }, new List<int> { 0 }),
            Ally("team-0-slot-1", 1, 0),
            Enemy("team-1-slot-0", 0, 1));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Force_Pull", new[] { new HexCoord(1, 0), new HexCoord(0, 1) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(SkillRejectReason.TargetHexNotLegal));
    }

    [Test]
    public void ForcePull_InitialTargets_ReturnsAllAlliesExceptCaster()
    {
        SkillDefinitionAsset skill = Skill("Force_Pull", "Active");
        BattleSnapshot snapshot = Snapshot(
            Actor(new List<string> { "Force_Pull" }, new List<int> { 0 }),
            Ally("team-0-slot-1", 1, 0),
            Ally("team-0-slot-2", 3, 0),
            Enemy("team-1-slot-0", 0, 1));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());

        Assert.That(targets.Count, Is.EqualTo(2));
        Assert.That(HasTarget(targets, 1, 0), Is.True);
        Assert.That(HasTarget(targets, 3, 0), Is.True);
    }

    [Test]
    public void ForcePull_AfterAllyTarget_ReturnsLegacyRadiusTwoAroundCaster()
    {
        SkillDefinitionAsset skill = Skill("Force_Pull", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 2, 2);
        actor.SkillIdsBySlot = new List<string> { "Force_Pull" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Ally("team-0-slot-1", 4, 4));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord> { new HexCoord(4, 4) });

        Assert.That(targets.Count, Is.EqualTo(18));
        Assert.That(HasTarget(targets, 0, 2), Is.True);
        Assert.That(HasTarget(targets, 4, 0), Is.True);
        Assert.That(HasTarget(targets, 4, 2), Is.True);
        Assert.That(HasTarget(targets, 2, 4), Is.True);
    }

    [Test]
    public void LongLick_InitialTargets_UsesLegacyRadiusThreeAroundCaster()
    {
        SkillDefinitionAsset skill = Skill("Long_Lick", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 2, 2);
        actor.SkillIdsBySlot = new List<string> { "Long_Lick" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(
            actor,
            Enemy("team-1-slot-0", 3, 2),
            Enemy("team-1-slot-1", 4, 4));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());

        Assert.That(targets.Count, Is.EqualTo(1));
        Assert.That(HasTarget(targets, 3, 2), Is.True);
        Assert.That(HasTarget(targets, 4, 4), Is.False);
    }

    [Test]
    public void LongLick_AfterEnemyTarget_ReturnsLegacyRadiusOneAroundCaster()
    {
        SkillDefinitionAsset skill = Skill("Long_Lick", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 2, 2);
        actor.SkillIdsBySlot = new List<string> { "Long_Lick" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 4, 2));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord> { new HexCoord(4, 2) });

        Assert.That(targets.Count, Is.EqualTo(6));
        Assert.That(HasTarget(targets, 2, 1), Is.True);
        Assert.That(HasTarget(targets, 2, 3), Is.True);
        Assert.That(HasTarget(targets, 3, 1), Is.True);
        Assert.That(HasTarget(targets, 1, 3), Is.True);
        Assert.That(HasTarget(targets, 1, 2), Is.True);
        Assert.That(HasTarget(targets, 3, 2), Is.True);
    }

    [Test]
    public void StoneThrow_RequiresEnemyUnitTargetAndRejectsEmptyHex()
    {
        SkillDefinitionAsset skill = Skill("Stone_Throw", "Active");
        BattleSnapshot snapshot = Snapshot(Actor(new List<string> { "Stone_Throw" }, new List<int> { 0 }));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Stone_Throw", new[] { new HexCoord(2, 0) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(SkillRejectReason.TargetHexNotLegal));
    }

    [Test]
    public void PassiveSkill_IsNotUsableAsActiveCast()
    {
        SkillDefinitionAsset skill = Skill("Cold_Blood", "Passive");
        BattleSnapshot snapshot = Snapshot(Actor(new List<string> { "Cold_Blood" }, new List<int> { 0 }));

        SkillValidationResult result = SkillRules.CanUse(SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(SkillRejectReason.SkillIsPassive));
    }

    [Test]
    public void StanceSkill_IsRepeatableNoCooldownNoTurnCost()
    {
        SkillDefinitionAsset skill = Skill("Range_Stance_Barb", "Active");
        BattleUnitSnapshot actor = Actor(new List<string> { "Range_Stance_Barb" }, new List<int> { 0 });
        actor.UsedSkillIdsThisTurn.Add("Range_Stance_Barb");
        BattleSnapshot snapshot = Snapshot(actor);

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Range_Stance_Barb", new List<HexCoord>()),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.CooldownTurns, Is.EqualTo(0));
        Assert.That(result.Cast.ConsumesTurn, Is.False);
        Assert.That(skill.ActivationRule.repeatableInTurn, Is.True);
    }

    [Test]
    public void Rush_EnemyOnForwardLine_StopsOnPreviousStraightHex()
    {
        SkillDefinitionAsset skill = Skill("Rush", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.SkillIdsBySlot = new List<string> { "Rush" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 4, 2));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Rush", new[] { new HexCoord(4, 2) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.ImpactHex.C, Is.EqualTo(4));
        Assert.That(result.Cast.ImpactHex.R, Is.EqualTo(2));
        Assert.That(result.Cast.DestinationHex.C, Is.EqualTo(3));
        Assert.That(result.Cast.DestinationHex.R, Is.EqualTo(2));
    }

    [Test]
    public void Rush_AllyOnForwardLine_AllowsPreviousStraightHex()
    {
        SkillDefinitionAsset skill = Skill("Rush", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.SkillIdsBySlot = new List<string> { "Rush" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Ally("team-0-slot-1", 4, 2));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Rush", new[] { new HexCoord(3, 2) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.DestinationHex.C, Is.EqualTo(3));
        Assert.That(result.Cast.DestinationHex.R, Is.EqualTo(2));
    }

    [Test]
    public void Rush_EnemyOffForwardLine_IsRejected()
    {
        SkillDefinitionAsset skill = Skill("Rush", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.SkillIdsBySlot = new List<string> { "Rush" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 2, 1));

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Rush", new[] { new HexCoord(2, 1) }),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(SkillRejectReason.TargetHexNotLegal));
    }

    [Test]
    public void ToxicFume_RequiresMovementThenAreaConfirmation()
    {
        SkillDefinitionAsset skill = Skill("Toxic_Fume", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.SkillIdsBySlot = new List<string> { "Toxic_Fume" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 4, 4));

        SkillContext context = SkillContext.Create(snapshot, "team-0-slot-0", skill, 0);
        List<SkillTarget> initialTargets = SkillRules.GetTargets(context, new List<HexCoord>());
        List<SkillTarget> confirmationTargets = SkillRules.GetTargets(
            context,
            new List<HexCoord> { new HexCoord(2, 1) });
        SkillValidationResult partialResult = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Toxic_Fume", new[] { new HexCoord(2, 1) }),
            context);
        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Toxic_Fume", new[] { new HexCoord(2, 1), new HexCoord(2, 1) }),
            context);

        Assert.That(HasTarget(initialTargets, 2, 1), Is.True);
        Assert.That(TargetCount(confirmationTargets), Is.EqualTo(1));
        Assert.That(HasTarget(confirmationTargets, 2, 1), Is.True);
        Assert.That(partialResult.IsValid, Is.False);
        Assert.That(partialResult.RejectReason, Is.EqualTo(SkillRejectReason.TargetCountMismatch));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.DestinationHex.C, Is.EqualTo(2));
        Assert.That(result.Cast.DestinationHex.R, Is.EqualTo(1));
        Assert.That(result.Cast.AffectedHexes.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Shapeshift_IsInstantSelfToggleAndDoesNotConsumeTurn()
    {
        SkillDefinitionAsset skill = Skill("Shapeshift", "Active");
        skill.ConfigureRules(
            new ActivationRuleData { cooldownTurns = 1, consumesTurn = true },
            new TargetingRuleData
            {
                targetFamily = SkillTargetFamily.Self,
                targetRoles = new[] { SkillTargetRole.ActorSelf },
                targetCount = 0
            },
            new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.DirectUnit },
            new[]
            {
                new SkillEffect
                {
                    effectType = SkillEffectType.ApplyStatus,
                    targetSource = SkillEffectTargetSource.Actor,
                    statusId = "Shapeshift"
                }
            });
        BattleUnitSnapshot actor = Actor(new List<string> { "Shapeshift" }, new List<int> { 0 });
        BattleSnapshot snapshot = Snapshot(actor);

        SkillValidationResult result = SkillRules.Validate(
            new SkillUse("team-0-slot-0", "Shapeshift", new List<HexCoord>()),
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Cast.ConsumesTurn, Is.False);
        Assert.That(result.Cast.CanMoveAfterUse, Is.True);
        Assert.That(result.Cast.Effects.Length, Is.EqualTo(1));
        Assert.That(result.Cast.Effects[0].effectType, Is.EqualTo(SkillEffectType.ToggleStance));
    }

    [Test]
    public void Slash_InitialMovementTargets_UseGeneratedOffsetRowNeighbours()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.MovementSpeed = 2;
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor);

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());

        Assert.That(HasTarget(targets, 1, 2), Is.True);
        Assert.That(HasTarget(targets, 2, 2), Is.True);
        Assert.That(HasTarget(targets, 0, 2), Is.True);
        Assert.That(HasTarget(targets, 1, 1), Is.True);
        Assert.That(HasTarget(targets, 0, 1), Is.True);
        Assert.That(HasTarget(targets, 1, 3), Is.True);
        Assert.That(HasTarget(targets, 0, 3), Is.True);
        Assert.That(HasTarget(targets, 2, 1), Is.False);
    }

    [Test]
    public void Slash_InitialMovementTargets_UseLegacyNeighboursWhenSnapshotIsLegacy()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 2);
        actor.MovementSpeed = 2;
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, true);

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());

        Assert.That(HasTarget(targets, 1, 2), Is.True);
        Assert.That(HasTarget(targets, 2, 2), Is.True);
        Assert.That(HasTarget(targets, 0, 2), Is.True);
        Assert.That(HasTarget(targets, 1, 1), Is.True);
        Assert.That(HasTarget(targets, 1, 3), Is.True);
        Assert.That(HasTarget(targets, 2, 1), Is.True);
        Assert.That(HasTarget(targets, 0, 3), Is.True);
        Assert.That(HasTarget(targets, 0, 1), Is.False);
    }

    [Test]
    public void Slash_InitialMovementTargets_MirrorLegacyPositiveMapWrap()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 4, 4);
        actor.MovementSpeed = 2;
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, true);

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());

        Assert.That(HasTarget(targets, 4, 4), Is.True);
        Assert.That(HasTarget(targets, 0, 4), Is.True);
        Assert.That(HasTarget(targets, 4, 0), Is.True);
        Assert.That(HasTarget(targets, 0, 3), Is.True);
        Assert.That(HasTarget(targets, 3, 0), Is.True);
    }

    [Test]
    public void Slash_InitialMovementTargets_MatchSharedMoveReachability()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 1);
        actor.MovementSpeed = 3;
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 3, 1));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord>());
        Dictionary<string, int> reachable = BattleHexGridUtility.FindReachableHexCosts(snapshot, actor);

        int expectedCount = 0;
        foreach (KeyValuePair<string, int> pair in reachable)
        {
            BattleHexSnapshot hex = FindHexByKey(snapshot, pair.Key);
            bool isActorHex = hex.C == actor.C && hex.R == actor.R;
            bool isOccupiedByOtherUnit = isActorHex == false && string.IsNullOrEmpty(hex.OccupyingUnitId) == false;
            if (pair.Value < actor.MovementSpeed && isOccupiedByOtherUnit == false)
            {
                expectedCount++;
                Assert.That(HasTarget(targets, hex.C, hex.R), Is.True, "Missing reachable Slash movement target " + pair.Key);
            }
            else
            {
                Assert.That(HasTarget(targets, hex.C, hex.R), Is.False, "Slash target should stop before movement boundary " + pair.Key);
            }
        }

        Assert.That(TargetCount(targets), Is.EqualTo(expectedCount));
    }

    [Test]
    public void Slash_DirectionalImpactTargets_AreShownAfterMovementDestination()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 1, 1);
        actor.MovementSpeed = 3;
        actor.SkillIdsBySlot = new List<string> { "Slash" };
        actor.CooldownsBySlot = new List<int> { 0 };
        BattleSnapshot snapshot = Snapshot(actor, Enemy("team-1-slot-0", 3, 1));

        List<SkillTarget> targets = SkillRules.GetTargets(
            SkillContext.Create(snapshot, "team-0-slot-0", skill, 0),
            new List<HexCoord> { new HexCoord(2, 1) });

        Assert.That(TargetCount(targets), Is.EqualTo(6));
        Assert.That(HasTarget(targets, 3, 1), Is.True);
        Assert.That(HasTarget(targets, 1, 1), Is.True);
        Assert.That(HasTarget(targets, 2, 0), Is.True);
        Assert.That(HasTarget(targets, 2, 2), Is.True);
    }

    [Test]
    public void Slash_AuthoredDamageOnlyEffect_IsResolvedAsMoveThenDamage()
    {
        SkillDefinitionAsset skill = Skill("Slash", "Active");
        skill.ConfigureRules(
            new ActivationRuleData { cooldownTurns = 1, consumesTurn = true },
            new TargetingRuleData(),
            new ResolutionRuleData(),
            new[]
            {
                new SkillEffect
                {
                    effectType = SkillEffectType.Damage,
                    targetSource = SkillEffectTargetSource.AffectedUnits,
                    damageMode = SkillDamageMode.BasicAttackDamage,
                    damageScale = 1f
                }
            });

        SkillEffect[] effects = skill.Effects;

        Assert.That(skill.ActivationRule.cooldownTurns, Is.EqualTo(2));
        Assert.That(effects.Length, Is.EqualTo(2));
        Assert.That(effects[0].effectType, Is.EqualTo(SkillEffectType.MoveUnit));
        Assert.That(effects[0].movementMode, Is.EqualTo(SkillMovementMode.NormalPathMove));
        Assert.That(effects[1].effectType, Is.EqualTo(SkillEffectType.Damage));
        Assert.That(effects[1].damageScale, Is.EqualTo(0.4f));
    }

    [Test]
    public void ToxicFume_AuthoredIncompleteEffects_AreResolvedAsMoveSelfStatusAndTaunt()
    {
        SkillDefinitionAsset skill = Skill("Toxic_Fume", "Active");
        skill.ConfigureRules(
            new ActivationRuleData { cooldownTurns = 2, consumesTurn = true },
            new TargetingRuleData
            {
                targetFamily = SkillTargetFamily.Movement,
                targetRoles = new[] { SkillTargetRole.MovementDestinationHex },
                targetCount = 1,
                radius = 1
            },
            new ResolutionRuleData(),
            new[]
            {
                new SkillEffect
                {
                    effectType = SkillEffectType.ApplyStatus,
                    targetSource = SkillEffectTargetSource.Actor,
                    statusId = "Toxic_Fume",
                    durationTurns = 2
                }
            });

        SkillEffect[] effects = skill.Effects;
        TargetingRuleData targeting = skill.TargetingRule;

        Assert.That(targeting.targetCount, Is.EqualTo(2));
        Assert.That(targeting.targetRoles, Is.EqualTo(new[] { SkillTargetRole.MovementDestinationHex, SkillTargetRole.AreaCenterHex }));
        Assert.That(targeting.allowDuplicateTargets, Is.True);
        Assert.That(effects.Length, Is.EqualTo(3));
        Assert.That(effects[0].effectType, Is.EqualTo(SkillEffectType.MoveUnit));
        Assert.That(effects[1].statusId, Is.EqualTo("Toxic_Fume"));
        Assert.That(effects[1].movementModifier, Is.EqualTo(-1));
        Assert.That(effects[1].counterAttacksModifier, Is.EqualTo(2));
        Assert.That(effects[2].statusId, Is.EqualTo("Taunt"));
        Assert.That(effects[2].targetSource, Is.EqualTo(SkillEffectTargetSource.AffectedUnits));
    }

    [Test]
    public void SpellOverTime_PositiveCounterAttackModifier_RestoresCounterAttackAvailability()
    {
        TosterHexUnit unit = new TosterHexUnit();
        unit.CounterAttacks = 1;
        unit.TempCounterAttacks = 0;
        unit.CounterAttackAvaible = false;

        new SpellOverTime(
            2,
            unit,
            unit,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            2,
            0,
            0,
            "Toxic_Fume",
            false);

        Assert.That(unit.CounterAttacks, Is.EqualTo(3));
        Assert.That(unit.TempCounterAttacks, Is.EqualTo(2));
        Assert.That(unit.CounterAttackAvaible, Is.True);
    }

    static SkillDefinitionAsset Skill(string skillId, string type)
    {
        SkillDefinitionAsset skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
        skill.Configure(skillId, type, string.Empty, string.Empty);
        return skill;
    }

    static BattleSnapshot Snapshot(BattleUnitSnapshot actor, params BattleUnitSnapshot[] others)
    {
        return Snapshot(actor, false, others);
    }

    static BattleSnapshot Snapshot(BattleUnitSnapshot actor, bool usesLegacyHexLayout, params BattleUnitSnapshot[] others)
    {
        List<BattleUnitSnapshot> units = new List<BattleUnitSnapshot> { actor };
        if (others != null)
        {
            units.AddRange(others);
        }

        List<BattleHexSnapshot> hexes = new List<BattleHexSnapshot>();
        for (int c = 0; c < 5; c++)
        {
            for (int r = 0; r < 5; r++)
            {
                hexes.Add(new BattleHexSnapshot
                {
                    C = c,
                    R = r,
                    IsWalkable = true,
                    OccupyingUnitId = Occupant(units, c, r)
                });
            }
        }

        return BattleSnapshotBuilder.Build(5, 5, hexes, units, actor.RuntimeUnitId, new BattleTurnStateSnapshot(), 123, "test-battle", 7, usesLegacyHexLayout);
    }

    static string Occupant(List<BattleUnitSnapshot> units, int c, int r)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].C == c && units[i].R == r)
            {
                return units[i].RuntimeUnitId;
            }
        }

        return string.Empty;
    }

    static bool HasTarget(List<SkillTarget> targets, int c, int r)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            SkillTarget target = targets[i];
            if (target != null && target.Hex != null && target.Hex.C == c && target.Hex.R == r)
            {
                return true;
            }
        }

        return false;
    }

    static int TargetCount(List<SkillTarget> targets)
    {
        return targets == null ? 0 : targets.Count;
    }

    static BattleHexSnapshot FindHexByKey(BattleSnapshot snapshot, string key)
    {
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.Hexes, Is.Not.Null);
        for (int i = 0; i < snapshot.Hexes.Count; i++)
        {
            BattleHexSnapshot hex = snapshot.Hexes[i];
            if (hex != null && BattleHexGridUtility.GetHexKey(hex.C, hex.R) == key)
            {
                return hex;
            }
        }

        Assert.Fail("Missing hex for key " + key);
        return null;
    }

    static BattleUnitSnapshot Actor(List<string> skillIds, List<int> cooldowns)
    {
        BattleUnitSnapshot actor = Unit("team-0-slot-0", 0, 0, 0, 0);
        actor.SkillIdsBySlot = skillIds;
        actor.CooldownsBySlot = cooldowns;
        return actor;
    }

    static BattleUnitSnapshot Enemy(string id, int c, int r)
    {
        return Unit(id, 1, 0, c, r);
    }

    static BattleUnitSnapshot Ally(string id, int c, int r)
    {
        return Unit(id, 0, 1, c, r);
    }

    static BattleUnitSnapshot Unit(string id, int team, int slot, int c, int r)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = id,
            TeamIndex = team,
            RosterIndexWithinTeam = slot,
            UnitName = "TestUnit",
            UnitType = "TestUnit",
            C = c,
            R = r,
            Amount = 10,
            TempHP = 20,
            BaseHP = 20,
            Attack = 5,
            Defense = 3,
            MovementSpeed = 3,
            Initiative = 7,
            MinDamage = 2,
            MaxDamage = 4,
            IsAlive = true,
            SkillIdsBySlot = new List<string>(),
            CooldownsBySlot = new List<int>(),
            UsedSkillIdsThisTurn = new List<string>()
        };
    }
}
#endif
