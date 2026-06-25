using System;
using System.Collections.Generic;
using UnityEngine;

public enum TacticalAIExecutionStatus
{
    Started,
    Busy,
    InvalidContext,
    NoLegalAction
}

[Serializable]
public sealed class TacticalAIExecutionAttempt
{
    public TacticalAIPlannedAction Action;
    public TacticalAIActionIntent Intent;
    public bool Started;
    public string FailureReason = string.Empty;
}

[Serializable]
public sealed class TacticalAIExecutionResult
{
    public TacticalAIExecutionStatus Status;
    public TacticalAIPlannedAction ExecutedAction;
    public TacticalAIActionIntent ExecutedIntent;
    public BattleSnapshot LiveSnapshot;
    public string Message = string.Empty;
    public List<TacticalAIExecutionAttempt> Attempts = new List<TacticalAIExecutionAttempt>();
}

public interface ITacticalAISkillActionExecutor
{
    bool TryExecuteSkillAction(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason);
}

public sealed class TacticalAIExecutionRuntimeContext
{
    public TacticalAIExecutionRuntimeContext(
        HexMap hexMap,
        MouseControler mouseControler,
        TurnManager turnManager,
        BattleActionLifecycle battleActionLifecycle)
    {
        HexMap = hexMap;
        MouseControler = mouseControler;
        TurnManager = turnManager;
        BattleActionLifecycle = battleActionLifecycle;
    }

    public HexMap HexMap { get; private set; }
    public MouseControler MouseControler { get; private set; }
    public TurnManager TurnManager { get; private set; }
    public BattleActionLifecycle BattleActionLifecycle { get; set; }

    public bool HasRequiredReferences
    {
        get { return HexMap != null && MouseControler != null && TurnManager != null; }
    }
}

public static class TacticalAIExecutionFallbackPlanner
{
    public static List<TacticalAIActionIntent> BuildAttemptQueue(
        IEnumerable<TacticalAIActionIntent> plannedIntents,
        BattleSnapshot liveSnapshot,
        TacticalAICandidateGenerationOptions options = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null,
        int maxFallbackCandidates = -1,
        TacticalAIResolvedProfile profile = null)
    {
        List<TacticalAIActionIntent> queue = new List<TacticalAIActionIntent>();
        HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);

        AddPlannedIntents(queue, seenKeys, plannedIntents, maxFallbackCandidates);
        if (queue.Count > 0)
        {
            return queue;
        }

        if (liveSnapshot == null)
        {
            return queue;
        }

        TacticalAIResolvedProfile resolvedProfile = profile ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        if (options != null)
        {
            resolvedProfile = resolvedProfile.Clone();
            resolvedProfile.MaxCandidatesPerActionType = options.MaxCandidatesPerActionType;
            resolvedProfile.MaxSkillCandidates = options.MaxSkillCandidates;
            resolvedProfile.MaxMoveCandidates = options.MaxMoveCandidates;
            resolvedProfile.MaxAttackCandidates = options.MaxAttackCandidates;
        }

        List<TacticalAIActionIntent> freshCandidates = TacticalAISearchCandidateExpander.BuildSearchCandidates(
            liveSnapshot,
            resolvedProfile,
            skillMetadataProvider);

        TacticalAIActionIntent defendFallback = null;
        TacticalAIActionIntent waitFallback = null;
        for (int i = 0; i < freshCandidates.Count; i++)
        {
            TacticalAIActionIntent candidate = freshCandidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.ActionType == TacticalAIActionType.Defend)
            {
                if (defendFallback == null)
                {
                    defendFallback = candidate;
                }
                continue;
            }

            if (candidate.ActionType == TacticalAIActionType.Wait)
            {
                if (waitFallback == null)
                {
                    waitFallback = candidate;
                }
                continue;
            }

