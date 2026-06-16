using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartRunScreenController : MonoBehaviour
{
    [SerializeField] private string defaultAccountPlayerId = "offline-player";
    [SerializeField] private string initialStartingArmyId = string.Empty;
    [SerializeField] private string initialRouteId = string.Empty;
    [SerializeField] private StartRunArmyCardView[] armyCards = new StartRunArmyCardView[0];
    [SerializeField] private StartRunRouteOptionView[] routeOptions = new StartRunRouteOptionView[0];
    [SerializeField] private StackRepresentation[] stackRows = new StackRepresentation[0];
    [SerializeField] private Image leaderIcon;
    [SerializeField] private TMP_Text armyPreviewName;
    [SerializeField] private TMP_Text armyPreviewValue;
    [SerializeField] private TMP_Text routeSummaryText;
    [SerializeField] private TMP_Text runtimeMessageText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button beginButton;

    private OfflineStartRunAdapter adapter;
    private DataMapper dataMapper;

    private string selectedStartingArmyId;
    private string selectedRouteId;

    private void Awake()
    {
        adapter = new OfflineStartRunAdapter();
        dataMapper = DataMapper.Instance;

        WireStaticButtons();

        selectedStartingArmyId = initialStartingArmyId;
        selectedRouteId = initialRouteId;

        RefreshScreen();
    }

    public void RefreshScreen()
    {
        StartRunScreenViewData viewData = adapter.BuildScreen(selectedStartingArmyId, selectedRouteId);
        if (viewData == null)
        {
            SetRuntimeMessage("Start Run screen data is missing.");
            return;
        }

        selectedStartingArmyId = viewData.SelectedStartingArmy == null
            ? string.Empty
            : viewData.SelectedStartingArmy.TemplateId;
        selectedRouteId = viewData.SelectedRoutePreview == null
            ? string.Empty
            : viewData.SelectedRoutePreview.RouteId;

        BindArmyCards(viewData);
        BindRouteOptions(viewData);
        BindSelectedArmy(viewData.SelectedStartingArmy);
        BindRouteSummary(viewData.SelectedRoutePreview);

        if (beginButton != null)
        {
            beginButton.interactable = viewData.CanBeginRun;
        }

        SetRuntimeMessage(viewData.ValidationMessage);
    }

    private void BindArmyCards(StartRunScreenViewData viewData)
    {
        for (int i = 0; i < armyCards.Length; i++)
        {
            StartRunArmyCardView card = armyCards[i];
            if (card == null)
            {
                continue;
            }

            StartingArmyOptionViewData option = viewData.StartingArmies != null && i < viewData.StartingArmies.Count
                ? viewData.StartingArmies[i]
                : null;
            bool isSelected = option != null && option.TemplateId == selectedStartingArmyId;
            card.Bind(option, isSelected, dataMapper);

            if (card.Button != null)
            {
                card.Button.onClick.RemoveAllListeners();
                if (option != null)
                {
                    string optionId = option.TemplateId;
                    card.Button.onClick.AddListener(delegate { OnStartingArmySelected(optionId); });
                }
            }
        }
    }

    private void BindRouteOptions(StartRunScreenViewData viewData)
    {
        for (int i = 0; i < routeOptions.Length; i++)
        {
            StartRunRouteOptionView route = routeOptions[i];
            if (route == null)
            {
                continue;
            }

            RoutePreviewViewData option = viewData.RoutePreviews != null && i < viewData.RoutePreviews.Count
                ? viewData.RoutePreviews[i]
                : null;
            bool isSelected = option != null && option.RouteId == selectedRouteId;
            route.Bind(option, isSelected);

            if (route.Button != null)
            {
                route.Button.onClick.RemoveAllListeners();
                if (option != null)
                {
                    string optionId = option.RouteId;
                    route.Button.onClick.AddListener(delegate { OnRouteSelected(optionId); });
                }
            }
        }
    }

    private void BindSelectedArmy(StartingArmyOptionViewData selectedArmy)
    {
        if (armyPreviewName != null)
        {
            armyPreviewName.text = selectedArmy == null ? "NO ARMY" : selectedArmy.DisplayName.ToUpperInvariant();
        }

        if (armyPreviewValue != null)
        {
            armyPreviewValue.text = selectedArmy == null
                ? "ARMY VALUE 0"
                : "ARMY VALUE " + selectedArmy.TotalArmyValue;
        }

        if (leaderIcon != null)
        {
            Sprite sprite = null;
            if (selectedArmy != null && selectedArmy.Stacks != null && selectedArmy.Stacks.Count > 0)
            {
                sprite = StartRunUiSpriteResolver.LoadUnitSprite(dataMapper, selectedArmy.Stacks[0].UnitId);
            }

            leaderIcon.sprite = sprite;
            leaderIcon.color = sprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        }

        for (int i = 0; i < stackRows.Length; i++)
        {
            if (stackRows[i] == null)
            {
                continue;
            }

            StartRunStackViewData stack = selectedArmy != null && selectedArmy.Stacks != null && i < selectedArmy.Stacks.Count
                ? selectedArmy.Stacks[i]
                : null;
            StackInfoData stackInfo = RunMetagameDisplayInfoFactory.FromStartRun(stack, dataMapper);
            stackRows[i].DisplayInfo(stackInfo);
        }
    }

    private void BindRouteSummary(RoutePreviewViewData selectedRoute)
    {
        if (routeSummaryText == null)
        {
            return;
        }

        if (selectedRoute == null)
        {
            routeSummaryText.text = "Select a route.";
            return;
        }

        routeSummaryText.text =
            "Route: " + selectedRoute.DisplayName +
            "\n" + selectedRoute.Description +
            "\nRecommended value " + selectedRoute.RecommendedArmyValue +
            "\nCurrent army value " + selectedRoute.CurrentArmyValue;
    }

    private void WireStaticButtons()
    {
        if (backButton != null)
        {
            if (backButton.onClick.GetPersistentEventCount() == 0)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(HandleBackButtonClicked);
            }
        }

        if (beginButton != null)
        {
            if (beginButton.onClick.GetPersistentEventCount() == 0)
            {
                beginButton.onClick.RemoveAllListeners();
                beginButton.onClick.AddListener(HandleBeginButtonClicked);
            }
        }
    }

    public void HandleBeginButtonClicked()
    {
        OnBeginClicked();
    }

    public void HandleBackButtonClicked()
    {
        OnBackClicked();
    }

    private void OnStartingArmySelected(string templateId)
    {
        selectedStartingArmyId = templateId;
        RefreshScreen();
    }

    private void OnRouteSelected(string routeId)
    {
        selectedRouteId = routeId;
        RefreshScreen();
    }

    private void OnBeginClicked()
    {
        StartRunResult result = adapter.BeginRun(defaultAccountPlayerId, selectedStartingArmyId, selectedRouteId);
        if (result == null)
        {
            SetRuntimeMessage("Run start returned no result.");
            return;
        }

        string resultMessage = result.Message + (result.Success && result.CreatedRun != null ? " " + result.CreatedRun.RunId : string.Empty);
        RefreshScreen();
        SetRuntimeMessage(resultMessage);
    }

    private void OnBackClicked()
    {
        gameObject.SetActive(false);
    }

    private void SetRuntimeMessage(string message)
    {
        if (runtimeMessageText != null)
        {
            runtimeMessageText.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }
    }

}
