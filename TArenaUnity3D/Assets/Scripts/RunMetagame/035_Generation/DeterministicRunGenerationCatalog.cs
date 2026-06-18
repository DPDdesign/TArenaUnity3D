using System;
using System.Collections.Generic;
using System.Globalization;
using Random = System.Random;

[Serializable]
public class RunGenerationSeed
{
    public int Value;

    public RunGenerationSeed(int value)
    {
        Value = value;
    }
}

[Serializable]
public class StartingArmyGeneratorConfig
{
    public RunGenerationSeed Seed;
    public int OfferCount;
    public int StackCount;
    public int TargetTotalValue;
    public int MinimumTotalValue;
    public int MaximumTotalValue;
    public int PerStackTolerancePercent;
    public int StartingGold;
    public int StartingRerollTokens;
    public int BattleSkipTokens;

    public StartingArmyGeneratorConfig(
        RunGenerationSeed seed,
        int offerCount,
        int stackCount,
        int targetTotalValue,
        int minimumTotalValue,
        int maximumTotalValue,
        int perStackTolerancePercent,
        int startingGold,
        int startingRerollTokens,
        int battleSkipTokens)
    {
        Seed = seed ?? new RunGenerationSeed(35035);
        OfferCount = Math.Max(1, offerCount);
        StackCount = Math.Max(1, stackCount);
        TargetTotalValue = Math.Max(1, targetTotalValue);
        MinimumTotalValue = Math.Max(0, minimumTotalValue);
        MaximumTotalValue = Math.Max(MinimumTotalValue, maximumTotalValue);
        PerStackTolerancePercent = Math.Max(0, perStackTolerancePercent);
        StartingGold = Math.Max(0, startingGold);
        StartingRerollTokens = Math.Max(0, startingRerollTokens);
        BattleSkipTokens = Math.Max(0, battleSkipTokens);
    }

    public static StartingArmyGeneratorConfig CreateDefault()
    {
        throw new InvalidOperationException("CreateDefault requires an ArmyGeneratorRuleSet. Use CreateDefault(ruleSet).");
    }

    public static StartingArmyGeneratorConfig CreateDefault(ArmyGeneratorRuleSet ruleSet)
    {
        if (ruleSet == null)
        {
            throw new ArgumentNullException("ruleSet");
        }

        return new StartingArmyGeneratorConfig(
            new RunGenerationSeed(35035),
            ruleSet.OfferCount,
            ruleSet.StackCount,
            CalculateTargetPower(ruleSet),
            ruleSet.MinPower,
            ruleSet.MaxPower,
            20,
            ruleSet.StartingGold,
            ruleSet.StartingRerollTokens,
            ruleSet.BattleSkipTokens);
    }

    public static StartingArmyGeneratorConfig CreateRuntimeRandomized(ArmyGeneratorRuleSet ruleSet)
    {
        StartingArmyGeneratorConfig config = CreateDefault(ruleSet);
        config.Seed = new RunGenerationSeed(CreateRuntimeSeed());
        return config;
    }

    private static int CalculateTargetPower(ArmyGeneratorRuleSet ruleSet)
    {
        return Math.Max(1, (ruleSet.MinPower + ruleSet.MaxPower) / 2);
    }

    private static int CreateRuntimeSeed()
    {
        unchecked
        {
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            seed = seed & 0x7fffffff;
            return seed == 0 ? 35035 : seed;
        }
    }
}

[Serializable]
public class RouteGeneratorConfig
{
    public RunGenerationSeed Seed;
    public List<string> CampaignIds;

    public RouteGeneratorConfig(RunGenerationSeed seed, List<string> campaignIds)
    {
        Seed = seed ?? new RunGenerationSeed(35035);
        CampaignIds = campaignIds ?? new List<string>();
    }

    public static RouteGeneratorConfig CreateDefault()
    {
        return new RouteGeneratorConfig(new RunGenerationSeed(35035), new List<string> { "forest", "desert", "castle" });
    }

    public static RouteGeneratorConfig CreateRuntimeRandomized(int seed)
    {
        return new RouteGeneratorConfig(new RunGenerationSeed(seed), new List<string> { "forest", "desert", "castle" });
    }
}

[Serializable]
public class StartRunGenerationUnlockContext
{
    public List<string> UnlockedUnitIds;
    public List<string> UnlockedSkillIds;

    public StartRunGenerationUnlockContext(List<string> unlockedUnitIds, List<string> unlockedSkillIds)
    {
        UnlockedUnitIds = unlockedUnitIds ?? new List<string>();
        UnlockedSkillIds = unlockedSkillIds ?? new List<string>();
    }

    public bool HasUnitRestrictions
    {
        get { return UnlockedUnitIds.Count > 0; }
    }

    public bool HasSkillRestrictions
    {
        get { return UnlockedSkillIds.Count > 0; }
    }

