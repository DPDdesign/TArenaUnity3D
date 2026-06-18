using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunMapController : MonoBehaviour
{
    private enum DetailMode
    {
        RouteNode,
        RouteSummary,
        Army
    }

    [Header("Route Nodes")]
    [SerializeField] private RunMapNodeRepresentation[] routeNodeRepresentations = new RunMapNodeRepresentation[0];

    [Header("Route Edges")]
    [SerializeField] private RunMapRouteEdgeBinding[] routeEdgeBindings = new RunMapRouteEdgeBinding[0];

    [Header("Top And Details")]
    [SerializeField] private TextMeshProUGUI selectedSectionTitleText;
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedTypeText;
    [SerializeField] private TextMeshProUGUI selectedEncounterText;
    [SerializeField] private TextMeshProUGUI possibleRewardsText;
    [SerializeField] private TextMeshProUGUI expectedRiskText;
    [SerializeField] private TextMeshProUGUI currentNodeText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Army Panel")]
    [SerializeField] private TMP_Text armySummaryText;
    [SerializeField] private GameObject armyStackRowPrefab;
    [SerializeField] private Transform armyStackRows;

    [Header("Bottom Bar")]
    [SerializeField] private TextMeshProUGUI runGoldText;
    [SerializeField] private TextMeshProUGUI stageProgressText;
    [SerializeField] private Slider stageProgressSlider;
    [SerializeField] private TextMeshProUGUI armyValueText;
    [SerializeField] private Button travelButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button viewArmyButton;

    private OfflineRunMapAdapter adapter;
    private OfflineRunContextDbReader contextReader;
    private DataMapper dataMapper;
    private RunMapCreateRequest request;
    private RunMapScreenViewData screen;
    private OfflineArmySnapshotRecord currentArmySnapshot;
    private readonly List<StackRepresentation> armyStackRowInstances = new List<StackRepresentation>();
    private string focusedNodeId;
    private string hoveredNodeId;
    private DetailMode detailMode;
    private bool initialized;

    private void OnEnable()
    {
        InitializeRunMap();
    }

    public void RefreshFromPersistedRun()
    {
        initialized = false;
        request = null;
        screen = null;
        currentArmySnapshot = null;
        hoveredNodeId = string.Empty;
        armyStackRowInstances.Clear();
        InitializeRunMap();
    }

    private void InitializeRunMap()
    {
        dataMapper = DataMapper.Instance;
        adapter = OfflineModeDatabaseComposition.CreateRunMapAdapter();
        contextReader = OfflineModeDatabaseComposition.CreateRunContextReader();
        request = BuildRequestFromPersistedRun();
        if (request == null)
        {
            screen = null;
            focusedNodeId = string.Empty;
            hoveredNodeId = string.Empty;
            detailMode = DetailMode.RouteSummary;
            initialized = true;
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        screen = adapter.CreateOrLoad(request, string.Empty);
        focusedNodeId = string.Empty;
        hoveredNodeId = string.Empty;
        detailMode = DetailMode.RouteSummary;
        initialized = true;

        RenderScreen("Run Map ready. Hover a route node to inspect it, or click an available node to travel.");
    }

    public void OnRouteNodeClicked(string nodeId)
    {
        FocusRouteNode(nodeId, true);
    }

    public void OnRouteNodeHovered(string nodeId)
    {
        PreviewRouteNode(nodeId);
    }

    public void OnRouteNodeHoverEnded(string nodeId)
    {
        EnsureInitialized();
        if (!HasRunMapRequest())
        {
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        if (hoveredNodeId != nodeId)
        {
            return;
        }

        hoveredNodeId = string.Empty;
        detailMode = string.IsNullOrEmpty(focusedNodeId) ? DetailMode.RouteSummary : DetailMode.RouteNode;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RenderScreen(string.IsNullOrEmpty(focusedNodeId) ? "Run Map ready." : "Route node focus restored.");
    }

    private void PreviewRouteNode(string nodeId)
    {
        EnsureInitialized();
        if (!HasRunMapRequest())
        {
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        hoveredNodeId = nodeId;
        detailMode = DetailMode.RouteNode;
        screen = adapter.CreateOrLoad(request, nodeId);
        RunMapNodeViewData node = FindNode(screen, nodeId);
        RenderScreen(BuildFocusMessage(node));
    }

    private void FocusRouteNode(string nodeId, bool attemptTravel)
    {
        EnsureInitialized();
        if (!HasRunMapRequest())
        {
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        hoveredNodeId = string.Empty;
        focusedNodeId = nodeId;
        detailMode = DetailMode.RouteNode;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RunMapNodeViewData node = FindNode(screen, focusedNodeId);
        string message = BuildFocusMessage(node);

        RenderScreen(message);

        if (attemptTravel)
        {
            OnTravelClicked();
        }
    }

    public void OnTravelClicked()
    {
        EnsureInitialized();
        if (!HasRunMapRequest())
        {
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        if (string.IsNullOrEmpty(focusedNodeId))
        {
            detailMode = DetailMode.RouteSummary;
            RenderScreen("Select an available route node before Travel.");
            return;
        }

        RunMapNodeViewData focused = FindNode(screen, focusedNodeId);
        if (detailMode != DetailMode.RouteNode || focused == null || !focused.CanTravel)
        {
            RenderScreen("Travel requires an available focused node.");
            return;
        }

        RunMapTravelResult result = adapter.Travel(new RunMapTravelCommand(request.RunId, focusedNodeId));
        screen = adapter.CreateOrLoad(request, focusedNodeId);

        if (result != null && result.Success)
        {
            focusedNodeId = string.Empty;
            hoveredNodeId = string.Empty;
            detailMode = DetailMode.RouteSummary;
            screen = adapter.CreateOrLoad(request, string.Empty);
            RenderScreen("Travel accepted. Current node, next route availability, and stage progress updated.");
            return;
        }

        string message = result == null ? "Travel failed." : result.Message;
        RenderScreen(message);
    }

    public void OnBackClicked()
    {
        EnsureInitialized();
        if (GameSceneManager.Instance != null)
        {
            OfflineModeDatabaseComposition.EndRunGenerationSession();
            GameSceneManager.Instance.ShowStartRun();
            return;
        }

        RenderScreen("Start Run screen manager is not available.");
    }

    public void OnViewArmyClicked()
    {
        EnsureInitialized();
        if (!HasRunMapRequest())
        {
            RenderScreen("No persisted run is ready for the Run Map screen.");
            return;
        }

        focusedNodeId = string.Empty;
        detailMode = DetailMode.Army;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RenderScreen("Inspecting current army summary for this run.");
    }

    private void EnsureInitialized()
    {
        if (!initialized || request == null)
        {
            InitializeRunMap();
        }
    }

    private void RenderScreen(string status)
    {
        if (screen == null)
        {
            RenderRouteNodes();
            RenderRouteEdges();
            RenderArmyPanel();
            SetText(statusText, "Run map data is not available.");
            SetText(runGoldText, "RUN GOLD\n0");
            SetText(stageProgressText, "Stage Progress 0 / 3");
            SetText(armyValueText, "Army Value\n0");
            if (stageProgressSlider != null)
            {
                stageProgressSlider.minValue = 0f;
                stageProgressSlider.maxValue = 3f;
                stageProgressSlider.value = 0f;
            }

            if (travelButton != null)
            {
                travelButton.interactable = false;
            }

            if (viewArmyButton != null)
            {
                viewArmyButton.interactable = false;
            }

            if (backButton != null)
            {
                backButton.interactable = GameSceneManager.Instance != null;
            }

            SetText(statusText, status);
            return;
        }

        RunMapNodeViewData focusedNode = detailMode == DetailMode.RouteNode ? FindNode(screen, ActiveNodeId()) : null;
        RenderRouteNodes();
        RenderRouteEdges();
        if (detailMode == DetailMode.Army)
        {
            RenderArmyDetails();
        }
        else if (detailMode == DetailMode.RouteSummary)
        {
            RenderRouteSummary();
        }
        else
        {
            RenderFocusedNode(focusedNode);
        }

        RenderArmyPanel();
        RenderBottomBar(focusedNode);
        SetText(statusText, status);
    }

    private RunMapCreateRequest BuildRequestFromPersistedRun()
    {
        if (contextReader == null)
        {
            return null;
        }

        OfflineRunContext context = contextReader.LoadLatestRunForNextScreen("RunMap");
        if (context == null || string.IsNullOrEmpty(context.RunIdText) || string.IsNullOrEmpty(context.SelectedRouteChoiceId))
        {
            currentArmySnapshot = null;
            return null;
        }

        currentArmySnapshot = context.CurrentArmySnapshot;
        return new RunMapCreateRequest(
            context.RunIdText,
            context.SelectedRouteChoiceId,
            context.CurrentRunGold,
            contextReader.ToRunMapCurrentArmy(context));
    }

    private bool HasRunMapRequest()
    {
        return adapter != null && request != null && !string.IsNullOrEmpty(request.RunId);
    }

    private void RenderArmyPanel()
    {
        if (armySummaryText != null)
        {
            armySummaryText.text = "Army Value\n" + ArmyValueLabel();
        }

        List<StackInfoData> stackInfos = new List<StackInfoData>();
        if (currentArmySnapshot != null && currentArmySnapshot.Stacks != null)
        {
            for (int i = 0; i < currentArmySnapshot.Stacks.Count; i++)
            {
                OfflineArmySnapshotStackRecord stack = currentArmySnapshot.Stacks[i];
                if (stack != null && stack.IsActive && string.IsNullOrEmpty(stack.UnitId) == false && stack.Amount > 0)
                {
                    stackInfos.Add(RunMetagameDisplayInfoFactory.FromOfflineSnapshotStack(stack, dataMapper));
                }
            }
        }

        if (screen != null && currentArmySnapshot == null)
        {
            Debug.LogWarning("[RunMapController] Current army snapshot is missing. Run Map cannot instantiate army stack rows.");
        }
        else if (screen != null && stackInfos.Count == 0)
        {
            Debug.LogWarning("[RunMapController] Current army snapshot has no active stacks to display.");
        }

        RunMetagameStackListPresenter.DisplayStackInfo(
            armyStackRows,
            armyStackRowPrefab,
            stackInfos,
            armyStackRowInstances);
    }

    private void RenderRouteNodes()
    {
        if (routeNodeRepresentations == null)
        {
            return;
        }

        List<RunMapNodeViewData> orderedNodes = FlattenNodes(screen);
        int nodeCount = orderedNodes.Count;
        if (nodeCount > routeNodeRepresentations.Length)
        {
            Debug.LogWarning("[RunMapController] Run Map has " + nodeCount.ToString() + " generated nodes but only " + routeNodeRepresentations.Length.ToString() + " route node representations are assigned.");
        }

        for (int i = 0; i < routeNodeRepresentations.Length; i++)
        {
            RunMapNodeRepresentation representation = routeNodeRepresentations[i];
            if (representation == null)
            {
                continue;
            }

            representation.HoverRequested = OnRouteNodeHovered;
            representation.HoverEnded = OnRouteNodeHoverEnded;
            representation.ClickRequested = OnRouteNodeClicked;

            RunMapNodeViewData node = i < nodeCount ? orderedNodes[i] : null;
            bool focused = node != null && node.NodeId == ActiveNodeId();
            representation.Bind(node, focused);
        }
    }

    private void RenderRouteEdges()
    {
        if (routeEdgeBindings == null)
        {
            return;
        }

        for (int i = 0; i < routeEdgeBindings.Length; i++)
        {
            RunMapRouteEdgeBinding binding = routeEdgeBindings[i];
            if (binding == null || binding.EdgeRepresentation == null)
            {
                continue;
            }

            string sourceNodeId = binding.SourceNode == null ? string.Empty : binding.SourceNode.NodeId;
            string targetNodeId = binding.TargetNode == null ? string.Empty : binding.TargetNode.NodeId;
            RunMapNodeViewData sourceNode = FindNode(screen, sourceNodeId);
            RunMapNodeViewData targetNode = FindNode(screen, targetNodeId);
            binding.EdgeRepresentation.Bind(sourceNode, targetNode);
        }
    }

    private string ActiveNodeId()
    {
        return string.IsNullOrEmpty(hoveredNodeId) ? focusedNodeId : hoveredNodeId;
    }

    private void RenderFocusedNode(RunMapNodeViewData focusedNode)
    {
        if (focusedNode == null)
        {
            SetText(selectedSectionTitleText, "ROUTE");
            SetText(selectedTitleText, "Select Route Node");
            SetText(selectedTypeText, "No node focused");
            SetText(selectedEncounterText, "Encounter: none");
            SetText(possibleRewardsText, "Possible Rewards: choose a route node.");
            SetText(expectedRiskText, "Expected Risk: unknown");
            SetText(currentNodeText, "Current: " + CurrentNodeLabel());
            return;
        }

        SetText(selectedSectionTitleText, DetailHeaderForNode(focusedNode));
        SetText(selectedTitleText, focusedNode.DisplayName);
        SetText(selectedTypeText, FormatNodeType(focusedNode.NodeType) + " / " + FormatNodeState(focusedNode.State));
        SetText(selectedEncounterText, "Encounter: " + EmptyAs(focusedNode.EncounterId, "none"));
        SetText(possibleRewardsText, "Possible Rewards: " + EmptyAs(focusedNode.PossibleRewardHint, "hidden"));
        SetText(expectedRiskText, "Expected Risk: " + EmptyAs(focusedNode.ExpectedRiskHint, "uncertain"));
        SetText(currentNodeText, "Current: " + CurrentNodeLabel());
    }

    private void RenderRouteSummary()
    {
        SetText(selectedSectionTitleText, "ROUTE CHOICES");
        SetText(selectedTitleText, "Balanced Frontier");
        SetText(selectedTypeText, "Offline route map / " + CountPlayablePaths().ToString() + " choices");
        SetText(selectedEncounterText, "Current: " + CurrentNodeLabel());
        SetText(possibleRewardsText, "Route Bias: " + RouteBiasSummary());
        SetText(expectedRiskText, "Expected Risk: visible as uncertain danger per node.");
        SetText(currentNodeText, "Current: " + CurrentNodeLabel());
    }

    private void RenderArmyDetails()
    {
        RunMapArmySummary army = screen == null ? null : screen.ArmySummary;
        SetText(selectedSectionTitleText, "YOUR ARMY");
        SetText(selectedTitleText, "Army Value " + ArmyValueLabel());
        SetText(selectedTypeText, army == null ? "0 stacks" : army.StackCount.ToString() + " stacks");
        SetText(selectedEncounterText, "Snapshot: " + (army == null ? "none" : EmptyAs(army.SnapshotId, "local-run-army")));
        SetText(possibleRewardsText, "Army Summary: " + (army == null ? "No army loaded." : EmptyAs(army.SummaryText, "Run army is ready.")));
        SetText(expectedRiskText, "Route Impact: upcoming battles can reduce the army before final proof.");
        SetText(currentNodeText, "Current: " + CurrentNodeLabel());
    }

    private void RenderBottomBar(RunMapNodeViewData focusedNode)
    {
        SetText(runGoldText, "RUN GOLD\n" + screen.RunGold.ToString());
        SetText(stageProgressText, "Stage Progress " + screen.StageProgress.ToString() + " / 3");
        SetText(armyValueText, "Army Value\n" + ArmyValueLabel());

        if (stageProgressSlider != null)
        {
            stageProgressSlider.minValue = 0f;
            stageProgressSlider.maxValue = 3f;
            stageProgressSlider.wholeNumbers = true;
            stageProgressSlider.value = Mathf.Clamp(screen.StageProgress, 0, 3);
        }

        if (travelButton != null)
        {
            travelButton.interactable = detailMode == DetailMode.RouteNode && focusedNode != null && focusedNode.CanTravel;
        }

        if (backButton != null)
        {
            backButton.interactable = true;
        }

        if (viewArmyButton != null)
        {
            viewArmyButton.interactable = true;
        }
    }

    private string BuildFocusMessage(RunMapNodeViewData node)
    {
        if (node == null)
        {
            return "Route node data missing.";
        }

        if (node.CanTravel)
        {
            return "Focused " + node.DisplayName + ". Travel will validate through OfflineRunMapAdapter.";
        }

        if (node.State == RunMapNodeState.Completed)
        {
            return "Focused " + node.DisplayName + ". This node is already completed.";
        }

        return "Focused " + node.DisplayName + ". This route node is locked by current route progress.";
    }

    private string CurrentNodeLabel()
    {
        if (screen == null || string.IsNullOrEmpty(screen.CurrentNodeId) || screen.CurrentNodeId == "run-start")
        {
            return "Run Start";
        }

        RunMapNodeViewData current = FindNode(screen, screen.CurrentNodeId);
        return current == null ? screen.CurrentNodeId : current.DisplayName;
    }

    private string ArmyValueLabel()
    {
        if (screen == null || screen.ArmySummary == null)
        {
            return "0";
        }

        return screen.ArmySummary.TotalArmyValue.ToString();
    }

    private int CountPlayablePaths()
    {
        if (screen == null || screen.Paths == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < screen.Paths.Count; i++)
        {
            RunMapPathViewData path = screen.Paths[i];
            if (path != null && path.PathId != "path-final")
            {
                count++;
            }
        }

        return count;
    }

    private string RouteBiasSummary()
    {
        if (screen == null || screen.Paths == null)
        {
            return "No route data.";
        }

        string summary = string.Empty;
        for (int i = 0; i < screen.Paths.Count; i++)
        {
            RunMapPathViewData path = screen.Paths[i];
            if (path == null || path.PathId == "path-final")
            {
                continue;
            }

            if (!string.IsNullOrEmpty(summary))
            {
                summary += " / ";
            }

            summary += path.DisplayName;
        }

        return string.IsNullOrEmpty(summary) ? "No route choices." : summary;
    }

    private static RunMapNodeViewData FindNode(RunMapScreenViewData data, string nodeId)
    {
        if (data == null || data.Paths == null || string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        for (int i = 0; i < data.Paths.Count; i++)
        {
            RunMapPathViewData path = data.Paths[i];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int j = 0; j < path.Nodes.Count; j++)
            {
                RunMapNodeViewData node = path.Nodes[j];
                if (node != null && node.NodeId == nodeId)
                {
                    return node;
                }
            }
        }

        return null;
    }

    public static List<RunMapNodeViewData> FlattenNodes(RunMapScreenViewData data)
    {
        List<RunMapNodeViewData> result = new List<RunMapNodeViewData>();
        if (data == null || data.Paths == null)
        {
            return result;
        }

        for (int i = 0; i < data.Paths.Count; i++)
        {
            RunMapPathViewData path = data.Paths[i];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int j = 0; j < path.Nodes.Count; j++)
            {
                RunMapNodeViewData node = path.Nodes[j];
                if (node != null)
                {
                    result.Add(node);
                }
            }
        }

        return result;
    }

    private static string FindFirstTravelNodeId(RunMapScreenViewData data)
    {
        if (data == null || data.Paths == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < data.Paths.Count; i++)
        {
            RunMapPathViewData path = data.Paths[i];
            if (path == null || path.Nodes == null)
            {
                continue;
            }

            for (int j = 0; j < path.Nodes.Count; j++)
            {
                RunMapNodeViewData node = path.Nodes[j];
                if (node != null && node.CanTravel)
                {
                    return node.NodeId;
                }
            }
        }

        return string.Empty;
    }

    private static string EmptyAs(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static string FormatNodeType(RunMapNodeType type)
    {
        switch (type)
        {
            case RunMapNodeType.Start:
                return "Start";
            case RunMapNodeType.Battle:
                return "Battle";
            case RunMapNodeType.Shop:
                return "Shop";
            case RunMapNodeType.RecruitReward:
                return "Recruit / Reward";
            case RunMapNodeType.FinalBoss:
                return "Final Battle";
            case RunMapNodeType.RandomEvent:
                return "Random Event";
            case RunMapNodeType.Empty:
                return "Empty";
            default:
                return type.ToString();
        }
    }

    private static string DetailHeaderForNode(RunMapNodeViewData node)
    {
        if (node == null)
        {
            return "ROUTE";
        }

        if (node.NodeType == RunMapNodeType.FinalBoss)
        {
            return "FINAL";
        }

        return FormatNodeType(node.NodeType).ToUpperInvariant();
    }

    private static string FormatNodeState(RunMapNodeState state)
    {
        switch (state)
        {
            case RunMapNodeState.Locked:
                return "Locked";
            case RunMapNodeState.Available:
                return "Available";
            case RunMapNodeState.Completed:
                return "Completed";
            case RunMapNodeState.Selected:
                return "Selected";
            default:
                return state.ToString();
        }
    }
}
