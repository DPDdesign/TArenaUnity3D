using System;
using System.Collections;
using UnityEngine;

public sealed class BattleActionLiveApplier
{
    readonly TacticalAIExecutionRuntimeContext runtimeContext;

    public BattleActionLiveApplier(TacticalAIExecutionRuntimeContext runtimeContext)
    {
        this.runtimeContext = runtimeContext;
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

        if (revalidatedAction.Result != null && revalidatedAction.Result.IsRejected)
        {
            failureReason = "BattleActionResult was rejected: " + revalidatedAction.Result.RejectReason;
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
                return TryApplyWait(actor, revalidatedAction, out failureReason);
            case BattleActionKind.Defend:
                return TryApplyDefend(actor, revalidatedAction, out failureReason);
            case BattleActionKind.Skill:
            case BattleActionKind.Stance:
                return TryApplySkill(revalidatedAction, out failureReason);
            default:
                failureReason = "Unsupported validated BattleAction kind: " + revalidatedAction.Action.ActionKind;
                return false;
        }
    }

    bool TryApplyMove(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        HexClass destinationHex = ResolveLiveHex(action.DestinationHex);
        if (destinationHex == null)
        {
            failureReason = "Could not resolve the live destination hex.";
            return false;
        }

        return TryRunBattleAction(
            actor,
            BattleActionLifecycleKind.Movement,
            "Move",
            () =>
            {
                actor.MovedThisTurn = true;
                if (actor.Waited || action.Action.AllowsPostMoveFollowUp == false)
                {
                    actor.Moved = true;
                }

                SendAIActionChat(actor, "rusza sie.");
            },
            () => ApplyResultSequence(actor, action),
            action.Action,
            out failureReason);
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

        return TryRunBattleAction(
            actor,
            BattleActionLifecycleKind.MoveAndAttack,
            "MoveAndAttack",
            () => actor.Moved = true,
            () => ApplyResultSequence(actor, action),
            action.Action,
            out failureReason);
    }

    bool TryApplyBasicRangedAttack(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        HexClass targetHex = ResolveLiveHex(action.TargetHex);
        if (targetHex == null)
        {
            failureReason = "Could not resolve the live ranged target hex.";
            return false;
        }

        return TryRunBattleAction(
            actor,
            BattleActionLifecycleKind.BasicRangedAttack,
            "BasicRangedAttack",
            () => actor.Moved = true,
            () => ApplyResultSequence(actor, action),
            action.Action,
            out failureReason);
    }

    bool TryApplyWait(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        return TryRunBattleAction(
            actor,
            BattleActionLifecycleKind.Wait,
            "Wait",
            () =>
            {
                SendAIActionChat(actor, "czeka.");
                actor.Waited = true;
            },
            null,
            action != null ? action.Action : null,
            out failureReason);
    }

    bool TryApplyDefend(TosterHexUnit actor, TacticalAIRevalidatedIntent action, out string failureReason)
    {
        return TryRunBattleAction(
            actor,
            BattleActionLifecycleKind.Defense,
            "Defense",
            () =>
            {
                SendAIActionChat(actor, "broni sie.");
                actor.Moved = true;
                actor.DefenceStance = true;
                actor.SpecialDef += 5;
            },
            () => DefenseAction(actor),
            action != null ? action.Action : null,
            out failureReason);
    }

    bool TryApplySkill(TacticalAIRevalidatedIntent action, out string failureReason)
    {
        if (action == null || action.Action == null || action.Action.SkillCast == null)
        {
            failureReason = "Skill BattleAction had no validated Battle Action skill payload.";
            return false;
        }

        TosterHexUnit actor = ResolveLiveUnit(action.Actor);
        if (actor == null)
        {
            failureReason = "Could not resolve live actor for skill BattleAction application.";
            return false;
        }

        SkillCast cast = action.Action.SkillCast;
        BattleActionSkillResultRuntime skillRuntime = new BattleActionSkillResultRuntime(runtimeContext, action.SkillSlot);
        return TryRunBattleAction(
            actor,
            action.Action.ActionKind == BattleActionKind.Stance ? BattleActionLifecycleKind.Skill : BattleActionLifecycleKind.Skill,
            cast.SkillId,
            null,
            () => skillRuntime.ApplySequence(cast, action.Result),
            action.Action,
            out failureReason);
    }

