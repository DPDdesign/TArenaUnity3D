using System;
using System.Collections.Generic;

public enum BattleActionKind
{
    Move,
    MoveAndAttack,
    BasicMeleeAttack,
    BasicRangedAttack,
    Wait,
    Defend,
    Skill,
    Stance,
    Passive,
    Trap,
    Automatic
}

public enum BattleActionResultEventType
{
    None,
    UnitMoved,
    DamageApplied,
    StatusApplied,
    TrapPlaced,
    TrapTriggered,
    UnitSpawned,
    StackAmountChanged,
    HpCostApplied,
    CooldownApplied,
    TurnCostApplied,
    WaitApplied,
    DefenseApplied,
    StanceChanged,
    PassiveTriggered,
    TrapExpired,
    ActionRejected
}

[Serializable]
public sealed class BattleActionUse
{
    public string ActorUnitId = string.Empty;
    public BattleActionKind ActionKind;
    public List<HexCoord> SelectedHexes = new List<HexCoord>();
    public string TargetUnitId = string.Empty;
    public int SkillSlot = -1;
    public string SkillId = string.Empty;
    public string ClientRequestId = string.Empty;
    public string BattleId = string.Empty;
    public int ActionIndex;
    public int ActionSeed;

    public BattleActionUse Clone()
    {
        return new BattleActionUse
        {
            ActorUnitId = ActorUnitId ?? string.Empty,
            ActionKind = ActionKind,
            SelectedHexes = BattleActionModelUtility.CopyHexes(SelectedHexes),
            TargetUnitId = TargetUnitId ?? string.Empty,
            SkillSlot = SkillSlot,
            SkillId = SkillId ?? string.Empty,
            ClientRequestId = ClientRequestId ?? string.Empty,
            BattleId = BattleId ?? string.Empty,
            ActionIndex = ActionIndex,
            ActionSeed = ActionSeed
        };
    }
}

[Serializable]
public sealed class BattleAction
{
    public string ActorUnitId = string.Empty;
    public BattleActionKind ActionKind;
    public List<HexCoord> SelectedHexes = new List<HexCoord>();
    public HexCoord DestinationHex;
    public HexCoord ImpactHex;
    public string PrimaryTargetUnitId = string.Empty;
    public List<string> TargetUnitIds = new List<string>();
    public List<string> AffectedUnitIds = new List<string>();
    public List<HexCoord> AffectedHexes = new List<HexCoord>();
    public int SkillSlot = -1;
    public string SkillId = string.Empty;
    public int TurnCost = 1;
    public bool EndsTurn = true;
    public bool AllowsPostMoveFollowUp;
    public int ActionIndex;
    public int ActionSeed;
    public string StableOrderKey = string.Empty;
    public SkillCast SkillCast;

    public BattleActionUse ToUse()
    {
        return new BattleActionUse
        {
            ActorUnitId = ActorUnitId ?? string.Empty,
            ActionKind = ActionKind,
            SelectedHexes = BattleActionModelUtility.CopyHexes(SelectedHexes),
            TargetUnitId = PrimaryTargetUnitId ?? string.Empty,
            SkillSlot = SkillSlot,
            SkillId = SkillId ?? string.Empty,
            ActionIndex = ActionIndex,
            ActionSeed = ActionSeed
        };
    }

    public BattleAction Clone()
    {
        return new BattleAction
        {
            ActorUnitId = ActorUnitId ?? string.Empty,
            ActionKind = ActionKind,
            SelectedHexes = BattleActionModelUtility.CopyHexes(SelectedHexes),
            DestinationHex = BattleActionModelUtility.CopyHex(DestinationHex),
            ImpactHex = BattleActionModelUtility.CopyHex(ImpactHex),
            PrimaryTargetUnitId = PrimaryTargetUnitId ?? string.Empty,
            TargetUnitIds = new List<string>(TargetUnitIds ?? new List<string>()),
            AffectedUnitIds = new List<string>(AffectedUnitIds ?? new List<string>()),
            AffectedHexes = BattleActionModelUtility.CopyHexes(AffectedHexes),
            SkillSlot = SkillSlot,
            SkillId = SkillId ?? string.Empty,
            TurnCost = TurnCost,
            EndsTurn = EndsTurn,
            AllowsPostMoveFollowUp = AllowsPostMoveFollowUp,
            ActionIndex = ActionIndex,
            ActionSeed = ActionSeed,
            StableOrderKey = StableOrderKey ?? string.Empty,
            SkillCast = SkillCast != null ? SkillCast.Clone() : null
        };
    }
}

[Serializable]
public sealed class BattleActionResultEvent
{
    public BattleActionResultEventType EventType = BattleActionResultEventType.None;
    public string ActorUnitId = string.Empty;
    public string TargetUnitId = string.Empty;
    public HexCoord Hex;
    public string StatusId = string.Empty;
    public string TrapId = string.Empty;
    public int Amount;
    public string Message = string.Empty;
    public int Duration;
    public int HpModifier;
    public int AttackModifier;
    public int DefenseModifier;
    public int MovementModifier;
    public int InitiativeModifier;
    public int MaxDamageModifier;
    public int MinDamageModifier;
    public int DamageOverTime;
    public int ResistanceModifier;
    public int CounterAttacksModifier;
    public int DamageModifier;
    public int SpecialResistanceModifier;
    public bool IsStackable;
    public bool RemoveTrap;
    public bool ShowTrapImmediately = true;
    public string PresentationSkillId = string.Empty;
    public int TrimPathSteps;
}

[Serializable]
public sealed class BattleActionResult
{
    public string ActorUnitId = string.Empty;
    public BattleActionKind ActionKind;
    public int ActionIndex;
    public bool IsRejected;
    public string RejectReason = string.Empty;
    public List<BattleActionResultEvent> Events = new List<BattleActionResultEvent>();

    public void Add(BattleActionResultEvent resultEvent)
    {
        if (resultEvent != null)
        {
            Events.Add(resultEvent);
        }
    }
}

public sealed class BattleActionValidationResult
{
    public bool IsValid;
    public string RejectReason = string.Empty;
    public BattleAction Action;

    public static BattleActionValidationResult Valid(BattleAction action)
    {
        return new BattleActionValidationResult
        {
            IsValid = true,
            Action = action
        };
    }

    public static BattleActionValidationResult Invalid(string reason)
    {
        return new BattleActionValidationResult
        {
            IsValid = false,
            RejectReason = reason ?? string.Empty
        };
    }
}

public static class BattleActionModelUtility
{
    public static HexCoord CopyHex(HexCoord source)
    {
        return source == null ? null : new HexCoord(source.C, source.R);
    }

    public static List<HexCoord> CopyHexes(IEnumerable<HexCoord> source)
    {
        List<HexCoord> result = new List<HexCoord>();
        if (source == null)
        {
            return result;
        }

        foreach (HexCoord hex in source)
        {
            if (hex != null)
            {
                result.Add(new HexCoord(hex.C, hex.R));
            }
        }

        return result;
    }
}

public static class BattleActionSkillUtility
{
    public static bool IsRepeatableToggleSkillId(string skillId)
    {
        return string.Equals(skillId, "Melee_Stance_Barb", StringComparison.Ordinal) ||
            string.Equals(skillId, "Range_Stance_Barb", StringComparison.Ordinal) ||
            string.Equals(skillId, "Melee_Stance_Lizard", StringComparison.Ordinal) ||
            string.Equals(skillId, "Range_Stance_Lizard", StringComparison.Ordinal);
    }
}
