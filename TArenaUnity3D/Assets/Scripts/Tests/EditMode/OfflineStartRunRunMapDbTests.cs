using System;
using System.IO;
using NUnit.Framework;

public class OfflineStartRunRunMapDbTests
{
    [Test]
    public void BeginRun_PersistsRunSnapshotAndSeededRouteMap()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService service = new StartRunService(
                catalog,
                catalog,
                new FakeUnitDefinitionSource(),
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult result = service.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));

            Assert.That(result.Success, Is.True);
            Assert.That(result.CreatedRun, Is.Not.Null);
            Assert.That(result.CreatedRun.RunId, Does.StartWith("run-"));
            Assert.That(result.CreatedRun.InitialArmySnapshot.SnapshotId, Does.StartWith("snapshot-"));

            RunMapService runMapService = new RunMapService(new DefaultRunMapPathCatalog(), new OfflineRunMapDbStore(databasePath, new DefaultRunMapPathCatalog()));
            RunMapScreenViewData screen = runMapService.CreateOrLoad(
                new RunMapCreateRequest(result.CreatedRun.RunId, result.CreatedRun.RoutePreviewOptionId, result.CreatedRun.StartingCurrency, null),
                string.Empty);

            Assert.That(screen.Paths.Count, Is.EqualTo(4));
            Assert.That(screen.RouteMapId, Does.StartWith("route-map-"));
            Assert.That(screen.CurrentNodeId, Is.EqualTo("run-start"));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void RunMap_TravelProgressSurvivesServiceRecreation()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                new FakeUnitDefinitionSource(),
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));
            Assert.That(startRun.Success, Is.True);

            RunMapService firstService = new RunMapService(new DefaultRunMapPathCatalog(), new OfflineRunMapDbStore(databasePath, new DefaultRunMapPathCatalog()));
            RunMapScreenViewData firstScreen = firstService.CreateOrLoad(
                new RunMapCreateRequest(startRun.CreatedRun.RunId, startRun.CreatedRun.RoutePreviewOptionId, startRun.CreatedRun.StartingCurrency, null),
                "node-pressure-1");
            Assert.That(firstScreen.Paths.Count, Is.EqualTo(4));

            RunMapTravelResult travel = firstService.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-pressure-1"));
            Assert.That(travel.Success, Is.True);

            RunMapService secondService = new RunMapService(new DefaultRunMapPathCatalog(), new OfflineRunMapDbStore(databasePath, new DefaultRunMapPathCatalog()));
            RunMapScreenViewData reloaded = secondService.CreateOrLoad(
                new RunMapCreateRequest(startRun.CreatedRun.RunId, startRun.CreatedRun.RoutePreviewOptionId, startRun.CreatedRun.StartingCurrency, null),
                string.Empty);

            Assert.That(reloaded.CurrentNodeId, Is.EqualTo("node-pressure-1"));
            Assert.That(reloaded.RouteProgress, Is.EqualTo(1));
            Assert.That(FindNode(reloaded, "node-pressure-2").State, Is.EqualTo(RunMapNodeState.Available));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_Test_" + Guid.NewGuid().ToString("N") + ".db");
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

    private class FakeUnitDefinitionSource : IStartRunUnitDefinitionSource
    {
        private readonly System.Collections.Generic.Dictionary<string, StartRunUnitDefinition> units = new System.Collections.Generic.Dictionary<string, StartRunUnitDefinition>
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
            return new StartRunUnitDefinition(unitId, displayName, tier, cost, new System.Collections.Generic.List<string>(skills));
        }
    }
}
