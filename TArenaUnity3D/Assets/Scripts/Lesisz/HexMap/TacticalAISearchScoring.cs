using System;
using System.Collections.Generic;
using System.Diagnostics;

[Serializable]
public sealed class TacticalAISearchPlan
{
    public TacticalAIPlannedAction BestAction;
    public List<TacticalAIPlannedAction> OrderedActions = new List<TacticalAIPlannedAction>();
    public TacticalAIActionIntent BestIntent;
    public List<TacticalAIActionIntent> OrderedActionIntents = new List<TacticalAIActionIntent>();
    public float BestScore;
    public int CompletedDepth;
    public int EvaluatedLeafCount;
    public bool CoveredOpponentResponse;
    public bool OpponentResponseReachable;
    public bool WatchdogExpired;
    public string PlannedSnapshotHash = string.Empty;
    public string ProfileHash = string.Empty;
}

public sealed class TacticalAISearchPlanner
{
    readonly TacticalAIPlanCache<TacticalAIPlannedAction> planCache =
        new TacticalAIPlanCache<TacticalAIPlannedAction>();

    public TacticalAISearchPlan BuildPlan(
        BattleSnapshot snapshot,
        TacticalAIProfile assignedProfile = null,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        TacticalAIResolvedProfile profile = TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(assignedProfile);
        return BuildPlan(snapshot, profile, skillMetadataProvider);
    }

    public TacticalAISearchPlan BuildPlan(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        profile = profile ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        TacticalAIPlanCacheValue<TacticalAIPlannedAction> cachedPlan;
        if (planCache.TryGetAdvisoryPlan(snapshot, profile, out cachedPlan))
        {
            return new TacticalAISearchPlan
            {
                BestAction = FirstOrNull(cachedPlan.OrderedActionIntents),
                OrderedActions = cachedPlan.OrderedActionIntents,
                BestIntent = LegacyIntentOrNull(FirstOrNull(cachedPlan.OrderedActionIntents)),
                OrderedActionIntents = ToLegacyIntents(cachedPlan.OrderedActionIntents),
                BestScore = cachedPlan.BestScore,
                CompletedDepth = cachedPlan.CompletedDepth,
                PlannedSnapshotHash = snapshot != null ? snapshot.SnapshotHash : string.Empty,
                ProfileHash = profile.ProfileHash
            };
        }

        TacticalAISearchPlan plan = TacticalAISearchEngine.Search(snapshot, profile, skillMetadataProvider);
        if (plan != null && plan.OrderedActions.Count > 0)
        {
            planCache.StoreAdvisoryPlan(
                snapshot,
                profile,
                plan.OrderedActions,
                plan.BestScore,
                plan.CompletedDepth);
        }

        return plan;
    }

    public void ClearCache()
    {
        planCache.Clear();
    }

    static TacticalAIPlannedAction FirstOrNull(List<TacticalAIPlannedAction> actions)
    {
        return actions != null && actions.Count > 0 ? actions[0] : null;
    }

    static TacticalAIActionIntent LegacyIntentOrNull(TacticalAIPlannedAction action)
    {
        return action != null ? action.LegacyIntent : null;
    }

    static List<TacticalAIActionIntent> ToLegacyIntents(List<TacticalAIPlannedAction> actions)
    {
        List<TacticalAIActionIntent> intents = new List<TacticalAIActionIntent>();
        if (actions == null)
        {
            return intents;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != null && actions[i].LegacyIntent != null)
            {
                intents.Add(actions[i].LegacyIntent);
            }
        }

        return intents;
    }
}

public static class TacticalAISearchEngine
{
    public static TacticalAISearchPlan Search(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        TacticalAISearchPlan emptyPlan = new TacticalAISearchPlan();
        if (snapshot == null || string.IsNullOrEmpty(snapshot.ActiveUnitId))
        {
            return emptyPlan;
        }

        profile = profile ?? TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null);
        skillMetadataProvider = skillMetadataProvider ?? TacticalAIDataMapperSkillMetadataProvider.Instance;

        BattleUnitSnapshot activeUnit = TacticalAISnapshotQuery.FindUnit(snapshot, snapshot.ActiveUnitId);
        if (activeUnit == null || activeUnit.IsAlive == false || activeUnit.Amount <= 0)
        {
            return emptyPlan;
        }

