using System;
using System.Collections.Generic;
using System.Globalization;
using Random = System.Random;

public sealed class RewardMapMaterializedGenerator
{
    private const int RewardSlotCount = 3;
    private const int MaxAttemptsPerSlot = 24;
    private const int RunGoldFallbackAmount = 60;

    private readonly IRewardMapUnitDefinitionSource unitSource;

    public RewardMapMaterializedGenerator(IRewardMapUnitDefinitionSource unitSource)
    {
        this.unitSource = unitSource;
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
        RewardMapArmySnapshot army = CloneArmy(armyAfterBattle, armyAfterBattle == null ? string.Empty : armyAfterBattle.SnapshotId);
        List<RewardMapCardViewData> cards = new List<RewardMapCardViewData>();
        List<string> usedSignatures = new List<string>();

        for (int slotIndex = 0; slotIndex < RewardSlotCount; slotIndex++)
        {
            RewardMapCardViewData card = BuildNormalCard(army, nodeId, runSeed, runSeedVersion, slotIndex, usedSignatures);
            if (card == null)
            {
                card = BuildRunGoldFallback(slotIndex);
            }

            cards.Add(card);
            usedSignatures.Add(BuildSignature(card));
        }

        RewardMapCardViewData focused = cards.Count == 0 ? null : cards[0];
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

    private RewardMapCardViewData BuildNormalCard(
        RewardMapArmySnapshot army,
        int nodeId,
        int runSeed,
        int runSeedVersion,
        int slotIndex,
        List<string> usedSignatures)
    {
        for (int attemptIndex = 0; attemptIndex < MaxAttemptsPerSlot; attemptIndex++)
        {
            Random random = new Random(BuildSeed(runSeed, nodeId, runSeedVersion, slotIndex, attemptIndex));
            int recipe = random.Next(4);
            RewardMapCardViewData card = BuildRecipeCard(army, recipe, slotIndex, random);
            string signature = BuildSignature(card);
            if (card != null && card.Legal && !string.IsNullOrEmpty(signature) && !usedSignatures.Contains(signature))
            {
                return card;
            }
        }

        return null;
    }

    private RewardMapCardViewData BuildRecipeCard(RewardMapArmySnapshot army, int recipe, int slotIndex, Random random)
    {
        switch (recipe)
        {
            case 0:
                return BuildAddNewStack(army, slotIndex, random);
            case 1:
                return BuildIncreaseStack(army, slotIndex, random);
            case 2:
                return BuildPromoteOrDowngrade(army, slotIndex, random, true);
            default:
                return BuildPromoteOrDowngrade(army, slotIndex, random, false);
        }
    }

    private RewardMapCardViewData BuildIncreaseStack(RewardMapArmySnapshot army, int slotIndex, Random random)
    {
        RewardMapStackSnapshot target = PickLiveStack(army, random);
        if (target == null)
        {
            return null;
        }

        int amount = Math.Max(1, (int)Math.Round(target.Amount * 0.3f));
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.AddUnits,
            target.StackId,
            target.UnitId,
            string.Empty,
            string.Empty,
            string.Empty,
            amount,
            0);

        return Card(
            "prd37-increase-stack-v1",
            slotIndex,
            RewardMapFamily.Mass,
            RewardMapIntention.Strengthen,
            "Grow",
            "Increase " + target.DisplayName,
            "Add 30 percent more units to this stack.",
            target.DisplayName + " x" + target.Amount,
            target.DisplayName + " +" + amount,
            target.StackId,
            operation);
    }

    private RewardMapCardViewData BuildPromoteOrDowngrade(RewardMapArmySnapshot army, int slotIndex, Random random, bool promote)
    {
        RewardMapStackSnapshot target = PickStackWithTierNeighbor(army, random, promote);
        if (target == null)
        {
            return null;
        }

        RunShopUnitDefinition oldUnit = unitSource == null ? null : unitSource.FindUnit(target.UnitId);
        RunShopUnitDefinition newUnit = FindTierNeighbor(target.UnitId, promote);
        if (oldUnit == null || newUnit == null || oldUnit.Cost <= 0 || newUnit.Cost <= 0)
        {
            return null;
        }

        int amount = Math.Max(1, (int)Math.Round((target.Amount * oldUnit.Cost * 1.2f) / newUnit.Cost));
        RewardMapOperation operation = new RewardMapOperation(
            promote ? RewardMapOperationType.PromoteStack : RewardMapOperationType.DowngradeStack,
            target.StackId,
            target.UnitId,
            newUnit.UnitId,
            string.Empty,
            string.Empty,
            amount,
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
            newUnit.DisplayName + " x" + amount,
            target.StackId,
            operation);
    }

