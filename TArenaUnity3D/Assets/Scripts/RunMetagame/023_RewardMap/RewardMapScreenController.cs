using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardMapScreenController : MonoBehaviour
{
    [Header("Sections")]
    [SerializeField] private RewardMapResultGainedPanelView resultGainedPanel;
    [SerializeField] private RewardMapRewardCardView[] rewardCards = new RewardMapRewardCardView[0];
    [SerializeField] private Transform changedStackPreviewRowsParent;
    [SerializeField] private GameObject changedStackPreviewRowPrefab;

    [Header("Army Panel")]
    [SerializeField] private TMP_Text armySummaryText;
    [SerializeField] private GameObject armyStackRowPrefab;
    [SerializeField] private Transform armyStackRows;

    [Header("Right Summary")]
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private TextMeshProUGUI focusedRewardTitleText;
    [SerializeField] private TextMeshProUGUI focusedRewardPreviewText;
    [SerializeField] private TextMeshProUGUI statusText;

    private DataMapper dataMapper;
    private OfflineRewardMapAdapter adapter;
    private OfflineRunContextDbReader runContextReader;
    private OfflineRunContext runContext;
    private RewardMapArmySnapshot currentArmy;
    private RewardMapChoiceViewData choice;
    private RewardMapApplyResult lastApplyResult;
    private int runGold;
    private int stageIndex;
    private string focusedRewardId;
    private bool rewardApplied;
    private bool initialized;
    private int refreshedFrame = -1;
    private bool changedStackPreviewModeResolved;
    private bool changedStackPreviewUsesSlotParents;
    private readonly List<StackRepresentation> changedStackPreviewRows = new List<StackRepresentation>();
    private readonly List<ChangedStackPreviewSlot> changedStackPreviewSlots = new List<ChangedStackPreviewSlot>();
    private readonly List<StackRepresentation> armyStackRowInstances = new List<StackRepresentation>();

    private sealed class ChangedStackPreviewSlot
    {
        public Transform Box;
        public GameObject RowRoot;
        public StackRepresentation Row;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            RefreshFromPersistedRun();
        }
    }

    private void Initialize()
    {
        dataMapper = DataMapper.Instance;
        adapter = OfflineModeDatabaseComposition.CreateRewardMapAdapter();
        runContextReader = OfflineModeDatabaseComposition.CreateRunContextReader();
        initialized = true;
    }

    public void RefreshFromPersistedRun()
    {
        ResetRewardScreenState();
        if (!LoadRunState())
        {
            return;
        }

        RefreshChoice(string.Empty);
        refreshedFrame = Time.frameCount;
        ClearSelectedUi();
    }

    private void ResetRewardScreenState()
    {
        runContext = null;
        currentArmy = null;
        choice = null;
        lastApplyResult = null;
        runGold = 0;
        stageIndex = 0;
        focusedRewardId = string.Empty;
        rewardApplied = false;
    }

    public void FocusReward(string rewardId)
    {
        PreviewReward(rewardId);
    }

    private void PreviewReward(string rewardId)
    {
        if (string.IsNullOrEmpty(rewardId))
        {
            return;
        }

        if (rewardApplied)
        {
            SetText(statusText, "Reward already applied. Continue to the next run node.");
            return;
        }

        focusedRewardId = rewardId;
        RefreshChoice(string.Empty);
    }

    public void ApplyReward(string rewardId)
    {
        if (Time.frameCount == refreshedFrame)
        {
            return;
        }

        if (choice == null)
        {
            SetText(statusText, "Reward blocked: no materialized choice.");
            return;
        }

        if (rewardApplied)
        {
            SetText(statusText, "Reward already applied.");
            return;
        }

        focusedRewardId = rewardId;
        RefreshChoice(string.Empty);
        RewardMapCardViewData reward = choice.FocusedCard;
        if (reward == null || reward.RewardId != rewardId)
        {
            SetText(statusText, "Reward blocked: selected reward was not found.");
            return;
        }

        RewardMapApplyResult result = adapter.Apply(new RewardMapApplyCommand(
            choice.ChoiceId,
            reward.RewardId,
            choice.RunGoldBeforeReward,
            choice.ArmyBeforeReward));

        lastApplyResult = result;
        SetText(statusText, result.Message);
        if (!result.Success)
        {
            return;
        }

        currentArmy = result.ArmyAfterReward;
        runGold = result.RunGoldAfterReward;
        rewardApplied = true;
        focusedRewardId = result.Reward == null ? focusedRewardId : result.Reward.RewardId;
        RenderAppliedResult(result);
        ShowRunMap();
    }

    public void SelectFocusedReward()
    {
        RewardMapCardViewData reward = choice == null ? null : choice.FocusedCard;
        if (reward == null)
        {
            SetText(statusText, "Select a reward.");
            return;
        }

        if (!reward.Legal)
        {
            SetText(statusText, "Focused reward cannot be applied.");
            return;
        }

        ApplyReward(reward.RewardId);
    }

    public void ContinueAfterReward()
    {
        if (!rewardApplied)
        {
            SetText(statusText, "Continue blocked: apply one reward first.");
            return;
        }

        ShowRunMap();
    }

    private void RefreshChoice(string statusMessage)
    {
        if (currentArmy == null && !LoadRunState())
        {
            return;
        }

        choice = adapter.BuildChoice(
            new RewardMapChoiceRequest(
                runContext.RunIdText,
                stageIndex,
                runGold,
                currentArmy,
                new RewardMapBattleResultSummary(runContext.CurrentNodeIdText, "Victory", 0, 0)),
            focusedRewardId);

        focusedRewardId = choice.FocusedCard == null ? focusedRewardId : choice.FocusedCard.RewardId;
        RenderChoice(statusMessage);
    }

    private bool LoadRunState()
    {
        runContext = runContextReader == null ? null : runContextReader.LoadLatestRunForNextScreen("Reward");
        if (runContext == null)
        {
            RenderUnavailable("Reward Map requires a persisted offline run whose next_screen is Reward.");
            return false;
        }

        currentArmy = runContextReader.ToRewardMapCurrentArmy(runContext);
        if (currentArmy == null || currentArmy.Stacks == null || currentArmy.Stacks.Count == 0)
        {
            RenderUnavailable("Reward Map requires current_army_snapshot_id on the active run.");
            return false;
        }

        runGold = Mathf.Max(0, runContext.CurrentRunGold);
        stageIndex = Mathf.Max(0, runContext.StageProgress);
        return true;
    }

    private void RenderChoice(string statusMessage)
    {
        if (choice == null)
        {
            return;
        }

        if (resultGainedPanel != null)
        {
            resultGainedPanel.Bind(choice.BattleResultSummary, choice.GainedSummary);
        }

        BindRewardCards();
        BindArmyPanel(choice.ArmyBeforeReward);

        RewardMapPreviewData preview = choice.FocusedPreview;
        RewardMapCardViewData focusedCard = choice.FocusedCard;
        RewardMapStackSnapshot changedStack = focusedCard == null || focusedCard.AfterStackPreview == null
            ? preview == null ? null : preview.AffectedStackPreview
            : focusedCard.AfterStackPreview;
        int changedSlotIndex = focusedCard == null ? -1 : focusedCard.AffectedSlotIndex;
        changedSlotIndex = RewardMapArmyFooterSlotMapper.ResolveVisibleSlotIndex(choice.ArmyBeforeReward, changedStack, changedSlotIndex);
        BindChangedStackPreview(changedStack, changedSlotIndex);
        BindFocusedSummary(preview);
        SetText(statusText, string.IsNullOrEmpty(statusMessage) ? BuildFocusStatus() : statusMessage);
    }

    private void RenderAppliedResult(RewardMapApplyResult result)
    {
        string affectedStackId = result.Reward == null ? string.Empty : result.Reward.AffectedStackId;
        BindRewardCards();
        BindArmyPanel(result.ArmyAfterReward);
        RewardMapStackSnapshot changedStack = result.Reward == null || result.Reward.AfterStackPreview == null
            ? FindStack(result.ArmyAfterReward, affectedStackId)
            : result.Reward.AfterStackPreview;
        int changedSlotIndex = result.Reward == null ? -1 : result.Reward.AffectedSlotIndex;
        changedSlotIndex = RewardMapArmyFooterSlotMapper.ResolveVisibleSlotIndex(result.ArmyAfterReward, changedStack, changedSlotIndex);
        BindChangedStackPreview(changedStack, changedSlotIndex);
        SetText(walletText, result.RunGoldAfterReward + " RUN GOLD");
        SetText(inventoryText, BuildInventorySummary());
        SetText(
            focusedRewardTitleText,
            result.Reward == null ? "Selected reward" : "Selected: " + result.Reward.Title);
        SetText(
            focusedRewardPreviewText,
            result.Reward == null ? result.Message : result.Reward.BeforeText + " -> " + result.Reward.AfterText);
        SetText(statusText, "Reward applied. Army preview and run gold are committed.");
    }

    private void BindRewardCards()
    {
        for (int i = 0; i < rewardCards.Length; i++)
        {
            RewardMapRewardCardView cardView = rewardCards[i];
            RewardMapCardViewData card = choice != null && choice.Cards != null && i < choice.Cards.Count ? choice.Cards[i] : null;
            if (cardView == null)
            {
                continue;
            }

            cardView.FocusRequested = FocusReward;
            cardView.ApplyRequested = ApplyReward;
            cardView.Bind(card, card != null && card.RewardId == focusedRewardId);
            if (cardView.Button != null)
            {
                cardView.Button.onClick.RemoveAllListeners();
                cardView.Button.interactable = card != null && card.Legal && !rewardApplied;
            }
        }
    }

    private void BindChangedStackPreview(RewardMapStackSnapshot stack, int slotIndex)
    {
        if (changedStackPreviewRowsParent != null && !changedStackPreviewRowsParent.gameObject.activeSelf)
        {
            changedStackPreviewRowsParent.gameObject.SetActive(true);
        }

        if (!changedStackPreviewModeResolved)
        {
            changedStackPreviewUsesSlotParents = changedStackPreviewRowsParent != null && changedStackPreviewRowsParent.childCount > 0;
            changedStackPreviewModeResolved = true;
        }

        if (changedStackPreviewUsesSlotParents)
        {
            BindChangedStackPreviewToSlots(stack, slotIndex);
            return;
        }

        List<StackInfoData> stacks = new List<StackInfoData>();
        if (stack != null)
        {
            stacks.Add(RunMetagameDisplayInfoFactory.FromRewardMap(stack, dataMapper));
        }

        RunMetagameStackListPresenter.DisplayStackInfo(
            changedStackPreviewRowsParent,
            changedStackPreviewRowPrefab,
            stacks,
            changedStackPreviewRows);
    }

    private void BindChangedStackPreviewToSlots(RewardMapStackSnapshot stack, int slotIndex)
    {
        EnsureChangedStackPreviewSlots();
        bool showSlot = stack != null && slotIndex >= 0 && slotIndex < changedStackPreviewSlots.Count;

        for (int i = 0; i < changedStackPreviewSlots.Count; i++)
        {
            ChangedStackPreviewSlot slot = changedStackPreviewSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool active = showSlot && i == slotIndex;
            if (active)
            {
                EnsureChangedStackPreviewRow(slot, i);
            }

            if (slot.RowRoot != null)
            {
                slot.RowRoot.SetActive(active);
            }

            if (active && slot.Row != null)
            {
                slot.Row.DisplayStackInfo(RunMetagameDisplayInfoFactory.FromRewardMap(stack, dataMapper));
            }
        }
    }

    private void EnsureChangedStackPreviewSlots()
    {
        if (changedStackPreviewRowsParent == null || changedStackPreviewSlots.Count > 0)
        {
            return;
        }

        int childCount = changedStackPreviewRowsParent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            changedStackPreviewSlots.Add(new ChangedStackPreviewSlot
            {
                Box = changedStackPreviewRowsParent.GetChild(i)
            });
        }
    }

    private void EnsureChangedStackPreviewRow(ChangedStackPreviewSlot slot, int index)
    {
        if (slot == null || slot.RowRoot != null || slot.Box == null || changedStackPreviewRowPrefab == null)
        {
            return;
        }

        GameObject rowObject = Instantiate(changedStackPreviewRowPrefab, slot.Box);
        rowObject.name = "ChangedStackPreview_" + (index + 1).ToString("00");
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        if (rowRect != null)
        {
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = Vector2.zero;
        }

        StackRepresentation row = rowObject.GetComponent<StackRepresentation>();
        if (row == null)
        {
            row = rowObject.GetComponentInChildren<StackRepresentation>(true);
        }

        rowObject.SetActive(false);
        slot.RowRoot = rowObject;
        slot.Row = row;
    }

    private void BindArmyPanel(RewardMapArmySnapshot army)
    {
        if (armySummaryText != null)
        {
            armySummaryText.text = "Army Value\n" + ArmyValueLabel(army);
        }

        List<StackInfoData> stackInfos = new List<StackInfoData>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                RewardMapStackSnapshot stack = army.Stacks[i];
                if (stack != null && !string.IsNullOrEmpty(stack.UnitId) && stack.Amount > 0)
                {
                    stackInfos.Add(RunMetagameDisplayInfoFactory.FromRewardMap(stack, dataMapper));
                }
            }
        }

        RunMetagameStackListPresenter.DisplayStackInfo(
            armyStackRows,
            armyStackRowPrefab,
            stackInfos,
            armyStackRowInstances);
    }

    private static string ArmyValueLabel(RewardMapArmySnapshot army)
    {
        return army == null ? "0" : Mathf.Max(0, army.TotalArmyValue).ToString();
    }

    private void BindFocusedSummary(RewardMapPreviewData preview)
    {
        RewardMapCardViewData card = choice.FocusedCard;
        int previewGold = preview == null ? choice.RunGoldBeforeReward : preview.RunGoldAfterReward;
        SetText(walletText, previewGold + " RUN GOLD");
        SetText(inventoryText, BuildInventorySummary());
        SetText(focusedRewardTitleText, card == null ? "No reward focused" : card.Title);
        SetText(
            focusedRewardPreviewText,
            card == null ? string.Empty : card.BeforeText + " -> " + card.AfterText);
    }

    private static void ShowRunMap()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.ShowRunMap();
        }
    }

    private string BuildFocusStatus()
    {
        if (choice == null || choice.FocusedCard == null)
        {
            return "Select a reward.";
        }

        string message = choice.FocusedPreview == null ? choice.Message : choice.FocusedPreview.Message;
        return "Previewing " + choice.FocusedCard.Title + ". " + message;
    }

    private string BuildInventorySummary()
    {
        return "Inventory: persisted run state";
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void ClearSelectedUi()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void RenderUnavailable(string message)
    {
        SetText(walletText, "0 RUN GOLD");
        SetText(inventoryText, "Inventory: unavailable");
        SetText(focusedRewardTitleText, "Reward unavailable");
        SetText(focusedRewardPreviewText, string.Empty);
        SetText(statusText, message);
        BindArmyPanel(null);
        BindChangedStackPreview(null, -1);
        BindRewardCards();
    }

    private static RewardMapStackSnapshot FindStack(RewardMapArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(stackId))
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.StackId == stackId)
            {
                return stack;
            }
        }

        return null;
    }
}
