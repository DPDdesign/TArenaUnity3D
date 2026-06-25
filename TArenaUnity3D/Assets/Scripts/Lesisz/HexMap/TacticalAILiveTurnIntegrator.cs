using System;
using System.Collections.Generic;
using UnityEngine;

public enum TacticalAILiveTurnStatus
{
    Started,
    FallbackToLegacy
}

[Serializable]
public sealed class TacticalAILiveTurnIntegrationResult
{
    public TacticalAILiveTurnStatus Status = TacticalAILiveTurnStatus.FallbackToLegacy;
    public string ActorUnitId = string.Empty;
    public string FallbackReason = string.Empty;
    public BattleSnapshot Snapshot;
    public TacticalAISearchPlan Plan;
    public TacticalAIExecutionResult ExecutionResult;
    public TacticalAIResolvedProfile Profile;

    public bool Started
    {
        get { return Status == TacticalAILiveTurnStatus.Started; }
    }
}

public sealed class TacticalAILiveTurnIntegrator
{
    readonly Func<BattleSnapshot> snapshotProvider;
    readonly Func<TacticalAIResolvedProfile> profileResolver;
    readonly Func<BattleSnapshot, TacticalAIResolvedProfile, TacticalAISearchPlan> planBuilder;
    readonly Func<IEnumerable<TacticalAIPlannedAction>, BattleSnapshot, TacticalAIExecutionResult> executor;

