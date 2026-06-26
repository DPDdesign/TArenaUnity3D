using System;

public enum TacticalAIActionType
{
    Move,
    MoveAndAttack,
    BasicRangedAttack,
    Skill,
    Defend,
    Wait
}

[Serializable]
public sealed class TacticalAIPlannedAction
{
    public TacticalAIActionType ActionType;
    public string ActorUnitId = string.Empty;
    public string StableOrderKey = string.Empty;
    public BattleActionUse Use;
    public BattleAction Action;
    public BattleActionResult Result;

    public BattleActionKind ActionKind
    {
        get { return Action != null ? Action.ActionKind : ToBattleActionKind(ActionType); }
    }

    public static TacticalAIPlannedAction FromBattleAction(BattleAction action, BattleActionResult result = null)
    {
        if (action == null)
        {
            return null;
        }

        TacticalAIActionType actionType = ToTacticalActionType(action.ActionKind);
        TacticalAIPlannedAction planned = new TacticalAIPlannedAction
        {
            ActionType = actionType,
            ActorUnitId = action.ActorUnitId ?? string.Empty,
            StableOrderKey = action.StableOrderKey ?? string.Empty,
            Use = action.ToUse(),
            Action = action.Clone(),
            Result = result
        };

        return planned;
    }

    public static BattleActionKind ToBattleActionKind(TacticalAIActionType actionType)
    {
        switch (actionType)
        {
            case TacticalAIActionType.Move:
                return BattleActionKind.Move;
            case TacticalAIActionType.MoveAndAttack:
                return BattleActionKind.MoveAndAttack;
            case TacticalAIActionType.BasicRangedAttack:
                return BattleActionKind.BasicRangedAttack;
            case TacticalAIActionType.Skill:
                return BattleActionKind.Skill;
            case TacticalAIActionType.Defend:
                return BattleActionKind.Defend;
            case TacticalAIActionType.Wait:
            default:
                return BattleActionKind.Wait;
        }
    }

    public static TacticalAIActionType ToTacticalActionType(BattleActionKind actionKind)
    {
        switch (actionKind)
        {
            case BattleActionKind.Move:
                return TacticalAIActionType.Move;
            case BattleActionKind.MoveAndAttack:
            case BattleActionKind.BasicMeleeAttack:
                return TacticalAIActionType.MoveAndAttack;
            case BattleActionKind.BasicRangedAttack:
                return TacticalAIActionType.BasicRangedAttack;
            case BattleActionKind.Skill:
            case BattleActionKind.Stance:
                return TacticalAIActionType.Skill;
            case BattleActionKind.Defend:
                return TacticalAIActionType.Defend;
            case BattleActionKind.Wait:
            default:
                return TacticalAIActionType.Wait;
        }
    }
}