    bool TryRunBattleAction(
        TosterHexUnit actor,
        BattleActionLifecycleKind kind,
        string label,
        Action commit,
        Func<IEnumerator> actionBody,
        BattleAction action,
        out string failureReason)
    {
        runtimeContext.BattleActionLifecycle = runtimeContext.BattleActionLifecycle ?? BattleActionLifecycle.EnsureInstance();
        if (runtimeContext.BattleActionLifecycle == null)
        {
            failureReason = "BattleActionLifecycle was unavailable for live BattleAction application.";
            return false;
        }

        bool started = runtimeContext.BattleActionLifecycle.TryRunAction(
            actor,
            kind,
            label,
            commit,
            actionBody,
            runtimeContext.MouseControler != null ? runtimeContext.MouseControler.CompleteBattleActionModeCleanup : null);
        if (started == false)
        {
            failureReason = "BattleActionLifecycle rejected live BattleAction application.";
            return false;
        }

        runtimeContext.BattleActionLifecycle.MarkActionCommitted(
            action != null ? action.ActionIndex : runtimeContext.BattleActionLifecycle.NextActionIndex);
        failureReason = string.Empty;
        return true;
    }

    IEnumerator ApplyResultSequence(TosterHexUnit actor, TacticalAIRevalidatedIntent action)
    {
        BattleActionResult result = action.Result;
        if (result == null || result.Events == null)
        {
            yield break;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            BattleActionResultEvent resultEvent = result.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            switch (resultEvent.EventType)
            {
                case BattleActionResultEventType.UnitMoved:
                    yield return ApplyMoveSequence(actor, resultEvent);
                    break;
                case BattleActionResultEventType.DamageApplied:
                    yield return ApplyDamageSequence(actor, action, resultEvent);
                    break;
            }
        }
    }

    IEnumerator ApplyMoveSequence(TosterHexUnit actor, BattleActionResultEvent resultEvent)
    {
        HexClass destination = ResolveLiveHex(resultEvent.Hex);
        if (actor == null || destination == null)
        {
            yield break;
        }

        if (actor.Hex == destination)
        {
            yield break;
        }

        if (runtimeContext.HexMap == null)
        {
            actor.TeleportToHex(destination);
            yield break;
        }

        bool previousMoveFlag = actor.move;
        actor.move = true;
        try
        {
            actor.Pathing_func(destination, false);
            yield return runtimeContext.HexMap.DoUnitMoves(actor);
        }
        finally
        {
            actor.move = previousMoveFlag;
        }
    }

    IEnumerator ApplyDamageSequence(TosterHexUnit actor, TacticalAIRevalidatedIntent action, BattleActionResultEvent resultEvent)
    {
        TosterHexUnit damageActor = ResolveDamageActor(actor, action, resultEvent);
        TosterHexUnit target = ResolveDamageTarget(actor, action, resultEvent);
        if (damageActor == null || target == null)
        {
            Debug.Log("[DEBUG-HITFLOW] ApplyDamageSequence skipped null actorOrTarget action=" +
                (action != null && action.Action != null ? action.Action.ActionKind.ToString() : "<null>") +
                " eventActor=" + (resultEvent != null ? resultEvent.ActorUnitId : "<null>") +
                " eventTarget=" + (resultEvent != null ? resultEvent.TargetUnitId : "<null>") +
                " damageActor=" + (damageActor != null ? damageActor.Name : "<null>") +
                " target=" + (target != null ? target.Name : "<null>"));
            yield break;
        }

        int damage = Math.Max(0, resultEvent.Amount);
        bool isCounterattack = IsCounterattackDamageEvent(action, resultEvent);
        if (isCounterattack)
        {
            damageActor.CounterAttackBools();
        }

        if (resultEvent.ConsumesActorPureDamage)
        {
            damageActor.SpecialPUREDMG = 0;
        }

        Debug.Log("[DEBUG-HITFLOW] ApplyDamageSequence action=" +
            (action != null && action.Action != null ? action.Action.ActionKind.ToString() : "<null>") +
            " isCounter=" + isCounterattack +
            " damage=" + damage +
            " damageActor=" + damageActor.Name +
            " target=" + target.Name +
            " targetView=" + (target.tosterView != null ? target.tosterView.name : "<null>") +
            " targetDead=" + target.isDead +
            " targetAmount=" + target.Amount);

        SendDamageChat(damageActor, damage, target);

        if (action.Action != null && action.Action.ActionKind == BattleActionKind.BasicRangedAttack)
        {
            FrontendResultReveal reveal = target.DealMePUREForFrontendReveal(
                damage,
                damageActor,
                FrontendResultRevealSource.BasicAttack);
            SkillPresentationManager.PlayBasicRangedAttack(damageActor, target, reveal);
            yield break;
        }

        yield return target.PlayBasicAttackRevealSequence(
            damageActor,
            damage,
            isCounterattack ? FrontendResultRevealSource.Counterattack : FrontendResultRevealSource.BasicAttack);
    }

