#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;

public class PRD42RewardMaterializedSlotContractTests
{
    private const int AverageStackValue = 5000;
    private const int ShapeTargetGain = 1000;
    private const int RawGrowthTargetGain = 1200;
    private const int TargetBandPercent = 20;

    [Test]
    public void Choice_UsesThreeDistinctNormalOperationTypes_WhenNormalSlotsAreLegal()
    {
        TestRewardUnitCatalog units = CreateValueParityCatalog();
        RewardMapChoiceViewData choice = BuildChoice(units, CreateHighValueArmy(), 17);

        Assert.That(choice.Cards.Count, Is.EqualTo(3));
        AssertNormalSlotsAreDistinct(choice, 3);
        Assert.That(CountOperation(choice, RewardMapOperationType.GainCurrency), Is.EqualTo(0));
    }

    [Test]
    public void OneImpossibleNormalSlot_BecomesDisabledCard_AndDoesNotCreateRunGold()
    {
        TestRewardUnitCatalog units = CreateValueParityCatalog();
        RewardMapChoiceViewData choice = FindChoice(
            units,
            CreateFullArmyWithNoOpenSlot(),
            delegate(RewardMapChoiceViewData candidate)
            {
                return CountDisabledNormalCards(candidate) == 1 &&
                    CountOperation(candidate, RewardMapOperationType.GainCurrency) == 0;
            });

        RewardMapCardViewData disabled = FindDisabledNormalCard(choice);

        Assert.That(choice.Cards.Count, Is.EqualTo(3));
        Assert.That(disabled, Is.Not.Null);
        Assert.That(disabled.Operation.Type, Is.EqualTo(RewardMapOperationType.AddStack));
        Assert.That(disabled.Legal, Is.False);
        Assert.That(disabled.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
        Assert.That(disabled.AfterText, Is.EqualTo("No legal target"));
        Assert.That(CountOperation(choice, RewardMapOperationType.GainCurrency), Is.EqualTo(0));
    }

    [Test]
    public void AllNormalSlotsImpossible_AppendsEmergencyRunGold_WithoutDroppingPlannedNormalSlots()
    {
        TestRewardUnitCatalog units = new TestRewardUnitCatalog();
        RewardMapChoiceViewData choice = BuildChoice(units, new RewardMapArmySnapshot("empty", 0, new List<RewardMapStackSnapshot>()), 29);

        Assert.That(choice.Cards.Count, Is.EqualTo(4));
        AssertNormalSlotsAreDistinct(choice, 3);

        for (int i = 0; i < 3; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            Assert.That(card.Legal, Is.False);
            Assert.That(card.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
            Assert.That(card.Operation.Type, Is.Not.EqualTo(RewardMapOperationType.GainCurrency));
        }

        RewardMapCardViewData fallback = choice.Cards[3];
        Assert.That(fallback.Legal, Is.True);
        Assert.That(fallback.Operation.Type, Is.EqualTo(RewardMapOperationType.GainCurrency));
        Assert.That(fallback.Operation.CurrencyDelta, Is.GreaterThan(0));
        Assert.That(choice.FocusedCard, Is.SameAs(fallback));
    }

    [Test]
    public void PromoteAndDowngrade_UseCatalogFactionMetadataBeforeNameFallback()
    {
        const int sourceFaction = 77;
        const int wrongFaction = 88;
        TestRewardUnitCatalog units = new TestRewardUnitCatalog()
            .Add("WrongDown", "I", 70, wrongFaction)
            .Add("RightDown", "I", 80, sourceFaction)
            .Add("WrongUp", "III", 120, wrongFaction)
            .Add("RightUp", "III", 130, sourceFaction)
            .Add("CatalogMid", "II", 100, sourceFaction)
            .Add("OpenWidth", "I", 90, 99);

        RewardMapArmySnapshot army = new RewardMapArmySnapshot(
            "metadata-army",
            5000,
            new List<RewardMapStackSnapshot>
            {
                Stack("slot-0", "CatalogMid", "Catalog Mid", "II", 50, 100)
            });

        RewardMapChoiceViewData choice = FindChoice(
            units,
            army,
            delegate(RewardMapChoiceViewData candidate)
            {
                return FindLegalCard(candidate, RewardMapOperationType.PromoteStack) != null &&
                    FindLegalCard(candidate, RewardMapOperationType.DowngradeStack) != null;
            });

        RewardMapCardViewData promote = FindLegalCard(choice, RewardMapOperationType.PromoteStack);
        RewardMapCardViewData downgrade = FindLegalCard(choice, RewardMapOperationType.DowngradeStack);

        Assert.That(promote.Operation.ToUnitId, Is.EqualTo("RightUp"));
        Assert.That(downgrade.Operation.ToUnitId, Is.EqualTo("RightDown"));
    }

    [Test]
    public void LegalCards_PreservePrd41ValueParityTargets()
    {
        TestRewardUnitCatalog units = CreateValueParityCatalog();
        RewardMapArmySnapshot army = CreateHighValueArmy();

        RewardMapCardViewData moreUnits = FindGeneratedLegalCard(units, army, RewardMapOperationType.AddUnits);
        RewardMapCardViewData addStack = FindGeneratedLegalCard(units, army, RewardMapOperationType.AddStack);
        RewardMapCardViewData promote = FindGeneratedLegalCard(units, army, RewardMapOperationType.PromoteStack);
        RewardMapCardViewData downgrade = FindGeneratedLegalCard(units, army, RewardMapOperationType.DowngradeStack);

        AssertGainWithinTarget(GeneratedGain(moreUnits, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(addStack, army, units), RawGrowthTargetGain);
        AssertGainWithinTarget(GeneratedGain(promote, army, units), ShapeTargetGain);
        AssertGainWithinTarget(GeneratedGain(downgrade, army, units), ShapeTargetGain);
    }

    private static RewardMapChoiceViewData BuildChoice(TestRewardUnitCatalog units, RewardMapArmySnapshot army, int seed)
    {
        RewardMapMaterializedGenerator generator = new RewardMapMaterializedGenerator(units);
        return generator.BuildChoice(
            "run-" + seed,
            4,
            seed,
            1,
            0,
            army,
            new RewardMapBattleResultSummary("battle-" + seed, "Victory", 0, 0));
    }

    private static RewardMapChoiceViewData FindChoice(
        TestRewardUnitCatalog units,
        RewardMapArmySnapshot army,
        Predicate<RewardMapChoiceViewData> predicate)
    {
        RewardMapMaterializedGenerator generator = new RewardMapMaterializedGenerator(units);
        for (int seed = 1; seed <= 500; seed++)
        {
            RewardMapChoiceViewData choice = generator.BuildChoice(
                "run-" + seed,
                4,
                seed,
                1,
                0,
                army,
                new RewardMapBattleResultSummary("battle-" + seed, "Victory", 0, 0));

            if (predicate(choice))
            {
                return choice;
            }
        }

        Assert.Fail("Could not find a deterministic PRD42 choice shape in the searched seed range.");
        return null;
    }

    private static RewardMapCardViewData FindGeneratedLegalCard(
        TestRewardUnitCatalog units,
        RewardMapArmySnapshot army,
        RewardMapOperationType operationType)
    {
        RewardMapChoiceViewData choice = FindChoice(
            units,
            army,
            delegate(RewardMapChoiceViewData candidate)
            {
                return FindLegalCard(candidate, operationType) != null;
            });

        return FindLegalCard(choice, operationType);
    }

    private static RewardMapCardViewData FindLegalCard(RewardMapChoiceViewData choice, RewardMapOperationType operationType)
    {
        for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card != null &&
                card.Legal &&
                card.Operation != null &&
                card.Operation.Type == operationType)
            {
                return card;
            }
        }

        return null;
    }

    private static RewardMapCardViewData FindDisabledNormalCard(RewardMapChoiceViewData choice)
    {
        for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card != null &&
                !card.Legal &&
                card.Operation != null &&
                card.Operation.Type != RewardMapOperationType.GainCurrency)
            {
                return card;
            }
        }

