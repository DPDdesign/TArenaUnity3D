using System;
using System.Collections.Generic;

public enum RunMapGameMode
{
    Offline,
    Online
}

public enum RunMapAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum RunMapNodeType
{
    Start,
    Battle,
    Shop,
    RecruitReward,
    FinalBoss
}

public enum RunMapNodeState
{
    Locked,
    Available,
    Completed,
    Selected
}

public enum RunMapTravelError
{
    None,
    MissingRun,
    MissingNode,
    LockedNode,
    FinalGateClosed,
    InvalidTravel
}

[Serializable]
public class RunMapNodeDefinition
{
    public string NodeId;
    public string PathId;
    public RunMapNodeType NodeType;
    public int StageIndex;
    public string DisplayName;
    public string PossibleRewardHint;
    public string ExpectedRiskHint;
    public string EncounterId;
    public string NextNodeId;

    public RunMapNodeDefinition(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        int stageIndex,
        string displayName,
        string possibleRewardHint,
        string expectedRiskHint,
        string encounterId,
        string nextNodeId)
    {
        NodeId = nodeId;
        PathId = pathId;
        NodeType = nodeType;
        StageIndex = Math.Max(0, stageIndex);
        DisplayName = displayName;
        PossibleRewardHint = possibleRewardHint;
        ExpectedRiskHint = expectedRiskHint;
        EncounterId = encounterId;
        NextNodeId = nextNodeId;
    }
}

[Serializable]
public class RunMapPathDefinition
{
    public string PathId;
    public string RouteChoiceId;
    public string DisplayName;
    public string BiasDescription;
    public List<RunMapNodeDefinition> Nodes;

    public RunMapPathDefinition(string pathId, string routeChoiceId, string displayName, string biasDescription, List<RunMapNodeDefinition> nodes)
    {
        PathId = pathId;
        RouteChoiceId = routeChoiceId;
        DisplayName = displayName;
        BiasDescription = biasDescription;
        Nodes = nodes ?? new List<RunMapNodeDefinition>();
    }
}

[Serializable]
public class RunMapArmySummary
{
    public string SnapshotId;
    public int TotalArmyValue;
    public int StackCount;
    public string SummaryText;

    public RunMapArmySummary(string snapshotId, int totalArmyValue, int stackCount, string summaryText)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = Math.Max(0, totalArmyValue);
        StackCount = Math.Max(0, stackCount);
        SummaryText = summaryText;
    }
}

[Serializable]
public class RunMapStateRecord
{
    public string RunId;
    public RunMapGameMode GameMode;
    public RunMapAuthoritySource AuthoritySource;
    public string SelectedRouteChoiceId;
    public string RouteMapId;
    public string CurrentNodeId;
    public int RouteProgress;
    public int StageProgress;
    public int RunGold;
    public List<string> CompletedNodeIds;
    public List<RunMapPathDefinition> Paths;

    public RunMapStateRecord(
        string runId,
        RunMapGameMode gameMode,
        RunMapAuthoritySource authoritySource,
        string selectedRouteChoiceId,
        string routeMapId,
        string currentNodeId,
        int routeProgress,
        int stageProgress,
        int runGold,
        List<string> completedNodeIds,
        List<RunMapPathDefinition> paths)
    {
        RunId = runId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        SelectedRouteChoiceId = selectedRouteChoiceId;
        RouteMapId = routeMapId;
        CurrentNodeId = currentNodeId;
        RouteProgress = Math.Max(0, routeProgress);
        StageProgress = Math.Max(0, stageProgress);
        RunGold = Math.Max(0, runGold);
        CompletedNodeIds = completedNodeIds ?? new List<string>();
        Paths = paths ?? new List<RunMapPathDefinition>();
    }
}

[Serializable]
public class RunMapNodeViewData
{
    public string NodeId;
    public string PathId;
    public RunMapNodeType NodeType;
    public RunMapNodeState State;
    public int StageIndex;
    public string DisplayName;
    public string PossibleRewardHint;
    public string ExpectedRiskHint;
    public string EncounterId;
    public bool CanTravel;