    TosterHexUnit ResolveDamageActor(TosterHexUnit actionActor, TacticalAIRevalidatedIntent action, BattleActionResultEvent resultEvent)
    {
        if (resultEvent == null || string.IsNullOrEmpty(resultEvent.ActorUnitId))
        {
            return actionActor;
        }

        if (action != null &&
            action.Actor != null &&
            string.Equals(resultEvent.ActorUnitId, action.Actor.RuntimeUnitId, System.StringComparison.Ordinal))
        {
            return actionActor;
        }

        if (action != null &&
            action.Target != null &&
            string.Equals(resultEvent.ActorUnitId, action.Target.RuntimeUnitId, System.StringComparison.Ordinal))
        {
            return ResolveLiveUnit(action.Target, IsCounterattackDamageEvent(action, resultEvent));
        }

        return null;
    }

    TosterHexUnit ResolveDamageTarget(TosterHexUnit actionActor, TacticalAIRevalidatedIntent action, BattleActionResultEvent resultEvent)
    {
        if (resultEvent == null || string.IsNullOrEmpty(resultEvent.TargetUnitId))
        {
            return ResolveLiveUnit(action != null ? action.Target : null);
        }

        if (action != null &&
            action.Actor != null &&
            string.Equals(resultEvent.TargetUnitId, action.Actor.RuntimeUnitId, System.StringComparison.Ordinal))
        {
            return actionActor;
        }

        if (action != null &&
            action.Target != null &&
            string.Equals(resultEvent.TargetUnitId, action.Target.RuntimeUnitId, System.StringComparison.Ordinal))
        {
            return ResolveLiveUnit(action.Target);
        }

        return null;
    }

    static bool IsCounterattackDamageEvent(TacticalAIRevalidatedIntent action, BattleActionResultEvent resultEvent)
    {
        if (action == null || action.Action == null || resultEvent == null)
        {
            return false;
        }

        bool isMeleeAction = action.Action.ActionKind == BattleActionKind.MoveAndAttack ||
            action.Action.ActionKind == BattleActionKind.BasicMeleeAttack;
        return isMeleeAction &&
            string.Equals(resultEvent.TargetUnitId, action.Action.ActorUnitId, System.StringComparison.Ordinal) &&
            string.Equals(resultEvent.ActorUnitId, action.Action.ActorUnitId, System.StringComparison.Ordinal) == false;
    }

    IEnumerator DefenseAction(TosterHexUnit actor)
    {
        if (actor != null && actor.tosterView != null)
        {
            yield return actor.tosterView.PlayAnimatorStateAndWaitForDefault("defense", 1.25f);
        }
    }

    TosterHexUnit ResolveLiveUnit(BattleUnitSnapshot snapshotUnit, bool allowDead = false)
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

        if (allowDead == false && (actor.isDead || actor.Amount <= 0))
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

    HexClass ResolveLiveHex(HexCoord coordinate)
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

    static void SendDamageChat(TosterHexUnit actor, int damage, TosterHexUnit target)
    {
        if (actor == null || target == null || Chat.chat == null)
        {
            return;
        }

        actor.TextToSend = actor.Name + " zadaje " + damage + " obrazen jednostce " + target.Name;
        Chat.chat.SendUnitActionMessage(actor, "zadaje " + damage + " obrazen jednostce " + target.Name);
    }
}
