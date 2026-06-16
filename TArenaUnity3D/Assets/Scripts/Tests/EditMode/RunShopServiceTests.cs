using System.Collections.Generic;
using NUnit.Framework;

public class RunShopServiceTests
{
    [Test]
    public void BuildVisit_ReturnsLimitedOffersAndFocusedPreview()
    {
        RunShopService service = CreateService(new InMemoryRunShopVisitStore());
        RunShopArmySnapshot army = CreateArmy();

        RunShopVisitViewData visit = service.BuildVisit(
            new RunShopVisitRequest("run-1", "shop-node-1", 120, army),
            "shop-recover-losses");

        Assert.That(visit.GameMode, Is.EqualTo(RunShopGameMode.Offline));
        Assert.That(visit.AuthoritySource, Is.EqualTo(RunShopAuthoritySource.LocalOfflineAdapter));
        Assert.That(visit.Offers.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(HasCategory(visit.Offers, RunShopOfferCategory.Resurrection), Is.True);
        Assert.That(HasCategory(visit.Offers, RunShopOfferCategory.Skill), Is.True);
        Assert.That(HasCategory(visit.Offers, RunShopOfferCategory.Stack), Is.True);
        Assert.That(HasCategory(visit.Offers, RunShopOfferCategory.UpgradeExchange), Is.True);
        Assert.That(HasCategory(visit.Offers, RunShopOfferCategory.Economy), Is.False);
        Assert.That(visit.FocusedOffer.OfferId, Is.EqualTo("shop-recover-losses"));
        Assert.That(visit.FocusedPreview.Error, Is.EqualTo(RunShopPurchaseError.None));
        Assert.That(visit.FocusedPreview.CurrencyAfterPurchase, Is.EqualTo(65));

        RunShopStackSnapshot previewRusher = FindStack(visit.FocusedPreview.ArmyAfterPurchase, "stack-rusher");
        Assert.That(previewRusher.Amount, Is.EqualTo(31));
        Assert.That(previewRusher.Lost, Is.EqualTo(2));
    }

    [Test]
    public void Purchase_AppliesSameResultAsPreviewAndMarksOfferPurchased()
    {
        InMemoryRunShopVisitStore store = new InMemoryRunShopVisitStore();
        RunShopService service = CreateService(store);
        RunShopArmySnapshot army = CreateArmy();
        RunShopVisitViewData visit = service.BuildVisit(
            new RunShopVisitRequest("run-1", "shop-node-1", 120, army),
            "shop-teach-skill");

        RunShopPreviewData preview = visit.FocusedPreview;
        RunShopPurchaseResult result = service.Purchase(new RunShopPurchaseCommand(
            visit.VisitId,
            "shop-teach-skill",
            visit.RunCurrency,
            visit.CurrentArmy));

        Assert.That(result.Success, Is.True);
        Assert.That(result.CurrencyAfterPurchase, Is.EqualTo(preview.CurrencyAfterPurchase));
        Assert.That(result.ArmyAfterPurchase.TotalArmyValue, Is.EqualTo(preview.ArmyAfterPurchase.TotalArmyValue));
        Assert.That(HasUnlockedSkill(FindStack(result.ArmyAfterPurchase, "stack-rusher"), "Rush"), Is.True);
        Assert.That(store.Purchases.Count, Is.EqualTo(1));

        RunShopPurchaseResult repeated = service.Purchase(new RunShopPurchaseCommand(
            visit.VisitId,
            "shop-teach-skill",
            result.CurrencyAfterPurchase,
            result.ArmyAfterPurchase));

        Assert.That(repeated.Success, Is.False);
        Assert.That(repeated.Error, Is.EqualTo(RunShopPurchaseError.AlreadyPurchased));

        RunShopVisitViewData refreshed = service.BuildVisit(
            new RunShopVisitRequest(visit.VisitId, "run-1", "shop-node-1", result.CurrencyAfterPurchase, result.ArmyAfterPurchase),
            "shop-teach-skill");

        Assert.That(refreshed.FocusedOffer.Purchased, Is.True);
        Assert.That(refreshed.CanBuyFocusedOffer, Is.False);
    }

    [Test]
    public void Purchase_RejectsInsufficientCurrency()
    {
        RunShopService service = CreateService(new InMemoryRunShopVisitStore());
        RunShopVisitViewData visit = service.BuildVisit(
            new RunShopVisitRequest("run-1", "shop-node-1", 10, CreateArmy()),
            "shop-hire-trappers");

        RunShopPurchaseResult result = service.Purchase(new RunShopPurchaseCommand(
            visit.VisitId,
            "shop-hire-trappers",
            visit.RunCurrency,
            visit.CurrentArmy));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(RunShopPurchaseError.InsufficientCurrency));
        Assert.That(result.CurrencyAfterPurchase, Is.EqualTo(10));
    }