    public bool AllowsUnit(string unitId)
    {
        return !HasUnitRestrictions || Contains(UnlockedUnitIds, unitId);
    }

    public bool AllowsSkill(string skillId)
    {
        return !HasSkillRestrictions || Contains(UnlockedSkillIds, skillId);
    }

    private static bool Contains(List<string> values, string value)
    {
        if (values == null || string.IsNullOrEmpty(value))
        {
            return false;
        }

        string normalizedValue = NormalizeUnlockToken(value);
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == value || NormalizeUnlockToken(values[i]) == normalizedValue)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeUnlockToken(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string lower = value.Trim().ToLowerInvariant();
        if (lower.StartsWith("unit-"))
        {
            lower = lower.Substring(5);
        }
        else if (lower.StartsWith("skill-"))
        {
            lower = lower.Substring(6);
        }

        char[] buffer = new char[lower.Length];
        int length = 0;
        for (int i = 0; i < lower.Length; i++)
        {
            if (char.IsLetterOrDigit(lower[i]))
            {
                buffer[length++] = lower[i];
            }
        }

        return new string(buffer, 0, length);
    }
}

public class DeterministicRunGenerationCatalog : IRequestedStartingArmyTemplateSource, IRunRoutePreviewSource, IRunMapPathCatalog
{
    private readonly IStartRunUnitPoolSource unitSource;
    private readonly ArmyGeneratorRuleSet ruleSet;
    private readonly StartingArmyGeneratorConfig armyConfig;
    private readonly RouteGeneratorConfig routeConfig;
    private readonly StartRunGenerationUnlockContext unlockContext;

    public DeterministicRunGenerationCatalog(IStartRunUnitPoolSource unitSource)
        : this(unitSource, null, null, RouteGeneratorConfig.CreateDefault(), null)
    {
    }

    public DeterministicRunGenerationCatalog(
        IStartRunUnitPoolSource unitSource,
        StartingArmyGeneratorConfig armyConfig,
        RouteGeneratorConfig routeConfig,
        StartRunGenerationUnlockContext unlockContext)
        : this(unitSource, null, armyConfig, routeConfig, unlockContext)
    {
    }

    public DeterministicRunGenerationCatalog(
        IStartRunUnitPoolSource unitSource,
        ArmyGeneratorRuleSet ruleSet,
        StartingArmyGeneratorConfig armyConfig,
        RouteGeneratorConfig routeConfig,
        StartRunGenerationUnlockContext unlockContext)
    {
        this.unitSource = unitSource;
        if (ruleSet == null)
        {
            throw new ArgumentNullException("ruleSet", "DeterministicRunGenerationCatalog requires an ArmyGeneratorRuleSet.");
        }

        this.ruleSet = ruleSet;
        this.armyConfig = armyConfig ?? StartingArmyGeneratorConfig.CreateDefault(ruleSet);
        this.routeConfig = routeConfig ?? RouteGeneratorConfig.CreateDefault();
        this.unlockContext = unlockContext ?? new StartRunGenerationUnlockContext(null, null);
    }

    public List<StartingArmyTemplate> ListStartingArmies()
    {
        return ListStartingArmies(armyConfig.OfferCount);
    }

    public List<StartingArmyTemplate> ListStartingArmies(int requestedOfferCount)
    {
        List<StartRunUnitDefinition> units = BuildAvailableUnitPool();
        List<StartingArmyTemplate> armies = new List<StartingArmyTemplate>();
        List<string> usedUnitSignatures = new List<string>();
        int offerCount = Math.Max(1, requestedOfferCount);

        for (int offerIndex = 0; offerIndex < offerCount; offerIndex++)
        {
            StartingArmyTemplate army = BuildDistinctArmyOffer(units, offerIndex, usedUnitSignatures);
            if (army == null || army.Stacks == null || army.Stacks.Count == 0)
            {
                continue;
            }

            armies.Add(army);
            string signature = BuildUnitSignature(army);
            if (!string.IsNullOrEmpty(signature) && !usedUnitSignatures.Contains(signature))
            {
                usedUnitSignatures.Add(signature);
            }
        }

        return armies;
    }

    public List<RoutePreviewTemplate> ListRoutePreviews()
    {
        List<RoutePreviewTemplate> previews = new List<RoutePreviewTemplate>();
        List<string> campaigns = routeConfig.CampaignIds == null || routeConfig.CampaignIds.Count == 0
            ? new List<string> { "forest", "desert", "castle" }
            : routeConfig.CampaignIds;

        for (int i = 0; i < campaigns.Count; i++)
        {
            string campaignId = NormalizeToken(campaigns[i], "campaign");
            string routeId = BuildRouteChoiceId(campaignId, 1);
            previews.Add(new RoutePreviewTemplate(
                routeId,
                ToDisplayName(campaignId) + " Mission 1",
                "Mission map with fixed battle, event, branch, shop, and final points.",
                armyConfig.TargetTotalValue));
        }

        return previews;
    }

