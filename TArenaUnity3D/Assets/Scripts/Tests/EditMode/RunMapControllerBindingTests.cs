using System.Collections.Generic;
using NUnit.Framework;

public class RunMapControllerBindingTests
{
    [Test]
    public void FlattenNodes_PreservesPathOrderThenNodeOrder()
    {
        RunMapScreenViewData screen = new RunMapScreenViewData(
            "run-1",
            "route-map-1",
            RunMapGameMode.Offline,
            RunMapAuthoritySource.LocalOfflineAdapter,
            "run-start",
            0,
            0,
            0,
            null,
            new List<RunMapPathViewData>
            {
                Path("path-main", Node("node-main-1"), Node("node-main-2")),
                Path("path-safe", Node("node-safe-1")),
                Path("path-risk", Node("node-risk-1"), Node("node-risk-2")),
                Path("path-final", Node("node-final"))
            },
            null,
            string.Empty);

        List<RunMapNodeViewData> nodes = RunMapController.FlattenNodes(screen);

        Assert.That(nodes.Count, Is.EqualTo(6));
        Assert.That(nodes[0].NodeId, Is.EqualTo("node-main-1"));
        Assert.That(nodes[1].NodeId, Is.EqualTo("node-main-2"));
        Assert.That(nodes[2].NodeId, Is.EqualTo("node-safe-1"));
        Assert.That(nodes[3].NodeId, Is.EqualTo("node-risk-1"));
        Assert.That(nodes[4].NodeId, Is.EqualTo("node-risk-2"));
        Assert.That(nodes[5].NodeId, Is.EqualTo("node-final"));
    }

    [Test]
    public void FlattenNodes_ToleratesMissingPathsAndNodes()
    {
        RunMapScreenViewData screen = new RunMapScreenViewData(
            "run-1",
            "route-map-1",
            RunMapGameMode.Offline,
            RunMapAuthoritySource.LocalOfflineAdapter,
            "run-start",
            0,
            0,
            0,
            null,
            new List<RunMapPathViewData>
            {
                null,
                new RunMapPathViewData("path-empty", "route", "Empty", string.Empty, null),
                Path("path-valid", null, Node("node-valid"))
            },
            null,
            string.Empty);

        List<RunMapNodeViewData> nodes = RunMapController.FlattenNodes(screen);

        Assert.That(nodes.Count, Is.EqualTo(1));
        Assert.That(nodes[0].NodeId, Is.EqualTo("node-valid"));
        Assert.That(RunMapController.FlattenNodes(null).Count, Is.EqualTo(0));
    }

    private static RunMapPathViewData Path(string pathId, params RunMapNodeViewData[] nodes)
    {
        List<RunMapNodeViewData> nodeList = new List<RunMapNodeViewData>();
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                nodeList.Add(nodes[i]);
            }
        }

        return new RunMapPathViewData(pathId, "route-balanced-frontier", pathId, string.Empty, nodeList);
    }

    private static RunMapNodeViewData Node(string nodeId)
    {
        return new RunMapNodeViewData(
            nodeId,
            "path-test",
            RunMapNodeType.Battle,
            RunMapNodeState.Available,
            1,
            nodeId,
            "Reward",
            "Risk",
            "enc-test",
            true);
    }
}
