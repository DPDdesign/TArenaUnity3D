using System;
using System.Collections.Generic;
using System.Globalization;
using Random = System.Random;

public sealed class RewardMapMaterializedGenerator
{
    public const int RewardSlotCount = 3;
    private const int RunGoldFallbackAmount = 60;
    private const float BaseRewardGainRatio = 0.20f;
    private const float RawGrowthRewardMultiplier = 1.20f;
    private const float ArmyShapeRewardMultiplier = 1.00f;

    private readonly IRewardMapUnitDefinitionSource unitSource;
    private readonly RewardGeneratorRuleSet rewardRuleSet;
    private readonly RewardGeneratorValueContext valueContext;

    public RewardMapMaterializedGenerator(IRewardMapUnitDefinitionSource unitSource)
        : this(unitSource, null, null)
    {
    }

    public RewardMapMaterializedGenerator(
        IRewardMapUnitDefinitionSource unitSource,
        RewardGeneratorRuleSet rewardRuleSet,
        RewardGeneratorValueContext valueContext)
    {
        this.unitSource = unitSource;
        this.rewardRuleSet = rewardRuleSet;
        this.valueContext = valueContext;
    }

    public RewardMapChoiceViewData BuildChoice(
        string runId,
        int nodeId,
        int runSeed,
        int runSeedVersion,
        int runGoldBeforeReward,
        RewardMapArmySnapshot armyAfterBattle,
        RewardMapBattleResultSummary battleSummary)
    {
        return BuildChoice(
            runId,
            nodeId,
            runSeed,
            runSeedVersion,
            runGoldBeforeReward,
            armyAfterBattle,
            battleSummary,
            null);
    }

    public RewardMapChoiceViewData BuildChoice(
        string runId,
        int nodeId,
        int runSeed,
        int runSeedVersion,
        int runGoldBeforeReward,
        RewardMapArmySnapshot armyAfterBattle,
        RewardMapBattleResultSummary battleSummary,
        IList<RewardMapOperationType> plannedOperationTypes)
    {
        RewardMapArmySnapshot army = CloneArmy(armyAfterBattle, armyAfterBattle == null ? string.Empty : armyAfterBattle.SnapshotId);
        List<RewardMapCardViewData> cards = new List<RewardMapCardViewData>();
        RewardMapOperationType[] plannedTypes = NormalizePlannedOperationTypes(plannedOperationTypes, runSeed, nodeId, runSeedVersion);
        int disabledNormalCount = 0;

        for (int slotIndex = 0; slotIndex < RewardSlotCount; slotIndex++)
        {
            RewardMapOperationType plannedType = plannedTypes[slotIndex];
            Random random = new Random(BuildSeed(runSeed, nodeId, runSeedVersion, slotIndex, (int)plannedType));
            RewardMapCardViewData card = BuildPlannedNormalCard(army, plannedType, slotIndex, random);
            if (card == null)
            {
                card = BuildDisabledNormalCard(army, plannedType, slotIndex);
            }

            if (!card.Legal)
            {
                disabledNormalCount++;
            }

            cards.Add(card);
        }

        if (disabledNormalCount == RewardSlotCount)
        {
            cards.Add(BuildRunGoldFallback(RewardSlotCount));
        }

        RewardMapCardViewData focused = FindFirstLegal(cards);
        if (focused == null && cards.Count > 0)
        {
            focused = cards[0];
        }

        RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
            "reward-choice-materialized-" + nodeId.ToString(CultureInfo.InvariantCulture),
            runId,
            RewardMapGameMode.Offline,
            RewardMapAuthoritySource.LocalOfflineAdapter,
            battleSummary,
            BuildGainedSummary(battleSummary),
            runGoldBeforeReward,
            army,
            cards,
            focused,
            null,
            "Materialized rewards loaded.");

