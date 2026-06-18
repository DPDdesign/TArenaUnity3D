using System.Collections.Generic;

public static class OfflineRouteMapSeedFactory
{
    public static OfflineRouteMapSeedRecord Create(
        int runId,
        int routeMapId,
        string selectedRouteChoiceId,
        List<RunMapPathDefinition> paths,
        int startingRoutePathId = 1,
        int startingNodeId = 1)
    {
        List<OfflineRoutePathSeedRecord> seededPaths = new List<OfflineRoutePathSeedRecord>();
        Dictionary<string, int> nodeIdByCatalogId = new Dictionary<string, int>();
        List<RunMapPathDefinition> sourcePaths = paths ?? new List<RunMapPathDefinition>();
        int nextRoutePathId = startingRoutePathId;
        int nextNodeId = startingNodeId;

        for (int pathIndex = 0; pathIndex < sourcePaths.Count; pathIndex++)
        {
            RunMapPathDefinition path = sourcePaths[pathIndex];
            if (path == null)
            {
                continue;
            }

            List<OfflineRouteNodeSeedRecord> seededNodes = new List<OfflineRouteNodeSeedRecord>();
            List<RunMapNodeDefinition> sourceNodes = path.Nodes ?? new List<RunMapNodeDefinition>();
            int routePathId = nextRoutePathId++;

            for (int nodeIndex = 0; nodeIndex < sourceNodes.Count; nodeIndex++)
            {
                RunMapNodeDefinition node = sourceNodes[nodeIndex];
                if (node == null)
                {
                    continue;
                }

                int nodeId = nextNodeId++;
                if (!string.IsNullOrEmpty(node.NodeId) && !nodeIdByCatalogId.ContainsKey(node.NodeId))
                {
                    nodeIdByCatalogId.Add(node.NodeId, nodeId);
                }

                seededNodes.Add(
                    new OfflineRouteNodeSeedRecord(
                        nodeId,
                        routeMapId,
                        routePathId,
                        ToDbNodeTypeId(node.NodeType),
                        ToDbNodeStateId(node.NodeType, node.StageIndex),
                        node.StageIndex,
                        node.DisplayName,
                        node.PossibleRewardHint,
                        node.ExpectedRiskHint,
                        node.EncounterId,
                        node.EncounterDifficulty,
                        0,
                        node.NodeId,
                        node.PathId));
            }

            seededPaths.Add(
                new OfflineRoutePathSeedRecord(
                    routePathId,
                    routeMapId,
                    path.PathId,
                    path.DisplayName,
                    path.BiasDescription,
                    pathIndex,
                    seededNodes));
        }

        ApplyNextNodeLinks(seededPaths, sourcePaths, nodeIdByCatalogId);

        return new OfflineRouteMapSeedRecord(routeMapId, runId, selectedRouteChoiceId, seededPaths);
    }

    private static void ApplyNextNodeLinks(
        List<OfflineRoutePathSeedRecord> seededPaths,
        List<RunMapPathDefinition> sourcePaths,
        Dictionary<string, int> nodeIdByCatalogId)
    {
        int seededPathIndex = 0;
        for (int pathIndex = 0; pathIndex < sourcePaths.Count; pathIndex++)
        {
            RunMapPathDefinition sourcePath = sourcePaths[pathIndex];
            if (sourcePath == null)
            {
                continue;
            }

            if (seededPathIndex >= seededPaths.Count)
            {
                return;
            }

            OfflineRoutePathSeedRecord seededPath = seededPaths[seededPathIndex++];
            List<RunMapNodeDefinition> sourceNodes = sourcePath.Nodes ?? new List<RunMapNodeDefinition>();
            int seededNodeIndex = 0;

            for (int nodeIndex = 0; nodeIndex < sourceNodes.Count; nodeIndex++)
            {
                RunMapNodeDefinition sourceNode = sourceNodes[nodeIndex];
                if (sourceNode == null)
                {
                    continue;
                }

                if (seededNodeIndex >= seededPath.Nodes.Count)
                {
                    return;
                }

                OfflineRouteNodeSeedRecord seededNode = seededPath.Nodes[seededNodeIndex++];
                seededNode.NextNodeIds.Clear();
                int nextNodeId;
                if (!string.IsNullOrEmpty(sourceNode.NextNodeId) && nodeIdByCatalogId.TryGetValue(sourceNode.NextNodeId, out nextNodeId))
                {
                    seededNode.NextNodeId = nextNodeId;
                    AddUnique(seededNode.NextNodeIds, nextNodeId);
                }

                if (sourceNode.NextNodeIds == null)
                {
                    continue;
                }

                for (int nextIndex = 0; nextIndex < sourceNode.NextNodeIds.Count; nextIndex++)
                {
                    string catalogNextNodeId = sourceNode.NextNodeIds[nextIndex];
                    if (!string.IsNullOrEmpty(catalogNextNodeId) && nodeIdByCatalogId.TryGetValue(catalogNextNodeId, out nextNodeId))
                    {
                        if (seededNode.NextNodeId <= 0)
                        {
                            seededNode.NextNodeId = nextNodeId;
                        }

                        AddUnique(seededNode.NextNodeIds, nextNodeId);
                    }
                }
            }
        }
    }

    private static void AddUnique(List<int> values, int value)
    {
        if (value <= 0 || values.Contains(value))
        {
            return;
        }

        values.Add(value);
    }

    private static int ToDbNodeTypeId(RunMapNodeType nodeType)
    {
        switch (nodeType)
        {
            case RunMapNodeType.Start:
                return (int)DBNodeTypeId.Start;
            case RunMapNodeType.Battle:
                return (int)DBNodeTypeId.Battle;
            case RunMapNodeType.Shop:
                return (int)DBNodeTypeId.Shop;
            case RunMapNodeType.RecruitReward:
                return (int)DBNodeTypeId.RecruitReward;
            case RunMapNodeType.FinalBoss:
                return (int)DBNodeTypeId.FinalBoss;
            case RunMapNodeType.RandomEvent:
                return (int)DBNodeTypeId.RandomEvent;
            case RunMapNodeType.Empty:
                return (int)DBNodeTypeId.Empty;
            default:
                return (int)DBNodeTypeId.Start;
        }
    }

    private static int ToDbNodeStateId(RunMapNodeType nodeType, int stageIndex)
    {
        if (nodeType == RunMapNodeType.Start || stageIndex <= 1)
        {
            return (int)DBNodeStateId.Available;
        }

        return (int)DBNodeStateId.Locked;
    }
}
