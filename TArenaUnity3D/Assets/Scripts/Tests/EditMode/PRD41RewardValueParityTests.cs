using System;
using System.Collections.Generic;
using NUnit.Framework;

public class PRD41RewardValueParityTests
{
    private const int AverageStackValue = 5000;
    private const int ShapeTargetGain = 1000;
    private const int RawGrowthTargetGain = 1200;
    private const int TargetBandPercent = 20;

    [Test]
    public void MoreUnitsAndAddStack_ScaleFromAverageLiveStackValue()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapArmySnapshot army = CreateHighValueArmy();

        RewardMapCardViewData moreUnits = FindGeneratedCard(units, army, RewardMapOperationType.AddUnits);
        RewardMapCardViewData addStack = FindGeneratedCard(units, army, RewardMapOperationType.AddStack);

        AssertGainWithinTarget(GeneratedGain(moreUnits, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(addStack, army, units), RawGrowthTargetGain);
        Assert.That(ArmyContainsUnit(army, addStack.Operation.UnitId), Is.False);
    }

    [Test]
    public void PromoteAndDowngrade_AddTargetValueToConvertedStack()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapArmySnapshot army = CreateHighValueArmy();

        RewardMapCardViewData promote = FindGeneratedCard(units, army, RewardMapOperationType.PromoteStack);
        RewardMapCardViewData downgrade = FindGeneratedCard(units, army, RewardMapOperationType.DowngradeStack);