        return choice;
    }

    private RewardMapCardViewData BuildPlannedNormalCard(
        RewardMapArmySnapshot army,
        RewardMapOperationType plannedType,
        int slotIndex,
        Random random)
    {
        switch (plannedType)
        {
            case RewardMapOperationType.AddStack:
                return BuildAddNewStack(army, slotIndex, random);
            case RewardMapOperationType.AddUnits:
                return BuildIncreaseStack(army, slotIndex, random);
            case RewardMapOperationType.PromoteStack:
                return BuildPromoteOrDowngrade(army, slotIndex, random, true);
            case RewardMapOperationType.DowngradeStack:
                return BuildPromoteOrDowngrade(army, slotIndex, random, false);
            default:
                return null;
        }
    }

    private RewardMapCardViewData BuildIncreaseStack(RewardMapArmySnapshot army, int slotIndex, Random random)
    {
        int targetGain = CalculateTargetGain(army, RewardMapOperationType.AddUnits, RawGrowthRewardMultiplier);
        List<RewardValueCandidate> candidates = new List<RewardValueCandidate>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack == null || stack.Amount <= 0)
            {
                continue;
            }

            int unitCost = UnitCostForStack(stack);
            if (unitCost <= 0)
            {
                continue;
            }

            int amount = CalculateAmountForTargetValue(targetGain, unitCost);
            int valueGain = amount * unitCost;
            candidates.Add(new RewardValueCandidate(stack, null, amount, Math.Abs(valueGain - targetGain)));
        }

        RewardValueCandidate selected = PickClosestCandidate(candidates, random);
        if (selected == null || selected.Stack == null)
        {
            return null;
        }

        RewardMapStackSnapshot target = selected.Stack;
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.AddUnits,
            target.StackId,
            target.UnitId,
            string.Empty,
            string.Empty,
            string.Empty,
            selected.Amount,
            0);

        return Card(
            "prd37-increase-stack-v1",
            slotIndex,
            RewardMapFamily.Mass,
            RewardMapIntention.Strengthen,
            "Grow",
            "Increase " + target.DisplayName,
            "Add more units near the run reward value target.",
            target.DisplayName + " x" + target.Amount,
            target.DisplayName + " +" + selected.Amount,
            target.StackId,
            operation);
    }

    private RewardMapCardViewData BuildPromoteOrDowngrade(RewardMapArmySnapshot army, int slotIndex, Random random, bool promote)
    {
        RewardMapOperationType operationType = promote ? RewardMapOperationType.PromoteStack : RewardMapOperationType.DowngradeStack;
        int targetGain = CalculateTargetGain(army, operationType, ArmyShapeRewardMultiplier);
        List<RewardValueCandidate> candidates = new List<RewardValueCandidate>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack == null || stack.Amount <= 0)
            {
                continue;
            }

            RunShopUnitDefinition oldUnit = unitSource == null ? null : unitSource.FindUnit(stack.UnitId);
            RunShopUnitDefinition newUnitCandidate = FindTierNeighbor(stack.UnitId, promote);
            if (oldUnit == null || newUnitCandidate == null || oldUnit.Cost <= 0 || newUnitCandidate.Cost <= 0)
            {
                continue;
            }

            int oldValue = StackValue(stack, oldUnit.Cost);
            int targetFinalValue = Math.Max(newUnitCandidate.Cost, oldValue + targetGain);
            int amount = CalculateAmountForTargetValue(targetFinalValue, newUnitCandidate.Cost);
            int finalValue = amount * newUnitCandidate.Cost;
            int valueGain = Math.Max(0, finalValue - oldValue);
            candidates.Add(new RewardValueCandidate(stack, newUnitCandidate, amount, Math.Abs(valueGain - targetGain)));
        }

        RewardValueCandidate selected = PickClosestCandidate(candidates, random);
        if (selected == null || selected.Stack == null || selected.Unit == null)
        {
            return null;
        }

        RewardMapStackSnapshot target = selected.Stack;
        RunShopUnitDefinition newUnit = selected.Unit;
        RewardMapOperation operation = new RewardMapOperation(
            promote ? RewardMapOperationType.PromoteStack : RewardMapOperationType.DowngradeStack,
            target.StackId,
            target.UnitId,
            newUnit.UnitId,
            string.Empty,
            string.Empty,
            selected.Amount,
            0);

        return Card(
            promote ? "prd37-promote-unit-v1" : "prd37-downgrade-unit-v1",
            slotIndex,
            promote ? RewardMapFamily.Quality : RewardMapFamily.Mass,
            promote ? RewardMapIntention.Pivot : RewardMapIntention.Stabilize,
            promote ? "Promote" : "Downgrade",
            target.DisplayName + " to " + newUnit.DisplayName,
            promote ? "Move one tier up in the same faction." : "Move one tier down in the same faction for more bodies.",
            target.DisplayName + " x" + target.Amount,
            newUnit.DisplayName + " x" + selected.Amount,
            target.StackId,
            operation);
    }

    private RewardMapCardViewData BuildAddNewStack(RewardMapArmySnapshot army, int slotIndex, Random random)
    {
        int formationSlot = RewardMapArmySlotRules.FindFirstFreeFormationSlot(army);
        if (formationSlot < 0)
        {
            return null;
        }

        List<RunShopUnitDefinition> candidates = ListUnitsNotInArmy(army);
        if (candidates.Count == 0)
        {
            return null;
        }

        int targetGain = CalculateTargetGain(army, RewardMapOperationType.AddStack, RawGrowthRewardMultiplier);
        List<RewardValueCandidate> valueCandidates = new List<RewardValueCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            RunShopUnitDefinition candidate = candidates[i];
            if (candidate == null || candidate.Cost <= 0)
            {
                continue;
            }

            int amountCandidate = CalculateAmountForTargetValue(targetGain, candidate.Cost);
            int stackValue = amountCandidate * candidate.Cost;
            valueCandidates.Add(new RewardValueCandidate(null, candidate, amountCandidate, Math.Abs(stackValue - targetGain)));
        }

        RewardValueCandidate selected = PickClosestCandidate(valueCandidates, random);
        if (selected == null || selected.Unit == null)
        {
            return null;
        }

        RunShopUnitDefinition unit = selected.Unit;
        string stackId = RewardMapArmySlotRules.ToFormationSlotStackId(formationSlot);
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.AddStack,
            string.Empty,
            unit.UnitId,
            string.Empty,
            string.Empty,
            stackId,
            selected.Amount,
            0);

        return Card(
            "prd37-add-new-stack-v1",
            slotIndex,
            RewardMapFamily.Width,
            RewardMapIntention.Pivot,
            "Add",
            "Recruit " + unit.DisplayName,
            "Add a new non-duplicate stack.",
            "Open stack slot",
            unit.DisplayName + " x" + selected.Amount + " joins",
            stackId,
            operation);
    }

    private RewardMapCardViewData BuildRunGoldFallback(int slotIndex)
    {
        int currencyDelta = CalculateRunGoldFallbackAmount();
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.GainCurrency,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            currencyDelta);

        return Card(
            "prd37-run-gold-fallback-v1",
            slotIndex,
            RewardMapFamily.Economy,
            RewardMapIntention.Stabilize,
            "Fallback",
            "Emergency RUN GOLD",
            "Used only when normal rewards cannot fill the offer.",
            "No legal normal reward",
            "+" + currencyDelta + " RUN GOLD",
            string.Empty,
            operation);
    }

    private RewardMapCardViewData BuildDisabledNormalCard(RewardMapArmySnapshot army, RewardMapOperationType operationType, int slotIndex)
    {
        RewardMapOperation operation = new RewardMapOperation(
            operationType,
            string.Empty,
            FirstUnitId(army),
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0);

        return Card(
            CatalogEntryIdFor(operationType),
            slotIndex,
            FamilyFor(operationType),
            IntentionFor(operationType),
            VerbFor(operationType),
            DisabledTitleFor(operationType),
            "This planned reward type has no legal target.",
            "Planned normal reward",
            "No legal target",
            string.Empty,
            operation,
            false,
            RewardMapError.NoLegalTarget);
    }

    private RewardMapCardViewData Card(
        string catalogEntryId,
        int slotIndex,
        RewardMapFamily family,
        RewardMapIntention intention,
        string verb,
        string title,
        string detail,
        string before,
        string after,
        string affectedStackId,
        RewardMapOperation operation,
        bool legal = true,
        RewardMapError error = RewardMapError.None)
    {
        return new RewardMapCardViewData(
            "reward-" + catalogEntryId + "-" + slotIndex.ToString(CultureInfo.InvariantCulture),
            catalogEntryId,
            family,
            intention,
            RewardMapRarity.Common,
            verb,
            title,
            detail,
            before,
            after,
            affectedStackId,
            legal,
            error,
            operation);
    }

    private RunShopUnitDefinition FindTierNeighbor(string unitId, bool promote)
    {
        RunShopUnitDefinition source = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (source == null)
        {
            return null;
        }

        int sourceFaction = UnitFactionId(source);
        if (sourceFaction <= 0)
        {
            return null;
        }

        int targetTier = TierNumber(source.Tier) + (promote ? 1 : -1);
        if (targetTier <= 0)
        {
            return null;
        }

        List<RunShopUnitDefinition> units = ListUnits();
        for (int i = 0; i < units.Count; i++)
        {
            RunShopUnitDefinition candidate = units[i];
            if (candidate != null &&
                candidate.UnitId != source.UnitId &&
                UnitFactionId(candidate) == sourceFaction &&
                TierNumber(candidate.Tier) == targetTier)
            {
                return candidate;
            }
        }

        return null;
    }

    private List<RunShopUnitDefinition> ListUnitsNotInArmy(RewardMapArmySnapshot army)
    {
        List<RunShopUnitDefinition> units = ListUnits();
        List<RunShopUnitDefinition> result = new List<RunShopUnitDefinition>();
        for (int i = 0; i < units.Count; i++)
        {
            RunShopUnitDefinition unit = units[i];
            if (unit != null && !ArmyContainsUnit(army, unit.UnitId))
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private List<RunShopUnitDefinition> ListUnits()
    {
        IRewardMapUnitPoolSource pool = unitSource as IRewardMapUnitPoolSource;
        if (pool != null)
        {
            return pool.ListUnits();
        }

        string[] fallbackIds = { "Rusher", "Thrower", "Healer", "Wisp", "Trapper", "Axeman", "StoneGolem", "StoneLord", "LizardMage", "Tank", "Specialist" };
        List<RunShopUnitDefinition> result = new List<RunShopUnitDefinition>();
        for (int i = 0; i < fallbackIds.Length; i++)
        {
            RunShopUnitDefinition unit = unitSource == null ? null : unitSource.FindUnit(fallbackIds[i]);
            if (unit != null)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private static RewardMapCardViewData FindFirstLegal(List<RewardMapCardViewData> cards)
    {
        for (int i = 0; cards != null && i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].Legal)
            {
                return cards[i];
            }
        }

        return null;
    }

    public static RewardMapOperationType[] PlanNormalOperationTypes(int runSeed, int nodeId, int runSeedVersion)
    {
        List<RewardMapOperationType> types = new List<RewardMapOperationType>
        {
            RewardMapOperationType.AddStack,
            RewardMapOperationType.AddUnits,
            RewardMapOperationType.PromoteStack,
            RewardMapOperationType.DowngradeStack
        };

        Random random = new Random(BuildSeed(runSeed, nodeId, runSeedVersion, 0, 0));
        for (int i = types.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            RewardMapOperationType temp = types[i];
            types[i] = types[swapIndex];
            types[swapIndex] = temp;
        }

        RewardMapOperationType[] planned = new RewardMapOperationType[RewardSlotCount];
        for (int i = 0; i < RewardSlotCount; i++)
        {
            planned[i] = types[i];
        }

        return planned;
    }

    private static RewardMapOperationType[] NormalizePlannedOperationTypes(
        IList<RewardMapOperationType> plannedOperationTypes,
        int runSeed,
        int nodeId,
        int runSeedVersion)
    {
        if (plannedOperationTypes == null || plannedOperationTypes.Count < RewardSlotCount)
        {
            return PlanNormalOperationTypes(runSeed, nodeId, runSeedVersion);
        }

        RewardMapOperationType[] result = new RewardMapOperationType[RewardSlotCount];
        for (int i = 0; i < RewardSlotCount; i++)
        {
            RewardMapOperationType plannedType = plannedOperationTypes[i];
            if (!IsNormalPlannedOperationType(plannedType))
            {
                return PlanNormalOperationTypes(runSeed, nodeId, runSeedVersion);
            }

            result[i] = plannedType;
        }

        return result;
    }

    private static bool IsNormalPlannedOperationType(RewardMapOperationType operationType)
    {
        return operationType == RewardMapOperationType.AddStack ||
            operationType == RewardMapOperationType.AddUnits ||
            operationType == RewardMapOperationType.PromoteStack ||
            operationType == RewardMapOperationType.DowngradeStack;
    }

    private static int UnitFactionId(RunShopUnitDefinition unit)
    {
        if (unit == null)
        {
            return UnitFactionResolver.UnknownFactionId;
        }

        return unit.FactionId > 0 ? unit.FactionId : UnitFactionResolver.ResolveFactionId(unit.UnitId);
    }

    private static string FirstUnitId(RewardMapArmySnapshot army)
    {
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && !string.IsNullOrEmpty(stack.UnitId))
            {
                return stack.UnitId;
            }
        }

        return string.Empty;
    }

    public static string CatalogEntryIdFor(RewardMapOperationType operationType)
    {
        switch (operationType)
        {
            case RewardMapOperationType.AddStack:
                return "prd37-add-new-stack-v1";
            case RewardMapOperationType.AddUnits:
                return "prd37-increase-stack-v1";
            case RewardMapOperationType.PromoteStack:
                return "prd37-promote-unit-v1";
            case RewardMapOperationType.DowngradeStack:
                return "prd37-downgrade-unit-v1";
            default:
                return "prd37-normal-disabled-v1";
        }
    }

    private static RewardMapFamily FamilyFor(RewardMapOperationType operationType)
    {
        switch (operationType)
        {
            case RewardMapOperationType.PromoteStack:
                return RewardMapFamily.Quality;
            case RewardMapOperationType.AddStack:
                return RewardMapFamily.Width;
            case RewardMapOperationType.AddUnits:
            case RewardMapOperationType.DowngradeStack:
            default:
                return RewardMapFamily.Mass;
        }
    }

    private static RewardMapIntention IntentionFor(RewardMapOperationType operationType)
    {
        switch (operationType)
        {
            case RewardMapOperationType.DowngradeStack:
                return RewardMapIntention.Stabilize;
            case RewardMapOperationType.AddUnits:
                return RewardMapIntention.Strengthen;
            case RewardMapOperationType.AddStack:
            case RewardMapOperationType.PromoteStack:
            default:
                return RewardMapIntention.Pivot;
        }
    }

    private static string VerbFor(RewardMapOperationType operationType)
    {
        switch (operationType)
        {
            case RewardMapOperationType.AddStack:
                return "Add";
            case RewardMapOperationType.AddUnits:
                return "Grow";
            case RewardMapOperationType.PromoteStack:
                return "Promote";
            case RewardMapOperationType.DowngradeStack:
                return "Downgrade";
            default:
                return "Reward";
        }
    }

    private static string DisabledTitleFor(RewardMapOperationType operationType)
    {
        switch (operationType)
        {
            case RewardMapOperationType.AddStack:
                return "Add unavailable";
            case RewardMapOperationType.AddUnits:
                return "Grow unavailable";
            case RewardMapOperationType.PromoteStack:
                return "Promote unavailable";
            case RewardMapOperationType.DowngradeStack:
                return "Downgrade unavailable";
            default:
                return "Reward unavailable";
        }
    }

    private static bool ArmyContainsUnit(RewardMapArmySnapshot army, string unitId)
    {
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null && army.Stacks[i].UnitId == unitId)
            {
                return true;
            }
        }

        return false;
    }

    private int AverageExistingStackValue(RewardMapArmySnapshot army)
    {
        if (army == null || army.Stacks == null || army.Stacks.Count == 0)
        {
            return 100;
        }

        int resolvedTotal = 0;
        int count = 0;
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.Amount > 0)
            {
                resolvedTotal += StackValue(stack, UnitCostForStack(stack));
                count++;
            }
        }

        int armyTotal = Math.Max(Math.Max(0, army.TotalArmyValue), resolvedTotal);
        return count == 0 ? 100 : Math.Max(1, armyTotal / count);
    }

    private int CalculateTargetGain(RewardMapArmySnapshot army, RewardMapOperationType operationType, float legacyFamilyMultiplier)
    {
        if (rewardRuleSet != null && valueContext != null)
        {
            return rewardRuleSet.CalculateRewardValue(
                operationType,
                valueContext.BattleLossValue,
                valueContext.EnemyArmyValue,
                valueContext.ArmyValueBeforeBattle);
        }

        return Math.Max(1, (int)Math.Round(AverageExistingStackValue(army) * BaseRewardGainRatio * legacyFamilyMultiplier));
    }

    private int CalculateRunGoldFallbackAmount()
    {
        if (rewardRuleSet == null || valueContext == null)
        {
            return RunGoldFallbackAmount;
        }

        return rewardRuleSet.CalculateArmyGrowthReward(
            valueContext.BattleLossValue,
            valueContext.EnemyArmyValue,
            valueContext.ArmyValueBeforeBattle);
    }

    private static int CalculateAmountForTargetValue(int targetValue, int unitCost)
    {
        if (unitCost <= 0)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Round((float)Math.Max(1, targetValue) / unitCost));
    }

    private int UnitCostForStack(RewardMapStackSnapshot stack)
    {
        RunShopUnitDefinition unit = unitSource == null || stack == null ? null : unitSource.FindUnit(stack.UnitId);
        if (unit != null && unit.Cost > 0)
        {
            return unit.Cost;
        }

        return stack == null || stack.Amount <= 0 ? 0 : Math.Max(1, stack.CombatValue / stack.Amount);
    }

    private static int StackValue(RewardMapStackSnapshot stack, int unitCost)
    {
        if (stack == null)
        {
            return 0;
        }

        int amountValue = Math.Max(0, stack.Amount) * Math.Max(0, unitCost);
        return Math.Max(Math.Max(0, stack.CombatValue), amountValue);
    }

    private static RewardValueCandidate PickClosestCandidate(List<RewardValueCandidate> candidates, Random random)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int bestScore = int.MaxValue;
        List<RewardValueCandidate> best = new List<RewardValueCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            RewardValueCandidate candidate = candidates[i];
            if (candidate == null || candidate.Amount <= 0)
            {
                continue;
            }

            if (candidate.Score < bestScore)
            {
                bestScore = candidate.Score;
                best.Clear();
                best.Add(candidate);
            }
            else if (candidate.Score == bestScore)
            {
                best.Add(candidate);
            }
        }

        if (best.Count == 0)
        {
            return null;
        }

        return random == null || best.Count == 1 ? best[0] : best[random.Next(best.Count)];
    }

    private static RewardMapArmySnapshot CloneArmy(RewardMapArmySnapshot army, string snapshotId)
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack == null)
            {
                continue;
            }

            List<RewardMapSkillState> skills = new List<RewardMapSkillState>();
            for (int skillIndex = 0; stack.Skills != null && skillIndex < stack.Skills.Count; skillIndex++)
            {
                RewardMapSkillState skill = stack.Skills[skillIndex];
                if (skill != null)
                {
                    skills.Add(new RewardMapSkillState(skill.SkillId, skill.Unlocked));
                }
            }

            stacks.Add(new RewardMapStackSnapshot(stack.StackId, stack.UnitId, stack.DisplayName, stack.Tier, stack.Level, stack.Amount, stack.Lost, stack.CombatValue, skills));
        }

        return new RewardMapArmySnapshot(snapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private static string BuildGainedSummary(RewardMapBattleResultSummary summary)
    {
        if (summary == null)
        {
            return "Gained: battle result loaded.";
        }

        return "Gained: " + summary.RunGoldGained + " RUN GOLD, losses " + summary.Losses;
    }

    private static int BuildSeed(int runSeed, int nodeId, int seedVersion, int slotIndex, int attemptIndex)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + runSeed;
            seed = seed * 31 + nodeId;
            seed = seed * 31 + seedVersion;
            seed = seed * 31 + slotIndex;
            seed = seed * 31 + attemptIndex;
            return seed;
        }
    }

    private static int TierNumber(string tier)
    {
        if (string.IsNullOrEmpty(tier))
        {
            return 1;
        }

        int parsed;
        if (int.TryParse(tier, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return Math.Max(1, parsed);
        }

        switch (tier.Trim().ToUpperInvariant())
        {
            case "I":
                return 1;
            case "II":
                return 2;
            case "III":
                return 3;
            case "IV":
                return 4;
            default:
                return 1;
        }
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "unit";
        }

        char[] buffer = new char[value.Length];
        int length = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char character = char.ToLowerInvariant(value[i]);
            if (char.IsLetterOrDigit(character))
            {
                buffer[length++] = character;
            }
        }

        return length == 0 ? "unit" : new string(buffer, 0, length);
    }

    private sealed class RewardValueCandidate
    {
        public readonly RewardMapStackSnapshot Stack;
        public readonly RunShopUnitDefinition Unit;
        public readonly int Amount;
        public readonly int Score;

        public RewardValueCandidate(RewardMapStackSnapshot stack, RunShopUnitDefinition unit, int amount, int score)
        {
            Stack = stack;
            Unit = unit;
            Amount = Math.Max(0, amount);
            Score = Math.Max(0, score);
        }
    }
}

public sealed class RewardGeneratorValueContext
{
    public readonly int BattleLossValue;
    public readonly int EnemyArmyValue;
    public readonly int ArmyValueBeforeBattle;

    public RewardGeneratorValueContext(int battleLossValue, int enemyArmyValue, int armyValueBeforeBattle)
    {
        BattleLossValue = Math.Max(0, battleLossValue);
        EnemyArmyValue = Math.Max(0, enemyArmyValue);
        ArmyValueBeforeBattle = Math.Max(0, armyValueBeforeBattle);
    }
}
