using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartRunScreenController : MonoBehaviour
{
    [SerializeField] private string defaultAccountPlayerId = "offline-player";
    [SerializeField] private string initialStartingArmyId = string.Empty;
    [SerializeField] private StartRunArmyCardView[] armyCards = new StartRunArmyCardView[0];
    [SerializeField] private StackRepresentation[] stackRows = new StackRepresentation[0];
    [SerializeField] private Image leaderIcon;
    [SerializeField] private TMP_Text armyPreviewName;
    [SerializeField] private TMP_Text armyPreviewValue;
    [SerializeField] private TMP_Text runStartingGoldText;
    [SerializeField] private TMP_Text runRollTokensText;
    [SerializeField] private TMP_Text battleSkipTokensText;
    [SerializeField] private TMP_Text runtimeMessageText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button beginButton;

    private OfflineStartRunAdapter adapter;
    private DataMapper dataMapper;

    private string selectedStartingArmyId;
    private string selectedRouteId;
    private bool initialized;
    private bool skipInitialOnEnable;

    private void Awake()
    {
        skipInitialOnEnable = true;
        StartNewOfferSession();
        dataMapper = DataMapper.Instance;

        WireStaticButtons();

        RefreshScreen();
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        if (skipInitialOnEnable)
        {
            skipInitialOnEnable = false;
            return;
        }

        StartNewOfferSession();
        RefreshScreen();
    }

    public void RefreshScreen()
    {
        StartRunScreenViewData viewData = adapter.BuildScreen(
            selectedStartingArmyId,
            string.Empty,
            armyCards == null ? 0 : armyCards.Length,
            defaultAccountPlayerId);
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
        BindSelectedArmy(viewData.SelectedStartingArmy);
        BindStartingAssets(viewData.StartingAssets);

        if (beginButton != null)
        {
            beginButton.interactable = viewData.CanBeginRun;
        }

        SetRuntimeMessage(viewData.ValidationError == StartRunValidationError.None
            ? "Starting army selected."
            : viewData.ValidationMessage);
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
                if (option != null && option.CanStartRun)
                {
                    string optionId = option.TemplateId;
                    card.Button.onClick.AddListener(delegate { OnStartingArmySelected(optionId); });
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

    private void BindStartingAssets(StartRunStartingAssetsViewData assets)
    {
        int runStartingGold = assets == null ? 0 : assets.RunStartingGold;
        int runRollTokens = assets == null ? 0 : assets.RunRollTokens;
        int battleSkipTokens = assets == null ? 0 : assets.BattleSkipTokens;

        SetText(runStartingGoldText, runStartingGold + " RUN GOLD");
        SetText(runRollTokensText, runRollTokens + " ROLL TOKENS");
        SetText(battleSkipTokensText, battleSkipTokens + " BATTLE SKIP TOKENS");
    }

    public string GetSelectedStartingArmyId()
    {
        return selectedStartingArmyId;
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

    private void OnBeginClicked()
    {
        StartRunResult result = adapter.BeginRun(
            defaultAccountPlayerId,
            selectedStartingArmyId,
            selectedRouteId,
            armyCards == null ? 0 : armyCards.Length);
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

        if (result.Success && result.CreatedRun != null)
        {
            ShowRunMap();
        }
    }

    private void OnBackClicked()
    {
        OfflineModeDatabaseComposition.EndRunGenerationSession();
        gameObject.SetActive(false);
    }

    private void StartNewOfferSession()
    {
        OfflineModeDatabaseComposition.StartNewRunGenerationSession(defaultAccountPlayerId);
        adapter = new OfflineStartRunAdapter();
        selectedStartingArmyId = initialStartingArmyId;
        selectedRouteId = string.Empty;
    }

    private void ShowRunMap()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("[StartRunScreenController] GameSceneManager instance is missing; cannot show Run Map.");
            return;
        }

        GameSceneManager.Instance.ShowRunMap();
    }

    private void SetRuntimeMessage(string message)
    {
        if (runtimeMessageText != null)
        {
            runtimeMessageText.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

}