        AssertGainWithinTarget(GeneratedGain(promote, army, units), ShapeTargetGain);
        AssertGainWithinTarget(GeneratedGain(downgrade, army, units), ShapeTargetGain);
    }

    [Test]
    public void AddStack_DoesNotReturnTinyLateRunReward()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapArmySnapshot army = CreateHighValueArmy();

        RewardMapCardViewData addStack = FindGeneratedCard(units, army, RewardMapOperationType.AddStack);
        int gain = GeneratedGain(addStack, army, units);

        Assert.That(gain, Is.GreaterThanOrEqualTo(MinAccepted(RawGrowthTargetGain)));
        Assert.That(gain, Is.GreaterThan(50));
    }

    [Test]
    public void Rewards_ScaleFromAmountTimesUnitCost_WhenSnapshotCombatValueIsPerUnit()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapArmySnapshot army = CreatePerUnitCombatValueArmy();

        RewardMapCardViewData moreUnits = FindGeneratedCard(units, army, RewardMapOperationType.AddUnits);
        RewardMapCardViewData addStack = FindGeneratedCard(units, army, RewardMapOperationType.AddStack);
        RewardMapCardViewData promote = FindGeneratedCard(units, army, RewardMapOperationType.PromoteStack);

        AssertGainWithinTarget(GeneratedGain(moreUnits, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(addStack, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(promote, army, units), ShapeTargetGain);
    }

    [Test]
    public void Rewards_UseGlobalArmyAverage_WhenStackSnapshotsAreLowerThanArmyTotal()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapArmySnapshot army = CreateGlobalTotalValueArmy();

        RewardMapCardViewData moreUnits = FindGeneratedCard(units, army, RewardMapOperationType.AddUnits);
        RewardMapCardViewData addStack = FindGeneratedCard(units, army, RewardMapOperationType.AddStack);

        AssertGainWithinTarget(GeneratedGain(moreUnits, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(addStack, army, units), RawGrowthTargetGain);
    }

    private static RewardMapCardViewData FindGeneratedCard(
        TestRewardUnitCatalog units,
        RewardMapArmySnapshot army,
        RewardMapOperationType operationType)
    {
        RewardMapMaterializedGenerator generator = new RewardMapMaterializedGenerator(units);
        for (int seed = 1; seed <= 5000; seed++)
        {
            RewardMapChoiceViewData choice = generator.BuildChoice(
                "run-" + seed,
                3,
                seed,
                1,
                0,
                army,
                new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0));

            for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
            {
                RewardMapCardViewData card = choice.Cards[i];
                if (card != null && card.Operation != null && card.Operation.Type == operationType)
                {
                    return card;
                }
            }
        }

        Assert.Fail("Could not generate operation type " + operationType + " in deterministic seed range.");
        return null;
    }

    private static int GeneratedGain(RewardMapCardViewData card, RewardMapArmySnapshot army, TestRewardUnitCatalog units)
    {
        Assert.That(card, Is.Not.Null);
        Assert.That(card.Operation, Is.Not.Null);

        RewardMapOperation operation = card.Operation;
        if (operation.Type == RewardMapOperationType.AddUnits)
        {
            RunShopUnitDefinition unit = units.FindUnit(operation.UnitId);
            Assert.That(unit, Is.Not.Null);
            return operation.Amount * unit.Cost;
        }

        if (operation.Type == RewardMapOperationType.AddStack)
        {
            RunShopUnitDefinition unit = units.FindUnit(operation.UnitId);
            Assert.That(unit, Is.Not.Null);
            return operation.Amount * unit.Cost;
        }

        if (operation.Type == RewardMapOperationType.PromoteStack || operation.Type == RewardMapOperationType.DowngradeStack)
        {
            RewardMapStackSnapshot source = FindStack(army, operation.StackId);
            RunShopUnitDefinition sourceUnit = units.FindUnit(operation.UnitId);
            RunShopUnitDefinition targetUnit = units.FindUnit(operation.ToUnitId);
            Assert.That(source, Is.Not.Null);
            Assert.That(sourceUnit, Is.Not.Null);
            Assert.That(targetUnit, Is.Not.Null);
            return operation.Amount * targetUnit.Cost - StackValue(source, sourceUnit.Cost);
        }

        throw new InvalidOperationException("Unsupported operation type " + operation.Type);
    }

    private static void AssertGainWithinTarget(int gain, int target)
    {
        Assert.That(gain, Is.GreaterThanOrEqualTo(MinAccepted(target)));
        Assert.That(gain, Is.LessThanOrEqualTo(MaxAccepted(target)));
    }

    private static int MinAccepted(int target)
    {
        return target - (target * TargetBandPercent / 100);
    }

    private static int MaxAccepted(int target)
    {
        return target + (target * TargetBandPercent / 100);
    }

    private static RewardMapArmySnapshot CreateHighValueArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 100, 50),
            Stack("slot-1", "Axeman", "Axeman", "II", 40, 125),
            Stack("slot-2", "Healer", "Healer", "I", 50, 100),
            Stack("slot-3", "Wisp", "Wisp", "I", 100, 50)
        };

        return new RewardMapArmySnapshot("army-high-value", AverageStackValue * stacks.Count, stacks);
    }

    private static RewardMapArmySnapshot CreatePerUnitCombatValueArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 100, 50, 50),
            Stack("slot-1", "Axeman", "Axeman", "II", 40, 125, 125),
            Stack("slot-2", "Healer", "Healer", "I", 50, 100, 100),
            Stack("slot-3", "Wisp", "Wisp", "I", 100, 50, 50)
        };

        return new RewardMapArmySnapshot("army-per-unit-combat-value", AverageStackValue * stacks.Count, stacks);
    }

    private static RewardMapArmySnapshot CreateGlobalTotalValueArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 10, 50),
            Stack("slot-1", "Axeman", "Axeman", "II", 4, 125),
            Stack("slot-2", "Healer", "Healer", "I", 5, 100),
            Stack("slot-3", "Wisp", "Wisp", "I", 10, 50)
        };

        return new RewardMapArmySnapshot("army-global-total-value", AverageStackValue * stacks.Count, stacks);
    }

    private static RewardMapStackSnapshot Stack(string stackId, string unitId, string displayName, string tier, int amount, int unitCost)
    {
        return Stack(stackId, unitId, displayName, tier, amount, unitCost, amount * unitCost);
    }

    private static RewardMapStackSnapshot Stack(string stackId, string unitId, string displayName, string tier, int amount, int unitCost, int combatValue)
    {
        return new RewardMapStackSnapshot(
            stackId,
            unitId,
            displayName,
            tier,
            1,
            amount,
            0,
            combatValue,
            new List<RewardMapSkillState>());
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

    private static RewardMapStackSnapshot FindStack(RewardMapArmySnapshot army, string stackId)
    {
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.StackId == stackId)
            {
                return stack;
            }
        }

        return null;
    }

    private static bool ArmyContainsUnit(RewardMapArmySnapshot army, string unitId)
    {
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.UnitId == unitId)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class TestRewardUnitCatalog : IRewardMapUnitPoolSource
    {
        private readonly Dictionary<string, RunShopUnitDefinition> units = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "I", 50) },
            { "Axeman", Unit("Axeman", "II", 125) },
            { "HeavyHitter", Unit("HeavyHitter", "III", 250) },
            { "Healer", Unit("Healer", "I", 100) },
            { "Tank", Unit("Tank", "II", 150) },
            { "Wisp", Unit("Wisp", "I", 50) },
            { "StoneGolem", Unit("StoneGolem", "II", 150) },
            { "Trapper", Unit("Trapper", "I", 60) },
            { "Specialist", Unit("Specialist", "I", 75) }
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

        private static RunShopUnitDefinition Unit(string unitId, string tier, int cost)
        {
            return new RunShopUnitDefinition(unitId, unitId, tier, cost, new List<string>());
        }
    }
}