    public List<RunMapPathDefinition> BuildPaths(string selectedRouteChoiceId)
    {
        string campaignId = ParseCampaignId(selectedRouteChoiceId);
        int missionIndex = ParseMissionIndex(selectedRouteChoiceId);
        int routeSeed = ParseSeedValue(selectedRouteChoiceId, routeConfig.Seed.Value);
        int seed = BuildSeed(routeSeed, StableHash(campaignId), missionIndex);
        string pathPrefix = campaignId + "-m" + missionIndex.ToString(CultureInfo.InvariantCulture);

        return new List<RunMapPathDefinition>
        {
            Path(pathPrefix + "-main", selectedRouteChoiceId, "Mission Approach", "Fixed opening: battle, event, battle before branch.",
                Node("node-1", pathPrefix + "-main", RunMapNodeType.Battle, EnemyEncounterDifficulty.Low, 1, "Outer Guard", "Battle reward", "Low risk", Encounter(campaignId, missionIndex, "node-1", "low", seed), "node-2"),
                Node("node-2", pathPrefix + "-main", RunMapNodeType.RandomEvent, 2, "Unmarked Detour", "Event outcome", "No direct battle risk", string.Empty, "node-3"),
                Node("node-3", pathPrefix + "-main", RunMapNodeType.Battle, EnemyEncounterDifficulty.Medium, 3, "Broken Road Fight", "Battle reward", "Medium risk", Encounter(campaignId, missionIndex, "node-3", "medium", seed), new List<string> { "node-4a", "node-4b" })),
            Path(pathPrefix + "-safe", selectedRouteChoiceId, "Safer Branch", "Lower pressure branch with fewer battle checks.",
                Node("node-4a", pathPrefix + "-safe", RunMapNodeType.Battle, EnemyEncounterDifficulty.Medium, 4, "Cautious Push", "Battle reward", "Medium risk", Encounter(campaignId, missionIndex, "node-4a", "medium", seed), "node-5a"),
                Node("node-5a", pathPrefix + "-safe", RunMapNodeType.RandomEvent, 5, "Camp Rumor", "Event outcome", "No direct battle risk", string.Empty, "node-6a"),
                Node("node-6a", pathPrefix + "-safe", RunMapNodeType.Battle, EnemyEncounterDifficulty.Medium, 6, "Supply Guard", "Battle reward", "Medium risk", Encounter(campaignId, missionIndex, "node-6a", "medium", seed), "node-9")),
            Path(pathPrefix + "-risk", selectedRouteChoiceId, "Risk Branch", "Harder pressure branch with better reward hints.",
                Node("node-4b", pathPrefix + "-risk", RunMapNodeType.Battle, EnemyEncounterDifficulty.High, 4, "Elite Roadblock", "Improved battle reward", "High risk", Encounter(campaignId, missionIndex, "node-4b", "high", seed), "node-5b"),
                Node("node-5b", pathPrefix + "-risk", RunMapNodeType.RandomEvent, 5, "Strange Shrine", "Event outcome", "Uncertain risk", string.Empty, "node-6b"),
                Node("node-6b", pathPrefix + "-risk", RunMapNodeType.Battle, EnemyEncounterDifficulty.High, 6, "Punishing Patrol", "Battle reward", "High risk", Encounter(campaignId, missionIndex, "node-6b", "high", seed), "node-7b"),
                Node("node-7b", pathPrefix + "-risk", RunMapNodeType.RandomEvent, 7, "Risk Bargain", "Event outcome", "Uncertain risk", string.Empty, "node-8b"),
                Node("node-8b", pathPrefix + "-risk", RunMapNodeType.Battle, EnemyEncounterDifficulty.High, 8, "Prize Escort", "Improved battle reward", "High risk", Encounter(campaignId, missionIndex, "node-8b", "high", seed), "node-9")),
            Path(pathPrefix + "-finale", selectedRouteChoiceId, "Mission Finale", "Shared shop and final battle.",
                Node("node-9", pathPrefix + "-finale", RunMapNodeType.Shop, 9, "Run Shop", "Shop offers", "No battle risk", string.Empty, "node-10"),
                Node("node-10", pathPrefix + "-finale", RunMapNodeType.FinalBoss, EnemyEncounterDifficulty.Boss, 10, "Mission Final Battle", "Saved army eligibility", "Final danger", Encounter(campaignId, missionIndex, "node-10", "final", seed), string.Empty))
        };
    }