            AddUnique(queue, seenKeys, candidate);
        }

        AddUnique(queue, seenKeys, defendFallback);
        AddUnique(queue, seenKeys, waitFallback);
        return queue;
    }

    public static List<TacticalAIPlannedAction> BuildActionAttemptQueue(
        IEnumerable<TacticalAIPlannedAction> plannedActions,
        BattleSnapshot liveSnapshot,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null,
        int maxFallbackCandidates = -1)
    {
        List<TacticalAIPlannedAction> queue = new List<TacticalAIPlannedAction>();
        HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);
        AddPlannedActions(queue, seenKeys, plannedActions, maxFallbackCandidates);
        if (queue.Count > 0)
        {
            return queue;
        }

        if (liveSnapshot == null)
        {
            return queue;
        }

        List<TacticalAIActionIntent> freshCandidates = TacticalAISearchCandidateExpander.BuildSearchCandidates(
            liveSnapshot,
            profile,
            skillMetadataProvider);
        for (int i = 0; i < freshCandidates.Count; i++)
        {
            AddUniqueAction(queue, seenKeys, TacticalAIPlannedAction.FromCandidateIntent(freshCandidates[i]));
        }

        return queue;
    }

    static void AddPlannedActions(
        List<TacticalAIPlannedAction> queue,
        HashSet<string> seenKeys,
        IEnumerable<TacticalAIPlannedAction> plannedActions,
        int maxFallbackCandidates)
    {
        if (plannedActions == null)
        {
            return;
        }

        int maxAttempts = maxFallbackCandidates >= 0
            ? Math.Max(1, maxFallbackCandidates + 1)
            : int.MaxValue;
        int addedCount = 0;
        foreach (TacticalAIPlannedAction action in plannedActions)
        {
            if (addedCount >= maxAttempts)
            {
                break;
            }

            if (AddUniqueAction(queue, seenKeys, action))
            {
                addedCount++;
            }
        }
    }

    static bool AddUniqueAction(
        List<TacticalAIPlannedAction> queue,
        HashSet<string> seenKeys,
        TacticalAIPlannedAction action)
    {
        if (queue == null || seenKeys == null || action == null)
        {
            return false;
        }

        string key = string.IsNullOrEmpty(action.StableOrderKey) ? action.ActionType + "|" + action.ActorUnitId : action.StableOrderKey;
        if (seenKeys.Add(key) == false)
        {
            return false;
        }

        queue.Add(action);
        return true;
    }

    static void AddPlannedIntents(
        List<TacticalAIActionIntent> queue,
        HashSet<string> seenKeys,
        IEnumerable<TacticalAIActionIntent> plannedIntents,
        int maxFallbackCandidates)
    {
        if (plannedIntents == null)
        {
            return;
        }

        int maxAttempts = maxFallbackCandidates >= 0
            ? Math.Max(1, maxFallbackCandidates + 1)
            : int.MaxValue;

        int addedCount = 0;
        foreach (TacticalAIActionIntent intent in plannedIntents)
        {
            if (intent == null)
            {
                continue;
            }

            if (addedCount >= maxAttempts)
            {
                break;
            }

            if (AddUnique(queue, seenKeys, intent))
            {
                addedCount++;
            }
        }
    }

    static bool AddUnique(
        List<TacticalAIActionIntent> queue,
        HashSet<string> seenKeys,
        TacticalAIActionIntent intent)
    {
        if (queue == null || seenKeys == null || intent == null)
        {
            return false;
        }

        string key = BuildIntentKey(intent);
        if (seenKeys.Add(key) == false)
        {
            return false;
        }

        queue.Add(intent);
        return true;
    }

    static string BuildIntentKey(TacticalAIActionIntent intent)
    {
        if (intent == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(intent.StableOrderKey) == false)
        {
            return intent.StableOrderKey;
        }

        return intent.ActionType + "|" +
            (intent.ActorUnitId ?? string.Empty) + "|" +
            GetCoordinateKey(intent.SourceHex) + "|" +
            GetCoordinateKey(intent.DestinationHex) + "|" +
            (intent.TargetUnitId ?? string.Empty) + "|" +
            GetCoordinateKey(intent.TargetHex) + "|" +
            intent.SkillSlot + "|" +
            (intent.SkillId ?? string.Empty);
    }

    static string GetCoordinateKey(TacticalAIHexCoordinate coordinate)
    {
        return coordinate == null ? string.Empty : coordinate.C + "," + coordinate.R;
    }
}

public sealed class TacticalAIExecutionBridge
{
    readonly TacticalAIExecutionRuntimeContext runtimeContext;
    readonly TacticalAIProfile assignedProfile;
    readonly ITacticalAISkillActionExecutor skillActionExecutor;
    readonly ITacticalAISkillMetadataProvider skillMetadataProvider;

