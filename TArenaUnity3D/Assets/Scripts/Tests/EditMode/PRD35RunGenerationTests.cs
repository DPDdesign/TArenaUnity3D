using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class PRD35RunGenerationTests
{
    [Test]
    public void BuildScreen_GeneratesDeterministicBalancedStartingArmies()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
        StartRunService service = new StartRunService(catalog, catalog, units, new InMemoryStartRunRecordStore());

        StartRunScreenViewData first = service.BuildScreen(string.Empty, string.Empty);
        StartRunScreenViewData second = service.BuildScreen(string.Empty, string.Empty);

        Assert.That(first.StartingArmies.Count, Is.EqualTo(3));
        Assert.That(first.RoutePreviews.Count, Is.EqualTo(3));
        Assert.That(first.StartingAssets.RunStartingGold, Is.EqualTo(150));
        Assert.That(first.StartingAssets.RunRollTokens, Is.EqualTo(1));
        Assert.That(first.StartingAssets.BattleSkipTokens, Is.EqualTo(0));
        Assert.That(second.SelectedStartingArmy.TemplateId, Is.EqualTo(first.SelectedStartingArmy.TemplateId));
        Assert.That(second.SelectedStartingArmy.TotalArmyValue, Is.EqualTo(first.SelectedStartingArmy.TotalArmyValue));
        Assert.That(CountUniqueArmyUnitSets(first.StartingArmies), Is.EqualTo(first.StartingArmies.Count));

        for (int i = 0; i < first.StartingArmies.Count; i++)
        {
            StartingArmyOptionViewData army = first.StartingArmies[i];
            Assert.That(army.Stacks.Count, Is.EqualTo(4));
            Assert.That(army.TotalArmyValue, Is.InRange(1450, 1750));
            Assert.That(CountFactions(army), Is.LessThanOrEqualTo(2));
            Assert.That(CountUniqueUnits(army), Is.GreaterThanOrEqualTo(2));

            for (int stackIndex = 0; stackIndex < army.Stacks.Count; stackIndex++)
            {
                StartRunStackViewData stack = army.Stacks[stackIndex];
                Assert.That(CountUnlockedSkills(stack), Is.EqualTo(1));
                Assert.That(units.SkillIsLegal(stack.UnitId, FirstUnlockedSkill(stack)), Is.True);
            }
        }
    }

    [Test]
    public void BuildScreen_ReturnsNoStartingArmyWhenUnlocksAreTooNarrow()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        StartRunGenerationUnlockContext unlocks = new StartRunGenerationUnlockContext(
            new List<string> { "Wisp" },
            new List<string> { "Blind_by_light" });
        DeterministicRunGenerationCatalog catalog = CreateCatalog(units, unlocks);
        StartRunService service = new StartRunService(catalog, catalog, units, new InMemoryStartRunRecordStore());

        StartRunScreenViewData screen = service.BuildScreen(string.Empty, string.Empty);

        Assert.That(screen.CanBeginRun, Is.False);
        Assert.That(screen.SelectedStartingArmy, Is.Null);
        Assert.That(screen.ValidationError, Is.EqualTo(StartRunValidationError.MissingStartingArmy));
        Assert.That(screen.StartingArmies.Count, Is.EqualTo(0));
    }

    [Test]
    public void BuildPaths_GeneratesFixedMissionTopologyWithBranchAndEncounterIds()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
        string routeId = catalog.ListRoutePreviews()[0].RouteId;

        List<RunMapPathDefinition> paths = catalog.BuildPaths(routeId);

        Assert.That(paths.Count, Is.EqualTo(4));
        Assert.That(CountNodes(paths), Is.EqualTo(13));
        Assert.That(FindNode(paths, "node-2").NodeType, Is.EqualTo(RunMapNodeType.RandomEvent));
        Assert.That(FindNode(paths, "node-7a"), Is.Null);
        Assert.That(FindNode(paths, "node-8a"), Is.Null);
        Assert.That(FindNode(paths, "node-9").NodeType, Is.EqualTo(RunMapNodeType.Shop));
        Assert.That(FindNode(paths, "node-10").NodeType, Is.EqualTo(RunMapNodeType.FinalBoss));
        Assert.That(FindNode(paths, "node-3").NextNodeIds, Does.Contain("node-4a"));
        Assert.That(FindNode(paths, "node-3").NextNodeIds, Does.Contain("node-4b"));

        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            for (int nodeIndex = 0; nodeIndex < paths[pathIndex].Nodes.Count; nodeIndex++)
            {
                RunMapNodeDefinition node = paths[pathIndex].Nodes[nodeIndex];
                if (node.NodeType == RunMapNodeType.Battle || node.NodeType == RunMapNodeType.FinalBoss)
                {
                    Assert.That(node.EncounterId, Is.Not.Empty);
                    Assert.That(new DefaultRunBattleEncounterCatalog().FindEncounter(node.NodeId, node.EncounterId), Is.Not.Null);
                }
            }
        }
    }

    [Test]
    public void RunMapService_OpensBothBranchesThenFinalThroughExplicitLinks()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
        RunMapService service = new RunMapService(catalog, new InMemoryRunMapStore());
        string routeId = catalog.ListRoutePreviews()[0].RouteId;
        service.CreateOrLoad(new RunMapCreateRequest("run-35", routeId, 150, null), string.Empty);

        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-1")).Success, Is.True);
        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-2")).Success, Is.True);
        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-3")).Success, Is.True);

        RunMapScreenViewData branchScreen = service.CreateOrLoad(new RunMapCreateRequest("run-35", routeId, 150, null), string.Empty);
        Assert.That(FindNode(branchScreen, "node-4a").State, Is.EqualTo(RunMapNodeState.Available));
        Assert.That(FindNode(branchScreen, "node-4b").State, Is.EqualTo(RunMapNodeState.Available));
        Assert.That(FindNode(branchScreen, "node-10").State, Is.EqualTo(RunMapNodeState.Locked));

        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-4a")).Success, Is.True);
        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-5a")).Success, Is.True);
        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-6a")).Success, Is.True);

        RunMapScreenViewData shopScreen = service.CreateOrLoad(new RunMapCreateRequest("run-35", routeId, 150, null), string.Empty);
        Assert.That(FindNode(shopScreen, "node-9").State, Is.EqualTo(RunMapNodeState.Available));

        Assert.That(service.Travel(new RunMapTravelCommand("run-35", "node-9")).Success, Is.True);
        RunMapScreenViewData finalScreen = service.CreateOrLoad(new RunMapCreateRequest("run-35", routeId, 150, null), string.Empty);
        Assert.That(FindNode(finalScreen, "node-10").State, Is.EqualTo(RunMapNodeState.Available));
    }

    [Test]
    public void DbReload_PreservesGeneratedRouteProgressAndBranchLinks()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            FakeUnitPoolSource units = new FakeUnitPoolSource();
            DeterministicRunGenerationCatalog catalog = CreateCatalog(units);
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, catalog));

            string routeId = catalog.ListRoutePreviews()[0].RouteId;
            string startingArmyId = catalog.ListStartingArmies()[0].TemplateId;
            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                startingArmyId,
                startingArmyId + "-v1",
                startingArmyId,
                routeId));
            Assert.That(startRun.Success, Is.True);

            RunMapService firstRunMap = new RunMapService(catalog, new OfflineRunMapDbStore(databasePath, catalog));
            firstRunMap.CreateOrLoad(new RunMapCreateRequest(startRun.CreatedRun.RunId, routeId, 150, null), string.Empty);
            Assert.That(firstRunMap.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-1")).Success, Is.True);
            Assert.That(firstRunMap.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-2")).Success, Is.True);
            Assert.That(firstRunMap.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-3")).Success, Is.True);

            RunMapService reloadedRunMap = new RunMapService(catalog, new OfflineRunMapDbStore(databasePath, catalog));
            RunMapScreenViewData reloaded = reloadedRunMap.CreateOrLoad(
                new RunMapCreateRequest(startRun.CreatedRun.RunId, routeId, 150, null),
                string.Empty);

            Assert.That(reloaded.CurrentNodeId, Is.EqualTo("node-3"));
            Assert.That(FindNode(reloaded, "node-4a").State, Is.EqualTo(RunMapNodeState.Available));
            Assert.That(FindNode(reloaded, "node-4b").State, Is.EqualTo(RunMapNodeState.Available));
            Assert.That(FindNode(reloaded, "node-2").NodeType, Is.EqualTo(RunMapNodeType.RandomEvent));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static DeterministicRunGenerationCatalog CreateCatalog(
        IStartRunUnitPoolSource units,
        StartRunGenerationUnlockContext unlocks = null)
    {
        ArmyGeneratorRuleSet ruleSet = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
        ruleSet.ConfigureMockDefaults();
        return new DeterministicRunGenerationCatalog(
            units,
            ruleSet,
            StartingArmyGeneratorConfig.CreateDefault(ruleSet),
            RouteGeneratorConfig.CreateDefault(),
            unlocks);
    }

    private static int CountUnlockedSkills(StartRunStackViewData stack)
    {
        int count = 0;
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i].Unlocked)
            {
                count++;
            }
        }

        return count;
    }

    private static string FirstUnlockedSkill(StartRunStackViewData stack)
    {
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i].Unlocked)
            {
                return stack.Skills[i].SkillId;
            }
        }

        return string.Empty;
    }

    private static int CountFactions(StartingArmyOptionViewData army)
    {
        List<int> factions = new List<int>();
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            int faction = FactionFor(army.Stacks[i].UnitId);
            if (!factions.Contains(faction))
            {
                factions.Add(faction);
            }
        }

        return factions.Count;
    }

    private static int CountUniqueUnits(StartingArmyOptionViewData army)
    {
        List<string> unitIds = new List<string>();
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            string unitId = army.Stacks[i].UnitId;
            if (!unitIds.Contains(unitId))
            {
                unitIds.Add(unitId);
            }
        }

        return unitIds.Count;
    }

    private static int CountUniqueArmyUnitSets(List<StartingArmyOptionViewData> armies)
    {
        List<string> signatures = new List<string>();
        for (int i = 0; armies != null && i < armies.Count; i++)
        {
            string signature = BuildUnitSignature(armies[i]);
            if (!signatures.Contains(signature))
            {
                signatures.Add(signature);
            }
        }

        return signatures.Count;
    }

    private static string BuildUnitSignature(StartingArmyOptionViewData army)
    {
        List<string> unitIds = new List<string>();
        for (int i = 0; army != null && army.Stacks != null && i < army.Stacks.Count; i++)
        {
            unitIds.Add(army.Stacks[i].UnitId);
        }

        unitIds.Sort();
        return string.Join("|", unitIds.ToArray());
    }

    private static int FactionFor(string unitId)
    {
        if (unitId == "LizardMage")
        {
            return UnitFactionResolver.LizardFactionId;
        }

        return UnitFactionResolver.ResolveFactionId(unitId);
    }

    private static int CountNodes(List<RunMapPathDefinition> paths)
    {
        int count = 0;
        for (int i = 0; i < paths.Count; i++)
        {
            count += paths[i].Nodes.Count;
        }

        return count;
    }

    private static RunMapNodeDefinition FindNode(List<RunMapPathDefinition> paths, string nodeId)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            for (int j = 0; j < paths[i].Nodes.Count; j++)
            {
                if (paths[i].Nodes[j].NodeId == nodeId)
                {
                    return paths[i].Nodes[j];
                }
            }
        }

        return null;
    }

    private static RunMapNodeViewData FindNode(RunMapScreenViewData screen, string nodeId)
    {
        for (int i = 0; i < screen.Paths.Count; i++)
        {
            for (int j = 0; j < screen.Paths[i].Nodes.Count; j++)
            {
                if (screen.Paths[i].Nodes[j].NodeId == nodeId)
                {
                    return screen.Paths[i].Nodes[j];
                }
            }
        }

        return null;
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_PRD35_Test_" + Guid.NewGuid().ToString("N") + ".db");
    }

    private static void TryDelete(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
        catch
        {
        }
    }

    private sealed class FakeUnitPoolSource : IStartRunUnitPoolSource
    {
        private readonly Dictionary<string, StartRunUnitDefinition> units = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "I", "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "I", "Range_Stance_Barb", "Double_Throw") },
            { "Healer", Unit("Healer", "I", "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "I", "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "I", "Spike_Trap", "Rope_Trap") },
            { "Specialist", Unit("Specialist", "II", "Force_Pull", "Stone_Stance") },
            { "StoneGolem", Unit("StoneGolem", "II", "Stone_Throw", "Stone_Skin") },
            { "StoneLord", Unit("StoneLord", "III", "Stone_Throw", "Stone_Skin") },
            { "LizardMage", Unit("LizardMage", "III", "Spike_Trap", "Force_Pull") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<StartRunUnitDefinition> ListUnits()
        {
            return new List<StartRunUnitDefinition>(units.Values);
        }

        public bool SkillIsLegal(string unitId, string skillId)
        {
            StartRunUnitDefinition unit = FindUnit(unitId);
            return unit != null && unit.SkillIds.Contains(skillId);
        }

        private static StartRunUnitDefinition Unit(string unitId, string tier, params string[] skills)
        {
            return new StartRunUnitDefinition(
                unitId,
                unitId,
                tier,
                100,
                FactionFor(unitId),
                UnitRoleCategory.Flexible,
                new List<string>(skills));
        }
    }
}