    private StartingArmyTemplate BuildDistinctArmyOffer(
        List<StartRunUnitDefinition> units,
        int offerIndex,
        List<string> usedUnitSignatures)
    {
        StartingArmyTemplate best = null;
        StartingArmyTemplate firstNonEmpty = null;
        int attemptCount = Math.Max(armyConfig.OfferCount, Math.Max(8, armyConfig.StackCount * 4));

        for (int attemptIndex = 0; attemptIndex < attemptCount; attemptIndex++)
        {
            StartingArmyTemplate candidate = BuildArmyOffer(units, offerIndex, attemptIndex);
            if (best == null)
            {
                best = candidate;
            }

            string signature = BuildUnitSignature(candidate);
            if (string.IsNullOrEmpty(signature))
            {
                continue;
            }

            if (firstNonEmpty == null)
            {
                firstNonEmpty = candidate;
            }

            if (usedUnitSignatures == null ||
                !usedUnitSignatures.Contains(signature))
            {
                return candidate;
            }
        }

        return firstNonEmpty ?? best;
    }

    private StartingArmyTemplate BuildArmyOffer(List<StartRunUnitDefinition> units, int offerIndex, int attemptIndex)
    {
        int attemptSalt = attemptIndex == 0 ? 7103 : BuildSeed(7103, attemptIndex, 9176);
        Random random = new Random(BuildSeed(armyConfig.Seed.Value, offerIndex, attemptSalt));
        List<int> factionPool = SelectFactionPool(units, random, offerIndex);
        List<StartRunUnitDefinition> raceUnits = FilterUnitsByFaction(units, factionPool);
        int[] composition = SelectTierComposition(raceUnits, random);
        if (composition == null || composition.Length == 0)
        {
            return null;
        }

        List<StartRunStackTemplate> stacks = new List<StartRunStackTemplate>();
        List<string> usedUnitIds = new List<string>();
        List<UnitRoleCategory> usedRoleCategories = new List<UnitRoleCategory>();

        for (int i = 0; i < armyConfig.StackCount; i++)
        {
            int tier = composition[Math.Min(i, composition.Length - 1)];
            StartRunUnitDefinition unit = PickUnitForTier(raceUnits, tier, usedUnitIds, usedRoleCategories, random);
            if (unit == null)
            {
                continue;
            }

            if (!usedUnitIds.Contains(unit.UnitId))
            {
                usedUnitIds.Add(unit.UnitId);
            }

            if (!usedRoleCategories.Contains(unit.UnitRoleCategory))
            {
                usedRoleCategories.Add(unit.UnitRoleCategory);
            }

            stacks.Add(new StartRunStackTemplate(
                unit.UnitId,
                unit.Tier,
                1,
                CalculateStackAmount(unit.Cost),
                BuildSkills(unit, random)));
        }

        if (stacks.Count != armyConfig.StackCount || !NormalizeStackPower(stacks))
        {
            return null;
        }

        string templateId = "generated-start-" + armyConfig.Seed.Value.ToString(CultureInfo.InvariantCulture) + "-" + (offerIndex + 1).ToString(CultureInfo.InvariantCulture);
        string displayName = BuildArmyName(stacks, offerIndex);
        string description = "Generated balanced start from the configured ArmyGeneratorRuleSet.";

        return new StartingArmyTemplate(
            templateId,
            templateId + "-v1",
            displayName,
            description,
            armyConfig.StartingGold,
            armyConfig.StartingRerollTokens,
            armyConfig.BattleSkipTokens,
            stacks);
    }

    private static string BuildUnitSignature(StartingArmyTemplate army)
    {
        if (army == null || army.Stacks == null || army.Stacks.Count == 0)
        {
            return string.Empty;
        }

        List<string> unitIds = new List<string>();
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            StartRunStackTemplate stack = army.Stacks[i];
            if (stack != null && !string.IsNullOrEmpty(stack.UnitId))
            {
                unitIds.Add(stack.UnitId);
            }
        }