        SearchContext context = new SearchContext(snapshot, profile, skillMetadataProvider, activeUnit.TeamIndex);
        List<ScoredRootIntent> scoredRootIntents = new List<ScoredRootIntent>();
        List<TacticalAIActionIntent> rootCandidates = TacticalAISearchCandidateExpander.BuildSearchCandidates(
            snapshot,
            profile,
            skillMetadataProvider);
        List<ScoredCandidate> prunedRootCandidates = ScoreAndPruneCandidates(
            snapshot,
            rootCandidates,
            activeUnit,
            false,
            context);

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < prunedRootCandidates.Count; i++)
        {
            TacticalAIActionIntent intent = prunedRootCandidates[i].Intent;
            scoredRootIntents.Add(new ScoredRootIntent
            {
                Intent = intent,
                Score = prunedRootCandidates[i].Score,
                CompletedDepth = 1,
                EvaluatedLeafCount = 0,
                CoveredOpponentResponse = false,
                StableOrderKey = intent != null ? intent.StableOrderKey : string.Empty
            });
        }

        for (int i = 0; i < prunedRootCandidates.Count; i++)
        {
            if (context.Budget.IsWatchdogExpired(stopwatch.ElapsedMilliseconds))
            {
                context.WatchdogExpired = true;
                break;
            }

            TacticalAIActionIntent intent = prunedRootCandidates[i].Intent;
            BattleSnapshot simulated = TacticalAISnapshotSimulator.ApplyIntent(snapshot, intent, skillMetadataProvider);
            simulated = BattleSnapshotTurnOrderEstimator.AdvanceToNextOpportunity(simulated);

            SearchBranchResult branch = SearchBranch(
                simulated,
                profile.SearchDepthPlies - 1,
                1,
                context,
                stopwatch);

            float actionScore = TacticalAISnapshotScorer.GetActionBias(intent, profile, activeUnit.TeamIndex, context.AITeamIndex);
            scoredRootIntents[i] = new ScoredRootIntent
            {
                Intent = intent,
                Score = branch.Score + actionScore,
                CompletedDepth = Math.Max(1, branch.CompletedDepth),
                EvaluatedLeafCount = branch.EvaluatedLeafCount,
                CoveredOpponentResponse = branch.CoveredOpponentResponse,
                StableOrderKey = intent != null ? intent.StableOrderKey : string.Empty
            };
        }

        scoredRootIntents.Sort((left, right) => CompareRootIntents(left, right, profile));

        TacticalAISearchPlan plan = new TacticalAISearchPlan
        {
            PlannedSnapshotHash = snapshot.SnapshotHash ?? string.Empty,
            ProfileHash = profile.ProfileHash ?? string.Empty,
            OpponentResponseReachable = context.OpponentResponseReachable,
            WatchdogExpired = context.WatchdogExpired,
            EvaluatedLeafCount = context.EvaluatedLeafCount
        };

        if (scoredRootIntents.Count > 0)
        {
            ScoredRootIntent best = scoredRootIntents[0];
            plan.BestAction = ToPlannedAction(snapshot, best.Intent, skillMetadataProvider);
            plan.BestIntent = plan.BestAction != null ? plan.BestAction.LegacyIntent : null;
            plan.BestScore = best.Score;
            plan.CompletedDepth = best.CompletedDepth;
            plan.CoveredOpponentResponse = best.CoveredOpponentResponse;

            int fallbackLimit = Math.Max(1, profile.MaxFallbackCandidates + 1);
            for (int i = 0; i < scoredRootIntents.Count && i < fallbackLimit; i++)
            {
                TacticalAIPlannedAction action = ToPlannedAction(snapshot, scoredRootIntents[i].Intent, skillMetadataProvider);
                if (action != null)
                {
                    plan.OrderedActions.Add(action);
                    if (action.LegacyIntent != null)
                    {
                        plan.OrderedActionIntents.Add(action.LegacyIntent);
                    }
                }
            }
        }

        return plan;
    }

    static TacticalAIPlannedAction ToPlannedAction(
        BattleSnapshot snapshot,
        TacticalAIActionIntent intent,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        if (intent == null)
        {
            return null;
        }

        BattleActionUse use = ToBattleActionUse(intent);
        BattleActionValidationResult validation = BattleActionRules.Validate(use, snapshot, skillMetadataProvider);
        if (validation.IsValid && validation.Action != null)
        {
            TacticalAIPlannedAction plannedAction = TacticalAIPlannedAction.FromBattleAction(
                validation.Action,
                BattleActionRules.Apply(snapshot, validation.Action));
            if (plannedAction != null &&
                validation.Action.ActionKind != BattleActionKind.Skill &&
                validation.Action.ActionKind != BattleActionKind.Stance)
            {
                plannedAction.LegacyIntent = intent;
            }

            return plannedAction;
        }

        return TacticalAIPlannedAction.FromCandidateIntent(intent);
    }

    static BattleActionUse ToBattleActionUse(TacticalAIActionIntent intent)
    {
        BattleActionUse use = new BattleActionUse
        {
            ActorUnitId = intent.ActorUnitId ?? string.Empty,
            ActionKind = TacticalAIPlannedAction.ToBattleActionKind(intent.ActionType),
            TargetUnitId = intent.TargetUnitId ?? string.Empty,
            SkillSlot = intent.SkillSlot,
            SkillId = intent.SkillId ?? string.Empty
        };

        if (intent.ValidatedSkillCast != null)
        {
            use.SelectedHexes = BattleActionModelUtility.CopyHexes(intent.ValidatedSkillCast.SelectedHexes);
            return use;
        }

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

    static SearchBranchResult SearchBranch(
        BattleSnapshot snapshot,
        int remainingDepth,
        int currentDepth,
        SearchContext context,
        Stopwatch stopwatch)
    {
        if (snapshot == null || remainingDepth <= 0)
        {
            return EvaluateLeaf(snapshot, currentDepth, context);
        }

        if (context.Budget.IsWatchdogExpired(stopwatch.ElapsedMilliseconds))
        {
            context.WatchdogExpired = true;
            return EvaluateLeaf(snapshot, currentDepth, context);
        }

        BattleUnitSnapshot activeUnit = TacticalAISnapshotQuery.FindUnit(snapshot, snapshot.ActiveUnitId);
        if (activeUnit == null || activeUnit.IsAlive == false || activeUnit.Amount <= 0)
        {
            return EvaluateLeaf(snapshot, currentDepth, context);
        }

        bool isOpponentPly = activeUnit.TeamIndex != context.AITeamIndex;
        if (isOpponentPly)
        {
            context.OpponentResponseReachable = true;
        }

        List<TacticalAIActionIntent> candidates = TacticalAISearchCandidateExpander.BuildSearchCandidates(
            snapshot,
            context.Profile,
            context.SkillMetadataProvider);
        if (candidates.Count == 0)
        {
            return EvaluateLeaf(snapshot, currentDepth, context);
        }

        List<ScoredCandidate> orderedCandidates = ScoreAndPruneCandidates(
            snapshot,
            candidates,
            activeUnit,
            isOpponentPly,
            context);

        SearchBranchResult best = null;
        for (int i = 0; i < orderedCandidates.Count; i++)
        {
            if (context.Budget.IsWatchdogExpired(stopwatch.ElapsedMilliseconds))
            {
                context.WatchdogExpired = true;
                break;
            }

            TacticalAIActionIntent intent = orderedCandidates[i].Intent;
            BattleSnapshot simulated = TacticalAISnapshotSimulator.ApplyIntent(snapshot, intent, context.SkillMetadataProvider);
            simulated = BattleSnapshotTurnOrderEstimator.AdvanceToNextOpportunity(simulated);

            SearchBranchResult child = SearchBranch(
                simulated,
                remainingDepth - 1,
                currentDepth + 1,
                context,
                stopwatch);
            child.Score += TacticalAISnapshotScorer.GetActionBias(intent, context.Profile, activeUnit.TeamIndex, context.AITeamIndex);
            child.CoveredOpponentResponse = child.CoveredOpponentResponse || isOpponentPly;

            if (best == null || IsBetterBranch(child, best, isOpponentPly, context.Profile))
            {
                best = child;
            }
        }

        return best ?? EvaluateLeaf(snapshot, currentDepth, context);
    }

    static SearchBranchResult EvaluateLeaf(
        BattleSnapshot snapshot,
        int currentDepth,
        SearchContext context)
    {
        context.EvaluatedLeafCount++;
        return new SearchBranchResult
        {
            Score = TacticalAISnapshotScorer.Score(context.RootSnapshot, snapshot, context.AITeamIndex, context.Profile),
            CompletedDepth = Math.Max(0, currentDepth),
            EvaluatedLeafCount = 1
        };
    }

    static List<ScoredCandidate> ScoreAndPruneCandidates(
        BattleSnapshot snapshot,
        List<TacticalAIActionIntent> candidates,
        BattleUnitSnapshot activeUnit,
        bool isOpponentPly,
        SearchContext context)
    {
        List<ScoredCandidate> scored = new List<ScoredCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            TacticalAIActionIntent intent = candidates[i];
            BattleSnapshot simulated = TacticalAISnapshotSimulator.ApplyIntent(snapshot, intent, context.SkillMetadataProvider);
            float score = TacticalAISnapshotScorer.Score(context.RootSnapshot, simulated, context.AITeamIndex, context.Profile);
            score += TacticalAISnapshotScorer.GetActionBias(intent, context.Profile, activeUnit.TeamIndex, context.AITeamIndex);
            scored.Add(new ScoredCandidate
            {
                Intent = intent,
                Score = score,
                StableOrderKey = intent != null ? intent.StableOrderKey : string.Empty
            });
        }

        scored.Sort((left, right) =>
        {
            int scoreCompare = isOpponentPly
                ? left.Score.CompareTo(right.Score)
                : right.Score.CompareTo(left.Score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return string.CompareOrdinal(left.StableOrderKey, right.StableOrderKey);
        });

        int beam = context.Budget.ClampBeamWidth(isOpponentPly, scored.Count);
        if (beam < scored.Count)
        {
            scored = PreserveActionTypeDiversity(scored, beam, isOpponentPly);
        }

        return scored;
    }

    static List<ScoredCandidate> PreserveActionTypeDiversity(List<ScoredCandidate> scored, int beam, bool isOpponentPly)
    {
        if (scored == null || scored.Count <= beam)
        {
            return scored;
        }

        TacticalAIActionType[] preferredTypes =
        {
            TacticalAIActionType.Skill,
            TacticalAIActionType.BasicRangedAttack,
            TacticalAIActionType.MoveAndAttack,
            TacticalAIActionType.Move,
            TacticalAIActionType.Wait,
            TacticalAIActionType.Defend
        };

        List<ScoredCandidate> kept = new List<ScoredCandidate>();
        for (int typeIndex = 0; typeIndex < preferredTypes.Length; typeIndex++)
        {
            if (kept.Count >= beam)
            {
                break;
            }

            TacticalAIActionType actionType = preferredTypes[typeIndex];
            int sourceIndex = FindFirstActionTypeIndex(scored, actionType);
            if (sourceIndex >= 0)
            {
                AddCandidateIfMissing(kept, scored[sourceIndex]);
            }
        }

        for (int i = 0; i < scored.Count && kept.Count < beam; i++)
        {
            AddCandidateIfMissing(kept, scored[i]);
        }

        kept.Sort((left, right) =>
        {
            int scoreCompare = isOpponentPly
                ? left.Score.CompareTo(right.Score)
                : right.Score.CompareTo(left.Score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            return string.CompareOrdinal(left.StableOrderKey, right.StableOrderKey);
        });
        return kept;
    }

    static void AddCandidateIfMissing(List<ScoredCandidate> candidates, ScoredCandidate candidate)
    {
        if (candidates == null || candidate.Intent == null)
        {
            return;
        }

        string stableKey = candidate.StableOrderKey ?? string.Empty;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i].StableOrderKey ?? string.Empty, stableKey, StringComparison.Ordinal))
            {
                return;
            }
        }

        candidates.Add(candidate);
    }

    static int FindFirstActionTypeIndex(List<ScoredCandidate> candidates, TacticalAIActionType actionType)
    {
        if (candidates == null)
        {
            return -1;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            TacticalAIActionIntent intent = candidates[i].Intent;
            if (intent != null && intent.ActionType == actionType)
            {
                return i;
            }
        }

        return -1;
    }

    static bool IsBetterBranch(
        SearchBranchResult candidate,
        SearchBranchResult currentBest,
        bool isOpponentPly,
        TacticalAIResolvedProfile profile)
    {
        if (profile.RequireOpponentResponseWhenReachable &&
            candidate.CoveredOpponentResponse != currentBest.CoveredOpponentResponse)
        {
            return candidate.CoveredOpponentResponse;
        }

        if (candidate.Score != currentBest.Score)
        {
            return isOpponentPly ? candidate.Score < currentBest.Score : candidate.Score > currentBest.Score;
        }

        return candidate.CompletedDepth > currentBest.CompletedDepth;
    }

    static int CompareRootIntents(ScoredRootIntent left, ScoredRootIntent right, TacticalAIResolvedProfile profile)
    {
        if (profile.RequireOpponentResponseWhenReachable &&
            left.CoveredOpponentResponse != right.CoveredOpponentResponse)
        {
            return left.CoveredOpponentResponse ? -1 : 1;
        }

        int scoreCompare = right.Score.CompareTo(left.Score);
        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        int depthCompare = right.CompletedDepth.CompareTo(left.CompletedDepth);
        if (depthCompare != 0)
        {
            return depthCompare;
        }

        return string.CompareOrdinal(left.StableOrderKey, right.StableOrderKey);
    }

    sealed class SearchContext
    {
        public readonly BattleSnapshot RootSnapshot;
        public readonly TacticalAIResolvedProfile Profile;
        public readonly ITacticalAISkillMetadataProvider SkillMetadataProvider;
        public readonly TacticalAIFixedBudget Budget;
        public readonly int AITeamIndex;
        public bool OpponentResponseReachable;
        public bool WatchdogExpired;
        public int EvaluatedLeafCount;

        public SearchContext(
            BattleSnapshot rootSnapshot,
            TacticalAIResolvedProfile profile,
            ITacticalAISkillMetadataProvider skillMetadataProvider,
            int aiTeamIndex)
        {
            RootSnapshot = rootSnapshot;
            Profile = profile;
            SkillMetadataProvider = skillMetadataProvider;
            Budget = new TacticalAIFixedBudget(profile);
            AITeamIndex = aiTeamIndex;
        }
    }

    sealed class SearchBranchResult
    {
        public float Score;
        public int CompletedDepth;
        public int EvaluatedLeafCount;
        public bool CoveredOpponentResponse;
    }

    struct ScoredCandidate
    {
        public TacticalAIActionIntent Intent;
        public float Score;
        public string StableOrderKey;
    }

    struct ScoredRootIntent
    {
        public TacticalAIActionIntent Intent;
        public float Score;
        public int CompletedDepth;
        public int EvaluatedLeafCount;
        public bool CoveredOpponentResponse;
        public string StableOrderKey;
    }
}

