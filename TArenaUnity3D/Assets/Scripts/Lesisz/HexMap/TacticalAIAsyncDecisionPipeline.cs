using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum TacticalAIAsyncDecisionState
{
    Idle,
    Running,
    Completed,
    Consumed,
    Stale,
    Faulted,
    Cancelled
}

public sealed class TacticalAICopiedSkillMetadataProvider : ITacticalAISkillMetadataProvider, ITacticalAISkillSpecProvider
{
    readonly Dictionary<string, TacticalAISkillMetadata> metadataBySkillId;
    readonly Dictionary<string, SkillDefinitionSpec> specsBySkillId;

    TacticalAICopiedSkillMetadataProvider(
        Dictionary<string, TacticalAISkillMetadata> metadataBySkillId,
        Dictionary<string, SkillDefinitionSpec> specsBySkillId)
    {
        this.metadataBySkillId = metadataBySkillId ?? new Dictionary<string, TacticalAISkillMetadata>(StringComparer.Ordinal);
        this.specsBySkillId = specsBySkillId ?? new Dictionary<string, SkillDefinitionSpec>(StringComparer.Ordinal);
    }

    public static TacticalAICopiedSkillMetadataProvider Capture(
        BattleSnapshot snapshot,
        ITacticalAISkillMetadataProvider liveProvider)
    {
        Dictionary<string, TacticalAISkillMetadata> copiedMetadata =
            new Dictionary<string, TacticalAISkillMetadata>(StringComparer.Ordinal);
        Dictionary<string, SkillDefinitionSpec> copiedSpecs =
            new Dictionary<string, SkillDefinitionSpec>(StringComparer.Ordinal);
        if (snapshot == null || snapshot.Units == null)
        {
            return new TacticalAICopiedSkillMetadataProvider(copiedMetadata, copiedSpecs);
        }

        ITacticalAISkillSpecProvider specProvider = liveProvider as ITacticalAISkillSpecProvider;

        for (int unitIndex = 0; unitIndex < snapshot.Units.Count; unitIndex++)
        {
            BattleUnitSnapshot unit = snapshot.Units[unitIndex];
            if (unit == null || unit.SkillIdsBySlot == null)
            {
                continue;
            }

            for (int skillIndex = 0; skillIndex < unit.SkillIdsBySlot.Count; skillIndex++)
            {
                string skillId = unit.SkillIdsBySlot[skillIndex];
                if (string.IsNullOrEmpty(skillId) || copiedMetadata.ContainsKey(skillId))
                {
                    continue;
                }

                SkillDefinitionSpec spec;
                if (specProvider != null && specProvider.TryGetSkillSpec(skillId, out spec) && spec != null)
                {
                    copiedSpecs.Add(skillId, spec);
                }

                TacticalAISkillMetadata liveMetadata;
                if (liveProvider != null && liveProvider.TryGetSkillMetadata(skillId, out liveMetadata) && liveMetadata != null)
                {
                    copiedMetadata.Add(skillId, CloneMetadata(liveMetadata));
                    continue;
                }

                copiedMetadata.Add(skillId, CreateFallbackMetadata(skillId));
            }
        }

        return new TacticalAICopiedSkillMetadataProvider(copiedMetadata, copiedSpecs);
    }

    public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
    {
        metadata = null;
        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        TacticalAISkillMetadata copiedMetadata;
        if (metadataBySkillId.TryGetValue(skillId, out copiedMetadata) == false || copiedMetadata == null)
        {
            return false;
        }

        metadata = CloneMetadata(copiedMetadata);
        return true;
    }

    public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
    {
        spec = null;
        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        return specsBySkillId.TryGetValue(skillId, out spec) && spec != null;
    }

    static TacticalAISkillMetadata CloneMetadata(TacticalAISkillMetadata metadata)
    {
        if (metadata == null)
        {
            return new TacticalAISkillMetadata();
        }

        return new TacticalAISkillMetadata
        {
            SkillId = metadata.SkillId ?? string.Empty,
            IsPassive = metadata.IsPassive,
            CanUseAfterMove = metadata.CanUseAfterMove,
            CanMoveAfterSkill = metadata.CanMoveAfterSkill,
            IsRepeatableToggle = metadata.IsRepeatableToggle
        };
    }

    static TacticalAISkillMetadata CreateFallbackMetadata(string skillId)
    {
        return new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
        };
    }
}

public sealed class TacticalAIAsyncTurnIntegrator
{
    sealed class PendingDecision
    {
        public int DecisionId;
        public TacticalAIAsyncDecisionState State;
        public string ActorUnitId = string.Empty;
        public BattleSnapshot Snapshot;
        public TacticalAIResolvedProfile Profile;
        public ITacticalAISkillMetadataProvider SkillMetadataProvider;
        public Task<TacticalAISearchPlan> Task;
        public TacticalAISearchPlan Plan;
        public string FaultMessage = string.Empty;
        public bool CompletionLogged;
    }

