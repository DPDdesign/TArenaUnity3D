using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "TArena/Skills/Skill Definition")]
public class SkillDefinitionAsset : ScriptableObject
{
    [SerializeField] private string skillName;
    [SerializeField] private string type;
    [TextArea(2, 6)]
    [SerializeField] private string info;
    [SerializeField] private string flags;
    [SerializeField] private ActivationRuleData activationRule = new ActivationRuleData();
    [SerializeField] private TargetingRuleData targetingRule = new TargetingRuleData();
    [SerializeField] private ResolutionRuleData resolutionRule = new ResolutionRuleData();
    [SerializeField] private SkillEffect[] effects = new SkillEffect[0];

    public string SkillName { get { return skillName; } }
    public string Type { get { return type; } }
    public string Info { get { return info; } }
    public string Flags { get { return flags; } }
    public ActivationRuleData ActivationRule { get { return SkillDefinitionMigrationDefaults.ResolveActivation(this, activationRule); } }
    public TargetingRuleData TargetingRule { get { return SkillDefinitionMigrationDefaults.ResolveTargeting(this, targetingRule); } }
    public ResolutionRuleData ResolutionRule { get { return SkillDefinitionMigrationDefaults.ResolveResolution(this, resolutionRule); } }
    public SkillEffect[] Effects { get { return SkillDefinitionMigrationDefaults.ResolveEffects(this, effects); } }

    public DataMapper.SkillDefinition ToSkillDefinition()
    {
        return new DataMapper.SkillDefinition(skillName, type, info, flags);
    }

#if UNITY_EDITOR
    public void Configure(string newSkillName, string newType, string newInfo, string newFlags)
    {
        skillName = newSkillName;
        type = newType;
        info = newInfo;
        flags = newFlags;
    }

    public void ConfigureRules(
        ActivationRuleData newActivationRule,
        TargetingRuleData newTargetingRule,
        ResolutionRuleData newResolutionRule,
        SkillEffect[] newEffects)
    {
        activationRule = newActivationRule == null ? new ActivationRuleData() : newActivationRule.Clone();
        targetingRule = newTargetingRule == null ? new TargetingRuleData() : newTargetingRule.Clone();
        resolutionRule = newResolutionRule == null ? new ResolutionRuleData() : newResolutionRule.Clone();
        effects = SkillEffect.CloneArray(newEffects);
    }
#endif
}

public enum SkillActivationKind
{
    Active,
    Passive,
    Stance
}

public enum SkillTargetFamily
{
    Self,
    UnitTarget,
    HexTarget,
    Movement
}

public enum SkillTargetRole
{
    None,
    ActorSelf,
    EnemyUnitHex,
    AllyUnitHex,
    AllyOrSelfUnitHex,
    AreaCenterHex,
    EmptyPlacementHex,
    RushLineHex,
    MovementDestinationHex,
    DirectionalImpactHex,
    EmptyDestinationHex,
    AdjacentEmptyDestinationHex,
    AutoAllEnemies,
    AutoAllAllies
}

public enum SkillTeamFilter
{
    Any,
    Enemy,
    Ally,
    AllyOrSelf
}

public enum SkillOccupancyRequirement
{
    Any,
    Empty,
    OccupiedUnit
}

public enum SkillResolutionFamily
{
    None,
    DirectUnit,
    MultiDirectUnit,
    EmptyHexPlacement,
    AreaAroundTarget,
    AreaAroundCaster,
    LineScan,
    MoveThenDirectionalAreaAttack,
    TeleportTargetToDestination,
    AroundPostMoveCaster,
    SpawnNearTarget
}

public enum SkillEffectType
{
    None,
    Damage,
    ApplyStatus,
    PlaceTrap,
    MoveUnit,
    ModifyStackAmount,
    SpawnUnit,
    ApplyHpCostOrSelfDamage,
    SetStanceMode,
    ToggleStance
}

public enum SkillEffectTargetSource
{
    Actor,
    PrimaryUnit,
    SelectedUnits,
    AffectedUnits,
    SelectedHexes,
    AffectedHexes,
    DestinationHex,
    TriggeringUnit
}

public enum SkillDamageMode
{
    None,
    BasicAttackDamage,
    RangedBasicAttackDamage,
    FixedDamageThroughDefense,
    PureDamage,
    DamageOverTime,
    PercentOfDamageTaken
}

public enum SkillMovementMode
{
    None,
    NormalPathMove,
    LineRush,
    TeleportTarget,
    MoveThenArea
}

[System.Serializable]
public class ActivationRuleData
{
    public SkillActivationKind activationKind = SkillActivationKind.Active;
    public int cooldownTurns;
    public bool consumesTurn = true;
    public bool canUseAfterMove;
    public bool canMoveAfterUse;
    public bool repeatableInTurn;
    public bool blocksWaitDefend;