public static class TacticalAISearchCandidateExpander
{
    public static List<TacticalAIActionIntent> BuildSearchCandidates(
        BattleSnapshot snapshot,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        TacticalAICandidateGenerationOptions options = BuildCandidateOptions(profile);
        List<TacticalAIActionIntent> rawCandidates = TacticalAICandidateGenerator.GenerateCandidates(
            snapshot,
            options,
            skillMetadataProvider);

        List<TacticalAIActionIntent> expanded = new List<TacticalAIActionIntent>();
        for (int i = 0; i < rawCandidates.Count; i++)
        {
            TacticalAIActionIntent candidate = rawCandidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.ActionType == TacticalAIActionType.Skill)
            {
                AddSkillTargetCandidates(expanded, snapshot, candidate, profile, skillMetadataProvider);
                continue;
            }

            expanded.Add(candidate);
        }

        expanded.Sort(CompareIntentStableOrder);
        ApplyProfileCandidateCaps(expanded, profile);
        return expanded;
    }

    static void ApplyProfileCandidateCaps(List<TacticalAIActionIntent> candidates, TacticalAIResolvedProfile profile)
    {
        if (candidates == null || profile == null)
        {
            return;
        }

        int skillLimit = Math.Min(profile.MaxCandidatesPerActionType, profile.MaxSkillCandidates);
        int moveLimit = Math.Min(profile.MaxCandidatesPerActionType, profile.MaxMoveCandidates);
        int attackLimit = Math.Min(profile.MaxCandidatesPerActionType, profile.MaxAttackCandidates);
        int defaultLimit = profile.MaxCandidatesPerActionType;

        int skillCount = 0;
        int moveCount = 0;
        int rangedAttackCount = 0;
        int moveAndAttackCount = 0;
        int waitCount = 0;
        int defendCount = 0;

        List<TacticalAIActionIntent> kept = new List<TacticalAIActionIntent>();
        for (int i = 0; i < candidates.Count; i++)
        {
            TacticalAIActionIntent candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.ActionType == TacticalAIActionType.Skill && ++skillCount > skillLimit)
            {
                continue;
            }
            else if (candidate.ActionType == TacticalAIActionType.Move && ++moveCount > moveLimit)
            {
                continue;
            }
            else if (candidate.ActionType == TacticalAIActionType.BasicRangedAttack && ++rangedAttackCount > attackLimit)
            {
                continue;
            }
            else if (candidate.ActionType == TacticalAIActionType.MoveAndAttack && ++moveAndAttackCount > attackLimit)
            {
                continue;
            }
            else if (candidate.ActionType == TacticalAIActionType.Wait && ++waitCount > defaultLimit)
            {
                continue;
            }
            else if (candidate.ActionType == TacticalAIActionType.Defend && ++defendCount > defaultLimit)
            {
                continue;
            }

            kept.Add(candidate);
        }

        candidates.Clear();
        candidates.AddRange(kept);
        candidates.Sort(CompareIntentStableOrder);
    }

    static void AddSkillTargetCandidates(
        List<TacticalAIActionIntent> expanded,
        BattleSnapshot snapshot,
        TacticalAIActionIntent skillCandidate,
        TacticalAIResolvedProfile profile,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        BattleUnitSnapshot actor = TacticalAISnapshotQuery.FindUnit(snapshot, skillCandidate.ActorUnitId);
        SkillDefinitionSpec definition = ResolveSkillSpec(skillCandidate.SkillId, skillMetadataProvider);
        if (actor == null || definition == null)
        {
            return;
        }

        SkillContext context = SkillContext.Create(snapshot, actor.RuntimeUnitId, definition, skillCandidate.SkillSlot);
        SkillValidationResult canUse = SkillRules.CanUse(context);
        if (canUse.IsValid == false)
        {
            return;
        }

        TargetingRuleData targeting = definition.TargetingRule;
        int targetCount = targeting != null ? Math.Max(0, targeting.targetCount) : 0;
        int limit = Math.Max(1, profile != null ? profile.MaxSkillCandidates : TacticalAICandidateGenerationOptions.Default.MaxSkillCandidates);
        List<SkillCast> validatedCasts = new List<SkillCast>();
        AddValidatedSkillCasts(validatedCasts, context, skillCandidate.SkillId, new List<HexCoord>(), targetCount, limit);

        for (int i = 0; i < validatedCasts.Count; i++)
        {
            TacticalAIActionIntent intent = CreateValidatedSkillIntent(skillCandidate, context, validatedCasts[i]);
            if (HasMeaningfulSkillPreview(intent != null ? intent.PreviewResult : null))
            {
                expanded.Add(intent);
            }
        }
    }

    static void AddValidatedSkillCasts(
        List<SkillCast> validatedCasts,
        SkillContext context,
        string skillId,
        List<HexCoord> selected,
        int targetCount,
        int limit)
    {
        if (validatedCasts == null || context == null || validatedCasts.Count >= limit)
        {
            return;
        }

        if (selected.Count >= targetCount)
        {
            SkillValidationResult validation = SkillRules.Validate(
                new SkillUse(context.ActorUnitId, skillId, selected),
                context);
            if (validation.IsValid && validation.Cast != null)
            {
                validatedCasts.Add(validation.Cast.Clone());
            }

            return;
        }

        List<SkillTarget> legalTargets = SkillRules.GetTargets(context, selected);
        legalTargets.Sort(CompareSkillTargets);
        for (int i = 0; i < legalTargets.Count && validatedCasts.Count < limit; i++)
        {
            SkillTarget target = legalTargets[i];
            if (target == null || target.Hex == null)
            {
                continue;
            }

            List<HexCoord> nextSelected = new List<HexCoord>(selected);
            nextSelected.Add(new HexCoord(target.Hex.C, target.Hex.R));
            AddValidatedSkillCasts(validatedCasts, context, skillId, nextSelected, targetCount, limit);
        }
    }

    static TacticalAIActionIntent CreateValidatedSkillIntent(
        TacticalAIActionIntent source,
        SkillContext context,
        SkillCast cast)
    {
        SkillResult preview = SkillRules.Preview(cast, context);
        string targetUnitId = cast != null ? FirstOrEmpty(cast.TargetUnitIds) : string.Empty;
        TacticalAIHexCoordinate targetHex = ToAIHex(FirstHex(cast != null ? cast.SelectedHexes : null));
        TacticalAIHexCoordinate destinationHex = ToAIHex(cast != null ? cast.DestinationHex : null);
        return new TacticalAIActionIntent
        {
            ActionType = TacticalAIActionType.Skill,
            ActorUnitId = source.ActorUnitId,
            SourceHex = CloneCoordinate(source.SourceHex),
            DestinationHex = destinationHex,
            TargetUnitId = targetUnitId,
            TargetHex = targetHex,
            SkillSlot = source.SkillSlot,
            SkillId = source.SkillId ?? string.Empty,
            ValidatedSkillCast = cast != null ? cast.Clone() : null,
            PreviewResult = preview,
            PredictedPriority = source.PredictedPriority,
            StableOrderKey = BuildSkillStableOrderKey(source, cast)
        };
    }

    static bool HasMeaningfulSkillPreview(SkillResult preview)
    {
        if (preview == null || preview.Events == null)
        {
            return false;
        }

        for (int i = 0; i < preview.Events.Count; i++)
        {
            SkillResultEvent resultEvent = preview.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            switch (resultEvent.EventType)
            {
                case SkillResultEventType.DamageApplied:
                case SkillResultEventType.StatusApplied:
                    if (string.IsNullOrEmpty(resultEvent.TargetUnitId) == false)
                    {
                        return true;
                    }
                    break;
                case SkillResultEventType.UnitMoved:
                case SkillResultEventType.TrapPlaced:
                case SkillResultEventType.UnitSpawned:
                    if (resultEvent.Hex != null)
                    {
                        return true;
                    }
                    break;
                case SkillResultEventType.StackAmountChanged:
                case SkillResultEventType.HpCostApplied:
                    if (resultEvent.Amount != 0)
                    {
                        return true;
                    }
                    break;
                case SkillResultEventType.StanceChanged:
                    return true;
            }
        }

        return false;
    }

    static SkillDefinitionSpec ResolveSkillSpec(
        string skillId,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        ITacticalAISkillSpecProvider specProvider = skillMetadataProvider as ITacticalAISkillSpecProvider;
        SkillDefinitionSpec spec;
        if (specProvider != null && specProvider.TryGetSkillSpec(skillId, out spec))
        {
            return spec;
        }

        SkillDefinitionAsset definition = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillId) : null;
        return SkillDefinitionSpec.FromAsset(definition);
    }

    static string BuildSkillStableOrderKey(TacticalAIActionIntent source, SkillCast cast)
    {
        return "Skill|" + (source != null ? source.ActorUnitId ?? string.Empty : string.Empty) + "|" +
            (source != null ? source.SkillSlot : -1) + "|" +
            (source != null ? source.SkillId ?? string.Empty : string.Empty) + "|" +
            BuildHexListKey(cast != null ? cast.SelectedHexes : null) + "|" +
            BuildStringListKey(cast != null ? cast.TargetUnitIds : null) + "|" +
            GetHexKey(cast != null ? cast.DestinationHex : null) + "|" +
            GetHexKey(cast != null ? cast.ImpactHex : null);
    }

    static int CompareSkillTargets(SkillTarget left, SkillTarget right)
    {
        string leftUnit = left != null ? left.UnitId ?? string.Empty : string.Empty;
        string rightUnit = right != null ? right.UnitId ?? string.Empty : string.Empty;
        int unitCompare = string.CompareOrdinal(leftUnit, rightUnit);
        if (unitCompare != 0)
        {
            return unitCompare;
        }

        return string.CompareOrdinal(GetHexKey(left != null ? left.Hex : null), GetHexKey(right != null ? right.Hex : null));
    }

    static string BuildHexListKey(List<HexCoord> hexes)
    {
        if (hexes == null || hexes.Count == 0)
        {
            return string.Empty;
        }

        List<string> keys = new List<string>();
        for (int i = 0; i < hexes.Count; i++)
        {
            keys.Add(GetHexKey(hexes[i]));
        }

        return string.Join(";", keys.ToArray());
    }

    static string BuildStringListKey(List<string> values)
    {
        if (values == null || values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.ToArray());
    }

    static string GetHexKey(HexCoord hex)
    {
        return hex == null ? string.Empty : hex.C + "," + hex.R;
    }

    static string FirstOrEmpty(List<string> values)
    {
        return values != null && values.Count > 0 ? values[0] ?? string.Empty : string.Empty;
    }

    static HexCoord FirstHex(List<HexCoord> hexes)
    {
        return hexes != null && hexes.Count > 0 ? hexes[0] : null;
    }

    static TacticalAIHexCoordinate ToAIHex(HexCoord hex)
    {
        return hex == null ? null : new TacticalAIHexCoordinate(hex.C, hex.R);
    }

    static TacticalAISkillMetadata ResolveSkillMetadata(
        string skillId,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        TacticalAISkillMetadata metadata;
        if (skillMetadataProvider != null && skillMetadataProvider.TryGetSkillMetadata(skillId, out metadata) && metadata != null)
        {
            metadata.IsRepeatableToggle = metadata.IsRepeatableToggle || TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId);
            return metadata;
        }

        return new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
        };
    }

    static TacticalAICandidateGenerationOptions BuildCandidateOptions(TacticalAIResolvedProfile profile)
    {
        if (profile == null)
        {
            return TacticalAICandidateGenerationOptions.Default;
        }

        return new TacticalAICandidateGenerationOptions
        {
            MaxCandidatesPerActionType = profile.MaxCandidatesPerActionType,
            MaxSkillCandidates = profile.MaxSkillCandidates,
            MaxMoveCandidates = profile.MaxMoveCandidates,
            MaxAttackCandidates = profile.MaxAttackCandidates
        };
    }

    static int CompareTargetsByDurability(BattleUnitSnapshot left, BattleUnitSnapshot right)
    {
        int durabilityCompare = TacticalAISnapshotQuery.GetUnitDurability(left).CompareTo(TacticalAISnapshotQuery.GetUnitDurability(right));
        if (durabilityCompare != 0)
        {
            return durabilityCompare;
        }

        return string.CompareOrdinal(left.RuntimeUnitId, right.RuntimeUnitId);
    }

    static int CompareIntentStableOrder(TacticalAIActionIntent left, TacticalAIActionIntent right)
    {
        return string.CompareOrdinal(
            left != null ? left.StableOrderKey : string.Empty,
            right != null ? right.StableOrderKey : string.Empty);
    }

    static TacticalAIHexCoordinate CloneCoordinate(TacticalAIHexCoordinate coordinate)
    {
        return coordinate == null ? null : new TacticalAIHexCoordinate(coordinate.C, coordinate.R);
    }
}