    public TacticalAIExecutionBridge(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillActionExecutor skillActionExecutor = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        this.runtimeContext = runtimeContext ?? new TacticalAIExecutionRuntimeContext(null, null, null, null);
        this.assignedProfile = assignedProfile;
        this.skillActionExecutor = skillActionExecutor ?? TacticalAISkillRulesExecutor.Instance;
        this.skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
    }

    public static TacticalAIExecutionBridge CreateFromScene(
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillActionExecutor skillActionExecutor = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        return new TacticalAIExecutionBridge(
            new TacticalAIExecutionRuntimeContext(
                UnityEngine.Object.FindObjectOfType<HexMap>(),
                UnityEngine.Object.FindObjectOfType<MouseControler>(),
                UnityEngine.Object.FindObjectOfType<TurnManager>(),
                BattleActionLifecycle.Instance),
            assignedProfile,
            skillActionExecutor,
            skillMetadataProvider);
    }

    public TacticalAIExecutionResult TryExecuteOrderedIntents(
        IEnumerable<TacticalAIActionIntent> orderedIntents,
        BattleSnapshot plannedSnapshot = null)
    {
        List<TacticalAIPlannedAction> actions = new List<TacticalAIPlannedAction>();
        if (orderedIntents != null)
        {
            foreach (TacticalAIActionIntent intent in orderedIntents)
            {
                TacticalAIPlannedAction action = TacticalAIPlannedAction.FromCandidateIntent(intent);
                if (action != null)
                {
                    actions.Add(action);
                }
            }
        }

        return TryExecuteOrderedActions(actions, plannedSnapshot);
    }

    public TacticalAIExecutionResult TryExecuteOrderedActions(
        IEnumerable<TacticalAIPlannedAction> orderedActions,
        BattleSnapshot plannedSnapshot = null)
    {
        TacticalAIExecutionResult result = new TacticalAIExecutionResult();
        if (runtimeContext.HasRequiredReferences == false)
        {
            result.Status = TacticalAIExecutionStatus.InvalidContext;
            result.Message = "Tactical AI execution bridge is missing live battle references.";
            return result;
        }

        runtimeContext.BattleActionLifecycle = runtimeContext.BattleActionLifecycle ?? BattleActionLifecycle.EnsureInstance();
        if (runtimeContext.BattleActionLifecycle != null && runtimeContext.BattleActionLifecycle.IsBusy)
        {
            result.Status = TacticalAIExecutionStatus.Busy;
            result.Message = "Battle action lifecycle is currently busy.";
            return result;
        }

        BattleSnapshot liveSnapshot = BattleSnapshotLiveAdapter.BuildSnapshot(
            runtimeContext.HexMap,
            runtimeContext.MouseControler,
            runtimeContext.TurnManager,
            runtimeContext.BattleActionLifecycle);

        result.LiveSnapshot = liveSnapshot;
        if (liveSnapshot == null)
        {
            result.Status = TacticalAIExecutionStatus.InvalidContext;
            result.Message = "Could not capture a live battle snapshot before execution.";
            return result;
        }

        TacticalAIResolvedProfile resolvedProfile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(assignedProfile);
        List<TacticalAIPlannedAction> attempts = TacticalAIExecutionFallbackPlanner.BuildActionAttemptQueue(
            orderedActions,
            liveSnapshot,
            resolvedProfile,
            skillMetadataProvider,
            resolvedProfile.MaxFallbackCandidates);

        for (int i = 0; i < attempts.Count; i++)
        {
            TacticalAIPlannedAction action = attempts[i];
            TacticalAIExecutionAttempt attempt = new TacticalAIExecutionAttempt
            {
                Action = action,
                Intent = action != null ? action.LegacyIntent : null
            };

            if (runtimeContext.BattleActionLifecycle != null && runtimeContext.BattleActionLifecycle.IsBusy)
            {
                attempt.FailureReason = "Battle action lifecycle became busy before executing the next AI attempt.";
                result.Attempts.Add(attempt);
                break;
            }

            TacticalAIRevalidatedIntent revalidatedIntent;
            if (TryRevalidateAction(
                    action,
                    liveSnapshot,
                    plannedSnapshot,
                    out revalidatedIntent,
                    out attempt.FailureReason) == false)
            {
                LogRejectedAttempt(action, attempt.FailureReason, "revalidation");
                result.Attempts.Add(attempt);
                continue;
            }

            if (TryExecuteLiveIntent(action, revalidatedIntent, out attempt.FailureReason))
            {
                attempt.Started = true;
                result.Attempts.Add(attempt);
                result.ExecutedAction = action;
                result.ExecutedIntent = action != null ? action.LegacyIntent : null;
                result.Status = TacticalAIExecutionStatus.Started;
                result.Message = "Started " + action.ActionType + " through the live battle lifecycle.";
                return result;
            }

            LogRejectedAttempt(action, attempt.FailureReason, "execution");
            result.Attempts.Add(attempt);
        }

        result.Status = TacticalAIExecutionStatus.NoLegalAction;
        result.Message = "No tactical AI intent could be revalidated and started in live battle state.";
        Debug.LogError("[TacticalAIExecutionBridge] " + result.Message + " Attempts: " + result.Attempts.Count);
        return result;
    }