    public TacticalAILiveTurnIntegrator(
        Func<BattleSnapshot> snapshotProvider,
        Func<TacticalAIResolvedProfile> profileResolver,
        Func<BattleSnapshot, TacticalAIResolvedProfile, TacticalAISearchPlan> planBuilder,
        Func<IEnumerable<TacticalAIPlannedAction>, BattleSnapshot, TacticalAIExecutionResult> executor)
    {
        this.snapshotProvider = snapshotProvider ?? (() => null);
        this.profileResolver = profileResolver ?? (() => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null));
        this.planBuilder = planBuilder ?? ((snapshot, profile) => new TacticalAISearchPlan());
        this.executor = executor ?? ((orderedIntents, plannedSnapshot) => new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.InvalidContext,
            Message = "No tactical AI execution function was configured."
        });
    }

    public static TacticalAILiveTurnIntegrator CreateFromScene(
        TacticalAIProfile assignedProfile = null,
        TacticalAISearchPlanner planner = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        TacticalAIProfile resolvedProfileAsset = assignedProfile ?? TacticalAIProfileCatalog.LoadNormalProfileAsset();
        TacticalAISearchPlanner resolvedPlanner = planner ?? new TacticalAISearchPlanner();
        ITacticalAISkillMetadataProvider resolvedSkillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
        TacticalAIExecutionBridge bridge = TacticalAIExecutionBridge.CreateFromScene(
            resolvedProfileAsset,
            null,
            resolvedSkillMetadataProvider);

        return new TacticalAILiveTurnIntegrator(
            BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot,
            () => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(resolvedProfileAsset),
            (snapshot, profile) => resolvedPlanner.BuildPlan(snapshot, profile, resolvedSkillMetadataProvider),
            (orderedActions, plannedSnapshot) => bridge.TryExecuteOrderedActions(orderedActions, plannedSnapshot));
    }

    public TacticalAILiveTurnIntegrationResult TryStartTurn()
    {
        TacticalAILiveTurnIntegrationResult result = new TacticalAILiveTurnIntegrationResult();

        BattleSnapshot snapshot = snapshotProvider();
        result.Snapshot = snapshot;
        result.ActorUnitId = snapshot != null ? snapshot.ActiveUnitId ?? string.Empty : string.Empty;

        if (snapshot == null)
        {
            result.FallbackReason = "SnapshotUnavailable";
            return result;
        }

        TacticalAIResolvedProfile profile = profileResolver() ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        result.Profile = profile;

        Debug.Log(BuildPlanningStartedLog(result.ActorUnitId, profile));

        TacticalAISearchPlan plan = planBuilder(snapshot, profile) ?? new TacticalAISearchPlan();
        result.Plan = plan;
        Debug.Log(BuildPlanLog(result.ActorUnitId, plan));

        if (plan.OrderedActions == null || plan.OrderedActions.Count == 0)
        {
            result.FallbackReason = "EmptyPlan";
            return result;
        }

        TacticalAIExecutionResult executionResult = executor(plan.OrderedActions, snapshot) ?? new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.InvalidContext,
            Message = "Tactical AI execution returned no result."
        };
        result.ExecutionResult = executionResult;

        if (executionResult.Status == TacticalAIExecutionStatus.Started && executionResult.ExecutedAction != null)
        {
            result.Status = TacticalAILiveTurnStatus.Started;
            Debug.Log(BuildStartedLog(result.ActorUnitId, executionResult.ExecutedAction));
            return result;
        }

        result.FallbackReason = BuildExecutionFallbackReason(executionResult);
        return result;
    }

    public static string BuildPlanningStartedLog(string actorUnitId, TacticalAIResolvedProfile profile)
    {
        string difficulty = profile != null && string.IsNullOrEmpty(profile.DifficultyName) == false
            ? profile.DifficultyName
            : "Normal";
        return "[TacticalAI] planning actor=" + Safe(actorUnitId) + " profile=" + difficulty;
    }

    public static string BuildPlanLog(string actorUnitId, TacticalAISearchPlan plan)
    {
        TacticalAIPlannedAction bestAction = plan != null ? plan.BestAction : null;
        int fallbackCount = plan != null && plan.OrderedActions != null
            ? Math.Max(0, plan.OrderedActions.Count - 1)
            : 0;

        return "[TacticalAI] plan actor=" + Safe(actorUnitId) +
            " best=" + DescribeAction(bestAction) +
            " score=" + FormatScore(plan != null ? plan.BestScore : 0f) +
            " depth=" + (plan != null ? plan.CompletedDepth : 0) +
            " fallbackCount=" + fallbackCount +
            " ranked=" + DescribeRankedActions(plan) +
            " opponentResponse=" + (plan != null && plan.CoveredOpponentResponse ? "covered" : "not-covered");
    }

    public static string BuildStartedLog(string actorUnitId, TacticalAIPlannedAction action)
    {
        return "[TacticalAI] started actor=" + Safe(actorUnitId) + " " + DescribeAction(action);
    }

    public static string BuildFallbackLog(
        string actorUnitId,
        TacticalAISearchPlan plan,
        TacticalAIExecutionResult executionResult,
        string fallbackReason)
    {
        int attempts = executionResult != null && executionResult.Attempts != null
            ? executionResult.Attempts.Count
            : 0;
        string status = executionResult != null ? executionResult.Status.ToString() : "None";

        return "[TacticalAI] fallback actor=" + Safe(actorUnitId) +
            " reason=" + Safe(fallbackReason) +
            " status=" + status +
            " attempts=" + attempts +
            " best=" + DescribeAction(plan != null ? plan.BestAction : null);
    }

    public static string BuildExecutionFallbackReason(TacticalAIExecutionResult executionResult)
    {
        if (executionResult == null)
        {
            return "MissingExecutionResult";
        }

        switch (executionResult.Status)
        {
            case TacticalAIExecutionStatus.Busy:
                return "BridgeBusy";
            case TacticalAIExecutionStatus.InvalidContext:
                return "InvalidContext";
            case TacticalAIExecutionStatus.NoLegalAction:
                return "NoLegalAction";
            case TacticalAIExecutionStatus.Started:
                return "StartedWithoutIntent";
            default:
                return Safe(executionResult.Message);
        }
    }

    static string DescribeIntent(TacticalAIActionIntent intent)
    {
        if (intent == null)
        {
            return "None";
        }

        string description = intent.ActionType.ToString();
        if (string.IsNullOrEmpty(intent.SkillId) == false)
        {
            description += " skill=" + intent.SkillId;
        }

        if (string.IsNullOrEmpty(intent.TargetUnitId) == false)
        {
            description += " target=" + intent.TargetUnitId;
        }
        else if (intent.TargetHex != null)
        {
            description += " targetHex=" + intent.TargetHex.C + "," + intent.TargetHex.R;
        }
        else if (intent.DestinationHex != null)
        {
            description += " destination=" + intent.DestinationHex.C + "," + intent.DestinationHex.R;
        }

        return description;
    }

    static string DescribeAction(TacticalAIPlannedAction action)
    {
        if (action == null)
        {
            return "None";
        }

        if (action.ActionType != TacticalAIActionType.Skill)
        {
            return DescribeIntent(action.LegacyIntent);
        }

        string description = "Skill";
        if (action.ValidatedSkillCast != null && string.IsNullOrEmpty(action.ValidatedSkillCast.SkillId) == false)
        {
            description += " skill=" + action.ValidatedSkillCast.SkillId;
        }

        if (action.ValidatedSkillCast != null && string.IsNullOrEmpty(action.ValidatedSkillCast.PrimaryTargetUnitId) == false)
        {
            description += " target=" + action.ValidatedSkillCast.PrimaryTargetUnitId;
        }

        return description;
    }

    static string DescribeRankedActions(TacticalAISearchPlan plan)
    {
        if (plan == null || plan.OrderedActions == null || plan.OrderedActions.Count == 0)
        {
            return "none";
        }

        int limit = Math.Min(5, plan.OrderedActions.Count);
        List<string> descriptions = new List<string>();
        for (int i = 0; i < limit; i++)
        {
            TacticalAIPlannedAction action = plan.OrderedActions[i];
            if (action == null)
            {
                continue;
            }

            if (action.ActionType == TacticalAIActionType.Skill && action.ValidatedSkillCast != null)
            {
                descriptions.Add("Skill:" + action.ValidatedSkillCast.SkillId);
            }
            else
            {
                descriptions.Add(action.ActionType.ToString());
            }
        }

        return descriptions.Count == 0 ? "none" : string.Join(">", descriptions.ToArray());
    }

    static string FormatScore(float score)
    {
        return score.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value;
    }
}