public static class BattleSnapshotTurnOrderEstimator
{
    public static BattleSnapshot AdvanceToNextOpportunity(BattleSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return null;
        }

        string nextUnitId = EstimateNextActiveUnitId(snapshot);
        BattleTurnStateSnapshot turnState = CloneTurnState(snapshot.TurnState);
        IEnumerable<BattleUnitSnapshot> units = snapshot.Units;

        if (string.IsNullOrEmpty(nextUnitId))
        {
            units = ResetUnitsForNewRound(snapshot.Units);
            turnState.RoundNumber += 1;
            BattleSnapshot resetSnapshot = BattleSnapshotBuilder.Build(
                snapshot.MapWidth,
                snapshot.MapHeight,
                snapshot.Hexes,
                units,
                string.Empty,
                turnState);
            nextUnitId = EstimateFutureRoundActiveUnitId(resetSnapshot);
            return BattleSnapshotBuilder.Build(
                resetSnapshot.MapWidth,
                resetSnapshot.MapHeight,
                resetSnapshot.Hexes,
                resetSnapshot.Units,
                nextUnitId,
                resetSnapshot.TurnState);
        }

        return BattleSnapshotBuilder.Build(
            snapshot.MapWidth,
            snapshot.MapHeight,
            snapshot.Hexes,
            snapshot.Units,
            nextUnitId,
            turnState);
    }

    public static string EstimateNextActiveUnitId(BattleSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return string.Empty;
        }

        List<int> teams = TacticalAISnapshotQuery.GetTeamIndexes(snapshot);
        if (teams.Count == 0)
        {
            return string.Empty;
        }

        List<BattleUnitSnapshot> teamCandidates = new List<BattleUnitSnapshot>();
        for (int i = 0; i < teams.Count; i++)
        {
            BattleUnitSnapshot candidate = SelectCurrentRoundTeamCandidate(snapshot, teams[i]);
            if (candidate != null)
            {
                teamCandidates.Add(candidate);
            }
        }

        return SelectCrossTeamCandidate(snapshot, teamCandidates, false);
    }

    static string EstimateFutureRoundActiveUnitId(BattleSnapshot snapshot)
    {
        List<int> teams = TacticalAISnapshotQuery.GetTeamIndexes(snapshot);
        List<BattleUnitSnapshot> teamCandidates = new List<BattleUnitSnapshot>();
        for (int i = 0; i < teams.Count; i++)
        {
            BattleUnitSnapshot candidate = SelectFutureRoundTeamCandidate(snapshot, teams[i]);
            if (candidate != null)
            {
                teamCandidates.Add(candidate);
            }
        }

        return SelectCrossTeamCandidate(snapshot, teamCandidates, true);
    }

    static BattleUnitSnapshot SelectCurrentRoundTeamCandidate(BattleSnapshot snapshot, int teamIndex)
    {
        BattleUnitSnapshot bestNonWaited = null;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (CanReceiveCurrentRoundTurn(unit, teamIndex) == false || unit.Waited)
            {
                continue;
            }

            if (bestNonWaited == null || CompareHigherInitiative(unit, bestNonWaited, snapshot) < 0)
            {
                bestNonWaited = unit;
            }
        }

        if (bestNonWaited != null)
        {
            return bestNonWaited;
        }

        BattleUnitSnapshot bestWaited = null;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (CanReceiveCurrentRoundTurn(unit, teamIndex) == false)
            {
                continue;
            }

            if (bestWaited == null || CompareLowerInitiative(unit, bestWaited, snapshot) < 0)
            {
                bestWaited = unit;
            }
        }

        return bestWaited;
    }

    static BattleUnitSnapshot SelectFutureRoundTeamCandidate(BattleSnapshot snapshot, int teamIndex)
    {
        BattleUnitSnapshot best = null;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || unit.TeamIndex != teamIndex || unit.IsAlive == false || unit.Amount <= 0)
            {
                continue;
            }

            if (best == null || CompareHigherInitiative(unit, best, snapshot) < 0)
            {
                best = unit;
            }
        }

        return best;
    }

    static string SelectCrossTeamCandidate(BattleSnapshot snapshot, List<BattleUnitSnapshot> candidates, bool futureRound)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return string.Empty;
        }

        candidates.Sort((left, right) =>
        {
            int compare = futureRound
                ? CompareHigherInitiative(left, right, snapshot)
                : CompareCurrentRoundCrossTeam(left, right, snapshot);
            return compare;
        });

        return candidates[0].RuntimeUnitId;
    }

    static int CompareCurrentRoundCrossTeam(BattleUnitSnapshot left, BattleUnitSnapshot right, BattleSnapshot snapshot)
    {
        if (left.Waited != right.Waited)
        {
            return left.Waited ? 1 : -1;
        }

        if (left.Waited && right.Waited)
        {
            return CompareLowerInitiative(left, right, snapshot);
        }

        return CompareHigherInitiative(left, right, snapshot);
    }

    static int CompareHigherInitiative(BattleUnitSnapshot left, BattleUnitSnapshot right, BattleSnapshot snapshot)
    {
        int initiativeCompare = right.Initiative.CompareTo(left.Initiative);
        if (initiativeCompare != 0)
        {
            return initiativeCompare;
        }

        int speedCompare = right.MovementSpeed.CompareTo(left.MovementSpeed);
        if (speedCompare != 0)
        {
            return speedCompare;
        }

        return CompareRosterOrder(left, right);
    }

    static int CompareLowerInitiative(BattleUnitSnapshot left, BattleUnitSnapshot right, BattleSnapshot snapshot)
    {
        int initiativeCompare = left.Initiative.CompareTo(right.Initiative);
        if (initiativeCompare != 0)
        {
            return initiativeCompare;
        }

        int speedCompare = left.MovementSpeed.CompareTo(right.MovementSpeed);
        if (speedCompare != 0)
        {
            return speedCompare;
        }

        return CompareRosterOrder(left, right);
    }

    static int CompareRosterOrder(BattleUnitSnapshot left, BattleUnitSnapshot right)
    {
        int teamCompare = left.TeamIndex.CompareTo(right.TeamIndex);
        if (teamCompare != 0)
        {
            return teamCompare;
        }

        return left.RosterIndexWithinTeam.CompareTo(right.RosterIndexWithinTeam);
    }

    static bool CanReceiveCurrentRoundTurn(BattleUnitSnapshot unit, int teamIndex)
    {
        return unit != null &&
            unit.TeamIndex == teamIndex &&
            unit.IsAlive &&
            unit.Amount > 0 &&
            unit.Moved == false;
    }

    static IEnumerable<BattleUnitSnapshot> ResetUnitsForNewRound(IEnumerable<BattleUnitSnapshot> units)
    {
        List<BattleUnitSnapshot> result = new List<BattleUnitSnapshot>();
        if (units == null)
        {
            return result;
        }

        foreach (BattleUnitSnapshot unit in units)
        {
            BattleUnitSnapshot clone = TacticalAISnapshotQuery.CloneUnit(unit);
            if (clone.IsAlive && clone.Amount > 0)
            {
                clone.Moved = false;
                clone.MovedThisTurn = false;
                clone.UsedSkillThisTurn = false;
                clone.UsedSkillIdsThisTurn.Clear();
                clone.CanMoveAfterSkillThisTurn = false;
                clone.Waited = false;
            }

            result.Add(clone);
        }

        return result;
    }

    static BattleTurnStateSnapshot CloneTurnState(BattleTurnStateSnapshot turnState)
    {
        if (turnState == null)
        {
            return new BattleTurnStateSnapshot();
        }

        return new BattleTurnStateSnapshot
        {
            RoundNumber = turnState.RoundNumber,
            IsResolvingNewTurnSequence = turnState.IsResolvingNewTurnSequence,
            IsActionBlocking = turnState.IsActionBlocking,
            ActiveActionKind = turnState.ActiveActionKind ?? string.Empty
        };
    }
}

