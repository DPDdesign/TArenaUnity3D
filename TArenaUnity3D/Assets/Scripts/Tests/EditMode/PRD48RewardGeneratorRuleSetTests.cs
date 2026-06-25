#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PRD48RewardGeneratorRuleSetTests
{
    [Test]
    public void DemoFormula_LowMediumHigh_CalculateExpectedArmyGrowthReward()
    {
        RewardGeneratorRuleSet low = CreateRuleSet(1.00f, 0.45f, 0.12f, 1.20f, 1.00f);
        RewardGeneratorRuleSet medium = CreateRuleSet(1.00f, 0.55f, 0.18f, 1.20f, 1.00f);
        RewardGeneratorRuleSet high = CreateRuleSet(0.80f, 0.65f, 0.25f, 1.20f, 1.00f);

        Assert.That(low.CalculateArmyGrowthReward(1000, 5000, 20000), Is.EqualTo(3400));
        Assert.That(medium.CalculateArmyGrowthReward(1000, 5000, 20000), Is.EqualTo(4600));
        Assert.That(high.CalculateArmyGrowthReward(1000, 5000, 20000), Is.EqualTo(5800));

        UnityEngine.Object.DestroyImmediate(low);
        UnityEngine.Object.DestroyImmediate(medium);
        UnityEngine.Object.DestroyImmediate(high);
    }

    [Test]
    public void Formula_ClampsBattleLossToZeroAndCapsRecoveryByEnemyValue()
    {
        RewardGeneratorRuleSet rules = CreateRuleSet(1.00f, 0.50f, 0.10f, 1.20f, 1.00f);

        Assert.That(rules.CalculateArmyGrowthReward(0, 5000, 20000), Is.EqualTo(2000));
        Assert.That(rules.CalculateArmyGrowthReward(10000, 1000, 20000), Is.EqualTo(2500));

        UnityEngine.Object.DestroyImmediate(rules);
    }

    [Test]
    public void Formula_UsesPreBattleArmyValueAndFamilyMultipliers()
    {
        RewardGeneratorRuleSet rules = CreateRuleSet(1.00f, 0.50f, 0.10f, 1.20f, 1.00f);

        Assert.That(rules.CalculateArmyGrowthReward(1000, 5000, 20000), Is.EqualTo(3000));
        Assert.That(rules.CalculateRewardValue(RewardMapOperationType.AddUnits, 1000, 5000, 20000), Is.EqualTo(3600));
        Assert.That(rules.CalculateRewardValue(RewardMapOperationType.AddStack, 1000, 5000, 20000), Is.EqualTo(3600));
        Assert.That(rules.CalculateRewardValue(RewardMapOperationType.PromoteStack, 1000, 5000, 20000), Is.EqualTo(3000));
        Assert.That(rules.CalculateRewardValue(RewardMapOperationType.DowngradeStack, 1000, 5000, 20000), Is.EqualTo(3000));

        UnityEngine.Object.DestroyImmediate(rules);
    }

    [Test]
    public void MaterializedGenerator_UsesRulesetBudgetAndKeepsPlannedOperationTypes()
    {
        RewardGeneratorRuleSet rules = CreateRuleSet(1.00f, 0.45f, 0.12f, 1.20f, 1.00f);
        RewardMapMaterializedGenerator generator = new RewardMapMaterializedGenerator(
            new TestRewardUnitCatalog(),
            rules,
            new RewardGeneratorValueContext(1000, 5000, 20000));

        RewardMapChoiceViewData choice = generator.BuildChoice(
            "run-48",
            48,
            4848,
            1,
            0,
            CreateArmy(),
            new RewardMapBattleResultSummary("battle-48", "Victory", 0, 0),
            new List<RewardMapOperationType>
            {
                RewardMapOperationType.AddUnits,
                RewardMapOperationType.PromoteStack,
                RewardMapOperationType.DowngradeStack
            });

        Assert.That(choice.Cards.Count, Is.EqualTo(3));
        Assert.That(choice.Cards[0].Operation.Type, Is.EqualTo(RewardMapOperationType.AddUnits));
        Assert.That(choice.Cards[1].Operation.Type, Is.EqualTo(RewardMapOperationType.PromoteStack));
        Assert.That(choice.Cards[2].Operation.Type, Is.EqualTo(RewardMapOperationType.DowngradeStack));
        Assert.That(choice.Cards[0].Operation.Amount, Is.EqualTo(41));
        Assert.That(choice.Cards[1].Operation.Amount, Is.EqualTo(42));

        UnityEngine.Object.DestroyImmediate(rules);
    }

    [Test]
    public void GeneratedBattleDifficultyWithoutRewardRuleset_FailsClearly()
    {
        EnemyEncounterRuleCatalog catalog = ScriptableObject.CreateInstance<EnemyEncounterRuleCatalog>();
        ArmyGeneratorRuleSet armyRules = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        catalog.Entries = new List<EnemyEncounterRule>
        {
            new EnemyEncounterRule(EnemyEncounterDifficulty.Low, armyRules, string.Empty)
        };

        EnemyEncounterRuleLookupResult result = catalog.Resolve(EnemyEncounterDifficulty.Low);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(EnemyEncounterRuleLookupError.MissingRewardGeneratorRuleSet));
        Assert.That(result.Message, Does.Contain("RewardGeneratorRuleSet"));

        UnityEngine.Object.DestroyImmediate(catalog);
        UnityEngine.Object.DestroyImmediate(armyRules);
    }

    private static RewardGeneratorRuleSet CreateRuleSet(float lossRate, float enemyRate, float growth, float quantity, float tier)
    {
        RewardGeneratorRuleSet rules = ScriptableObject.CreateInstance<RewardGeneratorRuleSet>();
        rules.Configure(lossRate, enemyRate, growth, quantity, tier);
        return rules;
    }

    private static RewardMapArmySnapshot CreateArmy()
    {
        return new RewardMapArmySnapshot(
            "army-prd48",
            5000,
            new List<RewardMapStackSnapshot>
            {
                new RewardMapStackSnapshot("slot-0", "Mid", "Mid", "II", 1, 50, 0, 5000, new List<RewardMapSkillState>())
            });
    }

    private sealed class TestRewardUnitCatalog : IRewardMapUnitPoolSource
    {
        private readonly Dictionary<string, RunShopUnitDefinition> units = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Low", new RunShopUnitDefinition("Low", "Low", "I", 50, 1, new List<string>()) },
            { "Mid", new RunShopUnitDefinition("Mid", "Mid", "II", 100, 1, new List<string>()) },
            { "High", new RunShopUnitDefinition("High", "High", "III", 200, 1, new List<string>()) },
            { "Width", new RunShopUnitDefinition("Width", "Width", "I", 100, 2, new List<string>()) }
        };

        public RunShopUnitDefinition FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<RunShopUnitDefinition> ListUnits()
        {
            return new List<RunShopUnitDefinition>(units.Values);
        }
    }
}
#endif
