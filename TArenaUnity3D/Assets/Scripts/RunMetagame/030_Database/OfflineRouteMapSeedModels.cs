using System;
using System.Collections.Generic;

[Serializable]
public class OfflineRouteMapSeedRecord
{
    public int RouteMapId;
    public int RunId;
    public string SelectedRouteChoiceId;
    public List<OfflineRoutePathSeedRecord> Paths;

    public OfflineRouteMapSeedRecord(int routeMapId, int runId, string selectedRouteChoiceId, List<OfflineRoutePathSeedRecord> paths)
    {
        RouteMapId = Math.Max(0, routeMapId);
        RunId = Math.Max(0, runId);
        SelectedRouteChoiceId = selectedRouteChoiceId;
        Paths = paths ?? new List<OfflineRoutePathSeedRecord>();
    }
}

[Serializable]
public class OfflineRoutePathSeedRecord
{
    public int RoutePathId;
    public int RouteMapId;
    public string PathId;
    public string DisplayName;
    public string BiasDescription;
    public int SortOrder;
    public List<OfflineRouteNodeSeedRecord> Nodes;

    public OfflineRoutePathSeedRecord(
        int routePathId,
        int routeMapId,
        string pathId,
        string displayName,
        string biasDescription,
        int sortOrder,
        List<OfflineRouteNodeSeedRecord> nodes)
    {
        RoutePathId = Math.Max(0, routePathId);
        RouteMapId = Math.Max(0, routeMapId);
        PathId = pathId;
        DisplayName = displayName;
        BiasDescription = biasDescription;
        SortOrder = Math.Max(0, sortOrder);
        Nodes = nodes ?? new List<OfflineRouteNodeSeedRecord>();
    }
}

[Serializable]
public class OfflineRouteNodeSeedRecord
{
    public int NodeId;
    public int RouteMapId;
    public int RoutePathId;
    public int NodeTypeId;
    public int NodeStateId;
    public int StageIndex;
    public string DisplayName;
    public string PossibleRewardHint;
    public string ExpectedRiskHint;
    public string EncounterId;
    public EnemyEncounterDifficulty EncounterDifficulty;
    public int NextNodeId;
    public List<int> NextNodeIds;
    public string CatalogNodeId;
    public string CatalogPathId;

    public OfflineRouteNodeSeedRecord(
        int nodeId,
        int routeMapId,
        int routePathId,
        int nodeTypeId,
        int nodeStateId,
        int stageIndex,
        string displayName,
        string possibleRewardHint,
        string expectedRiskHint,
        string encounterId,
        EnemyEncounterDifficulty encounterDifficulty,
        int nextNodeId,
        string catalogNodeId,
        string catalogPathId)
    {
        NodeId = Math.Max(0, nodeId);
        RouteMapId = Math.Max(0, routeMapId);
        RoutePathId = Math.Max(0, routePathId);
        NodeTypeId = Math.Max(0, nodeTypeId);
        NodeStateId = Math.Max(0, nodeStateId);
        StageIndex = Math.Max(0, stageIndex);
        DisplayName = displayName;
        PossibleRewardHint = possibleRewardHint;
        ExpectedRiskHint = expectedRiskHint;
        EncounterId = encounterId;
        EncounterDifficulty = encounterDifficulty;
        NextNodeId = Math.Max(0, nextNodeId);
        NextNodeIds = new List<int>();
        if (NextNodeId > 0)
        {
            NextNodeIds.Add(NextNodeId);
        }
        CatalogNodeId = catalogNodeId;
        CatalogPathId = catalogPathId;
    }
}