public static class TacticalAISnapshotSimulator
{
    public static BattleSnapshot ApplyIntent(
        BattleSnapshot snapshot,
        TacticalAIActionIntent intent,
        ITacticalAISkillMetadataProvider skillMetadataProvider = null)
    {
        if (snapshot == null || intent == null)
        {
            return snapshot;
        }

        List<BattleUnitSnapshot> units = TacticalAISnapshotQuery.CloneUnits(snapshot.Units);
        List<BattleHexSnapshot> hexes = TacticalAISnapshotQuery.CloneHexes(snapshot.Hexes);
        BattleUnitSnapshot actor = FindMutableUnit(units, intent.ActorUnitId);
        if (actor == null || actor.IsAlive == false || actor.Amount <= 0)
        {
            return Rebuild(snapshot, hexes, units, snapshot.ActiveUnitId);
        }

        switch (intent.ActionType)
        {
            case TacticalAIActionType.Move:
                ApplyMove(actor, intent.DestinationHex, hexes);
                actor.Moved = true;
                actor.MovedThisTurn = true;
                break;
            case TacticalAIActionType.MoveAndAttack:
                ApplyMove(actor, intent.DestinationHex, hexes);
                ApplyDamageToTarget(units, intent.TargetUnitId, TacticalAIDamagePredictor.PredictAverageDamage(actor, FindMutableUnit(units, intent.TargetUnitId)));
                actor.Moved = true;
                actor.MovedThisTurn = true;
                break;
            case TacticalAIActionType.BasicRangedAttack:
                ApplyDamageToTarget(units, intent.TargetUnitId, TacticalAIDamagePredictor.PredictAverageDamage(actor, FindMutableUnit(units, intent.TargetUnitId)));
                actor.Moved = true;
                break;
            case TacticalAIActionType.Wait:
                actor.Waited = true;
                break;
            case TacticalAIActionType.Defend:
                actor.Moved = true;
                actor.MovedThisTurn = true;
                break;
            case TacticalAIActionType.Skill:
                ApplySkill(snapshot, actor, units, hexes, intent, skillMetadataProvider);
                break;
        }

        RefreshOccupants(hexes, units);
        return Rebuild(snapshot, hexes, units, snapshot.ActiveUnitId);
    }

    static void ApplySkill(
        BattleSnapshot sourceSnapshot,
        BattleUnitSnapshot actor,
        List<BattleUnitSnapshot> units,
        List<BattleHexSnapshot> hexes,
        TacticalAIActionIntent intent,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        SkillCast cast = intent.ValidatedSkillCast != null ? intent.ValidatedSkillCast.Clone() : null;
        SkillDefinitionSpec definition = ResolveSkillSpec(intent.SkillId, skillMetadataProvider);
        if (cast == null && definition != null)
        {
            SkillContext context = SkillContext.Create(sourceSnapshot, actor.RuntimeUnitId, definition, intent.SkillSlot);
            SkillValidationResult validation = SkillRules.Validate(
                new SkillUse(actor.RuntimeUnitId, intent.SkillId, new List<HexCoord>()),
                context);
            cast = validation.IsValid ? validation.Cast : null;
        }

        if (cast == null)
        {
            return;
        }

        SkillResult result = intent.PreviewResult ?? SkillRules.Preview(
            cast,
            SkillContext.Create(sourceSnapshot, actor.RuntimeUnitId, definition, intent.SkillSlot));
        ApplySkillResultEvents(result, actor, units, hexes, cast);

        if (actor.UsedSkillIdsThisTurn == null)
        {
            actor.UsedSkillIdsThisTurn = new List<string>();
        }

        bool repeatable = cast.RepeatableInTurn;
        if (repeatable == false && actor.UsedSkillIdsThisTurn.Contains(intent.SkillId) == false)
        {
            actor.UsedSkillIdsThisTurn.Add(intent.SkillId ?? string.Empty);
        }

        actor.UsedSkillThisTurn = repeatable == false;
        actor.CanMoveAfterSkillThisTurn = cast.CanMoveAfterUse;

        if (repeatable == false && cast.ConsumesTurn && cast.CanMoveAfterUse == false)
        {
            actor.Moved = true;
        }
    }

    static void ApplySkillResultEvents(
        SkillResult result,
        BattleUnitSnapshot actor,
        List<BattleUnitSnapshot> units,
        List<BattleHexSnapshot> hexes,
        SkillCast cast)
    {
        if (result == null || result.Events == null)
        {
            return;
        }

        for (int i = 0; i < result.Events.Count; i++)
        {
            SkillResultEvent resultEvent = result.Events[i];
            if (resultEvent == null)
            {
                continue;
            }

            switch (resultEvent.EventType)
            {
                case SkillResultEventType.DamageApplied:
                    ApplyDamageToTarget(units, resultEvent.TargetUnitId, ResolveSkillDamage(actor, FindMutableUnit(units, resultEvent.TargetUnitId), resultEvent.Amount, cast));
                    break;
                case SkillResultEventType.HpCostApplied:
                    ApplyDamageToTarget(units, actor.RuntimeUnitId, Math.Max(0, resultEvent.Amount));
                    break;
                case SkillResultEventType.UnitMoved:
                    BattleUnitSnapshot movedUnit = FindMutableUnit(units, string.IsNullOrEmpty(resultEvent.TargetUnitId) ? actor.RuntimeUnitId : resultEvent.TargetUnitId);
                    if (movedUnit != null && resultEvent.Hex != null)
                    {
                        movedUnit.C = resultEvent.Hex.C;
                        movedUnit.R = resultEvent.Hex.R;
                    }
                    break;
                case SkillResultEventType.TrapPlaced:
                    BattleHexSnapshot trapHex = resultEvent.Hex != null ? FindMutableHex(hexes, resultEvent.Hex.C, resultEvent.Hex.R) : null;
                    if (trapHex != null)
                    {
                        trapHex.TrapName = string.IsNullOrEmpty(resultEvent.TrapId) ? resultEvent.SkillId : resultEvent.TrapId;
                        trapHex.TrapSourceUnitId = actor.RuntimeUnitId;
                    }
                    break;
                case SkillResultEventType.StackAmountChanged:
                    actor.Amount = Math.Max(0, actor.Amount + resultEvent.Amount);
                    actor.IsAlive = actor.Amount > 0;
                    break;
                case SkillResultEventType.StatusApplied:
                    ApplyStatusToTarget(units, actor, resultEvent, cast);
                    break;
                case SkillResultEventType.UnitSpawned:
                    ApplySpawnToSnapshot(units, hexes, actor, resultEvent, cast);
                    break;
                case SkillResultEventType.StanceChanged:
                    ApplyStanceToSnapshot(actor, cast);
                    break;
            }
        }
    }

    static void ApplyStanceToSnapshot(BattleUnitSnapshot actor, SkillCast cast)
    {
        if (actor == null || cast == null || string.IsNullOrEmpty(cast.SkillId))
        {
            return;
        }

        if (cast.SkillId.StartsWith("Range_Stance", StringComparison.Ordinal))
        {
            actor.IsRange = true;
            ReplaceStanceSkillId(actor, cast.SkillId, "Melee_Stance");
        }
        else if (cast.SkillId.StartsWith("Melee_Stance", StringComparison.Ordinal))
        {
            actor.IsRange = false;
            ReplaceStanceSkillId(actor, cast.SkillId, "Range_Stance");
        }
    }

    static void ReplaceStanceSkillId(BattleUnitSnapshot actor, string currentSkillId, string replacementPrefix)
    {
        if (actor == null ||
            actor.SkillIdsBySlot == null ||
            string.IsNullOrEmpty(currentSkillId) ||
            string.IsNullOrEmpty(replacementPrefix))
        {
            return;
        }

        int separator = currentSkillId.IndexOf('_', "Range_Stance".Length);
        if (separator < 0)
        {
            separator = currentSkillId.IndexOf('_', "Melee_Stance".Length);
        }

        if (separator < 0 || separator >= currentSkillId.Length)
        {
            return;
        }

        string replacementSkillId = replacementPrefix + currentSkillId.Substring(separator);
        for (int i = 0; i < actor.SkillIdsBySlot.Count; i++)
        {
            if (string.Equals(actor.SkillIdsBySlot[i], currentSkillId, StringComparison.Ordinal))
            {
                actor.SkillIdsBySlot[i] = replacementSkillId;
                return;
            }
        }
    }

