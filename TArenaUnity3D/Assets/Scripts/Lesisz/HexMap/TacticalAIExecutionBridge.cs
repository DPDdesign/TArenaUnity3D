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
    public TacticalAIActionIntent Intent;
    public bool Started;
    public string FailureReason = string.Empty;
}

[Serializable]
public sealed class TacticalAIExecutionResult
{
    public TacticalAIExecutionStatus Status;
    public TacticalAIActionIntent ExecutedIntent;
    public BattleSnapshot LiveSnapshot;
    public string Message = string.Empty;
    public List<TacticalAIExecutionAttempt> Attempts = new List<TacticalAIExecutionAttempt>();
}

public interface ITacticalAISkillIntentExecutor
{
    bool TryExecuteSkillIntent(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIActionIntent intent,
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
        int maxFallbackCandidates = -1)
    {
        List<TacticalAIActionIntent> queue = new List<TacticalAIActionIntent>();
        HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);

        AddPlannedIntents(queue, seenKeys, plannedIntents, maxFallbackCandidates);

        if (liveSnapshot == null)
        {
            return queue;
        }

        List<TacticalAIActionIntent> freshCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            liveSnapshot,
            options,
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
    readonly ITacticalAISkillIntentExecutor skillIntentExecutor;
    readonly ITacticalAISkillMetadataProvider skillMetadataProvider;

    public TacticalAIExecutionBridge(
        TacticalAIExecutionRuntimeContext runtimeContext,
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillIntentExecutor skillIntentExecutor = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        this.runtimeContext = runtimeContext ?? new TacticalAIExecutionRuntimeContext(null, null, null, null);
        this.assignedProfile = assignedProfile;
        this.skillIntentExecutor = skillIntentExecutor ?? TacticalAICastManagerSkillIntentExecutor.Instance;
        this.skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;
    }

    public static TacticalAIExecutionBridge CreateFromScene(
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillIntentExecutor skillIntentExecutor = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        return new TacticalAIExecutionBridge(
            new TacticalAIExecutionRuntimeContext(
                UnityEngine.Object.FindObjectOfType<HexMap>(),
                UnityEngine.Object.FindObjectOfType<MouseControler>(),
                UnityEngine.Object.FindObjectOfType<TurnManager>(),
                BattleActionLifecycle.Instance),
            assignedProfile,
            skillIntentExecutor,
            skillMetadataProvider);
    }

    public TacticalAIExecutionResult TryExecuteOrderedIntents(
        IEnumerable<TacticalAIActionIntent> orderedIntents,
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
        List<TacticalAIActionIntent> attempts = TacticalAIExecutionFallbackPlanner.BuildAttemptQueue(
            orderedIntents,
            liveSnapshot,
            BuildCandidateOptions(resolvedProfile),
            skillMetadataProvider,
            resolvedProfile.MaxFallbackCandidates);

        for (int i = 0; i < attempts.Count; i++)
        {
            TacticalAIActionIntent intent = attempts[i];
            TacticalAIExecutionAttempt attempt = new TacticalAIExecutionAttempt
            {
                Intent = intent
            };

            if (runtimeContext.BattleActionLifecycle != null && runtimeContext.BattleActionLifecycle.IsBusy)
            {
                attempt.FailureReason = "Battle action lifecycle became busy before executing the next AI attempt.";
                result.Attempts.Add(attempt);
                break;
            }

            TacticalAIRevalidatedIntent revalidatedIntent;
            if (TacticalAIIntentRevalidator.TryRevalidate(
                    intent,
                    liveSnapshot,
                    plannedSnapshot,
                    out revalidatedIntent,
                    out attempt.FailureReason,
                    skillMetadataProvider) == false)
            {
                result.Attempts.Add(attempt);
                continue;
            }

            if (TryExecuteLiveIntent(intent, revalidatedIntent, out attempt.FailureReason))
            {
                attempt.Started = true;
                result.Attempts.Add(attempt);
                result.ExecutedIntent = intent;
                result.Status = TacticalAIExecutionStatus.Started;
                result.Message = "Started " + intent.ActionType + " through the live battle lifecycle.";
                return result;
            }

            result.Attempts.Add(attempt);
        }

        result.Status = TacticalAIExecutionStatus.NoLegalAction;
        result.Message = "No tactical AI intent could be revalidated and started in live battle state.";
        Debug.LogWarning("[TacticalAIExecutionBridge] " + result.Message + " Attempts: " + result.Attempts.Count);
        return result;
    }

    bool TryExecuteLiveIntent(
        TacticalAIActionIntent intent,
        TacticalAIRevalidatedIntent revalidatedIntent,
        out string failureReason)
    {
        failureReason = string.Empty;

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
                if (skillIntentExecutor == null)
                {
                    failureReason = "Skill intents require the PRD046-F CastManager bridge and no skill executor is registered yet.";
                    return false;
                }

                if (skillIntentExecutor.TryExecuteSkillIntent(runtimeContext, intent, revalidatedIntent, out failureReason))
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