    [Test]
    public void BuildVisit_ReusesStoredVisitForSameRunAndNodeWithoutVisitId()
    {
        InMemoryRunShopVisitStore store = new InMemoryRunShopVisitStore();
        RunShopService service = CreateService(store);

        RunShopVisitViewData firstVisit = service.BuildVisit(
            new RunShopVisitRequest("run-1", "shop-node-1", 120, CreateArmy()),
            "shop-teach-skill");
        RunShopPurchaseResult purchase = service.Purchase(new RunShopPurchaseCommand(
            firstVisit.VisitId,
            "shop-teach-skill",
            firstVisit.RunCurrency,
            firstVisit.CurrentArmy));

        Assert.That(purchase.Success, Is.True);

        RunShopVisitViewData reopened = service.BuildVisit(
            new RunShopVisitRequest("run-1", "shop-node-1", 0, null),
            "shop-teach-skill");

        Assert.That(reopened.VisitId, Is.EqualTo(firstVisit.VisitId));
        Assert.That(reopened.RunCurrency, Is.EqualTo(purchase.CurrencyAfterPurchase));
        Assert.That(reopened.FocusedOffer.Purchased, Is.True);
        Assert.That(HasUnlockedSkill(FindStack(reopened.CurrentArmy, "stack-rusher"), "Rush"), Is.True);
    }

    private static RunShopService CreateService(InMemoryRunShopVisitStore store)
    {
        return new RunShopService(new FakeRunShopUnitDefinitionSource(), store);
    }

    private static RunShopArmySnapshot CreateArmy()
    {
        List<RunShopStackSnapshot> stacks = new List<RunShopStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", "Rusher", "I", 28, 5, 28 * 31, Skill("Chope", true), Skill("Rush", false)),
            Stack("stack-thrower", "Thrower", "Thrower", "I", 10, 0, 10 * 60, Skill("Range_Stance_Barb", true), Skill("Double_Throw", true), Skill("Axe_Rain", false)),
            Stack("stack-healer", "Healer", "Healer", "I", 5, 2, 5 * 60, Skill("Tough_Skin", true), Skill("Defence_Ritual", false)),
            Stack("stack-wisp", "Wisp", "Wisp", "I", 22, 0, 22 * 6, Skill("Blind_by_light", true), Skill("Unstoppable_Light", false))
        };

        int totalValue = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            totalValue += stacks[i].CombatValue;
        }

        return new RunShopArmySnapshot("army-1", totalValue, stacks);
    }

    private static RunShopStackSnapshot Stack(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int amount,
        int lost,
        int value,
        params RunShopSkillState[] skills)
    {
        return new RunShopStackSnapshot(
            stackId,
            unitId,
            displayName,
            tier,
            1,
            amount,
            lost,
            value,
            new List<RunShopSkillState>(skills));
    }

    private static RunShopSkillState Skill(string skillId, bool unlocked)
    {
        return new RunShopSkillState(skillId, unlocked);
    }

    private static bool HasCategory(List<RunShopOfferViewData> offers, RunShopOfferCategory category)
    {
        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].Category == category)
            {
                return true;
            }
        }

        return false;
    }

    private static RunShopStackSnapshot FindStack(RunShopArmySnapshot army, string stackId)
    {
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i].StackId == stackId)
            {
                return army.Stacks[i];
            }
        }

        return null;
    }

    private static bool HasUnlockedSkill(RunShopStackSnapshot stack, string skillId)
    {
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i].SkillId == skillId && stack.Skills[i].Unlocked)
            {
                return true;
            }
        }

        return false;
    }

    private class FakeRunShopUnitDefinitionSource : IRunShopUnitDefinitionSource
    {
        private readonly Dictionary<string, RunShopUnitDefinition> units = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw", "Axe_Rain") },
            { "Healer", Unit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap", "Rope_Trap") },
            { "Axeman", Unit("Axeman", "Axeman", "II", 97, "Slash", "Heavy_Fists") },
            { "StoneGolem", Unit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw", "Stone_Skin") }
        };

        public RunShopUnitDefinition FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        private static RunShopUnitDefinition Unit(
            string unitId,
            string displayName,
            string tier,
            int cost,
            params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