    static void ApplyStatusToTarget(
        List<BattleUnitSnapshot> units,
        BattleUnitSnapshot actor,
        SkillResultEvent resultEvent,
        SkillCast cast)
    {
        BattleUnitSnapshot target = FindMutableUnit(
            units,
            string.IsNullOrEmpty(resultEvent.TargetUnitId) ? actor.RuntimeUnitId : resultEvent.TargetUnitId);
        SkillEffect effect = FirstStatusEffect(cast, resultEvent.StatusId);
        if (target == null || effect == null)
        {
            return;
        }

        if (target.Statuses == null)
        {
            target.Statuses = new List<BattleStatusSnapshot>();
        }

        target.Statuses.Add(new BattleStatusSnapshot
        {
            StatusId = string.IsNullOrEmpty(effect.statusId) ? cast.SkillId : effect.statusId,
            SourceSkillId = cast.SkillId,
            SourceUnitId = actor.RuntimeUnitId,
            RemainingDurationOrTurns = Math.Max(0, effect.durationTurns),
            HpModifier = effect.hpModifier,
            AttackModifier = effect.attackModifier,
            DefenseModifier = effect.defenseModifier,
            MovementModifier = effect.movementModifier,
            InitiativeModifier = effect.initiativeModifier,
            MaxDamageModifier = effect.maxDamageModifier,
            MinDamageModifier = effect.minDamageModifier,
            DamageOverTime = effect.damageOverTime,
            ResistanceModifier = effect.resistanceModifier,
            CounterAttacksModifier = effect.counterAttacksModifier,
            DamageModifier = effect.damageModifier,
            IsStackable = effect.isStackable
        });
    }

    static void ApplySpawnToSnapshot(
        List<BattleUnitSnapshot> units,
        List<BattleHexSnapshot> hexes,
        BattleUnitSnapshot actor,
        SkillResultEvent resultEvent,
        SkillCast cast)
    {
        if (units == null || actor == null)
        {
            return;
        }

        SkillEffect effect = FirstSpawnEffect(cast);
        HexCoord destination = resultEvent.Hex ?? (cast != null ? cast.DestinationHex : null);
        if (effect == null || destination == null)
        {
            return;
        }

        BattleHexSnapshot spawnHex = FindMutableHex(hexes, destination.C, destination.R);
        if (spawnHex == null || string.IsNullOrEmpty(spawnHex.OccupyingUnitId) == false)
        {
            return;
        }

        int spawnedAmount = effect.stackAmountDelta > 0 ? effect.stackAmountDelta : Math.Max(1, actor.Amount / 2);
        string runtimeId = actor.RuntimeUnitId + "-spawn-" + units.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        BattleUnitSnapshot spawned = new BattleUnitSnapshot
        {
            RuntimeUnitId = runtimeId,
            TeamIndex = actor.TeamIndex,
            RosterIndexWithinTeam = units.Count,
            UnitName = string.IsNullOrEmpty(effect.unitId) ? actor.UnitName : effect.unitId,
            UnitType = string.IsNullOrEmpty(effect.unitId) ? actor.UnitType : effect.unitId,
            C = destination.C,
            R = destination.R,
            Amount = spawnedAmount,
            TempHP = actor.BaseHP,
            BaseHP = actor.BaseHP,
            Attack = actor.Attack,
            Defense = actor.Defense,
            MovementSpeed = actor.MovementSpeed,
            Initiative = actor.Initiative,
            MinDamage = actor.MinDamage,
            MaxDamage = actor.MaxDamage,
            IsAlive = spawnedAmount > 0,
            IsRange = actor.IsRange,
            Moved = true,
            MovedThisTurn = true,
            UsedSkillIdsThisTurn = new List<string>(),
            CooldownsBySlot = new List<int>(),
            SkillIdsBySlot = new List<string>(),
            Statuses = new List<BattleStatusSnapshot>()
        };

        units.Add(spawned);
        spawnHex.OccupyingUnitId = runtimeId;
    }

    static int ResolveSkillDamage(BattleUnitSnapshot actor, BattleUnitSnapshot target, int eventAmount, SkillCast cast)
    {
        if (eventAmount > 0)
        {
            return eventAmount;
        }

        SkillEffect effect = FirstDamageEffect(cast);
        if (effect == null)
        {
            return 0;
        }

        int baseDamage = TacticalAIDamagePredictor.PredictAverageDamage(actor, target);
        if (effect.damageMode == SkillDamageMode.FixedDamageThroughDefense && effect.fixedDamageValue > 0)
        {
            baseDamage = effect.fixedDamageValue;
        }

        return Math.Max(0, (int)Math.Ceiling(baseDamage * Math.Max(0f, effect.damageScale)));
    }

    static SkillEffect FirstDamageEffect(SkillCast cast)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].effectType == SkillEffectType.Damage)
            {
                return effects[i];
            }
        }

        return null;
    }

    static SkillEffect FirstStatusEffect(SkillCast cast, string statusId)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            SkillEffect effect = effects[i];
            if (effect == null || effect.effectType != SkillEffectType.ApplyStatus)
            {
                continue;
            }

            if (string.IsNullOrEmpty(statusId) || string.Equals(effect.statusId, statusId, StringComparison.Ordinal))
            {
                return effect;
            }
        }

        return null;
    }

    static SkillEffect FirstSpawnEffect(SkillCast cast)
    {
        SkillEffect[] effects = cast != null ? cast.Effects : null;
        if (effects == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].effectType == SkillEffectType.SpawnUnit)
            {
                return effects[i];
            }
        }

        return null;
    }

    static SkillDefinitionSpec ResolveSkillSpec(
        string skillId,
        ITacticalAISkillMetadataProvider skillMetadataProvider)
    {
        ITacticalAISkillSpecProvider specProvider = skillMetadataProvider as ITacticalAISkillSpecProvider;
        SkillDefinitionSpec spec;
        if (specProvider != null && specProvider.TryGetSkillSpec(skillId, out spec))
        {
            return spec;
        }

        SkillDefinitionAsset definition = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillId) : null;
        return SkillDefinitionSpec.FromAsset(definition);
    }

    static void ApplyMove(BattleUnitSnapshot actor, TacticalAIHexCoordinate destinationHex, List<BattleHexSnapshot> hexes)
    {
        if (actor == null || destinationHex == null)
        {
            return;
        }

        actor.C = destinationHex.C;
        actor.R = destinationHex.R;
    }

    static void ApplyDamageToTarget(List<BattleUnitSnapshot> units, string targetUnitId, int damage)
    {
        BattleUnitSnapshot target = FindMutableUnit(units, targetUnitId);
        if (target == null || damage <= 0)
        {
            return;
        }

        int baseHp = Math.Max(1, target.BaseHP);
        int totalHp = Math.Max(0, (Math.Max(0, target.Amount - 1) * baseHp) + Math.Max(0, target.TempHP));
        int remainingHp = Math.Max(0, totalHp - damage);
        if (remainingHp <= 0)
        {
            target.Amount = 0;
            target.TempHP = 0;
            target.IsAlive = false;
            target.Moved = true;
            return;
        }

        int fullStacksBeforeFront = (remainingHp - 1) / baseHp;
        target.Amount = fullStacksBeforeFront + 1;
        target.TempHP = remainingHp - (fullStacksBeforeFront * baseHp);
        target.IsAlive = target.Amount > 0;
    }

    static void RefreshOccupants(List<BattleHexSnapshot> hexes, List<BattleUnitSnapshot> units)
    {
        for (int i = 0; i < hexes.Count; i++)
        {
            hexes[i].OccupyingUnitId = string.Empty;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnitSnapshot unit = units[i];
            if (unit == null || unit.IsAlive == false || unit.Amount <= 0)
            {
                continue;
            }

            BattleHexSnapshot hex = TacticalAISnapshotQuery.FindHex(hexes, unit.C, unit.R);
            if (hex != null)
            {
                hex.OccupyingUnitId = unit.RuntimeUnitId;
            }
        }
    }

    static BattleUnitSnapshot FindMutableUnit(List<BattleUnitSnapshot> units, string unitId)
    {
        if (units == null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && string.Equals(units[i].RuntimeUnitId, unitId, StringComparison.Ordinal))
            {
                return units[i];
            }
        }

        return null;
    }

    static BattleHexSnapshot FindMutableHex(List<BattleHexSnapshot> hexes, int c, int r)
    {
        if (hexes == null)
        {
            return null;
        }

        for (int i = 0; i < hexes.Count; i++)
        {
            if (hexes[i] != null && hexes[i].C == c && hexes[i].R == r)
            {
                return hexes[i];
            }
        }

        return null;
    }

    static BattleSnapshot Rebuild(
        BattleSnapshot source,
        List<BattleHexSnapshot> hexes,
        List<BattleUnitSnapshot> units,
        string activeUnitId)
    {
        return BattleSnapshotBuilder.Build(
            source.MapWidth,
            source.MapHeight,
            hexes,
            units,
            activeUnitId,
            source.TurnState);
    }
}

