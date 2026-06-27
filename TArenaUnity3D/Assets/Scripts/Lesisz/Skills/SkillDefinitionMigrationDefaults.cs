using System;
using System.Collections.Generic;

public static class SkillDefinitionMigrationDefaults
{
    public static ActivationRuleData ResolveActivation(SkillDefinitionAsset skill, ActivationRuleData authored)
    {
        ActivationRuleData result = authored == null ? new ActivationRuleData() : authored.Clone();
        string skillId = GetSkillId(skill);

        if (IsPassive(skill))
        {
            result.activationKind = SkillActivationKind.Passive;
            result.consumesTurn = false;
            result.repeatableInTurn = false;
            result.cooldownTurns = 0;
            return result;
        }

        if (IsStance(skillId))
        {
            result.activationKind = SkillActivationKind.Stance;
            result.cooldownTurns = 0;
            result.consumesTurn = false;
            result.canUseAfterMove = true;
            result.canMoveAfterUse = true;
            result.repeatableInTurn = true;
            return result;
        }

        if (skillId == "Shapeshift")
        {
            result.activationKind = SkillActivationKind.Active;
            result.cooldownTurns = 0;
            result.consumesTurn = false;
            result.canUseAfterMove = true;
            result.canMoveAfterUse = true;
            result.repeatableInTurn = false;
            return result;
        }

        result.activationKind = SkillActivationKind.Active;
        result.cooldownTurns = result.cooldownTurns > 0 ? result.cooldownTurns : GetLegacyCooldown(skillId);
        if (skillId == "Slash")
        {
            result.cooldownTurns = 2;
        }

        result.canUseAfterMove = HasFlag(skill, "AM");
        result.canMoveAfterUse = HasFlag(skill, "NI");
        result.consumesTurn = HasFlag(skill, "NI") == false;
        result.repeatableInTurn = false;
        return result;
    }

    public static TargetingRuleData ResolveTargeting(SkillDefinitionAsset skill, TargetingRuleData authored)
    {
        TargetingRuleData result = authored == null ? new TargetingRuleData() : authored.Clone();
        string skillId = GetSkillId(skill);
        if (skillId == "Toxic_Fume")
        {
            result = Targeting(SkillTargetFamily.Movement, 2, SkillTargetRole.MovementDestinationHex, SkillTargetRole.AreaCenterHex);
            result.allowDuplicateTargets = true;
            result.radius = 1;
            return result;
        }

        if (result.targetRoles != null && result.targetRoles.Length > 0)
        {
            return result;
        }

        if (IsPassive(skill) || IsStance(skillId))
        {
            return Targeting(SkillTargetFamily.Self, 0);
        }

        switch (skillId)
        {
            case "Chope":
            case "Rage":
            case "Stone_Stance":
            case "Shapeshift":
                return Targeting(SkillTargetFamily.Self, 0, SkillTargetRole.ActorSelf);
            case "Insult":
                return Targeting(SkillTargetFamily.UnitTarget, 0, SkillTargetRole.AutoAllEnemies);
            case "Defence_Ritual":
                return Targeting(SkillTargetFamily.UnitTarget, 0, SkillTargetRole.AutoAllAllies);
            case "Hate":
            case "Blind_by_light":
                return Targeting(SkillTargetFamily.UnitTarget, 1, SkillTargetRole.EnemyUnitHex);
            case "Double_Throw":
                result = Targeting(SkillTargetFamily.UnitTarget, 2, SkillTargetRole.EnemyUnitHex, SkillTargetRole.EnemyUnitHex);
                result.allowDuplicateTargets = true;
                return result;
            case "Tough_Skin":
                return Targeting(SkillTargetFamily.UnitTarget, 1, SkillTargetRole.AllyOrSelfUnitHex);
            case "Axe_Rain":
            case "Fire_Ball":
                result = Targeting(SkillTargetFamily.HexTarget, 1, SkillTargetRole.AreaCenterHex);
                result.radius = 1;
                return result;
            case "Spike_Trap":
            case "Rope_Trap":
                result = Targeting(SkillTargetFamily.HexTarget, 1, SkillTargetRole.EmptyPlacementHex);
                result.occupancyRequirement = SkillOccupancyRequirement.Empty;
                result.requiresWalkable = true;
                result.rejectsExistingTrap = true;
                return result;
            case "Rush":
                return Targeting(SkillTargetFamily.Movement, 1, SkillTargetRole.RushLineHex);
            case "Slash":
                return Targeting(SkillTargetFamily.Movement, 2, SkillTargetRole.MovementDestinationHex, SkillTargetRole.DirectionalImpactHex);
            case "Heavy_Fists":
                return Targeting(SkillTargetFamily.Movement, 2, SkillTargetRole.MovementDestinationHex, SkillTargetRole.DirectionalImpactHex);
            case "Force_Pull":
                result = Targeting(SkillTargetFamily.Movement, 2, SkillTargetRole.AllyUnitHex, SkillTargetRole.EmptyDestinationHex);
                result.radius = 2;
                return result;
            case "Long_Lick":
                result = Targeting(SkillTargetFamily.Movement, 2, SkillTargetRole.EnemyUnitHex, SkillTargetRole.AdjacentEmptyDestinationHex);
                result.radius = 3;
                return result;
            case "Stone_Throw":
                return Targeting(SkillTargetFamily.UnitTarget, 1, SkillTargetRole.EnemyUnitHex);
            default:
                return result;
        }
    }