    public ActivationRuleData Clone()
    {
        return new ActivationRuleData
        {
            activationKind = activationKind,
            cooldownTurns = cooldownTurns,
            consumesTurn = consumesTurn,
            canUseAfterMove = canUseAfterMove,
            canMoveAfterUse = canMoveAfterUse,
            repeatableInTurn = repeatableInTurn,
            blocksWaitDefend = blocksWaitDefend
        };
    }
}

[System.Serializable]
public class TargetingRuleData
{
    public SkillTargetFamily targetFamily = SkillTargetFamily.Self;
    public SkillTargetRole[] targetRoles = new SkillTargetRole[0];
    public int targetCount;
    public bool allowDuplicateTargets;
    public SkillTeamFilter teamFilter = SkillTeamFilter.Any;
    public SkillOccupancyRequirement occupancyRequirement = SkillOccupancyRequirement.Any;
    public bool requiresWalkable = true;
    public bool requiresEmptyDestination;
    public bool rejectsExistingTrap;
    public int range;
    public int radius;

    public TargetingRuleData Clone()
    {
        return new TargetingRuleData
        {
            targetFamily = targetFamily,
            targetRoles = targetRoles == null ? new SkillTargetRole[0] : (SkillTargetRole[])targetRoles.Clone(),
            targetCount = targetCount,
            allowDuplicateTargets = allowDuplicateTargets,
            teamFilter = teamFilter,
            occupancyRequirement = occupancyRequirement,
            requiresWalkable = requiresWalkable,
            requiresEmptyDestination = requiresEmptyDestination,
            rejectsExistingTrap = rejectsExistingTrap,
            range = range,
            radius = radius
        };
    }
}

[System.Serializable]
public class ResolutionRuleData
{
    public SkillResolutionFamily resolutionFamily = SkillResolutionFamily.None;
    public int radius;
    public bool skipMissingPrimaryTarget;

    public ResolutionRuleData Clone()
    {
        return new ResolutionRuleData
        {
            resolutionFamily = resolutionFamily,
            radius = radius,
            skipMissingPrimaryTarget = skipMissingPrimaryTarget
        };
    }
}

[System.Serializable]
public class SkillEffect
{
    public SkillEffectType effectType = SkillEffectType.None;
    public SkillEffectTargetSource targetSource = SkillEffectTargetSource.Actor;
    public SkillDamageMode damageMode = SkillDamageMode.None;
    public SkillMovementMode movementMode = SkillMovementMode.None;
    public string statusId = string.Empty;
    public string trapId = string.Empty;
    public string unitId = string.Empty;
    public float damageScale = 1f;
    public int fixedDamageValue;
    public int durationTurns;
    public int hpModifier;
    public int attackModifier;
    public int defenseModifier;
    public int movementModifier;
    public int initiativeModifier;
    public int maxDamageModifier;
    public int minDamageModifier;
    public int damageOverTime;
    public int resistanceModifier;
    public int counterAttacksModifier;
    public int damageModifier;
    public int specialResistanceModifier;
    public bool isStackable;
    public int hpCost;
    public int stackAmountDelta;
    public bool skipIfNoTarget;

    public SkillEffect Clone()
    {
        return new SkillEffect
        {
            effectType = effectType,
            targetSource = targetSource,
            damageMode = damageMode,
            movementMode = movementMode,
            statusId = statusId,
            trapId = trapId,
            unitId = unitId,
            damageScale = damageScale,
            fixedDamageValue = fixedDamageValue,
            durationTurns = durationTurns,
            hpModifier = hpModifier,
            attackModifier = attackModifier,
            defenseModifier = defenseModifier,
            movementModifier = movementModifier,
            initiativeModifier = initiativeModifier,
            maxDamageModifier = maxDamageModifier,
            minDamageModifier = minDamageModifier,
            damageOverTime = damageOverTime,
            resistanceModifier = resistanceModifier,
            counterAttacksModifier = counterAttacksModifier,
            damageModifier = damageModifier,
            specialResistanceModifier = specialResistanceModifier,
            isStackable = isStackable,
            hpCost = hpCost,
            stackAmountDelta = stackAmountDelta,
            skipIfNoTarget = skipIfNoTarget
        };
    }

    public static SkillEffect[] CloneArray(SkillEffect[] source)
    {
        if (source == null || source.Length == 0)
        {
            return new SkillEffect[0];
        }

        SkillEffect[] result = new SkillEffect[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i] == null ? new SkillEffect() : source[i].Clone();
        }

        return result;
    }
}
