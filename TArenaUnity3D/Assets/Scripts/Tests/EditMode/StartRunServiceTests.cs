using System.Collections.Generic;
using NUnit.Framework;

public class StartRunServiceTests
{
    [Test]
    public void BuildScreen_ReturnsStartingArmiesInspectorDataAndRoutePreviews()
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        StartRunService service = CreateService(catalog, catalog, new InMemoryStartRunRecordStore());

        StartRunScreenViewData viewData = service.BuildScreen("barbarian-starter", "iron-line");

        Assert.That(viewData.CanBeginRun, Is.True);
        Assert.That(viewData.StartingArmies.Count, Is.EqualTo(3));
        Assert.That(viewData.RoutePreviews.Count, Is.EqualTo(3));
        Assert.That(viewData.SelectedStartingArmy.TemplateId, Is.EqualTo("barbarian-starter"));
        Assert.That(viewData.SelectedStartingArmy.Stacks.Count, Is.EqualTo(4));
        Assert.That(viewData.SelectedStartingArmy.Stacks[0].UnitId, Is.EqualTo("Rusher"));
        Assert.That(viewData.SelectedStartingArmy.Stacks[0].Tier, Is.EqualTo("I"));
        Assert.That(viewData.SelectedStartingArmy.Stacks[0].Level, Is.EqualTo(1));
        Assert.That(viewData.SelectedStartingArmy.Stacks[0].Amount, Is.EqualTo(28));
        Assert.That(viewData.SelectedStartingArmy.Stacks[0].CombatValue, Is.EqualTo(28 * 31));
        Assert.That(viewData.SelectedRoutePreview.RouteId, Is.EqualTo("iron-line"));
        Assert.That(viewData.SelectedRoutePreview.CurrentArmyValue, Is.EqualTo(viewData.SelectedStartingArmy.TotalArmyValue));
    }

    [Test]
    public void BeginRun_CreatesOfflineRunRecordAndInitialArmySnapshot()
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        InMemoryStartRunRecordStore recordStore = new InMemoryStartRunRecordStore();
        StartRunService service = CreateService(catalog, catalog, recordStore);

        StartRunResult result = service.BeginRun(new StartRunCommand(
            "offline-player",
            "barbarian-starter",
            "barbarian-starter-v1",
            "barbarian-starter",
            "iron-line"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.CreatedRun, Is.Not.Null);
        Assert.That(result.CreatedRun.GameMode, Is.EqualTo(StartRunGameMode.Offline));
        Assert.That(result.CreatedRun.AuthoritySource, Is.EqualTo(StartRunAuthoritySource.LocalOfflineAdapter));
        Assert.That(result.CreatedRun.RunStatus, Is.EqualTo("active"));
        Assert.That(result.CreatedRun.StartingArmyTemplateId, Is.EqualTo("barbarian-starter"));
        Assert.That(result.CreatedRun.StartingArmyVariantId, Is.EqualTo("barbarian-starter-v1"));
        Assert.That(result.CreatedRun.RoutePreviewOptionId, Is.EqualTo("iron-line"));
        Assert.That(result.CreatedRun.InitialArmySnapshot.Stacks.Count, Is.EqualTo(4));
        Assert.That(recordStore.Records.Count, Is.EqualTo(1));
    }

    [Test]
    public void BeginRun_RejectsStartingArmyWithIllegalSkill()
    {
        IStartingArmyTemplateSource invalidArmySource = new SingleArmySource(new StartingArmyTemplate(
            "invalid-army",
            "invalid-army-v1",
            "Invalid Army",
            "Contains one illegal skill.",
            0,
            new List<StartRunStackTemplate>
            {
                new StartRunStackTemplate(
                    "Rusher",
                    "I",
                    1,
                    10,
                    new List<StartRunSkillTemplate>
                    {
                        new StartRunSkillTemplate("Stone_Throw", true)
                    })
            }));
        DefaultStartRunCatalog routeSource = new DefaultStartRunCatalog();
        InMemoryStartRunRecordStore recordStore = new InMemoryStartRunRecordStore();
        StartRunService service = CreateService(invalidArmySource, routeSource, recordStore);

        StartRunResult result = service.BeginRun(new StartRunCommand(
            "offline-player",
            "invalid-army",
            "invalid-army-v1",
            "invalid-army",
            "iron-line"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(StartRunValidationError.InvalidArmy));
        Assert.That(result.CreatedRun, Is.Null);
        Assert.That(recordStore.Records.Count, Is.EqualTo(0));
    }

    [Test]
    public void BuildScreen_UsesUnitSourceTierInsteadOfTemplateTier()
    {
        IStartingArmyTemplateSource armySource = new SingleArmySource(new StartingArmyTemplate(
            "tier-from-catalog",
            "tier-from-catalog-v1",
            "Tier From Catalog",
            "Template tier should not be the unit source of truth.",
            0,
            new List<StartRunStackTemplate>
            {
                new StartRunStackTemplate(
                    "Rusher",
                    "III",
                    1,
                    10,
                    new List<StartRunSkillTemplate>
                    {
                        new StartRunSkillTemplate("Chope", true)
                    })
            }));
        DefaultStartRunCatalog routeSource = new DefaultStartRunCatalog();
        StartRunService service = CreateService(armySource, routeSource, new InMemoryStartRunRecordStore());

        StartRunScreenViewData viewData = service.BuildScreen("tier-from-catalog", "iron-line");

        Assert.That(viewData.SelectedStartingArmy.Stacks[0].Tier, Is.EqualTo("I"));
    }

    private static StartRunService CreateService(
        IStartingArmyTemplateSource armySource,
        IRunRoutePreviewSource routeSource,
        InMemoryStartRunRecordStore recordStore)
    {
        return new StartRunService(
            armySource,
            routeSource,
            new FakeUnitDefinitionSource(),
            recordStore);
    }

    private class SingleArmySource : IStartingArmyTemplateSource
    {
        private readonly StartingArmyTemplate army;

        public SingleArmySource(StartingArmyTemplate army)
        {
            this.army = army;
        }

        public List<StartingArmyTemplate> ListStartingArmies()
        {
            return new List<StartingArmyTemplate> { army };
        }
    }

    private class FakeUnitDefinitionSource : IStartRunUnitDefinitionSource
    {
        private readonly Dictionary<string, StartRunUnitDefinition> units = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw", "Axe_Rain") },
            { "Healer", Unit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap", "Rope_Trap") },
            { "Specialist", Unit("Specialist", "Specialist", "II", 129, "Force_Pull", "Stone_Stance") },
            { "StoneGolem", Unit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw", "Stone_Skin") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        private static StartRunUnitDefinition Unit(
            string unitId,
            string displayName,
            string tier,
            int cost,
            params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