    public static ResolutionRuleData ResolveResolution(SkillDefinitionAsset skill, ResolutionRuleData authored)
    {
        ResolutionRuleData result = authored == null ? new ResolutionRuleData() : authored.Clone();
        if (result.resolutionFamily != SkillResolutionFamily.None)
        {
            return result;
        }

        string skillId = GetSkillId(skill);
        switch (skillId)
        {
            case "Chope":
                result.resolutionFamily = SkillResolutionFamily.AreaAroundCaster;
                result.radius = 1;
                return result;
            case "Axe_Rain":
            case "Fire_Ball":
                result.resolutionFamily = SkillResolutionFamily.AreaAroundTarget;
                result.radius = 1;
                return result;
            case "Spike_Trap":
            case "Rope_Trap":
                result.resolutionFamily = SkillResolutionFamily.EmptyHexPlacement;
                return result;
            case "Double_Throw":
                result.resolutionFamily = SkillResolutionFamily.MultiDirectUnit;
                return result;
            case "Rush":
                result.resolutionFamily = SkillResolutionFamily.LineScan;
                result.skipMissingPrimaryTarget = true;
                return result;
            case "Slash":
            case "Heavy_Fists":
                result.resolutionFamily = SkillResolutionFamily.MoveThenDirectionalAreaAttack;
                result.radius = 1;
                return result;
            case "Force_Pull":
            case "Long_Lick":
                result.resolutionFamily = SkillResolutionFamily.TeleportTargetToDestination;
                return result;
            case "Toxic_Fume":
                result.resolutionFamily = SkillResolutionFamily.AroundPostMoveCaster;
                result.radius = 1;
                return result;
            case "Stone_Throw":
                result.resolutionFamily = SkillResolutionFamily.SpawnNearTarget;
                return result;
            case "Insult":
            case "Defence_Ritual":
                result.resolutionFamily = SkillResolutionFamily.MultiDirectUnit;
                return result;
            case "Rage":
            case "Stone_Stance":
            case "Shapeshift":
                result.resolutionFamily = SkillResolutionFamily.DirectUnit;
                return result;
            default:
                result.resolutionFamily = SkillResolutionFamily.DirectUnit;
                return result;
        }
    }

