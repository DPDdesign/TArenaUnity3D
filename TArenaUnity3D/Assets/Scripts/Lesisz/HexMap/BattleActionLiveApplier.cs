using UnityEngine;

public sealed class BattleActionLiveApplier
{
    readonly TacticalAIExecutionRuntimeContext runtimeContext;
    readonly ITacticalAISkillActionExecutor skillActionExecutor;

    public BattleActionLiveApplier(
        TacticalAIExecutionRuntimeContext runtimeContext,
        ITacticalAISkillActionExecutor skillActionExecutor)
    {
        this.runtimeContext = runtimeContext;
        this.skillActionExecutor = skillActionExecutor;
    }

    public bool TryApply(TacticalAIRevalidatedIntent revalidatedAction, out string failureReason)
    {
        failureReason = string.Empty;
        if (revalidatedAction == null || revalidatedAction.Action == null)
        {
            failureReason = "No validated BattleAction was supplied for live application.";
            return false;
        }

        TosterHexUnit actor = ResolveLiveUnit(revalidatedAction.Actor);
        if (actor == null)
        {
            failureReason = "Could not resolve the live actor object for " +
                (revalidatedAction.Actor != null ? revalidatedAction.Actor.RuntimeUnitId : string.Empty) + ".";
            return false;
        }

        switch (revalidatedAction.Action.ActionKind)
        {
            case BattleActionKind.Move:
                return TryApplyMove(actor, revalidatedAction, out failureReason);
            case BattleActionKind.MoveAndAttack:
            case BattleActionKind.BasicMeleeAttack:
                return TryApplyMoveAndAttack(actor, revalidatedAction, out failureReason);
            case BattleActionKind.BasicRangedAttack:
                return TryApplyBasicRangedAttack(actor, revalidatedAction, out failureReason);
            case BattleActionKind.Wait:
                return TryApplyWait(actor, out failureReason);
            case BattleActionKind.Defend:
                return TryApplyDefend(actor, out failureReason);
            case BattleActionKind.Skill:
            case BattleActionKind.Stance:
                return TryApplySkill(revalidatedAction, out failureReason);
            default:
                failureReason = "Unsupported validated BattleAction kind: " + revalidatedAction.Action.ActionKind;
                return false;
        }
    }

    // TODO_LEGACY_REVIEW: Non-skill commits still delegate to MouseControler adapters after BattleActionRules revalidation.
    bool TryApplyMove(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        HexClass destinationHex = ResolveLiveHex(action.DestinationHex);
        if (destinationHex == null)
        {
            failureReason = "Could not resolve the live destination hex.";
            return false;
        }

        if (runtimeContext.MouseControler.TryStartMoveAction(destinationHex, actor))
        {
            SendAIActionChat(actor, "rusza sie.");
            actor.Moved = true;
            failureReason = string.Empty;
            return true;
        }

        failureReason = "Live movement applier rejected the move action.";
        return false;
    }

    bool TryApplyMoveAndAttack(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        HexClass destinationHex = ResolveLiveHex(action.DestinationHex);
        TosterHexUnit target = ResolveLiveUnit(action.Target);
        if (destinationHex == null || target == null)
        {
            failureReason = "Could not resolve the live move-and-attack destination or target.";
            return false;
        }

        if (runtimeContext.MouseControler.TryStartMoveAndAttackAction(destinationHex, target, actor))
        {
            failureReason = string.Empty;
            return true;
        }

        failureReason = "Live movement applier rejected the move-and-attack action.";
        return false;
    }

    bool TryApplyBasicRangedAttack(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        HexClass targetHex = ResolveLiveHex(action.TargetHex);
        if (targetHex == null)
        {
            failureReason = "Could not resolve the live ranged target hex.";
            return false;
        }

        if (runtimeContext.MouseControler.TryStartBasicRangedAttackAction(targetHex, actor))
        {
            failureReason = string.Empty;
            return true;
        }

        failureReason = "Live movement applier rejected the basic ranged attack.";
        return false;
    }

    bool TryApplyWait(TosterHexUnit actor, out string failureReason)
    {
        if (runtimeContext.MouseControler.TryStartWaitAction(actor))
        {
            failureReason = string.Empty;
            return true;
        }

        failureReason = "Live movement applier rejected the wait action.";
        return false;
    }

    bool TryApplyDefend(TosterHexUnit actor, out string failureReason)
    {
        if (runtimeContext.MouseControler.TryStartDefenseAction(actor))
        {
            failureReason = string.Empty;
            return true;
        }

        failureReason = "Live movement applier rejected the defend action.";
        return false;
    }

    bool TryApplySkill(TacticalAIRevalidatedIntent action, out string failureReason)
    {
        if (skillActionExecutor == null)
        {
            failureReason = "Skill BattleActions require a shared SkillRules executor and no skill executor is registered.";
            return false;
        }

        return skillActionExecutor.TryExecuteSkillAction(runtimeContext, action, out failureReason);
    }

    TosterHexUnit ResolveLiveUnit(BattleUnitSnapshot snapshotUnit)
    {
        if (snapshotUnit == null ||
            runtimeContext.HexMap == null ||
            runtimeContext.HexMap.Teams == null ||
            snapshotUnit.TeamIndex < 0 ||
            snapshotUnit.TeamIndex >= runtimeContext.HexMap.Teams.Count)
        {
            return null;
        }

        TeamClass team = runtimeContext.HexMap.Teams[snapshotUnit.TeamIndex];
        if (team == null || team.Tosters == null ||
            snapshotUnit.RosterIndexWithinTeam < 0 ||
            snapshotUnit.RosterIndexWithinTeam >= team.Tosters.Count)
        {
            return null;
        }

        TosterHexUnit actor = team.Tosters[snapshotUnit.RosterIndexWithinTeam];
        if (actor == null)
        {
            return null;
        }

        if (string.Equals(actor.Name, snapshotUnit.UnitName, System.StringComparison.Ordinal) == false)
        {
            return null;
        }

        HexClass actorHex = actor.Hex;
        int liveC = actorHex != null ? actorHex.C : actor.C;
        int liveR = actorHex != null ? actorHex.R : actor.R;
        if (liveC != snapshotUnit.C || liveR != snapshotUnit.R)
        {
            return null;
        }

        if (actor.isDead || actor.Amount <= 0)
        {
            return null;
        }

        return actor;
    }

    HexClass ResolveLiveHex(TacticalAIHexCoordinate coordinate)
    {
        if (coordinate == null || runtimeContext.HexMap == null)
        {
            return null;
        }

        return runtimeContext.HexMap.GetHexAt(coordinate.C, coordinate.R);
    }

    static void SendAIActionChat(TosterHexUnit actor, string actionText)
    {
        if (actor == null || Chat.chat == null)
        {
            return;
        }

        actor.TextToSend = actor.Name + " " + actionText;
        Chat.chat.SendUnitActionMessage(actor, actionText);
    }
}