        return null;
    }

    private static int CountDisabledNormalCards(RewardMapChoiceViewData choice)
    {
        int count = 0;
        for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card != null &&
                !card.Legal &&
                card.Operation != null &&
                card.Operation.Type != RewardMapOperationType.GainCurrency)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountOperation(RewardMapChoiceViewData choice, RewardMapOperationType operationType)
    {
        int count = 0;
        for (int i = 0; choice != null && choice.Cards != null && i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card != null && card.Operation != null && card.Operation.Type == operationType)
            {
                count++;
            }
        }

        return count;
    }

    private static void AssertNormalSlotsAreDistinct(RewardMapChoiceViewData choice, int slotCount)
    {
        HashSet<RewardMapOperationType> types = new HashSet<RewardMapOperationType>();
        for (int i = 0; i < slotCount; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            Assert.That(card.Operation, Is.Not.Null);
            Assert.That(card.Operation.Type, Is.Not.EqualTo(RewardMapOperationType.GainCurrency));
            Assert.That(types.Add(card.Operation.Type), Is.True);
        }
    }

    private static int GeneratedGain(RewardMapCardViewData card, RewardMapArmySnapshot army, TestRewardUnitCatalog units)
    {
        Assert.That(card, Is.Not.Null);
        Assert.That(card.Legal, Is.True);
        Assert.That(card.Operation, Is.Not.Null);

        RewardMapOperation operation = card.Operation;
        if (operation.Type == RewardMapOperationType.AddUnits || operation.Type == RewardMapOperationType.AddStack)
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

    private static RewardMapArmySnapshot CreateFullArmyWithNoOpenSlot()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 100, 50),
            Stack("slot-1", "Axeman", "Axeman", "II", 40, 125),
            Stack("slot-2", "Healer", "Healer", "I", 50, 100),
            Stack("slot-3", "Tank", "Tank", "II", 34, 150),
            Stack("slot-4", "Wisp", "Wisp", "I", 100, 50),
            Stack("slot-5", "StoneGolem", "Stone Golem", "II", 34, 150),
            Stack("slot-6", "Trapper", "Trapper", "I", 84, 60)
        };

        return new RewardMapArmySnapshot("army-full", AverageStackValue * stacks.Count, stacks);
    }

    private static RewardMapStackSnapshot Stack(string stackId, string unitId, string displayName, string tier, int amount, int unitCost)
    {
        return new RewardMapStackSnapshot(
            stackId,
            unitId,
            displayName,
            tier,
            1,
            amount,
            0,
            amount * unitCost,
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

    private static TestRewardUnitCatalog CreateValueParityCatalog()
    {
        return new TestRewardUnitCatalog()
            .Add("Rusher", "I", 50, UnitFactionResolver.BarbarianFactionId)
            .Add("Axeman", "II", 125, UnitFactionResolver.BarbarianFactionId)
            .Add("HeavyHitter", "III", 250, UnitFactionResolver.BarbarianFactionId)
            .Add("Healer", "I", 100, UnitFactionResolver.LizardFactionId)
            .Add("Tank", "II", 150, UnitFactionResolver.LizardFactionId)
            .Add("Wisp", "I", 50, UnitFactionResolver.GolemElementalFactionId)
            .Add("StoneGolem", "II", 150, UnitFactionResolver.GolemElementalFactionId)
            .Add("Trapper", "I", 60, UnitFactionResolver.LizardFactionId)
            .Add("Specialist", "I", 75, UnitFactionResolver.LizardFactionId);
    }

    private sealed class TestRewardUnitCatalog : IRewardMapUnitPoolSource
    {
        private readonly Dictionary<string, RunShopUnitDefinition> units = new Dictionary<string, RunShopUnitDefinition>();
        private readonly List<RunShopUnitDefinition> orderedUnits = new List<RunShopUnitDefinition>();

        public TestRewardUnitCatalog Add(string unitId, string tier, int cost, int factionId)
        {
            RunShopUnitDefinition unit = new RunShopUnitDefinition(unitId, unitId, tier, cost, factionId, new List<string>());
            units[unitId] = unit;
            orderedUnits.Add(unit);
            return this;
        }

        public RunShopUnitDefinition FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<RunShopUnitDefinition> ListUnits()
        {
            return new List<RunShopUnitDefinition>(orderedUnits);
        }
    }
}
#endif