    static void LogRejectedAttempt(TacticalAIPlannedAction action, string failureReason, string phase)
    {
        if (action == null)
        {
            return;
        }

        Debug.LogWarning(
            "[TacticalAIExecutionBridge] Rejected action phase=" + phase +
            " type=" + action.ActionType +
            " actor=" + (action.ActorUnitId ?? string.Empty) +
            " skill=" + (action.ValidatedSkillCast != null ? action.ValidatedSkillCast.SkillId : string.Empty) +
            " reason=" + (failureReason ?? string.Empty));
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

    bool TryRevalidateAction(
        TacticalAIPlannedAction action,
        BattleSnapshot liveSnapshot,
        BattleSnapshot plannedSnapshot,
        out TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        revalidatedIntent = null;
        failureReason = string.Empty;
        if (action == null)
        {
            failureReason = "Planned action was null.";
            return false;
        }

        if (action.ActionType != TacticalAIActionType.Skill)
        {
            if (action.Use != null)
            {
                BattleActionValidationResult actionValidation = BattleActionRules.Validate(action.Use, liveSnapshot, skillMetadataProvider);
                if (actionValidation.IsValid == false || actionValidation.Action == null)
                {
                    failureReason = "BattleActionRules rejected planned action: " + actionValidation.RejectReason;
                    return false;
                }

                revalidatedIntent = ToRevalidatedIntent(actionValidation.Action, liveSnapshot);
                if (revalidatedIntent == null)
                {
                    failureReason = "BattleActionRules validated action but live revalidation could not resolve actor/target.";
                    return false;
                }

                revalidatedIntent.Use = action.Use.Clone();
                revalidatedIntent.Result = BattleActionRules.Apply(liveSnapshot, actionValidation.Action);
                return true;
            }

            return TacticalAIIntentRevalidator.TryRevalidate(
                action.LegacyIntent,
                liveSnapshot,
                plannedSnapshot,
                out revalidatedIntent,
                out failureReason,
                skillMetadataProvider);
        }

        if (liveSnapshot == null)
        {
            failureReason = "Live snapshot was unavailable.";
            return false;
        }

        SkillCast plannedCast = action.ValidatedSkillCast;
        SkillUse submittedUse = action.SubmittedSkillUse;
        if (plannedCast == null || submittedUse == null)
        {
            failureReason = "Skill planned action did not carry SkillUse and SkillCast.";
            return false;
        }

        BattleUnitSnapshot liveActor = TacticalAISnapshotQuery.FindUnit(liveSnapshot, plannedCast.ActorUnitId);
        if (liveActor == null || liveActor.IsAlive == false || liveActor.Amount <= 0)
        {
            failureReason = "Skill actor is no longer alive/actionable.";
            return false;
        }

        if (string.Equals(liveSnapshot.ActiveUnitId, plannedCast.ActorUnitId, StringComparison.Ordinal) == false)
        {
            failureReason = "Skill actor is no longer the live active unit.";
            return false;
        }

        SkillDefinitionSpec spec = ResolveSkillSpec(plannedCast.SkillId);
        if (spec == null)
        {
            failureReason = "Skill planned action has no copied or live skill spec.";
            return false;
        }

        SkillContext context = SkillContext.Create(liveSnapshot, plannedCast.ActorUnitId, spec, ResolveSkillSlot(liveActor, plannedCast.SkillId));
        SkillValidationResult validation = SkillRules.Validate(submittedUse, context);
        if (validation.IsValid == false || validation.Cast == null)
        {
            failureReason = "SkillRules rejected planned action: " + validation.RejectReason;
            return false;
        }

        revalidatedIntent = new TacticalAIRevalidatedIntent
        {
            ActionType = TacticalAIActionType.Skill,
            Use = submittedUse != null
                ? new BattleActionUse
                {
                    ActorUnitId = submittedUse.ActorUnitId,
                    ActionKind = BattleActionKind.Skill,
                    SkillSlot = context.SkillSlot,
                    SkillId = submittedUse.SkillId,
                    SelectedHexes = BattleActionModelUtility.CopyHexes(submittedUse.SelectedHexes)
                }
                : null,
            Action = action.Action,
            Result = action.Result,
            Actor = liveActor,
            SkillSlot = context.SkillSlot,
            SkillId = plannedCast.SkillId,
            ValidatedSkillCast = validation.Cast.Clone(),
            Target = TacticalAISnapshotQuery.FindUnit(liveSnapshot, validation.Cast.PrimaryTargetUnitId),
            TargetHex = ToAIHex(FirstHex(validation.Cast.SelectedHexes)),
            DestinationHex = ToAIHex(validation.Cast.DestinationHex)
        };
        return true;
    }

    static TacticalAIRevalidatedIntent ToRevalidatedIntent(BattleAction action, BattleSnapshot liveSnapshot)
    {
        if (action == null || liveSnapshot == null)
        {
            return null;
        }

        BattleUnitSnapshot actor = TacticalAISnapshotQuery.FindUnit(liveSnapshot, action.ActorUnitId);
        if (actor == null)
        {
            return null;
        }

        BattleUnitSnapshot target = string.IsNullOrEmpty(action.PrimaryTargetUnitId)
            ? null
            : TacticalAISnapshotQuery.FindUnit(liveSnapshot, action.PrimaryTargetUnitId);

        return new TacticalAIRevalidatedIntent
        {
            ActionType = TacticalAIPlannedAction.ToTacticalActionType(action.ActionKind),
            Action = action.Clone(),
            Actor = actor,
            Target = target,
            DestinationHex = ToAIHex(action.DestinationHex),
            TargetHex = ToAIHex(action.ImpactHex),
            SkillSlot = action.SkillSlot,
            SkillId = action.SkillId ?? string.Empty,
            ValidatedSkillCast = action.SkillCast != null ? action.SkillCast.Clone() : null
        };
    }

    bool TryExecuteLiveIntent(
        TacticalAIPlannedAction action,
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        failureReason = string.Empty;

        if (revalidatedIntent != null && revalidatedIntent.Action != null)
        {
            BattleActionLiveApplier applier = new BattleActionLiveApplier(runtimeContext, skillActionExecutor);
            if (applier.TryApply(revalidatedIntent, out failureReason))
            {
                return true;
            }

            if (string.IsNullOrEmpty(failureReason))
            {
                failureReason = "BattleAction live applier rejected the action.";
            }

            return false;
        }

        TosterHexUnit actor = ResolveLiveUnit(revalidatedIntent.Actor);
        if (actor == null)
        {
            failureReason = "Could not resolve the live actor object for " + revalidatedIntent.Actor.RuntimeUnitId + ".";
            return false;
        }

        switch (revalidatedIntent.ActionType)
        {
            case TacticalAIActionType.Move:
            {
                HexClass destinationHex = ResolveLiveHex(revalidatedIntent.DestinationHex);
                if (destinationHex == null)
                {
                    failureReason = "Could not resolve the live destination hex.";
                    return false;
                }

                if (runtimeContext.MouseControler.TryStartMoveAction(destinationHex, actor))
                {
                    SendAIActionChat(actor, "rusza sie.");
                    // Player movement can keep a unit open for a follow-up skill. AI V1 commits
                    // one ranked action per turn, so plain AI movement must finish that unit.
                    actor.Moved = true;
                    return true;
                }

                failureReason = "MouseControler rejected the move action during live validation.";
                return false;
            }
            case TacticalAIActionType.MoveAndAttack:
            {
                HexClass destinationHex = ResolveLiveHex(revalidatedIntent.DestinationHex);
                TosterHexUnit target = ResolveLiveUnit(revalidatedIntent.Target);
                if (destinationHex == null || target == null)
                {
                    failureReason = "Could not resolve the live move-and-attack destination or target.";
                    return false;
                }

                if (runtimeContext.MouseControler.TryStartMoveAndAttackAction(destinationHex, target, actor))
                {
                    return true;
                }

                failureReason = "MouseControler rejected the move-and-attack action during live validation.";
                return false;
            }
            case TacticalAIActionType.BasicRangedAttack:
            {
                HexClass targetHex = ResolveLiveHex(revalidatedIntent.TargetHex);
                if (targetHex == null)
                {
                    failureReason = "Could not resolve the live ranged target hex.";
                    return false;
                }

                if (runtimeContext.MouseControler.TryStartBasicRangedAttackAction(targetHex, actor))
                {
                    return true;
                }

                failureReason = "MouseControler rejected the basic ranged attack during live validation.";
                return false;
            }
            case TacticalAIActionType.Wait:
                if (runtimeContext.MouseControler.TryStartWaitAction(actor))
                {
                    return true;
                }

                failureReason = "MouseControler rejected the wait action during live validation.";
                return false;
            case TacticalAIActionType.Defend:
                if (runtimeContext.MouseControler.TryStartDefenseAction(actor))
                {
                    return true;
                }

                failureReason = "MouseControler rejected the defend action during live validation.";
                return false;
            case TacticalAIActionType.Skill:
                if (skillActionExecutor == null)
                {
                    failureReason = "Skill intents require a shared SkillRules executor and no skill executor is registered.";
                    return false;
                }

                if (skillActionExecutor.TryExecuteSkillAction(runtimeContext, revalidatedIntent, out failureReason))
                {
                    return true;
                }

                if (string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "Skill intent executor rejected the skill action.";
                }

                Debug.LogWarning(
                    "[TacticalAIExecutionBridge] Rejected skill intent actor=" + revalidatedIntent.Actor.RuntimeUnitId +
                    " skillSlot=" + revalidatedIntent.SkillSlot +
                    " skillId=" + revalidatedIntent.SkillId +
                    " reason=" + failureReason);
                return false;
            default:
                failureReason = "Unsupported tactical AI action type: " + revalidatedIntent.ActionType;
                return false;
        }
    }

    SkillDefinitionSpec ResolveSkillSpec(string skillId)
    {
        ITacticalAISkillSpecProvider specProvider = skillMetadataProvider as ITacticalAISkillSpecProvider;
        SkillDefinitionSpec spec;
        if (specProvider != null && specProvider.TryGetSkillSpec(skillId, out spec))
        {
            return spec;
        }

        SkillDefinitionAsset asset = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillId) : null;
        return SkillDefinitionSpec.FromAsset(asset);
    }