public static class TacticalAIDamagePredictor
{
    public static int PredictAverageDamage(BattleUnitSnapshot attacker, BattleUnitSnapshot defender)
    {
        if (attacker == null || defender == null || attacker.IsAlive == false || attacker.Amount <= 0)
        {
            return 0;
        }

        float averageBaseDamage = (Math.Max(0, attacker.MinDamage) + Math.Max(0, attacker.MaxDamage)) * 0.5f;
        float attack = GetModifiedAttack(attacker);
        float defense = GetModifiedDefense(defender);
        float attackDefenseDelta = attack - defense;
        float multiplier = 1f;
        if (attackDefenseDelta > 0f)
        {
            multiplier += attackDefenseDelta * 0.04f;
        }
        else if (attackDefenseDelta < 0f)
        {
            multiplier += attackDefenseDelta * 0.014f;
        }

        multiplier = Math.Max(0.1f, multiplier);
        return (int)Math.Ceiling(averageBaseDamage * attacker.Amount * multiplier);
    }

    static float GetModifiedAttack(BattleUnitSnapshot unit)
    {
        int modifier = 0;
        if (unit != null && unit.Statuses != null)
        {
            for (int i = 0; i < unit.Statuses.Count; i++)
            {
                modifier += unit.Statuses[i] != null ? unit.Statuses[i].AttackModifier : 0;
            }
        }

        return Math.Max(0, (unit != null ? unit.Attack : 0) + modifier);
    }

    static float GetModifiedDefense(BattleUnitSnapshot unit)
    {
        int modifier = 0;
        if (unit != null && unit.Statuses != null)
        {
            for (int i = 0; i < unit.Statuses.Count; i++)
            {
                modifier += unit.Statuses[i] != null ? unit.Statuses[i].DefenseModifier : 0;
            }
        }

        return Math.Max(0, (unit != null ? unit.Defense : 0) + modifier);
    }
}

public static class TacticalAISnapshotScorer
{
    public static float Score(
        BattleSnapshot baseline,
        BattleSnapshot current,
        int aiTeamIndex,
        TacticalAIResolvedProfile profile)
    {
        if (baseline == null || current == null)
        {
            return 0f;
        }

        TacticalAIScoringWeights weights = profile != null && profile.ScoringWeights != null
            ? profile.ScoringWeights
            : new TacticalAIScoringWeights();

        bool aiAlive = TacticalAISnapshotQuery.HasAliveTeam(current, aiTeamIndex);
        bool enemyAlive = TacticalAISnapshotQuery.HasAliveEnemy(current, aiTeamIndex);
        if (aiAlive && enemyAlive == false)
        {
            return weights.WinScore;
        }

        if (aiAlive == false)
        {
            return weights.LossScore;
        }

        float enemyValueRemoved = Math.Max(0f,
            TacticalAISnapshotQuery.GetTeamValue(baseline, -1, aiTeamIndex) -
            TacticalAISnapshotQuery.GetTeamValue(current, -1, aiTeamIndex));
        float ownValueLost = Math.Max(0f,
            TacticalAISnapshotQuery.GetTeamValue(baseline, aiTeamIndex, aiTeamIndex) -
            TacticalAISnapshotQuery.GetTeamValue(current, aiTeamIndex, aiTeamIndex));

        int enemyKills = Math.Max(0,
            TacticalAISnapshotQuery.GetDeadUnitCount(current, -1, aiTeamIndex) -
            TacticalAISnapshotQuery.GetDeadUnitCount(baseline, -1, aiTeamIndex));
        int ownLosses = Math.Max(0,
            TacticalAISnapshotQuery.GetDeadUnitCount(current, aiTeamIndex, aiTeamIndex) -
            TacticalAISnapshotQuery.GetDeadUnitCount(baseline, aiTeamIndex, aiTeamIndex));

        float score = 0f;
        score += enemyValueRemoved * weights.EnemyValueRemoved;
        score -= ownValueLost * weights.OwnValueLost;
        score += enemyKills * weights.EnemyStackKillBonus;
        score -= ownLosses * weights.OwnStackLossPenalty;
        score += (enemyValueRemoved - ownValueLost) * weights.DamageEfficiency;
        score += ScoreThreatControl(current, aiTeamIndex) * weights.ThreatControl;
        score += ScorePositionSafety(current, aiTeamIndex) * weights.PositionSafety;
        score += ScoreProgressTempo(baseline, current, aiTeamIndex) * weights.ProgressTempo;
        return score;
    }

    public static float GetActionBias(
        TacticalAIActionIntent intent,
        TacticalAIResolvedProfile profile,
        int actorTeamIndex,
        int aiTeamIndex)
    {
        if (intent == null || profile == null || profile.ActionTypeBiases == null)
        {
            return 0f;
        }

        float bias;
        switch (intent.ActionType)
        {
            case TacticalAIActionType.Skill:
                bias = profile.ActionTypeBiases.Skill;
                break;
            case TacticalAIActionType.BasicRangedAttack:
                bias = profile.ActionTypeBiases.Attack;
                break;
            case TacticalAIActionType.MoveAndAttack:
                bias = profile.ActionTypeBiases.MoveAndAttack;
                break;
            case TacticalAIActionType.Move:
                bias = profile.ActionTypeBiases.Move;
                break;
            case TacticalAIActionType.Defend:
                bias = profile.ActionTypeBiases.Defend;
                break;
            case TacticalAIActionType.Wait:
                bias = profile.ActionTypeBiases.Wait;
                break;
            default:
                bias = 0f;
                break;
        }

        return actorTeamIndex == aiTeamIndex ? bias : -bias;
    }

    static float ScoreThreatControl(BattleSnapshot snapshot, int aiTeamIndex)
    {
        float aiThreat = 0f;
        float enemyThreat = 0f;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || unit.IsAlive == false || unit.Amount <= 0)
            {
                continue;
            }

            float threatenedValue = CountThreatenedEnemies(snapshot, unit) * TacticalAISnapshotQuery.EstimateUnitValue(unit);
            if (unit.TeamIndex == aiTeamIndex)
            {
                aiThreat += threatenedValue;
            }
            else
            {
                enemyThreat += threatenedValue;
            }
        }

        return aiThreat - enemyThreat;
    }

    static float ScorePositionSafety(BattleSnapshot snapshot, int aiTeamIndex)
    {
        float exposedOwnValue = 0f;
        float exposedEnemyValue = 0f;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || unit.IsAlive == false || unit.Amount <= 0)
            {
                continue;
            }

            if (IsThreatenedByEnemy(snapshot, unit))
            {
                if (unit.TeamIndex == aiTeamIndex)
                {
                    exposedOwnValue += TacticalAISnapshotQuery.EstimateUnitValue(unit);
                }
                else
                {
                    exposedEnemyValue += TacticalAISnapshotQuery.EstimateUnitValue(unit);
                }
            }
        }

        return exposedEnemyValue - exposedOwnValue;
    }

    static float ScoreProgressTempo(BattleSnapshot baseline, BattleSnapshot current, int aiTeamIndex)
    {
        float baselineDistance = AverageNearestEnemyDistance(baseline, aiTeamIndex);
        float currentDistance = AverageNearestEnemyDistance(current, aiTeamIndex);
        if (baselineDistance <= 0f || currentDistance <= 0f)
        {
            return 0f;
        }

        return baselineDistance - currentDistance;
    }

    static int CountThreatenedEnemies(BattleSnapshot snapshot, BattleUnitSnapshot unit)
    {
        int count = 0;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot other = snapshot.Units[i];
            if (other == null || other.TeamIndex == unit.TeamIndex || other.IsAlive == false || other.Amount <= 0)
            {
                continue;
            }

            if (CanThreaten(unit, other))
            {
                count++;
            }
        }

        return count;
    }

    static bool IsThreatenedByEnemy(BattleSnapshot snapshot, BattleUnitSnapshot unit)
    {
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot other = snapshot.Units[i];
            if (other == null || other.TeamIndex == unit.TeamIndex || other.IsAlive == false || other.Amount <= 0)
            {
                continue;
            }

            if (CanThreaten(other, unit))
            {
                return true;
            }
        }

        return false;
    }

    static bool CanThreaten(BattleUnitSnapshot attacker, BattleUnitSnapshot target)
    {
        if (attacker.IsRange)
        {
            return true;
        }

        return TacticalAISnapshotQuery.HexDistance(attacker.C, attacker.R, target.C, target.R) <= Math.Max(1, attacker.MovementSpeed + 1);
    }

    static float AverageNearestEnemyDistance(BattleSnapshot snapshot, int aiTeamIndex)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || unit.TeamIndex != aiTeamIndex || unit.IsAlive == false || unit.Amount <= 0)
            {
                continue;
            }

            int distance = TacticalAISnapshotQuery.FindNearestEnemyDistance(snapshot, unit);
            if (distance < int.MaxValue / 2)
            {
                total += distance;
                count++;
            }
        }

        return count == 0 ? 0f : total / count;
    }
}

public static class TacticalAISnapshotQuery
{
    public static BattleUnitSnapshot FindUnit(BattleSnapshot snapshot, string unitId)
    {
        if (snapshot == null || snapshot.Units == null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && string.Equals(unit.RuntimeUnitId, unitId, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        return null;
    }

    public static BattleHexSnapshot FindHex(List<BattleHexSnapshot> hexes, int c, int r)
    {
        if (hexes == null)
        {
            return null;
        }

        for (int i = 0; i < hexes.Count; i++)
        {
            BattleHexSnapshot hex = hexes[i];
            if (hex != null && hex.C == c && hex.R == r)
            {
                return hex;
            }
        }

        return null;
    }

    public static List<BattleUnitSnapshot> FindAliveEnemies(BattleSnapshot snapshot, int teamIndex)
    {
        List<BattleUnitSnapshot> enemies = new List<BattleUnitSnapshot>();
        if (snapshot == null || snapshot.Units == null)
        {
            return enemies;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.TeamIndex != teamIndex && unit.IsAlive && unit.Amount > 0)
            {
                enemies.Add(unit);
            }
        }

        return enemies;
    }

    public static List<int> GetTeamIndexes(BattleSnapshot snapshot)
    {
        List<int> teams = new List<int>();
        if (snapshot == null || snapshot.Units == null)
        {
            return teams;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null || teams.Contains(unit.TeamIndex))
            {
                continue;
            }

            teams.Add(unit.TeamIndex);
        }

        teams.Sort();
        return teams;
    }