    public static SkillEffect[] ResolveEffects(SkillDefinitionAsset skill, SkillEffect[] authored)
    {
        string skillId = GetSkillId(skill);
        if (skillId == "Slash")
        {
            return new[]
            {
                Move(SkillMovementMode.NormalPathMove, SkillEffectTargetSource.Actor, false),
                Damage(SkillDamageMode.BasicAttackDamage, SkillEffectTargetSource.AffectedUnits, 0.4f, false)
            };
        }

        if (skillId == "Toxic_Fume")
        {
            return new[]
            {
                Move(SkillMovementMode.NormalPathMove, SkillEffectTargetSource.Actor, false),
                Status("Toxic_Fume", SkillEffectTargetSource.Actor, 2, movement: -1, counterAttacks: 2),
                Status("Taunt", SkillEffectTargetSource.AffectedUnits, 2)
            };
        }

        if (skillId == "Shapeshift")
        {
            return new[]
            {
                new SkillEffect
                {
                    effectType = SkillEffectType.ToggleStance,
                    targetSource = SkillEffectTargetSource.Actor
                }
            };
        }

        if (authored != null && authored.Length > 0)
        {
            return SkillEffect.CloneArray(authored);
        }

        if (IsStance(skillId))
        {
            return new[]
            {
                new SkillEffect
                {
                    effectType = SkillEffectType.ToggleStance,
                    targetSource = SkillEffectTargetSource.Actor
                }
            };
        }

        List<SkillEffect> effects = new List<SkillEffect>();
        switch (skillId)
        {
            case "Spike_Trap":
            case "Rope_Trap":
                effects.Add(PlaceTrap(skillId));
                break;
            case "Rush":
                effects.Add(Move(SkillMovementMode.LineRush, SkillEffectTargetSource.Actor, true));
                effects.Add(Damage(SkillDamageMode.BasicAttackDamage, SkillEffectTargetSource.PrimaryUnit, 1f, true));
                effects.Add(Status("Rush", SkillEffectTargetSource.Actor, 1, hp: 2, attack: 2, defense: 2, stackable: true));
                break;
            case "Double_Throw":
                effects.Add(Damage(SkillDamageMode.RangedBasicAttackDamage, SkillEffectTargetSource.SelectedUnits, 0.4f, false));
                break;
            case "Heavy_Fists":
                effects.Add(HpCost(20));
                effects.Add(Move(SkillMovementMode.NormalPathMove, SkillEffectTargetSource.Actor, false));
                effects.Add(Damage(SkillDamageMode.BasicAttackDamage, SkillEffectTargetSource.AffectedUnits, 0.7f, false));
                break;
            case "Force_Pull":
            case "Long_Lick":
                effects.Add(Move(SkillMovementMode.TeleportTarget, SkillEffectTargetSource.PrimaryUnit, false));
                if (skillId == "Long_Lick")
                {
                    effects.Add(Status("Taunt", SkillEffectTargetSource.PrimaryUnit, 2));
                }
                break;
            case "Chope":
            case "Axe_Rain":
            case "Fire_Ball":
                effects.Add(Damage(SkillDamageMode.BasicAttackDamage, SkillEffectTargetSource.AffectedUnits, 1f, false));
                break;
            case "Hate":
                effects.Add(Status("Hate", SkillEffectTargetSource.Actor, 2));
                effects.Add(Status("Hate", SkillEffectTargetSource.PrimaryUnit, 2));
                break;
            case "Insult":
                effects.Add(Status("Insult", SkillEffectTargetSource.AffectedUnits, 2, movement: -1, initiative: -1));
                break;
            case "Rage":
                effects.Add(Status("Rage", SkillEffectTargetSource.Actor, 2));
                break;
            case "Tough_Skin":
                effects.Add(Status("Tough_Skin", SkillEffectTargetSource.PrimaryUnit, 2, specialResistance: 15));
                break;
            case "Defence_Ritual":
                effects.Add(Status("Defence_Ritual", SkillEffectTargetSource.AffectedUnits, 2, defense: 1));
                break;
            case "Stone_Stance":
                effects.Add(Status("Stone_Stance", SkillEffectTargetSource.Actor, 2, counterAttacks: -1, specialResistance: 100));
                break;
            case "Blind_by_light":
                effects.Add(Status("Blind", SkillEffectTargetSource.PrimaryUnit, 2));
                break;
            case "Stone_Throw":
                effects.Add(Damage(SkillDamageMode.FixedDamageThroughDefense, SkillEffectTargetSource.PrimaryUnit, 1f, false));
                effects.Add(ModifyStack(-1));
                effects.Add(Spawn("StoneGolem"));
                break;
            default:
                break;
        }

        return effects.ToArray();
    }

