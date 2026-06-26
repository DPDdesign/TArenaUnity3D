#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BattleActionLegalActionGenerationTests
{
    [Test]
    public void WaitAndDefend_AreAvailableOnlyBeforeMovementAndNonToggleSkillUse()
    {
        BattleSnapshot openingSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: false, usedSkillThisTurn: false, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<BattleAction> openingActions = BattleActionRules.GenerateLegalActions(
            openingSnapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(openingActions, BattleActionKind.Wait), Is.True);
        Assert.That(HasAction(openingActions, BattleActionKind.Defend), Is.True);

        BattleSnapshot movedSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: true, usedSkillThisTurn: false, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<BattleAction> movedActions = BattleActionRules.GenerateLegalActions(
            movedSnapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(movedActions, BattleActionKind.Wait), Is.False);
        Assert.That(HasAction(movedActions, BattleActionKind.Defend), Is.False);

        BattleSnapshot usedSkillSnapshot = CreateSnapshot(
            ActorUnit(0, 0, movedThisTurn: false, usedSkillThisTurn: true, waited: false),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        List<BattleAction> usedSkillActions = BattleActionRules.GenerateLegalActions(
            usedSkillSnapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(HasAction(usedSkillActions, BattleActionKind.Wait), Is.False);
        Assert.That(HasAction(usedSkillActions, BattleActionKind.Defend), Is.False);
    }

    [Test]
    public void MoveActions_ExcludeOccupiedDestinations()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 3, 0),
            AllyUnit("team-0-slot-1", 0, 1, 1, 0));

        List<BattleAction> actions = BattleActionRules.GenerateLegalActions(
            snapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(
            ContainsMoveDestination(actions, 1, 0),
            Is.False,
            "Occupied allied hex should not be emitted as a move destination.");
    }

    [Test]
    public void MoveAndAttackActions_IncludeCurrentHexWhenAlreadyAdjacent()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0),
            EnemyUnit("team-1-slot-0", 1, 0, 1, 0));

        List<BattleAction> actions = BattleActionRules.GenerateLegalActions(
            snapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        BattleAction action = FindAction(actions, BattleActionKind.MoveAndAttack, "team-1-slot-0");
        Assert.That(action, Is.Not.Null);
        Assert.That(action.DestinationHex, Is.Not.Null);
        Assert.That(action.DestinationHex.C, Is.EqualTo(0));
        Assert.That(action.DestinationHex.R, Is.EqualTo(0));
    }

    [Test]
    public void BasicRangedAttackActions_TargetEnemyUnitsOnly()
    {
        BattleSnapshot snapshot = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0),
            AllyUnit("team-0-slot-1", 0, 1, 0, 1));

        List<BattleAction> actions = BattleActionRules.GenerateLegalActions(
            snapshot,
            CreateProfile(),
            new TestSkillMetadataProvider());

        List<BattleAction> rangedActions = FindActions(actions, BattleActionKind.BasicRangedAttack);
        Assert.That(rangedActions.Count, Is.EqualTo(1));
        Assert.That(rangedActions[0].PrimaryTargetUnitId, Is.EqualTo("team-1-slot-0"));
    }

    [Test]
    public void PassiveAndCooldownBlockedSkills_AreNotLegalActions()
    {
        BattleUnitSnapshot actor = ActorUnit(0, 0);
        actor.SkillIdsBySlot = new List<string> { "BattleCry", "StoneSkinPassive", "Dash" };
        actor.CooldownsBySlot = new List<int> { 0, 0, 2 };

        TestSkillMetadataProvider metadataProvider = new TestSkillMetadataProvider();
        metadataProvider.AddUnitTargetDamage("BattleCry");
        metadataProvider.AddPassive("StoneSkinPassive");
        metadataProvider.AddSelfSkill("Dash", canUseAfterMove: true, canMoveAfterSkill: true);

        List<BattleAction> actions = BattleActionRules.GenerateLegalActions(
            CreateSnapshot(actor, EnemyUnit("team-1-slot-0", 1, 0, 2, 0)),
            CreateProfile(),
            metadataProvider);

        List<BattleAction> skillActions = FindActions(actions, BattleActionKind.Skill);
        Assert.That(skillActions.Count, Is.EqualTo(1));
        Assert.That(skillActions[0].SkillSlot, Is.EqualTo(0));
        Assert.That(skillActions[0].SkillId, Is.EqualTo("BattleCry"));
        Assert.That(skillActions.Select(action => action.StableOrderKey).Distinct().Count(), Is.EqualTo(skillActions.Count));
    }

    [Test]
    public void StanceSkills_GenerateStanceBattleActionsAndUseSharedToggleUtility()
    {
        BattleUnitSnapshot meleeActor = ActorUnit(0, 0, isRange: false);
        meleeActor.SkillIdsBySlot = new List<string> { "Range_Stance_Barb", "Melee_Stance_Barb" };
        meleeActor.CooldownsBySlot = new List<int> { 0, 0 };

        List<BattleAction> meleeActions = BattleActionRules.GenerateLegalActions(
            CreateSnapshot(meleeActor, EnemyUnit("team-1-slot-0", 1, 0, 2, 0)),
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(ContainsSkill(meleeActions, "Range_Stance_Barb"), Is.True);
        Assert.That(ContainsSkill(meleeActions, "Melee_Stance_Barb"), Is.True);
        Assert.That(HasAction(meleeActions, BattleActionKind.Stance), Is.True);

        BattleUnitSnapshot rangeActor = ActorUnit(0, 0, isRange: true);
        rangeActor.SkillIdsBySlot = new List<string> { "Range_Stance_Barb", "Melee_Stance_Barb" };
        rangeActor.CooldownsBySlot = new List<int> { 0, 0 };

        List<BattleAction> rangeActions = BattleActionRules.GenerateLegalActions(
            CreateSnapshot(rangeActor, EnemyUnit("team-1-slot-0", 1, 0, 2, 0)),
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(ContainsSkill(rangeActions, "Range_Stance_Barb"), Is.True);
        Assert.That(ContainsSkill(rangeActions, "Melee_Stance_Barb"), Is.True);
        Assert.That(BattleActionSkillUtility.IsRepeatableToggleSkillId("Range_Stance_Barb"), Is.True);
        Assert.That(BattleActionSkillUtility.IsRepeatableToggleSkillId("BattleCry"), Is.False);
    }

    [Test]
    public void LegalActionOrdering_IsStableAcrossEquivalentSnapshots()
    {
        BattleSnapshot first = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-1", 1, 1, 3, 1),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0));

        BattleSnapshot second = CreateSnapshot(
            ActorUnit(0, 0, isRange: true),
            EnemyUnit("team-1-slot-0", 1, 0, 2, 0),
            EnemyUnit("team-1-slot-1", 1, 1, 3, 1));

        List<BattleAction> firstActions = BattleActionRules.GenerateLegalActions(
            first,
            CreateProfile(),
            new TestSkillMetadataProvider());

        List<BattleAction> secondActions = BattleActionRules.GenerateLegalActions(
            second,
            CreateProfile(),
            new TestSkillMetadataProvider());

        Assert.That(GetStableKeys(firstActions), Is.EqualTo(GetStableKeys(secondActions)));
    }

    static TacticalAIResolvedProfile CreateProfile()
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        profile.MaxCandidatesPerActionType = 16;
        profile.MaxMoveCandidates = 16;
        profile.MaxAttackCandidates = 16;
        profile.MaxSkillCandidates = 16;
        profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        return profile;
    }

    static BattleSnapshot CreateSnapshot(BattleUnitSnapshot actor, params BattleUnitSnapshot[] others)
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
                    OccupyingUnitId = FindOccupant(units, c, r)
                });
            }
        }

        return BattleSnapshotBuilder.Build(
            5,
            5,
            hexes,
            units,
            actor.RuntimeUnitId,
            new BattleTurnStateSnapshot());
    }

    static string FindOccupant(List<BattleUnitSnapshot> units, int c, int r)
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

    static BattleUnitSnapshot ActorUnit(
        int c,
        int r,
        bool movedThisTurn = false,
        bool usedSkillThisTurn = false,
        bool waited = false,
        bool isRange = false)
    {
        return BaseUnit("team-0-slot-0", 0, 0, c, r, movedThisTurn, usedSkillThisTurn, waited, isRange);
    }

    static BattleUnitSnapshot AllyUnit(string runtimeUnitId, int teamIndex, int rosterIndex, int c, int r)
    {
        return BaseUnit(runtimeUnitId, teamIndex, rosterIndex, c, r, false, false, false, false);
    }

    static BattleUnitSnapshot EnemyUnit(string runtimeUnitId, int teamIndex, int rosterIndex, int c, int r)
    {
        return BaseUnit(runtimeUnitId, teamIndex, rosterIndex, c, r, false, false, false, false);
    }

    static BattleUnitSnapshot BaseUnit(
        string runtimeUnitId,
        int teamIndex,
        int rosterIndex,
        int c,
        int r,
        bool movedThisTurn,
        bool usedSkillThisTurn,
        bool waited,
        bool isRange)
    {
        return new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeUnitId,
            TeamIndex = teamIndex,
            RosterIndexWithinTeam = rosterIndex,
            UnitName = "Unit",
            UnitType = "Unit",
            C = c,
            R = r,
            Amount = 5,
            TempHP = 20,
            BaseHP = 20,
            Attack = 5,
            Defense = 4,
            MovementSpeed = 3,
            Initiative = 7,
            MinDamage = 2,
            MaxDamage = 4,
            IsAlive = true,
            IsRange = isRange,
            Waited = waited,
            Moved = false,
            MovedThisTurn = movedThisTurn,
            UsedSkillThisTurn = usedSkillThisTurn,
            UsedSkillIdsThisTurn = new List<string>(),
            CanMoveAfterSkillThisTurn = false,
            CooldownsBySlot = new List<int> { 0 },
            SkillIdsBySlot = new List<string> { "BattleCry" },
            Statuses = new List<BattleStatusSnapshot>()
        };
    }

    static bool HasAction(List<BattleAction> actions, BattleActionKind actionKind)
    {
        return FindAction(actions, actionKind, null) != null;
    }

    static BattleAction FindAction(
        List<BattleAction> actions,
        BattleActionKind actionKind,
        string targetUnitId)
    {
        if (actions == null)
        {
            return null;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            BattleAction action = actions[i];
            if (action.ActionKind != actionKind)
            {
                continue;
            }

            if (targetUnitId == null || action.PrimaryTargetUnitId == targetUnitId)
            {
                return action;
            }
        }

        return null;
    }

    static List<BattleAction> FindActions(List<BattleAction> actions, BattleActionKind actionKind)
    {
        List<BattleAction> matches = new List<BattleAction>();
        if (actions == null)
        {
            return matches;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionKind == actionKind)
            {
                matches.Add(actions[i]);
            }
        }

        return matches;
    }

    static bool ContainsMoveDestination(List<BattleAction> actions, int c, int r)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            BattleAction action = actions[i];
            if (action.ActionKind == BattleActionKind.Move &&
                action.DestinationHex != null &&
                action.DestinationHex.C == c &&
                action.DestinationHex.R == r)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsSkill(List<BattleAction> actions, string skillId)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            BattleAction action = actions[i];
            if ((action.ActionKind == BattleActionKind.Skill || action.ActionKind == BattleActionKind.Stance) &&
                string.Equals(action.SkillId, skillId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    static List<string> GetStableKeys(List<BattleAction> actions)
    {
        List<string> keys = new List<string>();
        for (int i = 0; i < actions.Count; i++)
        {
            keys.Add(actions[i].StableOrderKey);
        }

        return keys;
    }

    sealed class TestSkillMetadataProvider : ITacticalAISkillMetadataProvider, ITacticalAISkillSpecProvider
    {
        readonly Dictionary<string, SkillDefinitionAsset> definitions =
            new Dictionary<string, SkillDefinitionAsset>();

        public TestSkillMetadataProvider()
        {
            AddUnitTargetDamage("BattleCry");
            AddStance("Range_Stance_Barb");
            AddStance("Melee_Stance_Barb");
        }

        public void AddUnitTargetDamage(string skillId)
        {
            definitions[skillId] = Skill(
                skillId,
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Active,
                    consumesTurn = true
                },
                new TargetingRuleData
                {
                    targetFamily = SkillTargetFamily.UnitTarget,
                    targetRoles = new[] { SkillTargetRole.EnemyUnitHex },
                    targetCount = 1,
                    requiresWalkable = true
                },
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.DirectUnit },
                new[]
                {
                    new SkillEffect
                    {
                        effectType = SkillEffectType.Damage,
                        targetSource = SkillEffectTargetSource.PrimaryUnit,
                        damageMode = SkillDamageMode.BasicAttackDamage,
                        damageScale = 1f
                    }
                });
        }

        public void AddSelfSkill(string skillId, bool canUseAfterMove, bool canMoveAfterSkill)
        {
            definitions[skillId] = Skill(
                skillId,
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Active,
                    consumesTurn = true,
                    canUseAfterMove = canUseAfterMove,
                    canMoveAfterUse = canMoveAfterSkill
                },
                SelfTargeting(),
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.None },
                new[] { new SkillEffect { effectType = SkillEffectType.None } });
        }

        public void AddPassive(string skillId)
        {
            definitions[skillId] = Skill(
                skillId,
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Passive,
                    consumesTurn = false
                },
                SelfTargeting(),
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.None },
                new[] { new SkillEffect { effectType = SkillEffectType.None } });
        }

        void AddStance(string skillId)
        {
            definitions[skillId] = Skill(
                skillId,
                new ActivationRuleData
                {
                    activationKind = SkillActivationKind.Stance,
                    consumesTurn = false,
                    repeatableInTurn = true
                },
                SelfTargeting(),
                new ResolutionRuleData { resolutionFamily = SkillResolutionFamily.None },
                new[] { new SkillEffect { effectType = SkillEffectType.ToggleStance } });
        }

        public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
        {
            SkillDefinitionAsset definition;
            definitions.TryGetValue(skillId ?? string.Empty, out definition);
            ActivationRuleData activation = definition != null ? definition.ActivationRule : new ActivationRuleData();
            metadata = new TacticalAISkillMetadata
            {
                SkillId = skillId ?? string.Empty,
                IsPassive = activation.activationKind == SkillActivationKind.Passive,
                CanUseAfterMove = activation.canUseAfterMove,
                CanMoveAfterSkill = activation.canMoveAfterUse,
                IsRepeatableToggle = activation.repeatableInTurn || BattleActionSkillUtility.IsRepeatableToggleSkillId(skillId)
            };
            return true;
        }

        public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
        {
            SkillDefinitionAsset definition;
            if (definitions.TryGetValue(skillId ?? string.Empty, out definition) == false || definition == null)
            {
                spec = null;
                return false;
            }

            spec = SkillDefinitionSpec.FromAsset(definition);
            return spec != null;
        }

        static TargetingRuleData SelfTargeting()
        {
            return new TargetingRuleData
            {
                targetFamily = SkillTargetFamily.Self,
                targetRoles = new[] { SkillTargetRole.ActorSelf },
                targetCount = 0,
                requiresWalkable = true
            };
        }

        static SkillDefinitionAsset Skill(
            string skillId,
            ActivationRuleData activation,
            TargetingRuleData targeting,
            ResolutionRuleData resolution,
            SkillEffect[] effects)
        {
            SkillDefinitionAsset skill = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            skill.Configure(skillId, "Active", string.Empty, string.Empty);
            skill.ConfigureRules(activation, targeting, resolution, effects);
            return skill;
        }
    }
}
#endif