    public static bool HasAliveTeam(BattleSnapshot snapshot, int teamIndex)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return false;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.TeamIndex == teamIndex && unit.IsAlive && unit.Amount > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAliveEnemy(BattleSnapshot snapshot, int aiTeamIndex)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return false;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit != null && unit.TeamIndex != aiTeamIndex && unit.IsAlive && unit.Amount > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static float GetTeamValue(BattleSnapshot snapshot, int requestedTeamIndex, int aiTeamIndex)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return 0f;
        }

        float value = 0f;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null)
            {
                continue;
            }

            bool matches = requestedTeamIndex >= 0
                ? unit.TeamIndex == requestedTeamIndex
                : unit.TeamIndex != aiTeamIndex;
            if (matches)
            {
                value += EstimateUnitValue(unit);
            }
        }

        return value;
    }

    public static int GetDeadUnitCount(BattleSnapshot snapshot, int requestedTeamIndex, int aiTeamIndex)
    {
        if (snapshot == null || snapshot.Units == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot unit = snapshot.Units[i];
            if (unit == null)
            {
                continue;
            }

            bool matches = requestedTeamIndex >= 0
                ? unit.TeamIndex == requestedTeamIndex
                : unit.TeamIndex != aiTeamIndex;
            if (matches && (unit.IsAlive == false || unit.Amount <= 0))
            {
                count++;
            }
        }

        return count;
    }

    public static float EstimateUnitValue(BattleUnitSnapshot unit)
    {
        if (unit == null || unit.IsAlive == false || unit.Amount <= 0)
        {
            return 0f;
        }

        int hp = Math.Max(1, unit.BaseHP + SumStatus(unit, StatusValueKind.Hp));
        int attack = Math.Max(0, unit.Attack + SumStatus(unit, StatusValueKind.Attack));
        int defense = Math.Max(0, unit.Defense + SumStatus(unit, StatusValueKind.Defense));
        int minDamage = Math.Max(0, unit.MinDamage + SumStatus(unit, StatusValueKind.MinDamage));
        int maxDamage = Math.Max(0, unit.MaxDamage + SumStatus(unit, StatusValueKind.MaxDamage));
        float averageDamage = (minDamage + maxDamage) * 0.5f;
        return unit.Amount * (hp + attack + defense + averageDamage);
    }

    enum StatusValueKind
    {
        Hp,
        Attack,
        Defense,
        MinDamage,
        MaxDamage
    }

    static int SumStatus(BattleUnitSnapshot unit, StatusValueKind kind)
    {
        int total = 0;
        if (unit == null || unit.Statuses == null)
        {
            return total;
        }

        for (int i = 0; i < unit.Statuses.Count; i++)
        {
            BattleStatusSnapshot status = unit.Statuses[i];
            if (status == null)
            {
                continue;
            }

            switch (kind)
            {
                case StatusValueKind.Hp:
                    total += status.HpModifier;
                    break;
                case StatusValueKind.Attack:
                    total += status.AttackModifier;
                    break;
                case StatusValueKind.Defense:
                    total += status.DefenseModifier;
                    break;
                case StatusValueKind.MinDamage:
                    total += status.MinDamageModifier;
                    break;
                case StatusValueKind.MaxDamage:
                    total += status.MaxDamageModifier;
                    break;
            }
        }

        return total;
    }

    public static int GetUnitDurability(BattleUnitSnapshot unit)
    {
        if (unit == null)
        {
            return int.MaxValue;
        }

        return Math.Max(0, unit.Amount - 1) * Math.Max(1, unit.BaseHP) + Math.Max(0, unit.TempHP);
    }

    public static int FindNearestEnemyDistance(BattleSnapshot snapshot, BattleUnitSnapshot unit)
    {
        int nearest = int.MaxValue;
        if (snapshot == null || unit == null || snapshot.Units == null)
        {
            return nearest;
        }

        for (int i = 0; i < snapshot.Units.Count; i++)
        {
            BattleUnitSnapshot other = snapshot.Units[i];
            if (other == null || other.TeamIndex == unit.TeamIndex || other.IsAlive == false || other.Amount <= 0)
            {
                continue;
            }

            nearest = Math.Min(nearest, HexDistance(unit.C, unit.R, other.C, other.R));
        }

        return nearest;
    }

    public static int HexDistance(int c1, int r1, int c2, int r2)
    {
        int s1 = -(c1 + r1);
        int s2 = -(c2 + r2);
        return Math.Max(Math.Abs(c1 - c2), Math.Max(Math.Abs(r1 - r2), Math.Abs(s1 - s2)));
    }

    public static List<BattleUnitSnapshot> CloneUnits(IEnumerable<BattleUnitSnapshot> units)
    {
        List<BattleUnitSnapshot> result = new List<BattleUnitSnapshot>();
        if (units == null)
        {
            return result;
        }

        foreach (BattleUnitSnapshot unit in units)
        {
            result.Add(CloneUnit(unit));
        }

        return result;
    }

    public static List<BattleHexSnapshot> CloneHexes(IEnumerable<BattleHexSnapshot> hexes)
    {
        List<BattleHexSnapshot> result = new List<BattleHexSnapshot>();
        if (hexes == null)
        {
            return result;
        }

        foreach (BattleHexSnapshot hex in hexes)
        {
            if (hex == null)
            {
                continue;
            }

            result.Add(new BattleHexSnapshot
            {
                C = hex.C,
                R = hex.R,
                IsWalkable = hex.IsWalkable,
                OccupyingUnitId = hex.OccupyingUnitId ?? string.Empty,
                TrapName = hex.TrapName ?? string.Empty,
                TrapRemainingDurationOrTurns = hex.TrapRemainingDurationOrTurns,
                TrapSourceUnitId = hex.TrapSourceUnitId ?? string.Empty
            });
        }

        return result;
    }

    public static BattleUnitSnapshot CloneUnit(BattleUnitSnapshot unit)
    {
        if (unit == null)
        {
            return null;
        }

        return new BattleUnitSnapshot
        {
            RuntimeUnitId = unit.RuntimeUnitId ?? string.Empty,
            TeamIndex = unit.TeamIndex,
            RosterIndexWithinTeam = unit.RosterIndexWithinTeam,
            UnitName = unit.UnitName ?? string.Empty,
            UnitType = unit.UnitType ?? string.Empty,
            C = unit.C,
            R = unit.R,
            Amount = unit.Amount,
            TempHP = unit.TempHP,
            BaseHP = unit.BaseHP,
            Attack = unit.Attack,
            Defense = unit.Defense,
            MovementSpeed = unit.MovementSpeed,
            Initiative = unit.Initiative,
            MinDamage = unit.MinDamage,
            MaxDamage = unit.MaxDamage,
            IsAlive = unit.IsAlive,
            IsRange = unit.IsRange,
            Waited = unit.Waited,
            Moved = unit.Moved,
            MovedThisTurn = unit.MovedThisTurn,
            UsedSkillThisTurn = unit.UsedSkillThisTurn,
            UsedSkillIdsThisTurn = unit.UsedSkillIdsThisTurn == null
                ? new List<string>()
                : new List<string>(unit.UsedSkillIdsThisTurn),
            CanMoveAfterSkillThisTurn = unit.CanMoveAfterSkillThisTurn,
            CooldownsBySlot = unit.CooldownsBySlot == null
                ? new List<int>()
                : new List<int>(unit.CooldownsBySlot),
            SkillIdsBySlot = unit.SkillIdsBySlot == null
                ? new List<string>()
                : new List<string>(unit.SkillIdsBySlot),
            Statuses = CloneStatuses(unit.Statuses)
        };
    }

    static List<BattleStatusSnapshot> CloneStatuses(IEnumerable<BattleStatusSnapshot> statuses)
    {
        List<BattleStatusSnapshot> result = new List<BattleStatusSnapshot>();
        if (statuses == null)
        {
            return result;
        }

        foreach (BattleStatusSnapshot status in statuses)
        {
            if (status == null)
            {
                continue;
            }

            result.Add(new BattleStatusSnapshot
            {
                StatusId = status.StatusId ?? string.Empty,
                SourceSkillId = status.SourceSkillId ?? string.Empty,
                SourceUnitId = status.SourceUnitId ?? string.Empty,
                RemainingDurationOrTurns = status.RemainingDurationOrTurns,
                HpModifier = status.HpModifier,
                AttackModifier = status.AttackModifier,
                DefenseModifier = status.DefenseModifier,
                MovementModifier = status.MovementModifier,
                InitiativeModifier = status.InitiativeModifier,
                MaxDamageModifier = status.MaxDamageModifier,
                MinDamageModifier = status.MinDamageModifier,
                DamageOverTime = status.DamageOverTime,
                ResistanceModifier = status.ResistanceModifier,
                CounterAttacksModifier = status.CounterAttacksModifier,
                DamageModifier = status.DamageModifier,
                IsStackable = status.IsStackable
            });
        }

        return result;
    }
}
