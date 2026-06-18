using TMPro;
using UnityEngine;

public class RewardMapScreenController : MonoBehaviour
{
    [Header("Sections")]
    [SerializeField] private RewardMapResultGainedPanelView resultGainedPanel;
    [SerializeField] private RewardMapRewardCardView[] rewardCards = new RewardMapRewardCardView[0];
    [SerializeField] private RewardMapArmyPreviewUnitView[] armyPreviewUnits = new RewardMapArmyPreviewUnitView[0];

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

    private void Awake()
    {
        dataMapper = DataMapper.Instance;
        adapter = OfflineModeDatabaseComposition.CreateRewardMapAdapter();
        runContextReader = OfflineModeDatabaseComposition.CreateRunContextReader();
        if (!LoadRunState())
        {
            return;
        }

        RefreshChoice(string.Empty);
    }

    public void FocusReward(string rewardId)
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
        if (choice == null || choice.FocusedCard == null)
        {
            SetText(statusText, "Reward blocked: no focused reward.");
            return;
        }

        ApplyReward(choice.FocusedCard.RewardId);
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

        RewardMapPreviewData preview = choice.FocusedPreview;
        RewardMapArmySnapshot previewArmy = preview == null ? choice.ArmyBeforeReward : preview.ArmyAfterReward;
        string affectedStackId = preview == null || preview.AffectedStackPreview == null
            ? string.Empty
            : preview.AffectedStackPreview.StackId;

        BindArmyPreview(previewArmy, affectedStackId);
        BindFocusedSummary(preview);
        SetText(statusText, string.IsNullOrEmpty(statusMessage) ? BuildFocusStatus() : statusMessage);
    }

    private void RenderAppliedResult(RewardMapApplyResult result)
    {
        string affectedStackId = result.Reward == null ? string.Empty : result.Reward.AffectedStackId;
        BindRewardCards();
        BindArmyPreview(result.ArmyAfterReward, affectedStackId);
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
            cardView.Bind(card, card != null && card.RewardId == focusedRewardId);
            if (cardView.Button != null)
            {
                cardView.Button.onClick.RemoveAllListeners();
                cardView.Button.interactable = card != null && card.Legal && !rewardApplied;
                if (card != null && card.Legal && !rewardApplied)
                {
                    string rewardId = card.RewardId;
                    cardView.Button.onClick.AddListener(delegate { ApplyReward(rewardId); });
                }
            }
        }
    }

    private void BindArmyPreview(RewardMapArmySnapshot army, string affectedStackId)
    {
        for (int i = 0; i < armyPreviewUnits.Length; i++)
        {
            RewardMapArmyPreviewUnitView unitView = armyPreviewUnits[i];
            RewardMapStackSnapshot stack = army != null && army.Stacks != null && i < army.Stacks.Count
                ? army.Stacks[i]
                : null;

            if (unitView != null)
            {
                unitView.Bind(stack, dataMapper, affectedStackId);
            }
        }
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

    private void RenderUnavailable(string message)
    {
        SetText(walletText, "0 RUN GOLD");
        SetText(inventoryText, "Inventory: unavailable");
        SetText(focusedRewardTitleText, "Reward unavailable");
        SetText(focusedRewardPreviewText, string.Empty);
        SetText(statusText, message);
        BindArmyPreview(null, string.Empty);
        BindRewardCards();
    }
}
