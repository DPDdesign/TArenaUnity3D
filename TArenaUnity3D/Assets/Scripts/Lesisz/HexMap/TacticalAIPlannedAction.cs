using System;

[Serializable]
public sealed class TacticalAIPlannedAction
{
    public TacticalAIActionType ActionType;
    public string ActorUnitId = string.Empty;
    public string StableOrderKey = string.Empty;
    public BattleActionUse Use;
    public BattleAction Action;
    public BattleActionResult Result;
    public TacticalAIActionIntent LegacyIntent;
    public SkillUse SubmittedSkillUse;
    public SkillCast ValidatedSkillCast;
    public SkillResult PreviewResult;

    public BattleActionKind ActionKind
    {
        get { return Action != null ? Action.ActionKind : ToBattleActionKind(ActionType); }
    }

    public static TacticalAIPlannedAction FromLegacyIntent(TacticalAIActionIntent intent)
    {
        return new TacticalAIPlannedAction
        {
            ActionType = intent != null ? intent.ActionType : TacticalAIActionType.Wait,
            ActorUnitId = intent != null ? intent.ActorUnitId ?? string.Empty : string.Empty,
            StableOrderKey = intent != null ? intent.StableOrderKey ?? string.Empty : string.Empty,
            LegacyIntent = intent,
            Use = ToUse(intent)
        };
    }

    public static TacticalAIPlannedAction FromCandidateIntent(TacticalAIActionIntent intent)
    {
        if (intent == null)
        {
            return null;
        }

        if (intent.ActionType != TacticalAIActionType.Skill)
        {
            return FromLegacyIntent(intent);
        }

        SkillCast cast = intent.ValidatedSkillCast != null ? intent.ValidatedSkillCast.Clone() : null;
        SkillUse use = cast != null
            ? new SkillUse(cast.ActorUnitId, cast.SkillId, cast.SelectedHexes)
            : new SkillUse(intent.ActorUnitId, intent.SkillId, new HexCoord[0]);

        return FromSkill(intent.ActorUnitId, intent.StableOrderKey, use, cast, intent.PreviewResult);
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
            Result = result,
            ValidatedSkillCast = action.SkillCast != null ? action.SkillCast.Clone() : null,
            PreviewResult = ConvertToSkillResultIfAvailable(action, result)
        };

        if (action.SkillCast != null)
        {
            planned.SubmittedSkillUse = new SkillUse(action.SkillCast.ActorUnitId, action.SkillCast.SkillId, action.SkillCast.SelectedHexes);
        }

        return planned;
    }

    public static TacticalAIPlannedAction FromSkill(
        string actorUnitId,
        string stableOrderKey,
        SkillUse submittedSkillUse,
        SkillCast validatedSkillCast,
        SkillResult previewResult)
    {
        return new TacticalAIPlannedAction
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = actorUnitId ?? string.Empty,
            StableOrderKey = stableOrderKey ?? string.Empty,
            Use = new BattleActionUse
            {
                ActorUnitId = actorUnitId ?? string.Empty,
                ActionKind = BattleActionKind.Skill,
                SkillId = validatedSkillCast != null ? validatedSkillCast.SkillId : string.Empty,
                SelectedHexes = validatedSkillCast != null
                    ? BattleActionModelUtility.CopyHexes(validatedSkillCast.SelectedHexes)
                    : new System.Collections.Generic.List<HexCoord>()
            },
            SubmittedSkillUse = submittedSkillUse,
            ValidatedSkillCast = validatedSkillCast != null ? validatedSkillCast.Clone() : null,
            PreviewResult = previewResult
        };
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

    static BattleActionUse ToUse(TacticalAIActionIntent intent)
    {
        if (intent == null)
        {
            return null;
        }

        BattleActionUse use = new BattleActionUse
        {
            ActorUnitId = intent.ActorUnitId ?? string.Empty,
            ActionKind = ToBattleActionKind(intent.ActionType),
            TargetUnitId = intent.TargetUnitId ?? string.Empty,
            SkillSlot = intent.SkillSlot,
            SkillId = intent.SkillId ?? string.Empty
        };

        if (intent.DestinationHex != null)
        {
            use.SelectedHexes.Add(new HexCoord(intent.DestinationHex.C, intent.DestinationHex.R));
        }

        if (intent.TargetHex != null)
        {
            use.SelectedHexes.Add(new HexCoord(intent.TargetHex.C, intent.TargetHex.R));
        }

        return use;
    }

    static SkillResult ConvertToSkillResultIfAvailable(BattleAction action, BattleActionResult result)
    {
        if (action == null || action.SkillCast == null)
        {
            return null;
        }

        return SkillRules.Preview(action.SkillCast, null);
    }
}
