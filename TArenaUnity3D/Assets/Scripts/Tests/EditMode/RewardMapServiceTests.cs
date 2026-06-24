using System.Collections.Generic;
using NUnit.Framework;

public class RewardMapServiceTests
{
    [Test]
    public void TemplateCatalog_RepresentsAllSixRewardFamilies()
    {
        List<RewardMapTemplate> templates = new DefaultRewardMapTemplateCatalog().ListTemplates();

        Assert.That(HasFamily(templates, RewardMapFamily.Mass), Is.True);
        Assert.That(HasFamily(templates, RewardMapFamily.Quality), Is.True);
        Assert.That(HasFamily(templates, RewardMapFamily.Width), Is.True);
        Assert.That(HasFamily(templates, RewardMapFamily.Skill), Is.True);
        Assert.That(HasFamily(templates, RewardMapFamily.Recovery), Is.True);
        Assert.That(HasFamily(templates, RewardMapFamily.Economy), Is.True);
        Assert.That(templates.Count, Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void BuildChoice_ReturnsOneOfThreeIntentionsWithBattleAndGainedSummary()
    {
        RewardMapService service = CreateService(new InMemoryRewardMapChoiceStore());

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, CreateArmy(), new RewardMapBattleResultSummary("battle-1", "Victory", 7, 25)),
            string.Empty);

        Assert.That(choice.Cards.Count, Is.EqualTo(3));
        Assert.That(HasIntention(choice.Cards, RewardMapIntention.Stabilize), Is.True);
        Assert.That(HasIntention(choice.Cards, RewardMapIntention.Strengthen), Is.True);
        Assert.That(HasIntention(choice.Cards, RewardMapIntention.Pivot), Is.True);
        Assert.That(choice.BattleResultSummary.ResultLabel, Is.EqualTo("Victory"));
        Assert.That(choice.GainedSummary, Does.Contain("25 RUN GOLD"));
        Assert.That(choice.FocusedPreview.Error, Is.EqualTo(RewardMapError.None));
    }

