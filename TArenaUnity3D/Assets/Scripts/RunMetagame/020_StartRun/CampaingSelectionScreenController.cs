using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CampaingSelectionScreenController : MonoBehaviour
{
    [SerializeField] private string defaultAccountPlayerId = "offline-player";
    [SerializeField] private string initialStartingArmyId = string.Empty;
    [SerializeField] private string initialRouteId = string.Empty;
    [SerializeField] private StartRunRouteOptionView[] routeOptions = new StartRunRouteOptionView[0];
    [SerializeField] private TMP_Text selectedArmyText;
    [SerializeField] private TMP_Text routeSummaryText;
    [SerializeField] private TMP_Text runtimeMessageText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button beginButton;

    private OfflineStartRunAdapter adapter;
    private string accountPlayerId;
    private string selectedStartingArmyId;
    private string selectedRouteId;

    private void Awake()
    {
        adapter = new OfflineStartRunAdapter();
        accountPlayerId = string.IsNullOrEmpty(defaultAccountPlayerId) ? "offline-player" : defaultAccountPlayerId;
        selectedStartingArmyId = initialStartingArmyId;
        selectedRouteId = initialRouteId;

        WireStaticButtons();
        RefreshScreen();
    }

    public void OpenForStartingArmy(string newAccountPlayerId, string startingArmyId)
    {
        adapter = new OfflineStartRunAdapter();
        accountPlayerId = string.IsNullOrEmpty(newAccountPlayerId) ? "offline-player" : newAccountPlayerId;
        selectedStartingArmyId = startingArmyId;
        RefreshScreen();
    }

    public void RefreshScreen()
    {
        StartRunScreenViewData viewData = adapter.BuildScreen(selectedStartingArmyId, selectedRouteId, 0, accountPlayerId);
        if (viewData == null)
        {
            SetRuntimeMessage("Campaign Selection data is missing.");
            return;
        }

        selectedStartingArmyId = viewData.SelectedStartingArmy == null
            ? string.Empty
            : viewData.SelectedStartingArmy.TemplateId;
        selectedRouteId = viewData.SelectedRoutePreview == null
            ? string.Empty
            : viewData.SelectedRoutePreview.RouteId;

        BindRouteOptions(viewData);
        BindSelectedArmy(viewData.SelectedStartingArmy);
        BindRouteSummary(viewData.SelectedRoutePreview);

        if (beginButton != null)
        {
            beginButton.interactable = viewData.CanBeginRun;
        }

        SetRuntimeMessage(viewData.ValidationMessage);
    }

    public void HandleBeginButtonClicked()
    {
        OnBeginClicked();
    }

    public void HandleBackButtonClicked()
    {
        gameObject.SetActive(false);
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
        if (selectedArmyText == null)
        {
            return;
        }

        if (selectedArmy == null)
        {
            selectedArmyText.text = "STARTING ARMY: NONE";
            return;
        }

        selectedArmyText.text = "STARTING ARMY: " + selectedArmy.DisplayName.ToUpperInvariant() +
            " / VALUE " + selectedArmy.TotalArmyValue;
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
            selectedRoute.DisplayName.ToUpperInvariant() +
            "\n" + selectedRoute.Description +
            "\nRecommended value " + selectedRoute.RecommendedArmyValue +
            "\nCurrent army value " + selectedRoute.CurrentArmyValue;
    }

    private void WireStaticButtons()
    {
        if (backButton != null && backButton.onClick.GetPersistentEventCount() == 0)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(HandleBackButtonClicked);
        }

        if (beginButton != null && beginButton.onClick.GetPersistentEventCount() == 0)
        {
            beginButton.onClick.RemoveAllListeners();
            beginButton.onClick.AddListener(HandleBeginButtonClicked);
        }
    }

    private void OnRouteSelected(string routeId)
    {
        selectedRouteId = routeId;
        RefreshScreen();
    }

    private void OnBeginClicked()
    {
        StartRunResult result = adapter.BeginRun(accountPlayerId, selectedStartingArmyId, selectedRouteId);
        if (result == null)
        {
            SetRuntimeMessage("Run start returned no result.");
            return;
        }

        string resultMessage = result.Message + (result.Success && result.CreatedRun != null
            ? " " + result.CreatedRun.RunId
            : string.Empty);
        RefreshScreen();
        SetRuntimeMessage(resultMessage);
    }

    private void SetRuntimeMessage(string message)
    {
        if (runtimeMessageText != null)
        {
            runtimeMessageText.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }
    }
}
