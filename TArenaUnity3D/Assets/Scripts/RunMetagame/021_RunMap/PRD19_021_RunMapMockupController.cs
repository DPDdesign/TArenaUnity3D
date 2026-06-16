using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PRD19_021_RunMapMockupController : MonoBehaviour
{
    private enum DetailMode
    {
        RouteNode,
        RouteSummary,
        Army
    }

    [Serializable]
    private class RouteNodeBinding
    {
        [SerializeField] private string nodeId;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Image lockedOverlay;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI stateText;

        public string NodeId
        {
            get { return nodeId; }
        }

        public void Render(RunMapNodeViewData node, bool focused, bool current)
        {
            if (button != null)
            {
                button.interactable = node != null;
            }

            if (node == null)
            {
                SetText(titleText, nodeId);
                SetText(typeText, "Missing data");
                SetText(stateText, "OFF");
                SetImageColor(background, new Color(0.16f, 0.14f, 0.12f, 1f));
                SetActive(selectionFrame, false);
                SetActive(lockedOverlay, true);
                return;
            }

            SetText(titleText, node.DisplayName);
            SetText(typeText, FormatNodeType(node.NodeType));
            SetText(stateText, current ? "CURRENT" : FormatNodeState(node.State));
            SetImageColor(background, NodeColor(node, focused, current));
            SetActive(selectionFrame, focused || current);
            SetActive(lockedOverlay, node.State == RunMapNodeState.Locked);
        }

        private static Color NodeColor(RunMapNodeViewData node, bool focused, bool current)
        {
            if (current)
            {
                return new Color(0.86f, 0.50f, 0.12f, 1f);
            }

            if (focused)
            {
                return new Color(0.78f, 0.55f, 0.18f, 1f);
            }

            if (node.State == RunMapNodeState.Completed)
            {
                return new Color(0.26f, 0.42f, 0.24f, 1f);
            }

            if (node.CanTravel)
            {
                return new Color(0.52f, 0.20f, 0.12f, 1f);
            }

            return new Color(0.18f, 0.16f, 0.14f, 1f);
        }
    }

    [Header("Mock Run Data")]
    [SerializeField] private string mockRunId = "run-900001";
    [SerializeField] private string selectedRouteChoiceId = "route-balanced-frontier";
    [SerializeField] private int startingRunGold = 360;

    [Header("Route Nodes")]
    [SerializeField] private RouteNodeBinding[] routeNodes;

    [Header("Top And Details")]
    [SerializeField] private TextMeshProUGUI selectedSectionTitleText;
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedTypeText;
    [SerializeField] private TextMeshProUGUI selectedEncounterText;
    [SerializeField] private TextMeshProUGUI possibleRewardsText;
    [SerializeField] private TextMeshProUGUI expectedRiskText;
    [SerializeField] private TextMeshProUGUI currentNodeText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Bottom Bar")]
    [SerializeField] private TextMeshProUGUI runGoldText;
    [SerializeField] private TextMeshProUGUI stageProgressText;
    [SerializeField] private Slider stageProgressSlider;
    [SerializeField] private TextMeshProUGUI armyValueText;
    [SerializeField] private Button travelButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button viewArmyButton;

    private OfflineRunMapAdapter adapter;
    private RunMapCreateRequest request;
    private RunMapScreenViewData screen;
    private string focusedNodeId;
    private DetailMode detailMode;
    private bool initialized;

    private void OnEnable()
    {
        if (!initialized)
        {
            InitializeMockup();
        }
    }

    public void InitializeMockup()
    {
        adapter = new OfflineRunMapAdapter();
        request = new RunMapCreateRequest(
            mockRunId,
            selectedRouteChoiceId,
            startingRunGold,
            new RunMapArmySummary("mock-army-run-map", 2850, 5, "Five stacks ready for route pressure."));

        screen = adapter.CreateOrLoad(request, string.Empty);
        focusedNodeId = FindFirstTravelNodeId(screen);
        detailMode = DetailMode.RouteNode;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        initialized = true;

        RenderScreen("Run Map ready. Select a route node, then Travel.");
    }

    public void OnRouteNodeClicked(string nodeId)
    {
        EnsureInitialized();

        focusedNodeId = nodeId;
        detailMode = DetailMode.RouteNode;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RunMapNodeViewData node = FindNode(screen, focusedNodeId);
        string message = BuildFocusMessage(node);

        RenderScreen(message);
    }

    public void OnTravelClicked()
    {
        EnsureInitialized();

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
            RunMapNodeViewData traveled = result.TraveledNode;
            focusedNodeId = FindPreferredFocusAfterTravel(screen, traveled == null ? focusedNodeId : traveled.NodeId);
            detailMode = DetailMode.RouteNode;
            screen = adapter.CreateOrLoad(request, focusedNodeId);
            RenderScreen("Travel accepted. Current node, next route availability, and stage progress updated.");
            return;
        }

        string message = result == null ? "Travel failed." : result.Message;
        RenderScreen(message);
    }

    public void OnBackClicked()
    {
        EnsureInitialized();
        focusedNodeId = string.Empty;
        detailMode = DetailMode.RouteSummary;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RenderScreen("Route choice summary opened. Select an available node to continue this run.");
    }

    public void OnViewArmyClicked()
    {
        EnsureInitialized();
        focusedNodeId = string.Empty;
        detailMode = DetailMode.Army;
        screen = adapter.CreateOrLoad(request, focusedNodeId);
        RenderScreen("Inspecting current army summary for this run.");
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            InitializeMockup();
        }
    }

    private void RenderScreen(string status)
    {
        if (screen == null)
        {
            SetText(statusText, "Run map data is not available.");
            return;
        }

        RunMapNodeViewData focusedNode = detailMode == DetailMode.RouteNode ? FindNode(screen, focusedNodeId) : null;
        RenderRouteNodes();
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

        RenderBottomBar(focusedNode);
        SetText(statusText, status);
    }

    private void RenderRouteNodes()
    {
        if (routeNodes == null)
        {
            return;
        }

        for (int i = 0; i < routeNodes.Length; i++)
        {
            RouteNodeBinding binding = routeNodes[i];
            if (binding == null)
            {
                continue;
            }

            RunMapNodeViewData node = FindNode(screen, binding.NodeId);
            bool focused = node != null && node.NodeId == focusedNodeId;
            bool current = node != null && node.NodeId == screen.CurrentNodeId;
            binding.Render(node, focused, current);
        }
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

    private static string FindPreferredFocusAfterTravel(RunMapScreenViewData data, string traveledNodeId)
    {
        string firstTravel = FindFirstTravelNodeId(data);
        return string.IsNullOrEmpty(firstTravel) ? traveledNodeId : firstTravel;
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

    private static void SetImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetActive(Graphic graphic, bool active)
    {
        if (graphic != null)
        {
            graphic.gameObject.SetActive(active);
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
