using System;
using System.Collections.Generic;

public class RunMapService
{
    private readonly IRunMapPathCatalog pathCatalog;
    private readonly IRunMapStore store;

    public RunMapService(IRunMapPathCatalog pathCatalog, IRunMapStore store)
    {
        this.pathCatalog = pathCatalog;
        this.store = store;
    }

    public RunMapScreenViewData CreateOrLoad(RunMapCreateRequest request, string selectedNodeId)
    {
        if (request == null || string.IsNullOrEmpty(request.RunId))
        {
            return EmptyScreen("Missing run.");
        }

        RunMapStateRecord state = store == null ? null : store.Find(request.RunId);
        if (state == null)
        {
            List<RunMapPathDefinition> paths = pathCatalog == null
                ? new List<RunMapPathDefinition>()
                : pathCatalog.BuildPaths(request.SelectedRouteChoiceId);

            state = new RunMapStateRecord(
                request.RunId,
                RunMapGameMode.Offline,
                RunMapAuthoritySource.LocalOfflineAdapter,
                request.SelectedRouteChoiceId,
                "route-map-" + Guid.NewGuid().ToString("N"),
                "run-start",
                0,
                0,
                request.StartingRunGold,
                new List<string>(),
                paths);

            if (store != null)
            {
                RunMapStateRecord persisted = store.Save(state);
                if (persisted != null)
                {
                    state = persisted;
                }
            }
        }

        return BuildScreen(state, request.ArmySummary, selectedNodeId);
    }

    public RunMapTravelResult Travel(RunMapTravelCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RunId))
        {
            return Fail(RunMapTravelError.MissingRun, "Missing run.", null, null);
        }

        RunMapStateRecord state = store == null ? null : store.Find(command.RunId);
        if (state == null)
        {
            return Fail(RunMapTravelError.MissingRun, "Run map was not found.", null, null);
        }

        RunMapNodeDefinition node = FindNode(state.Paths, command.NodeId);
        if (node == null)
        {
            return Fail(RunMapTravelError.MissingNode, "Route node was not found.", null, state);
        }

        RunMapNodeState nodeState = DetermineNodeState(state, node);
        if (nodeState != RunMapNodeState.Available && nodeState != RunMapNodeState.Selected)
        {
            return Fail(RunMapTravelError.LockedNode, "Route node is not available.", ToNodeView(state, node, nodeState), state);
        }

        if (node.NodeType == RunMapNodeType.FinalBoss && state.RouteProgress < 2)
        {
            return Fail(RunMapTravelError.FinalGateClosed, "Final node opens after enough route progress.", ToNodeView(state, node, nodeState), state);
        }

        if (!Contains(state.CompletedNodeIds, node.NodeId))
        {
            state.CompletedNodeIds.Add(node.NodeId);
        }

        state.CurrentNodeId = node.NodeId;
        state.RouteProgress = Math.Max(state.RouteProgress, node.StageIndex);
        state.StageProgress = Math.Max(state.StageProgress, node.StageIndex);

        if (store != null)
        {
            RunMapStateRecord persisted = store.Save(state);
            if (persisted != null)
            {
                state = persisted;
            }
        }

        RunMapNodeViewData traveled = ToNodeView(state, node, RunMapNodeState.Completed);
        return new RunMapTravelResult(true, RunMapTravelError.None, "Travel accepted.", traveled, state);
    }

    private RunMapScreenViewData BuildScreen(RunMapStateRecord state, RunMapArmySummary armySummary, string selectedNodeId)
    {
        List<RunMapPathViewData> paths = new List<RunMapPathViewData>();
        RunMapNodeViewData selected = null;

        for (int i = 0; i < state.Paths.Count; i++)
        {
            RunMapPathDefinition path = state.Paths[i];
            List<RunMapNodeViewData> nodes = new List<RunMapNodeViewData>();
            for (int j = 0; j < path.Nodes.Count; j++)
            {
                RunMapNodeDefinition node = path.Nodes[j];
                RunMapNodeState nodeState = DetermineNodeState(state, node);
                if (node.NodeId == selectedNodeId && nodeState == RunMapNodeState.Available)
                {
                    nodeState = RunMapNodeState.Selected;
                }

                RunMapNodeViewData view = ToNodeView(state, node, nodeState);
                nodes.Add(view);
                if (node.NodeId == selectedNodeId)
                {
                    selected = view;
                }
            }

            paths.Add(new RunMapPathViewData(path.PathId, path.RouteChoiceId, path.DisplayName, path.BiasDescription, nodes));
        }

        return new RunMapScreenViewData(
            state.RunId,
            state.RouteMapId,
            state.GameMode,
            state.AuthoritySource,
            state.CurrentNodeId,
            state.RouteProgress,
            state.StageProgress,
            state.RunGold,
            armySummary,
            paths,
            selected,
            "Run Map ready.");
    }

    private RunMapNodeViewData ToNodeView(RunMapStateRecord state, RunMapNodeDefinition node, RunMapNodeState nodeState)
    {
        return new RunMapNodeViewData(
            node.NodeId,
            node.PathId,
            node.NodeType,
            nodeState,
            node.StageIndex,
            node.DisplayName,
            node.PossibleRewardHint,
            node.ExpectedRiskHint,
            node.EncounterId,
            nodeState == RunMapNodeState.Available || nodeState == RunMapNodeState.Selected);
    }

    private RunMapNodeState DetermineNodeState(RunMapStateRecord state, RunMapNodeDefinition node)
    {
        if (Contains(state.CompletedNodeIds, node.NodeId))
        {
            return RunMapNodeState.Completed;
        }

        if (node.StageIndex == 1 && state.RouteProgress == 0)
        {
            return RunMapNodeState.Available;
        }

        if (node.NodeType == RunMapNodeType.FinalBoss)
        {
            return state.RouteProgress >= 2 ? RunMapNodeState.Available : RunMapNodeState.Locked;
        }

        if (!string.IsNullOrEmpty(state.CurrentNodeId))
        {
            RunMapNodeDefinition current = FindNode(state.Paths, state.CurrentNodeId);
            if (current != null && current.NextNodeId == node.NodeId)
            {
                return RunMapNodeState.Available;
            }
        }

        return RunMapNodeState.Locked;
    }

    private static RunMapNodeDefinition FindNode(List<RunMapPathDefinition> paths, string nodeId)
    {
        if (paths == null || string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i] == null || paths[i].Nodes == null)
            {
                continue;
            }

            for (int j = 0; j < paths[i].Nodes.Count; j++)
            {
                if (paths[i].Nodes[j] != null && paths[i].Nodes[j].NodeId == nodeId)
                {
                    return paths[i].Nodes[j];
                }
            }
        }

        return null;
    }

    private static bool Contains(List<string> values, string value)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    private RunMapScreenViewData EmptyScreen(string message)
    {
        return new RunMapScreenViewData(
            string.Empty,
            string.Empty,
            RunMapGameMode.Offline,
            RunMapAuthoritySource.LocalOfflineAdapter,
            string.Empty,
            0,
            0,
            0,
            null,
            new List<RunMapPathViewData>(),
            null,
            message);
    }

    private RunMapTravelResult Fail(RunMapTravelError error, string message, RunMapNodeViewData node, RunMapStateRecord state)
    {
        return new RunMapTravelResult(false, error, message, node, state);
    }
}