    static TargetingRuleData Targeting(SkillTargetFamily family, int targetCount, params SkillTargetRole[] roles)
    {
        return new TargetingRuleData
        {
            targetFamily = family,
            targetCount = targetCount,
            targetRoles = roles ?? new SkillTargetRole[0],
            requiresWalkable = true
        };
    }

    static SkillEffect Damage(SkillDamageMode mode, SkillEffectTargetSource targetSource, float scale, bool skipIfNoTarget)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.Damage,
            damageMode = mode,
            targetSource = targetSource,
            damageScale = scale,
            skipIfNoTarget = skipIfNoTarget
        };
    }

    static SkillEffect Status(
        string statusId,
        SkillEffectTargetSource targetSource,
        int duration,
        int hp = 0,
        int attack = 0,
        int defense = 0,
        int movement = 0,
        int initiative = 0,
        int maxDamage = 0,
        int minDamage = 0,
        int damageOverTime = 0,
        int resistance = 0,
        int counterAttacks = 0,
        int damage = 0,
        int specialResistance = 0,
        bool stackable = false)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.ApplyStatus,
            targetSource = targetSource,
            statusId = statusId,
            durationTurns = duration,
            hpModifier = hp,
            attackModifier = attack,
            defenseModifier = defense,
            movementModifier = movement,
            initiativeModifier = initiative,
            maxDamageModifier = maxDamage,
            minDamageModifier = minDamage,
            damageOverTime = damageOverTime,
            resistanceModifier = resistance,
            counterAttacksModifier = counterAttacks,
            damageModifier = damage,
            specialResistanceModifier = specialResistance,
            isStackable = stackable
        };
    }

    static SkillEffect PlaceTrap(string trapId)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.PlaceTrap,
            targetSource = SkillEffectTargetSource.SelectedHexes,
            trapId = trapId,
            durationTurns = 999
        };
    }

    static SkillEffect Move(SkillMovementMode mode, SkillEffectTargetSource targetSource, bool skipIfNoTarget)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.MoveUnit,
            movementMode = mode,
            targetSource = targetSource,
            skipIfNoTarget = skipIfNoTarget
        };
    }

    static SkillEffect HpCost(int hpCost)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.ApplyHpCostOrSelfDamage,
            targetSource = SkillEffectTargetSource.Actor,
            hpCost = hpCost
        };
    }

    static SkillEffect ModifyStack(int amountDelta)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.ModifyStackAmount,
            targetSource = SkillEffectTargetSource.Actor,
            stackAmountDelta = amountDelta
        };
    }

    static SkillEffect Spawn(string unitId)
    {
        return new SkillEffect
        {
            effectType = SkillEffectType.SpawnUnit,
            targetSource = SkillEffectTargetSource.DestinationHex,
            unitId = unitId
        };
    }

    static string GetSkillId(SkillDefinitionAsset skill)
    {
        return skill == null ? string.Empty : skill.SkillName ?? string.Empty;
    }

    static bool IsPassive(SkillDefinitionAsset skill)
    {
        return skill != null && string.Equals(skill.Type, "Passive", StringComparison.Ordinal);
    }

    static bool IsStance(string skillId)
    {
        return string.IsNullOrEmpty(skillId) == false &&
            (skillId.StartsWith("Melee_Stance", StringComparison.Ordinal) ||
             skillId.StartsWith("Range_Stance", StringComparison.Ordinal));
    }

    static bool HasFlag(SkillDefinitionAsset skill, string flag)
    {
        if (skill == null || string.IsNullOrEmpty(skill.Flags) || string.IsNullOrEmpty(flag))
        {
            return false;
        }

        string[] parts = skill.Flags.Split(new char[] { ' ', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == flag)
            {
                return true;
            }
        }

        return false;
    }

    static int GetLegacyCooldown(string skillId)
    {
        switch (skillId)
        {
            case "Axe_Rain":
            case "Hate":
            case "Rope_Trap":
            case "Tough_Skin":
            case "Toxic_Fume":
                return 2;
            case "Rage":
            case "Spike_Trap":
            case "Blind_by_light":
            case "Stone_Throw":
                return 3;
            case "Insult":
                return 4;
            case "Stone_Stance":
                return 5;
            default:
                return 1;
        }
    }
}