    public RunMapNodeViewData(
        string nodeId,
        string pathId,
        RunMapNodeType nodeType,
        RunMapNodeState state,
        int stageIndex,
        string displayName,
        string possibleRewardHint,
        string expectedRiskHint,
        string encounterId,
        bool canTravel)
    {
        NodeId = nodeId;
        PathId = pathId;
        NodeType = nodeType;
        State = state;
        StageIndex = Math.Max(0, stageIndex);
        DisplayName = displayName;
        PossibleRewardHint = possibleRewardHint;
        ExpectedRiskHint = expectedRiskHint;
        EncounterId = encounterId;
        CanTravel = canTravel;
    }
}

[Serializable]
public class RunMapPathViewData
{
    public string PathId;
    public string RouteChoiceId;
    public string DisplayName;
    public string BiasDescription;
    public List<RunMapNodeViewData> Nodes;

    public RunMapPathViewData(string pathId, string routeChoiceId, string displayName, string biasDescription, List<RunMapNodeViewData> nodes)
    {
        PathId = pathId;
        RouteChoiceId = routeChoiceId;
        DisplayName = displayName;
        BiasDescription = biasDescription;
        Nodes = nodes ?? new List<RunMapNodeViewData>();
    }
}

[Serializable]
public class RunMapScreenViewData
{
    public string RunId;
    public string RouteMapId;
    public RunMapGameMode GameMode;
    public RunMapAuthoritySource AuthoritySource;
    public string CurrentNodeId;
    public int RouteProgress;
    public int StageProgress;
    public int RunGold;
    public RunMapArmySummary ArmySummary;
    public List<RunMapPathViewData> Paths;
    public RunMapNodeViewData SelectedNode;
    public string Message;

    public RunMapScreenViewData(
        string runId,
        string routeMapId,
        RunMapGameMode gameMode,
        RunMapAuthoritySource authoritySource,
        string currentNodeId,
        int routeProgress,
        int stageProgress,
        int runGold,
        RunMapArmySummary armySummary,
        List<RunMapPathViewData> paths,
        RunMapNodeViewData selectedNode,
        string message)
    {
        RunId = runId;
        RouteMapId = routeMapId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        CurrentNodeId = currentNodeId;
        RouteProgress = Math.Max(0, routeProgress);
        StageProgress = Math.Max(0, stageProgress);
        RunGold = Math.Max(0, runGold);
        ArmySummary = armySummary;
        Paths = paths ?? new List<RunMapPathViewData>();
        SelectedNode = selectedNode;
        Message = message;
    }
}

[Serializable]
public class RunMapCreateRequest
{
    public string RunId;
    public string SelectedRouteChoiceId;
    public int StartingRunGold;
    public RunMapArmySummary ArmySummary;

    public RunMapCreateRequest(string runId, string selectedRouteChoiceId, int startingRunGold, RunMapArmySummary armySummary)
    {
        RunId = runId;
        SelectedRouteChoiceId = selectedRouteChoiceId;
        StartingRunGold = Math.Max(0, startingRunGold);
        ArmySummary = armySummary;
    }
}

[Serializable]
public class RunMapTravelCommand
{
    public string RunId;
    public string NodeId;

    public RunMapTravelCommand(string runId, string nodeId)
    {
        RunId = runId;
        NodeId = nodeId;
    }
}

[Serializable]
public class RunMapTravelResult
{
    public bool Success;
    public RunMapTravelError Error;
    public string Message;
    public RunMapNodeViewData TraveledNode;
    public RunMapStateRecord StateAfterTravel;

    public RunMapTravelResult(bool success, RunMapTravelError error, string message, RunMapNodeViewData traveledNode, RunMapStateRecord stateAfterTravel)
    {
        Success = success;
        Error = error;
        Message = message;
        TraveledNode = traveledNode;
        StateAfterTravel = stateAfterTravel;
    }
}