        unitIds.Sort();
        return string.Join("|", unitIds.ToArray());
    }

    private List<StartRunUnitDefinition> BuildAvailableUnitPool()
    {
        List<StartRunUnitDefinition> source = unitSource == null ? new List<StartRunUnitDefinition>() : unitSource.ListUnits();
        SortUnits(source);

        List<StartRunUnitDefinition> filtered = new List<StartRunUnitDefinition>();
        for (int i = 0; i < source.Count; i++)
        {
            StartRunUnitDefinition unit = source[i];
            if (IsUsableUnit(unit) && IsAllowedFaction(unit) && unlockContext.AllowsUnit(unit.UnitId) && HasAllowedSkill(unit))
            {
                filtered.Add(unit);
            }
        }

        return filtered;
    }

    private bool CanBuildArmy(List<StartRunUnitDefinition> units)
    {
        if (units == null || units.Count == 0)
        {
            return false;
        }

        return BuildValidTierCompositions(units).Count > 0;
    }

    private bool CanBuildVariedArmy(List<StartRunUnitDefinition> units, int minimumUniqueUnits)
    {
        if (units == null || units.Count == 0)
        {
            return false;
        }

        int clampedMinimum = Math.Max(1, Math.Min(armyConfig.StackCount, minimumUniqueUnits));
        List<int[]> compositions = BuildValidTierCompositions(units);
        for (int i = 0; i < compositions.Count; i++)
        {
            int[] composition = compositions[i];
            if (CanBuildComposition(units, composition) && CountUniqueUnitsForComposition(units, composition) >= clampedMinimum)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanBuildComposition(List<StartRunUnitDefinition> units, int[] composition)
    {
        if (units == null || composition == null)
        {
            return false;
        }

        for (int i = 0; i < composition.Length; i++)
        {
            if (!HasTier(units, composition[i]))
            {
                return false;
            }
        }

        return true;
    }

    private int MinimumUniqueUnitsFor(List<StartRunUnitDefinition> units)
    {
        return Math.Max(1, Math.Min(3, Math.Min(armyConfig.StackCount, CountUniqueUnitIds(units))));
    }

    private static int CountUniqueUnitsForComposition(List<StartRunUnitDefinition> units, int[] composition)
    {
        List<string> usedUnitIds = new List<string>();
        if (units == null || composition == null)
        {
            return 0;
        }

        for (int i = 0; i < composition.Length; i++)
        {
            for (int j = 0; j < units.Count; j++)
            {
                StartRunUnitDefinition unit = units[j];
                if (unit != null && TierNumber(unit.Tier) == composition[i] && !usedUnitIds.Contains(unit.UnitId))
                {
                    usedUnitIds.Add(unit.UnitId);
                    break;
                }
            }
        }

        return usedUnitIds.Count;
    }

    private static int CountUniqueUnitIds(List<StartRunUnitDefinition> units)
    {
        List<string> unitIds = new List<string>();
        if (units == null)
        {
            return 0;
        }

        for (int i = 0; i < units.Count; i++)
        {
            StartRunUnitDefinition unit = units[i];
            if (unit != null && !unitIds.Contains(unit.UnitId))
            {
                unitIds.Add(unit.UnitId);
            }
        }

        return unitIds.Count;
    }

    private int[] SelectTierComposition(List<StartRunUnitDefinition> units, Random random)
    {
        List<int[]> valid = BuildValidTierCompositions(units);
        List<int[]> preferred = new List<int[]>();
        int minimumUniqueUnits = MinimumUniqueUnitsFor(units);

        for (int i = 0; i < valid.Count; i++)
        {
            int[] composition = valid[i];
            if (CanBuildComposition(units, composition) && CountUniqueUnitsForComposition(units, composition) >= minimumUniqueUnits)
            {
                preferred.Add(composition);
            }
        }

        if (preferred.Count > 0)
        {
            return preferred[random.Next(preferred.Count)];
        }

        return valid.Count == 0 ? null : valid[random.Next(valid.Count)];
    }

    private List<int[]> BuildValidTierCompositions(List<StartRunUnitDefinition> units)
    {
        List<int[]> result = new List<int[]>();
        int[] maxCounts = TierMaxCounts();
        int[] counts = new int[4];
        List<int> current = new List<int>();
        BuildValidTierCompositions(units, maxCounts, counts, current, result);
        return result;
    }

    private void BuildValidTierCompositions(
        List<StartRunUnitDefinition> units,
        int[] maxCounts,
        int[] counts,
        List<int> current,
        List<int[]> result)
    {
        if (current.Count == armyConfig.StackCount)
        {
            int[] composition = current.ToArray();
            if (CanBuildComposition(units, composition))
            {
                result.Add(composition);
            }

            return;
        }

        for (int tier = 1; tier <= 4; tier++)
        {
            int index = tier - 1;
            if (counts[index] >= maxCounts[index] || !HasTier(units, tier))
            {
                continue;
            }

            counts[index]++;
            current.Add(tier);
            BuildValidTierCompositions(units, maxCounts, counts, current, result);
            current.RemoveAt(current.Count - 1);
            counts[index]--;
        }
    }

    private int[] TierMaxCounts()
    {
        return new[]
        {
            ruleSet.MaxTier1Count,
            ruleSet.MaxTier2Count,
            ruleSet.MaxTier3Count,
            ruleSet.MaxTier4Count
        };
    }

    private List<int> SelectFactionPool(List<StartRunUnitDefinition> units, Random random, int offerIndex)
    {
        List<int> factions = BuildFactionList(units);
        List<List<int>> validPools = new List<List<int>>();
        List<List<int>> preferredPools = new List<List<int>>();
        int minimumUniqueUnits = MinimumUniqueUnitsFor(units);
        int mixCount = Math.Max(1, Math.Min(ruleSet.FactionMixCount, factions.Count));

        BuildFactionPools(factions, mixCount, 0, new List<int>(), validPools, preferredPools, units, minimumUniqueUnits);

        if (preferredPools.Count > 0)
        {
            int index = Math.Abs(random.Next(preferredPools.Count) + offerIndex) % preferredPools.Count;
            return preferredPools[index];
        }

        if (validPools.Count > 0)
        {
            int index = Math.Abs(random.Next(validPools.Count) + offerIndex) % validPools.Count;
            return validPools[index];
        }

        return new List<int>();
    }

    private void BuildFactionPools(
        List<int> factions,
        int mixCount,
        int startIndex,
        List<int> current,
        List<List<int>> validPools,
        List<List<int>> preferredPools,
        List<StartRunUnitDefinition> units,
        int minimumUniqueUnits)
    {
        if (current.Count > 0)
        {
            List<StartRunUnitDefinition> poolUnits = FilterUnitsByFaction(units, current);
            if (CanBuildArmy(poolUnits))
            {
                List<int> pool = new List<int>(current);
                validPools.Add(pool);
                if (CanBuildVariedArmy(poolUnits, minimumUniqueUnits))
                {
                    preferredPools.Add(pool);
                }
            }
        }

        if (current.Count == mixCount)
        {
            return;
        }

        for (int i = startIndex; i < factions.Count; i++)
        {
            current.Add(factions[i]);
            BuildFactionPools(factions, mixCount, i + 1, current, validPools, preferredPools, units, minimumUniqueUnits);
            current.RemoveAt(current.Count - 1);
        }
    }

    private static List<int> BuildFactionList(List<StartRunUnitDefinition> units)
    {
        List<int> factions = new List<int>();
        for (int i = 0; i < units.Count; i++)
        {
            int faction = units[i] == null ? UnitFactionResolver.UnknownFactionId : units[i].FactionId;
            if (faction > 0 && !factions.Contains(faction))
            {
                factions.Add(faction);
            }
        }

        factions.Sort();
        return factions;
    }

    private static List<StartRunUnitDefinition> FilterUnitsByFaction(List<StartRunUnitDefinition> units, List<int> factionPool)
    {
        List<StartRunUnitDefinition> result = new List<StartRunUnitDefinition>();
        if (units == null || factionPool == null || factionPool.Count == 0)
        {
            return result;
        }

        for (int i = 0; i < units.Count; i++)
        {
            StartRunUnitDefinition unit = units[i];
            int faction = unit == null ? UnitFactionResolver.UnknownFactionId : unit.FactionId;
            if (factionPool.Contains(faction))
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private StartRunUnitDefinition PickUnitForTier(
        List<StartRunUnitDefinition> units,
        int tier,
        List<string> usedUnitIds,
        List<UnitRoleCategory> usedRoleCategories,
        Random random)
    {
        List<StartRunUnitDefinition> matching = new List<StartRunUnitDefinition>();
        for (int i = 0; i < units.Count; i++)
        {
            StartRunUnitDefinition unit = units[i];
            if (TierNumber(unit.Tier) == tier)
            {
                matching.Add(unit);
            }
        }

        if (matching.Count > 1)
        {
            List<StartRunUnitDefinition> unused = new List<StartRunUnitDefinition>();
            for (int i = 0; i < matching.Count; i++)
            {
                if (usedUnitIds == null || !usedUnitIds.Contains(matching[i].UnitId))
                {
                    unused.Add(matching[i]);
                }
            }

            if (unused.Count > 0)
            {
                matching = unused;
            }
        }

        if (matching.Count > 1)
        {
            List<StartRunUnitDefinition> unusedRoles = new List<StartRunUnitDefinition>();
            for (int i = 0; i < matching.Count; i++)
            {
                if (usedRoleCategories == null || !usedRoleCategories.Contains(matching[i].UnitRoleCategory))
                {
                    unusedRoles.Add(matching[i]);
                }
            }

            if (unusedRoles.Count > 0)
            {
                matching = unusedRoles;
            }
        }

        if (matching.Count == 0)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (usedUnitIds == null || !usedUnitIds.Contains(units[i].UnitId))
                {
                    matching.Add(units[i]);
                }
            }
        }

        if (matching.Count == 0)
        {
            matching.AddRange(units);
        }

        return matching.Count == 0 ? null : matching[random.Next(matching.Count)];
    }

    private List<StartRunSkillTemplate> BuildSkills(StartRunUnitDefinition unit, Random random)
    {
        List<string> legal = new List<string>();
        for (int i = 0; unit != null && unit.SkillIds != null && i < unit.SkillIds.Count; i++)
        {
            if (unlockContext.AllowsSkill(unit.SkillIds[i]))
            {
                legal.Add(unit.SkillIds[i]);
            }
        }

        List<StartRunSkillTemplate> result = new List<StartRunSkillTemplate>();
        if (legal.Count > 0)
        {
            result.Add(new StartRunSkillTemplate(legal[random.Next(legal.Count)], true));
        }

        return result;
    }

    private int CalculateStackAmount(int unitCost)
    {
        if (unitCost <= 0)
        {
            return 1;
        }

        float targetStackValue = (float)armyConfig.TargetTotalValue / armyConfig.StackCount;
        return Math.Max(1, (int)Math.Round(targetStackValue / unitCost));
    }

    private bool NormalizeStackPower(List<StartRunStackTemplate> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return false;
        }

        int guard = 0;
        while (guard++ < 1000)
        {
            int totalPower = CalculateTotalPower(stacks);
            if (totalPower >= ruleSet.MinPower && totalPower <= ruleSet.MaxPower)
            {
                return true;
            }

            if (totalPower < ruleSet.MinPower)
            {
                StartRunStackTemplate stackToIncrease = FindIncreaseStack(stacks, ruleSet.MaxPower - totalPower);
                if (stackToIncrease == null)
                {
                    return false;
                }

                stackToIncrease.Amount++;
                continue;
            }

            StartRunStackTemplate stackToReduce = FindReduceStack(stacks);
            if (stackToReduce == null)
            {
                return false;
            }

            stackToReduce.Amount--;
        }

        return false;
    }

    private int CalculateTotalPower(List<StartRunStackTemplate> stacks)
    {
        int total = 0;
        for (int i = 0; stacks != null && i < stacks.Count; i++)
        {
            StartRunStackTemplate stack = stacks[i];
            StartRunUnitDefinition unit = stack == null || unitSource == null ? null : unitSource.FindUnit(stack.UnitId);
            if (unit != null)
            {
                total += Math.Max(0, stack.Amount) * Math.Max(0, unit.Cost);
            }
        }

        return total;
    }

    private StartRunStackTemplate FindIncreaseStack(List<StartRunStackTemplate> stacks, int maxAddedPower)
    {
        StartRunStackTemplate best = null;
        int bestCost = 0;
        for (int i = 0; stacks != null && i < stacks.Count; i++)
        {
            StartRunStackTemplate stack = stacks[i];
            StartRunUnitDefinition unit = stack == null || unitSource == null ? null : unitSource.FindUnit(stack.UnitId);
            int cost = unit == null ? 0 : unit.Cost;
            if (cost <= 0 || cost > maxAddedPower)
            {
                continue;
            }

            if (best == null || cost > bestCost)
            {
                best = stack;
                bestCost = cost;
            }
        }

        return best;
    }

    private StartRunStackTemplate FindReduceStack(List<StartRunStackTemplate> stacks)
    {
        StartRunStackTemplate best = null;
        int bestCost = 0;
        for (int i = 0; stacks != null && i < stacks.Count; i++)
        {
            StartRunStackTemplate stack = stacks[i];
            StartRunUnitDefinition unit = stack == null || unitSource == null ? null : unitSource.FindUnit(stack.UnitId);
            int cost = unit == null ? 0 : unit.Cost;
            if (cost <= 0 || stack.Amount <= 1)
            {
                continue;
            }

            if (best == null || cost > bestCost)
            {
                best = stack;
                bestCost = cost;
            }
        }

        return best;
    }

    private string BuildArmyName(List<StartRunStackTemplate> stacks, int offerIndex)
    {
        string[] adjectives = { "Stone", "Wisp", "Iron", "Bright", "Dust", "Wild" };
        string[] nouns = { "Spark", "Screen", "Line", "Pact", "Guard", "Surge" };
        int seed = BuildSeed(armyConfig.Seed.Value, offerIndex, stacks == null ? 0 : stacks.Count);
        return adjectives[Math.Abs(seed) % adjectives.Length] + " " + nouns[Math.Abs(seed / 7) % nouns.Length];
    }

    private bool HasAllowedSkill(StartRunUnitDefinition unit)
    {
        if (unit == null || unit.SkillIds == null || unit.SkillIds.Count == 0)
        {
            return false;
        }

        if (!unlockContext.HasSkillRestrictions)
        {
            return true;
        }

        for (int i = 0; i < unit.SkillIds.Count; i++)
        {
            if (unlockContext.AllowsSkill(unit.SkillIds[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAllowedFaction(StartRunUnitDefinition unit)
    {
        int[] allowedFactionIds = ruleSet.AllowedFactionIds;
        if (allowedFactionIds.Length == 0)
        {
            return false;
        }

        int factionId = unit == null ? UnitFactionResolver.UnknownFactionId : unit.FactionId;
        for (int i = 0; i < allowedFactionIds.Length; i++)
        {
            if (allowedFactionIds[i] == factionId)
            {
                return true;
            }
        }

        return false;
    }

    private static RunMapPathDefinition Path(string pathId, string routeChoiceId, string displayName, string bias, params RunMapNodeDefinition[] nodes)
    {
        return new RunMapPathDefinition(pathId, routeChoiceId, displayName, bias, new List<RunMapNodeDefinition>(nodes));
    }

    private static RunMapNodeDefinition Node(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        EnemyEncounterDifficulty difficulty,
        int stage,
        string displayName,
        string rewardHint,
        string riskHint,
        string encounterId,
        string nextNodeId)
    {
        return new RunMapNodeDefinition(nodeId, pathId, nodeType, stage, displayName, rewardHint, riskHint, encounterId, difficulty, nextNodeId);
    }

    private static RunMapNodeDefinition Node(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        EnemyEncounterDifficulty difficulty,
        int stage,
        string displayName,
        string rewardHint,
        string riskHint,
        string encounterId,
        List<string> nextNodeIds)
    {
        return new RunMapNodeDefinition(nodeId, pathId, nodeType, stage, displayName, rewardHint, riskHint, encounterId, difficulty, nextNodeIds);
    }

    private static RunMapNodeDefinition Node(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        int stage,
        string displayName,
        string rewardHint,
        string riskHint,
        string encounterId,
        string nextNodeId)
    {
        return new RunMapNodeDefinition(nodeId, pathId, nodeType, stage, displayName, rewardHint, riskHint, encounterId, nextNodeId);
    }

    private static RunMapNodeDefinition Node(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        int stage,
        string displayName,
        string rewardHint,
        string riskHint,
        string encounterId,
        List<string> nextNodeIds)
    {
        return new RunMapNodeDefinition(nodeId, pathId, nodeType, stage, displayName, rewardHint, riskHint, encounterId, nextNodeIds);
    }

    private static string Encounter(string campaignId, int missionIndex, string nodeId, string band, int seed)
    {
        int variant = Math.Abs(BuildSeed(seed, StableHash(nodeId), StableHash(band))) % 3 + 1;
        return "enc-prd35-" + campaignId + "-m" + missionIndex.ToString(CultureInfo.InvariantCulture) + "-" + nodeId + "-" + band + "-" + variant.ToString(CultureInfo.InvariantCulture);
    }

    private string BuildRouteChoiceId(string campaignId, int missionIndex)
    {
        return "campaign-" + campaignId + "-mission-" + missionIndex.ToString(CultureInfo.InvariantCulture) + "-seed-" + routeConfig.Seed.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string ParseCampaignId(string routeChoiceId)
    {
        if (string.IsNullOrEmpty(routeChoiceId))
        {
            return "forest";
        }

        string[] parts = routeChoiceId.Split('-');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "campaign")
            {
                return NormalizeToken(parts[i + 1], "forest");
            }
        }

        return "forest";
    }

    private static int ParseMissionIndex(string routeChoiceId)
    {
        if (string.IsNullOrEmpty(routeChoiceId))
        {
            return 1;
        }

        string[] parts = routeChoiceId.Split('-');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "mission")
            {
                int parsed;
                if (int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return Math.Max(1, parsed);
                }
            }
        }

        return 1;
    }

    private static int ParseSeedValue(string routeChoiceId, int fallback)
    {
        if (string.IsNullOrEmpty(routeChoiceId))
        {
            return fallback;
        }

        string[] parts = routeChoiceId.Split('-');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "seed")
            {
                int parsed;
                if (int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed == 0 ? fallback : parsed;
                }
            }
        }

        return fallback;
    }

    private static bool IsUsableUnit(StartRunUnitDefinition unit)
    {
        return unit != null && !string.IsNullOrEmpty(unit.UnitId) && unit.Cost > 0 && unit.SkillIds != null && unit.SkillIds.Count > 0;
    }

    private static bool HasTier(List<StartRunUnitDefinition> units, int tier)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (TierNumber(units[i].Tier) == tier)
            {
                return true;
            }
        }

        return false;
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
            case "V":
                return 5;
            default:
                return 1;
        }
    }

    private static void SortUnits(List<StartRunUnitDefinition> units)
    {
        units.Sort(delegate(StartRunUnitDefinition a, StartRunUnitDefinition b)
        {
            string left = a == null ? string.Empty : a.UnitId;
            string right = b == null ? string.Empty : b.UnitId;
            return string.CompareOrdinal(left, right);
        });
    }

    private static int BuildSeed(int a, int b, int c)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + a;
            seed = seed * 31 + b;
            seed = seed * 31 + c;
            return seed;
        }
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 23;
            string text = value ?? string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash;
        }
    }

    private static string NormalizeToken(string value, string fallback)
    {
        if (string.IsNullOrEmpty(value))
        {
            return fallback;
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

        return length == 0 ? fallback : new string(buffer, 0, length);
    }

    private static string ToDisplayName(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "Campaign";
        }

        return char.ToUpperInvariant(token[0]) + token.Substring(1);
    }

}
