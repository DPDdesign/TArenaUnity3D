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
    readonly Func<IEnumerable<TacticalAIActionIntent>, BattleSnapshot, TacticalAIExecutionResult> executor;

    public TacticalAILiveTurnIntegrator(
        Func<BattleSnapshot> snapshotProvider,
        Func<TacticalAIResolvedProfile> profileResolver,
        Func<BattleSnapshot, TacticalAIResolvedProfile, TacticalAISearchPlan> planBuilder,
        Func<IEnumerable<TacticalAIActionIntent>, BattleSnapshot, TacticalAIExecutionResult> executor)
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
            (orderedIntents, plannedSnapshot) => bridge.TryExecuteOrderedIntents(orderedIntents, plannedSnapshot));
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

        if (plan.OrderedActionIntents == null || plan.OrderedActionIntents.Count == 0)
        {
            result.FallbackReason = "EmptyPlan";
            return result;
        }

        TacticalAIExecutionResult executionResult = executor(plan.OrderedActionIntents, snapshot) ?? new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.InvalidContext,
            Message = "Tactical AI execution returned no result."
        };
        result.ExecutionResult = executionResult;

        if (executionResult.Status == TacticalAIExecutionStatus.Started && executionResult.ExecutedIntent != null)
        {
            result.Status = TacticalAILiveTurnStatus.Started;
            Debug.Log(BuildStartedLog(result.ActorUnitId, executionResult.ExecutedIntent));
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
        TacticalAIActionIntent bestIntent = plan != null ? plan.BestIntent : null;
        int fallbackCount = plan != null && plan.OrderedActionIntents != null
            ? Math.Max(0, plan.OrderedActionIntents.Count - 1)
            : 0;

        return "[TacticalAI] plan actor=" + Safe(actorUnitId) +
            " best=" + DescribeIntent(bestIntent) +
            " score=" + FormatScore(plan != null ? plan.BestScore : 0f) +
            " depth=" + (plan != null ? plan.CompletedDepth : 0) +
            " fallbackCount=" + fallbackCount +
            " opponentResponse=" + (plan != null && plan.CoveredOpponentResponse ? "covered" : "not-covered");
    }

    public static string BuildStartedLog(string actorUnitId, TacticalAIActionIntent intent)
    {
        return "[TacticalAI] started actor=" + Safe(actorUnitId) + " " + DescribeIntent(intent);
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
            " best=" + DescribeIntent(plan != null ? plan.BestIntent : null);
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

    static string FormatScore(float score)
    {
        return score.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value;
    }
}