    [Test]
    public void Apply_MatchesFocusedPreview()
    {
        InMemoryRewardMapChoiceStore store = new InMemoryRewardMapChoiceStore();
        RewardMapService service = CreateService(store);
        RewardMapArmySnapshot army = CreateArmy();
        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, army, new RewardMapBattleResultSummary("battle-1", "Victory", 7, 25)),
            "reward-reward-grow-rusher");

        RewardMapApplyResult result = service.Apply(new RewardMapApplyCommand(
            choice.ChoiceId,
            choice.FocusedCard.RewardId,
            choice.RunGoldBeforeReward,
            choice.ArmyBeforeReward));

        Assert.That(result.Success, Is.True);
        Assert.That(result.ArmyAfterReward.TotalArmyValue, Is.EqualTo(choice.FocusedPreview.ArmyAfterReward.TotalArmyValue));
        Assert.That(FindStack(result.ArmyAfterReward, "stack-rusher").Amount, Is.EqualTo(40));
    }

    [Test]
    public void TemplateRewards_TargetCurrentArmyStackIdsByUnit()
    {
        RewardMapService service = CreateService(new InMemoryRewardMapChoiceStore());
        RewardMapArmySnapshot army = CreateSlotArmy();

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, army, new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-grow-rusher");

        Assert.That(choice.FocusedCard.Legal, Is.True);
        Assert.That(choice.FocusedCard.BeforeStackPreview.StackId, Is.EqualTo("slot-0"));
        Assert.That(choice.FocusedCard.AfterStackPreview.StackId, Is.EqualTo("slot-0"));
        Assert.That(choice.FocusedCard.AffectedSlotIndex, Is.EqualTo(0));
        Assert.That(choice.FocusedCard.Operation.StackId, Is.EqualTo("slot-0"));
        Assert.That(choice.FocusedPreview.AffectedStackPreview.StackId, Is.EqualTo("slot-0"));
        Assert.That(FindStack(choice.FocusedPreview.ArmyAfterReward, "slot-0").Amount, Is.EqualTo(40));
    }

    [Test]
    public void SkillReward_WithUnlockedSkill_ReturnsNoLegalTarget()
    {
        RewardMapService service = CreateService(new InMemoryRewardMapChoiceStore());
        RewardMapArmySnapshot army = CreateArmy();
        FindStack(army, "stack-rusher").Skills.Add(new RewardMapSkillState("Rush", true));

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, army, new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-teach-rush");

        RewardMapCardViewData skillCard = FindCard(choice.Cards, "reward-reward-teach-rush");
        if (skillCard != null)
        {
            Assert.That(skillCard.Legal, Is.False);
            Assert.That(skillCard.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
        }
    }

    [Test]
    public void AddStackReward_UsesFirstFreeFormationSlot()
    {
        RewardMapService service = new RewardMapService(
            new SingleRewardTemplateCatalog(new RewardMapTemplate(
                "reward-add-specialist",
                RewardMapFamily.Width,
                RewardMapIntention.Pivot,
                RewardMapRarity.Common,
                "Add",
                "Recruit Specialist",
                "Add a new stack.",
                new RewardMapOperation(RewardMapOperationType.AddStack, string.Empty, "Specialist", string.Empty, string.Empty, string.Empty, 6, 0))),
            new FakeRewardMapUnitSource(),
            new InMemoryRewardMapChoiceStore());

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, CreateArmy(), new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-add-specialist");

        Assert.That(choice.FocusedCard.Legal, Is.True);
        Assert.That(choice.FocusedPreview.AffectedStackPreview.StackId, Is.EqualTo("slot-4"));
        Assert.That(choice.FocusedPreview.AffectedStackPreview.UnitId, Is.EqualTo("Specialist"));
    }

    [Test]
    public void AddStackReward_WithFullArmy_ReturnsNoLegalTarget()
    {
        RewardMapService service = new RewardMapService(
            new SingleRewardTemplateCatalog(new RewardMapTemplate(
                "reward-add-specialist",
                RewardMapFamily.Width,
                RewardMapIntention.Pivot,
                RewardMapRarity.Common,
                "Add",
                "Recruit Specialist",
                "Add a new stack.",
                new RewardMapOperation(RewardMapOperationType.AddStack, string.Empty, "Specialist", string.Empty, string.Empty, string.Empty, 6, 0))),
            new FakeRewardMapUnitSource(),
            new InMemoryRewardMapChoiceStore());

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, CreateFullArmy(), new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-add-specialist");

        Assert.That(choice.FocusedCard.Legal, Is.False);
        Assert.That(choice.FocusedCard.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
        Assert.That(choice.FocusedPreview.AffectedStackPreview, Is.Null);
    }

    [Test]
    public void DowngradeStack_AllowsConvertedAmountGreaterThanSourceAmount()
    {
        RewardMapService service = new RewardMapService(
            new SingleRewardTemplateCatalog(new RewardMapTemplate(
                "reward-downgrade-thrower",
                RewardMapFamily.Mass,
                RewardMapIntention.Stabilize,
                RewardMapRarity.Common,
                "Downgrade",
                "Thrower To Rusher",
                "Move one tier down for more bodies.",
                new RewardMapOperation(RewardMapOperationType.DowngradeStack, "stack-thrower", "Thrower", "Rusher", string.Empty, string.Empty, 24, 0))),
            new FakeRewardMapUnitSource(),
            new InMemoryRewardMapChoiceStore());

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, CreateArmy(), new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-downgrade-thrower");

        Assert.That(choice.FocusedCard.Legal, Is.True);
        Assert.That(choice.FocusedCard.BeforeStackPreview.UnitId, Is.EqualTo("Thrower"));
        Assert.That(choice.FocusedCard.AfterStackPreview.UnitId, Is.EqualTo("Rusher"));
        Assert.That(choice.FocusedCard.AffectedSlotIndex, Is.EqualTo(1));
        Assert.That(choice.FocusedPreview.Error, Is.EqualTo(RewardMapError.None));
        Assert.That(choice.FocusedPreview.AffectedStackPreview.UnitId, Is.EqualTo("Rusher"));
        Assert.That(choice.FocusedPreview.AffectedStackPreview.Amount, Is.EqualTo(24));
    }

    [Test]
    public void UnitTransformReward_WithMismatchedSourceUnit_ReturnsNoLegalTarget()
    {
        RewardMapService service = new RewardMapService(
            new SingleRewardTemplateCatalog(new RewardMapTemplate(
                "reward-downgrade-thrower-wrong-slot",
                RewardMapFamily.Mass,
                RewardMapIntention.Stabilize,
                RewardMapRarity.Common,
                "Downgrade",
                "Thrower To Rusher",
                "Move one tier down for more bodies.",
                new RewardMapOperation(RewardMapOperationType.DowngradeStack, "stack-rusher", "Thrower", "Rusher", string.Empty, string.Empty, 24, 0))),
            new FakeRewardMapUnitSource(),
            new InMemoryRewardMapChoiceStore());

        RewardMapChoiceViewData choice = service.BuildChoice(
            new RewardMapChoiceRequest("run-1", 1, 40, CreateArmy(), new RewardMapBattleResultSummary("battle-1", "Victory", 0, 0)),
            "reward-reward-downgrade-thrower-wrong-slot");

        Assert.That(choice.FocusedCard.Legal, Is.False);
        Assert.That(choice.FocusedCard.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
        Assert.That(choice.FocusedCard.AfterStackPreview, Is.Null);
        Assert.That(choice.FocusedPreview.Error, Is.EqualTo(RewardMapError.NoLegalTarget));
    }

    private static RewardMapService CreateService(InMemoryRewardMapChoiceStore store)
    {
        return new RewardMapService(new DefaultRewardMapTemplateCatalog(), new FakeRewardMapUnitSource(), store);
    }

    private static RewardMapArmySnapshot CreateArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", "Rusher", "I", 28, 5, 28 * 31, Skill("Chope", true)),
            Stack("stack-thrower", "Thrower", "Thrower", "I", 10, 0, 10 * 60, Skill("Range_Stance_Barb", true)),
            Stack("stack-healer", "Healer", "Healer", "I", 5, 2, 5 * 60, Skill("Tough_Skin", true)),
            Stack("stack-wisp", "Wisp", "Wisp", "I", 22, 0, 22 * 6, Skill("Blind_by_light", true))
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RewardMapArmySnapshot("army-1", total, stacks);
    }

    private static RewardMapArmySnapshot CreateSlotArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 28, 5, 28 * 31, Skill("Chope", true)),
            Stack("slot-1", "Thrower", "Thrower", "I", 10, 0, 10 * 60, Skill("Range_Stance_Barb", true)),
            Stack("slot-2", "Healer", "Healer", "I", 5, 2, 5 * 60, Skill("Tough_Skin", true)),
            Stack("slot-3", "Wisp", "Wisp", "I", 22, 0, 22 * 6, Skill("Blind_by_light", true))
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RewardMapArmySnapshot("army-slot", total, stacks);
    }

    private static RewardMapArmySnapshot CreateFullArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("slot-0", "Rusher", "Rusher", "I", 28, 0, 28 * 31, Skill("Chope", true)),
            Stack("slot-1", "Thrower", "Thrower", "I", 10, 0, 10 * 60, Skill("Range_Stance_Barb", true)),
            Stack("slot-2", "Healer", "Healer", "I", 5, 0, 5 * 60, Skill("Tough_Skin", true)),
            Stack("slot-3", "Wisp", "Wisp", "I", 22, 0, 22 * 6, Skill("Blind_by_light", true)),
            Stack("slot-4", "Trapper", "Trapper", "I", 8, 0, 8 * 45, Skill("Spike_Trap", true)),
            Stack("slot-5", "Axeman", "Axeman", "II", 4, 0, 4 * 97, Skill("Slash", true)),
            Stack("slot-6", "StoneGolem", "Stone Golem", "II", 3, 0, 3 * 67, Skill("Stone_Throw", true))
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RewardMapArmySnapshot("army-full", total, stacks);
    }

    private static RewardMapStackSnapshot Stack(string stackId, string unitId, string displayName, string tier, int amount, int lost, int value, params RewardMapSkillState[] skills)
    {
        return new RewardMapStackSnapshot(stackId, unitId, displayName, tier, 1, amount, lost, value, new List<RewardMapSkillState>(skills));
    }

    private static RewardMapSkillState Skill(string skillId, bool unlocked)
    {
        return new RewardMapSkillState(skillId, unlocked);
    }

    private static bool HasFamily(List<RewardMapTemplate> templates, RewardMapFamily family)
    {
        for (int i = 0; i < templates.Count; i++)
        {
            if (templates[i].Family == family)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasIntention(List<RewardMapCardViewData> cards, RewardMapIntention intention)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].Intention == intention)
            {
                return true;
            }
        }

        return false;
    }

    private static RewardMapStackSnapshot FindStack(RewardMapArmySnapshot army, string stackId)
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

    private static RewardMapCardViewData FindCard(List<RewardMapCardViewData> cards, string rewardId)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].RewardId == rewardId)
            {
                return cards[i];
            }
        }

        return null;
    }

    private class SingleRewardTemplateCatalog : IRewardMapTemplateCatalog
    {
        private readonly RewardMapTemplate template;

        public SingleRewardTemplateCatalog(RewardMapTemplate template)
        {
            this.template = template;
        }

        public List<RewardMapTemplate> ListTemplates()
        {
            return new List<RewardMapTemplate> { template };
        }
    }

    private class FakeRewardMapUnitSource : IRewardMapUnitDefinitionSource
    {
        private readonly Dictionary<string, RunShopUnitDefinition> units = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw") },
            { "Healer", Unit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Axeman", Unit("Axeman", "Axeman", "II", 97, "Slash") },
            { "StoneGolem", Unit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw") },
            { "Specialist", Unit("Specialist", "Specialist", "I", 55, "Focus") }
        };

        public RunShopUnitDefinition FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        private static RunShopUnitDefinition Unit(string unitId, string displayName, string tier, int cost, params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