    static int ResolveSkillSlot(BattleUnitSnapshot actor, string skillId)
    {
        if (actor == null || actor.SkillIdsBySlot == null)
        {
            return -1;
        }

        for (int i = 0; i < actor.SkillIdsBySlot.Count; i++)
        {
            if (string.Equals(actor.SkillIdsBySlot[i], skillId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    static HexCoord FirstHex(List<HexCoord> hexes)
    {
        return hexes != null && hexes.Count > 0 ? hexes[0] : null;
    }

    static TacticalAIHexCoordinate ToAIHex(HexCoord hex)
    {
        return hex == null ? null : new TacticalAIHexCoordinate(hex.C, hex.R);
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

        if (string.Equals(actor.Name, snapshotUnit.UnitName, StringComparison.Ordinal) == false)
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

    static TacticalAICandidateGenerationOptions BuildCandidateOptions(TacticalAIResolvedProfile resolvedProfile)
    {
        if (resolvedProfile == null)
        {
            return TacticalAICandidateGenerationOptions.Default;
        }

        return new TacticalAICandidateGenerationOptions
        {
            MaxCandidatesPerActionType = resolvedProfile.MaxCandidatesPerActionType,
            MaxSkillCandidates = resolvedProfile.MaxSkillCandidates,
            MaxMoveCandidates = resolvedProfile.MaxMoveCandidates,
            MaxAttackCandidates = resolvedProfile.MaxAttackCandidates
        };
    }
}
