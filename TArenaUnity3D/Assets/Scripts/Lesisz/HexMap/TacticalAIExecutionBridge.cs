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
    public bool Started;
    public string FailureReason = string.Empty;
}

[Serializable]
public sealed class TacticalAIExecutionResult
{
    public TacticalAIExecutionStatus Status;
    public TacticalAIPlannedAction ExecutedAction;
    public BattleSnapshot LiveSnapshot;
    public string Message = string.Empty;
    public List<TacticalAIExecutionAttempt> Attempts = new List<TacticalAIExecutionAttempt>();
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

        List<BattleAction> freshActions = BattleActionRules.GenerateLegalActions(
            liveSnapshot,
            profile,
            skillMetadataProvider);
        for (int i = 0; i < freshActions.Count; i++)
        {
            BattleAction action = freshActions[i];
            AddUniqueAction(queue, seenKeys, TacticalAIPlannedAction.FromBattleAction(
                action,
                BattleActionRules.Apply(liveSnapshot, action)));
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

}

public sealed class TacticalAIExecutionBridge
{
    readonly TacticalAIExecutionRuntimeContext runtimeContext;
    readonly TacticalAIProfile assignedProfile;
    readonly ITacticalAISkillMetadataProvider skillMetadataProvider;

    public TacticalAIExecutionBridge(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        this.runtimeContext = runtimeContext ?? new TacticalAIExecutionRuntimeContext(null, null, null, null);
        this.assignedProfile = assignedProfile;
        this.skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
    }

    public static TacticalAIExecutionBridge CreateFromScene(
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        return new TacticalAIExecutionBridge(
            new TacticalAIExecutionRuntimeContext(
                UnityEngine.Object.FindObjectOfType<HexMap>(),
                UnityEngine.Object.FindObjectOfType<MouseControler>(),
                UnityEngine.Object.FindObjectOfType<TurnManager>(),
                BattleActionLifecycle.Instance),
            assignedProfile,
            skillMetadataProvider);
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
                Action = action
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

            if (TryExecuteLiveIntent(revalidatedIntent, out attempt.FailureReason))
            {
                attempt.Started = true;
                result.Attempts.Add(attempt);
                result.ExecutedAction = action;
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
            " skill=" + (action.Action != null ? action.Action.SkillId : string.Empty) +
            " reason=" + (failureReason ?? string.Empty));
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

            revalidatedIntent.Use = actionValidation.Action.ToUse();
            revalidatedIntent.Result = BattleActionRules.Apply(liveSnapshot, actionValidation.Action);
            return true;
        }

        failureReason = "Planned action did not carry a BattleActionUse payload.";
        return false;
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
            SkillId = action.SkillId ?? string.Empty
        };
    }

    bool TryExecuteLiveIntent(
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (revalidatedIntent == null)
        {
            failureReason = "No revalidated action was supplied for live execution.";
            return false;
        }

        if (revalidatedIntent.Action != null)
        {
            BattleActionLiveApplier applier = new BattleActionLiveApplier(runtimeContext);
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

        failureReason = "Planned action did not carry a validated BattleAction for live application.";
        return false;
    }

    static TacticalAIHexCoordinate ToAIHex(HexCoord hex)
    {
        return hex == null ? null : new TacticalAIHexCoordinate(hex.C, hex.R);
    }

}
