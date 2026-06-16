using NUnit.Framework;

public class RunMapServiceTests
{
    [Test]
    public void CreateOrLoad_BuildsThreeRouteChoicesAndFinalGate()
    {
        RunMapService service = CreateService();

        RunMapScreenViewData screen = service.CreateOrLoad(
            new RunMapCreateRequest("run-1", "route-balanced-frontier", 40, new RunMapArmySummary("army-1", 1200, 3, "3 stacks")),
            "node-pressure-1");

        Assert.That(screen.GameMode, Is.EqualTo(RunMapGameMode.Offline));
        Assert.That(screen.AuthoritySource, Is.EqualTo(RunMapAuthoritySource.LocalOfflineAdapter));
        Assert.That(screen.Paths.Count, Is.EqualTo(4));
        Assert.That(CountAvailable(screen), Is.EqualTo(3));
        Assert.That(screen.SelectedNode.DisplayName, Is.EqualTo("Border Clash"));
        Assert.That(screen.SelectedNode.PossibleRewardHint, Does.Contain("Mass"));
        Assert.That(screen.SelectedNode.ExpectedRiskHint, Does.Contain("Uncertain"));
        Assert.That(FindNode(screen, "node-final").State, Is.EqualTo(RunMapNodeState.Locked));
        Assert.That(screen.RunGold, Is.EqualTo(40));
    }

    [Test]
    public void Travel_AllowsAvailableNodeAndPersistsProgress()
    {
        InMemoryRunMapStore store = new InMemoryRunMapStore();
        RunMapService service = new RunMapService(new DefaultRunMapPathCatalog(), store);
        service.CreateOrLoad(new RunMapCreateRequest("run-1", "route-balanced-frontier", 0, null), string.Empty);

        RunMapTravelResult result = service.Travel(new RunMapTravelCommand("run-1", "node-pressure-1"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.StateAfterTravel.RouteProgress, Is.EqualTo(1));
        Assert.That(store.Find("run-1").CurrentNodeId, Is.EqualTo("node-pressure-1"));

        RunMapScreenViewData screen = service.CreateOrLoad(new RunMapCreateRequest("run-1", "route-balanced-frontier", 0, null), string.Empty);
        Assert.That(FindNode(screen, "node-pressure-2").State, Is.EqualTo(RunMapNodeState.Available));
    }

    [Test]
    public void Travel_RejectsFinalBeforeRouteProgress()
    {
        RunMapService service = CreateService();
        service.CreateOrLoad(new RunMapCreateRequest("run-1", "route-balanced-frontier", 0, null), string.Empty);

        RunMapTravelResult result = service.Travel(new RunMapTravelCommand("run-1", "node-final"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(RunMapTravelError.LockedNode));
    }

    private static RunMapService CreateService()
    {
        return new RunMapService(new DefaultRunMapPathCatalog(), new InMemoryRunMapStore());
    }

    private static int CountAvailable(RunMapScreenViewData screen)
    {
        int count = 0;
        for (int i = 0; i < screen.Paths.Count; i++)
        {
            for (int j = 0; j < screen.Paths[i].Nodes.Count; j++)
            {
                if (screen.Paths[i].Nodes[j].State == RunMapNodeState.Available || screen.Paths[i].Nodes[j].State == RunMapNodeState.Selected)
                {
                    count++;
                }
            }
        }

        return count;
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
}