    private RewardMapCardViewData BuildAddNewStack(RewardMapArmySnapshot army, int slotIndex, Random random)
    {
        List<RunShopUnitDefinition> candidates = ListUnitsNotInArmy(army);
        if (candidates.Count == 0)
        {
            return null;
        }

        RunShopUnitDefinition unit = candidates[random.Next(candidates.Count)];
        if (unit == null || unit.Cost <= 0)
        {
            return null;
        }

        int targetValue = Math.Max(unit.Cost, (int)Math.Round(AverageExistingStackValue(army) * 1.2f));
        int amount = Math.Max(1, (int)Math.Round((float)targetValue / unit.Cost));
        string stackId = "reward-stack-" + NormalizeId(unit.UnitId) + "-" + slotIndex.ToString(CultureInfo.InvariantCulture);
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.AddStack,
            string.Empty,
            unit.UnitId,
            string.Empty,
            string.Empty,
            stackId,
            amount,
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
            unit.DisplayName + " x" + amount + " joins",
            stackId,
            operation);
    }

    private RewardMapCardViewData BuildRunGoldFallback(int slotIndex)
    {
        RewardMapOperation operation = new RewardMapOperation(
            RewardMapOperationType.GainCurrency,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            RunGoldFallbackAmount);

        return Card(
            "prd37-run-gold-fallback-v1",
            slotIndex,
            RewardMapFamily.Economy,
            RewardMapIntention.Stabilize,
            "Fallback",
            "Emergency RUN GOLD",
            "Used only when normal rewards cannot fill the offer.",
            "No legal normal reward",
            "+" + RunGoldFallbackAmount + " RUN GOLD",
            string.Empty,
            operation);
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
        RewardMapOperation operation)
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
            true,
            RewardMapError.None,
            operation);
    }

    private RewardMapStackSnapshot PickLiveStack(RewardMapArmySnapshot army, Random random)
    {
        List<RewardMapStackSnapshot> live = new List<RewardMapStackSnapshot>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.Amount > 0)
            {
                live.Add(stack);
            }
        }

        return live.Count == 0 ? null : live[random.Next(live.Count)];
    }

    private RewardMapStackSnapshot PickStackWithTierNeighbor(RewardMapArmySnapshot army, Random random, bool promote)
    {
        List<RewardMapStackSnapshot> candidates = new List<RewardMapStackSnapshot>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.Amount > 0 && FindTierNeighbor(stack.UnitId, promote) != null)
            {
                candidates.Add(stack);
            }
        }

        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
    }

    private RunShopUnitDefinition FindTierNeighbor(string unitId, bool promote)
    {
        RunShopUnitDefinition source = unitSource == null ? null : unitSource.FindUnit(unitId);
        if (source == null)
        {
            return null;
        }

        int sourceFaction = UnitFactionResolver.ResolveFactionId(source.UnitId);
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
                UnitFactionResolver.ResolveFactionId(candidate.UnitId) == sourceFaction &&
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

    private static int AverageExistingStackValue(RewardMapArmySnapshot army)
    {
        if (army == null || army.Stacks == null || army.Stacks.Count == 0)
        {
            return 100;
        }

        int total = 0;
        int count = 0;
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.Amount > 0)
            {
                total += Math.Max(0, stack.CombatValue);
                count++;
            }
        }

        return count == 0 ? 100 : Math.Max(1, total / count);
    }

    private static string BuildSignature(RewardMapCardViewData card)
    {
        if (card == null || card.Operation == null)
        {
            return string.Empty;
        }

        RewardMapOperation operation = card.Operation;
        return operation.Type + "|" + operation.StackId + "|" + operation.UnitId + "|" + operation.ToUnitId;
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
}