    readonly Func<BattleSnapshot> snapshotProvider;
    readonly Func<TacticalAIResolvedProfile> profileResolver;
    readonly Func<BattleSnapshot, TacticalAIResolvedProfile, ITacticalAISkillMetadataProvider, TacticalAISearchPlan> planBuilder;
    readonly Func<BattleSnapshot, ITacticalAISkillMetadataProvider> skillMetadataCapture;
    readonly Func<IEnumerable<TacticalAIPlannedAction>, BattleSnapshot, TacticalAIExecutionResult> executor;

    PendingDecision pendingDecision;
    int nextDecisionId;

    public TacticalAIAsyncTurnIntegrator(
        Func<BattleSnapshot> snapshotProvider,
        Func<TacticalAIResolvedProfile> profileResolver,
        Func<BattleSnapshot, TacticalAIResolvedProfile, ITacticalAISkillMetadataProvider, TacticalAISearchPlan> planBuilder,
        Func<BattleSnapshot, ITacticalAISkillMetadataProvider> skillMetadataCapture,
        Func<IEnumerable<TacticalAIPlannedAction>, BattleSnapshot, TacticalAIExecutionResult> executor)
    {
        this.snapshotProvider = snapshotProvider ?? (() => null);
        this.profileResolver = profileResolver ?? (() => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null));
        this.planBuilder = planBuilder ?? ((snapshot, profile, skillMetadataProvider) => new TacticalAISearchPlan());
        this.skillMetadataCapture = skillMetadataCapture ?? (snapshot => TacticalAICopiedSkillMetadataProvider.Capture(snapshot, null));
        this.executor = executor ?? ((orderedIntents, plannedSnapshot) => new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.InvalidContext,
            Message = "No tactical AI execution function was configured."
        });
    }

    public static TacticalAIAsyncTurnIntegrator CreateFromScene(
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillActionExecutor skillActionExecutor = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        TacticalAIProfile resolvedProfileAsset = assignedProfile ?? TacticalAIProfileCatalog.LoadNormalProfileAsset();
        ITacticalAISkillMetadataProvider liveSkillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
        TacticalAIExecutionBridge bridge = TacticalAIExecutionBridge.CreateFromScene(
            resolvedProfileAsset,
            skillActionExecutor,
            liveSkillMetadataProvider);

        return new TacticalAIAsyncTurnIntegrator(
            BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot,
            () => TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(resolvedProfileAsset),
            (snapshot, profile, copiedSkillMetadataProvider) => TacticalAISearchEngine.Search(snapshot, profile, copiedSkillMetadataProvider),
            snapshot => TacticalAICopiedSkillMetadataProvider.Capture(snapshot, liveSkillMetadataProvider),
            (orderedActions, plannedSnapshot) => bridge.TryExecuteOrderedActions(orderedActions, plannedSnapshot));
    }

    public bool TryBeginTurn(out TacticalAILiveTurnIntegrationResult immediateResult)
    {
        immediateResult = null;
        BattleSnapshot snapshot = snapshotProvider();
        if (snapshot == null)
        {
            immediateResult = BuildFallbackResult(null, null, null, null, "SnapshotUnavailable");
            return false;
        }

        TacticalAIResolvedProfile profile = profileResolver() ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        profile = profile.Clone();
        if (string.IsNullOrEmpty(profile.ProfileHash))
        {
            profile.ProfileHash = TacticalAIProfileHasher.ComputeHash(profile);
        }

        if (pendingDecision != null &&
            (pendingDecision.State == TacticalAIAsyncDecisionState.Running || pendingDecision.State == TacticalAIAsyncDecisionState.Completed))
        {
            pendingDecision.State = TacticalAIAsyncDecisionState.Cancelled;
        }

        pendingDecision = new PendingDecision
        {
            DecisionId = ++nextDecisionId,
            State = TacticalAIAsyncDecisionState.Running,
            ActorUnitId = snapshot.ActiveUnitId ?? string.Empty,
            Snapshot = snapshot,
            Profile = profile,
            SkillMetadataProvider = skillMetadataCapture(snapshot)
                ?? TacticalAICopiedSkillMetadataProvider.Capture(snapshot, null)
        };

        Debug.Log(BuildAsyncPlanningStartedLog(pendingDecision));

        BattleSnapshot workerSnapshot = pendingDecision.Snapshot;
        TacticalAIResolvedProfile workerProfile = pendingDecision.Profile.Clone();
        ITacticalAISkillMetadataProvider workerSkillMetadata = pendingDecision.SkillMetadataProvider;
        pendingDecision.Task = Task.Run(() => planBuilder(workerSnapshot, workerProfile, workerSkillMetadata));
        return true;
    }

    public bool TryCompleteTurn(out TacticalAILiveTurnIntegrationResult result)
    {
        result = null;
        if (pendingDecision == null)
        {
            return false;
        }

        if (pendingDecision.State == TacticalAIAsyncDecisionState.Cancelled)
        {
            result = BuildFallbackResult(
                pendingDecision.ActorUnitId,
                pendingDecision.Snapshot,
                pendingDecision.Profile,
                pendingDecision.Plan,
                "AsyncCancelled");
            pendingDecision = null;
            return true;
        }

        if (pendingDecision.State == TacticalAIAsyncDecisionState.Running)
        {
            if (pendingDecision.Task == null || pendingDecision.Task.IsCompleted == false)
            {
                return false;
            }

            if (pendingDecision.Task.IsCanceled)
            {
                pendingDecision.State = TacticalAIAsyncDecisionState.Cancelled;
                result = BuildFallbackResult(
                    pendingDecision.ActorUnitId,
                    pendingDecision.Snapshot,
                    pendingDecision.Profile,
                    pendingDecision.Plan,
                    "AsyncCancelled");
                pendingDecision = null;
                return true;
            }

            if (pendingDecision.Task.IsFaulted)
            {
                pendingDecision.State = TacticalAIAsyncDecisionState.Faulted;
                pendingDecision.FaultMessage = pendingDecision.Task.Exception != null
                    ? pendingDecision.Task.Exception.GetBaseException().Message
                    : "Unknown async planner fault.";
                Debug.LogWarning(BuildAsyncFaultedLog(pendingDecision));
                result = BuildFallbackResult(
                    pendingDecision.ActorUnitId,
                    pendingDecision.Snapshot,
                    pendingDecision.Profile,
                    pendingDecision.Plan,
                    "AsyncFaulted");
                pendingDecision = null;
                return true;
            }

            pendingDecision.Plan = pendingDecision.Task.Result ?? new TacticalAISearchPlan();
            pendingDecision.State = TacticalAIAsyncDecisionState.Completed;
            if (pendingDecision.CompletionLogged == false)
            {
                pendingDecision.CompletionLogged = true;
                Debug.Log(BuildAsyncPlanningCompletedLog(pendingDecision));
            }
        }

        if (pendingDecision.State != TacticalAIAsyncDecisionState.Completed)
        {
            return false;
        }

        if (pendingDecision.Plan == null || pendingDecision.Plan.OrderedActions == null || pendingDecision.Plan.OrderedActions.Count == 0)
        {
            Debug.LogError("[TacticalAI] async-empty-plan actor=" + Safe(pendingDecision.ActorUnitId) + " reason=NoLegalRankedActions");
            result = BuildFallbackResult(
                pendingDecision.ActorUnitId,
                pendingDecision.Snapshot,
                pendingDecision.Profile,
                pendingDecision.Plan,
                "EmptyPlan");
            pendingDecision = null;
            return true;
        }

        BattleSnapshot currentSnapshot = snapshotProvider();
        TacticalAIResolvedProfile currentProfile = profileResolver() ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        if (string.IsNullOrEmpty(currentProfile.ProfileHash))
        {
            currentProfile.ProfileHash = TacticalAIProfileHasher.ComputeHash(currentProfile);
        }

        string staleReason;
        if (TryValidateCurrentState(pendingDecision, currentSnapshot, currentProfile, out staleReason) == false)
        {
            pendingDecision.State = TacticalAIAsyncDecisionState.Stale;
            Debug.LogWarning(BuildAsyncStaleLog(pendingDecision, staleReason));
            result = BuildFallbackResult(
                pendingDecision.ActorUnitId,
                pendingDecision.Snapshot,
                pendingDecision.Profile,
                pendingDecision.Plan,
                staleReason);
            pendingDecision = null;
            return true;
        }

        TacticalAIExecutionResult executionResult = executor(
            pendingDecision.Plan.OrderedActions,
            pendingDecision.Snapshot) ?? new TacticalAIExecutionResult
        {
            Status = TacticalAIExecutionStatus.InvalidContext,
            Message = "Tactical AI execution returned no result."
        };

        if (executionResult.Status == TacticalAIExecutionStatus.Busy)
        {
            return false;
        }

        if (executionResult.Status == TacticalAIExecutionStatus.Started && executionResult.ExecutedAction != null)
        {
            result = new TacticalAILiveTurnIntegrationResult
            {
                Status = TacticalAILiveTurnStatus.Started,
                ActorUnitId = pendingDecision.ActorUnitId,
                Snapshot = pendingDecision.Snapshot,
                Plan = pendingDecision.Plan,
                ExecutionResult = executionResult,
                Profile = pendingDecision.Profile
            };
            Debug.Log(TacticalAILiveTurnIntegrator.BuildStartedLog(
                pendingDecision.ActorUnitId,
                executionResult.ExecutedAction));
            pendingDecision.State = TacticalAIAsyncDecisionState.Consumed;
            pendingDecision = null;
            return true;
        }

        string fallbackReason = TacticalAILiveTurnIntegrator.BuildExecutionFallbackReason(executionResult);
        result = new TacticalAILiveTurnIntegrationResult
        {
            Status = TacticalAILiveTurnStatus.FallbackToLegacy,
            ActorUnitId = pendingDecision.ActorUnitId,
            Snapshot = pendingDecision.Snapshot,
            Plan = pendingDecision.Plan,
            ExecutionResult = executionResult,
            Profile = pendingDecision.Profile,
            FallbackReason = fallbackReason
        };
        pendingDecision = null;
        return true;
    }

    static TacticalAILiveTurnIntegrationResult BuildFallbackResult(
        string actorUnitId,
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        TacticalAISearchPlan plan,
        string fallbackReason)
    {
        return new TacticalAILiveTurnIntegrationResult
        {
            Status = TacticalAILiveTurnStatus.FallbackToLegacy,
            ActorUnitId = actorUnitId ?? string.Empty,
            Snapshot = snapshot,
            Profile = profile,
            Plan = plan,
            FallbackReason = fallbackReason ?? string.Empty
        };
    }

    static bool TryValidateCurrentState(
        PendingDecision decision,
        BattleSnapshot currentSnapshot,
        TacticalAIResolvedProfile currentProfile,
        out string staleReason)
    {
        staleReason = string.Empty;
        if (decision == null)
        {
            staleReason = "AsyncDecisionMissing";
            return false;
        }

        if (currentSnapshot == null)
        {
            staleReason = "LiveSnapshotUnavailable";
            return false;
        }

        if (string.Equals(decision.ActorUnitId, currentSnapshot.ActiveUnitId ?? string.Empty, StringComparison.Ordinal) == false)
        {
            staleReason = "ActiveActorChanged";
            return false;
        }

        string plannedSnapshotHash = decision.Snapshot != null ? decision.Snapshot.SnapshotHash ?? string.Empty : string.Empty;
        if (string.Equals(plannedSnapshotHash, currentSnapshot.SnapshotHash ?? string.Empty, StringComparison.Ordinal) == false)
        {
            staleReason = "SnapshotHashChanged";
            return false;
        }

        string plannedProfileHash = decision.Profile != null ? decision.Profile.ProfileHash ?? string.Empty : string.Empty;
        string currentProfileHash = currentProfile != null ? currentProfile.ProfileHash ?? string.Empty : string.Empty;
        if (string.Equals(plannedProfileHash, currentProfileHash, StringComparison.Ordinal) == false)
        {
            staleReason = "ProfileChanged";
            return false;
        }

        return true;
    }

    static string BuildAsyncPlanningStartedLog(PendingDecision decision)
    {
        return "[TacticalAI] async-start actor=" + Safe(decision != null ? decision.ActorUnitId : string.Empty) +
            " profile=" + Safe(decision != null && decision.Profile != null ? decision.Profile.DifficultyName : string.Empty) +
            " snapshot=" + Safe(decision != null && decision.Snapshot != null ? decision.Snapshot.SnapshotHash : string.Empty) +
            " decisionId=" + (decision != null ? decision.DecisionId : 0);
    }

    static string BuildAsyncPlanningCompletedLog(PendingDecision decision)
    {
        string log = "[TacticalAI] async-complete actor=" + Safe(decision != null ? decision.ActorUnitId : string.Empty) +
            " decisionId=" + (decision != null ? decision.DecisionId : 0) +
            " " + TacticalAILiveTurnIntegrator.BuildPlanLog(
                decision != null ? decision.ActorUnitId : string.Empty,
                decision != null ? decision.Plan : null);

        if (decision != null && decision.Plan != null && decision.Plan.WatchdogExpired)
        {
            log += " watchdog=expired";
        }

        return log;
    }

    static string BuildAsyncFaultedLog(PendingDecision decision)
    {
        return "[TacticalAI] async-fault actor=" + Safe(decision != null ? decision.ActorUnitId : string.Empty) +
            " decisionId=" + (decision != null ? decision.DecisionId : 0) +
            " error=" + Safe(decision != null ? decision.FaultMessage : string.Empty);
    }

    static string BuildAsyncStaleLog(PendingDecision decision, string staleReason)
    {
        return "[TacticalAI] async-stale actor=" + Safe(decision != null ? decision.ActorUnitId : string.Empty) +
            " decisionId=" + (decision != null ? decision.DecisionId : 0) +
            " reason=" + Safe(staleReason);
    }

    static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value;
    }
}
