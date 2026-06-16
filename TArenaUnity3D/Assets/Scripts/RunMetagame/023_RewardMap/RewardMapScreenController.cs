using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RewardMapScreenController : MonoBehaviour
{
    [Header("Sample Request")]
    [SerializeField] private string runId = "offline-run";
    [SerializeField] private int stageIndex = 2;
    [SerializeField] private int startingRunGoldBeforeReward = 360;
    [SerializeField] private int sampleInventorySupplies = 2;

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

    [Header("Commands")]
    [SerializeField] private RewardMapCommandButtonView selectCommandButton;
    [SerializeField] private RewardMapCommandButtonView continueCommandButton;

    private DataMapper dataMapper;
    private OfflineRewardMapAdapter adapter;
    private RewardMapArmySnapshot currentArmy;
    private RewardMapChoiceViewData choice;
    private RewardMapApplyResult lastApplyResult;
    private int runGold;
    private string focusedRewardId;
    private bool rewardApplied;

    private void Awake()
    {
        dataMapper = DataMapper.Instance;
        adapter = OfflineModeDatabaseComposition.CreateRewardMapAdapter();
        currentArmy = BuildSampleArmy();
        runGold = Mathf.Max(0, startingRunGoldBeforeReward);
        WireStaticButtons();
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

    public void SelectFocusedReward()
    {
        if (choice == null || choice.FocusedCard == null)
        {
            SetText(statusText, "Select blocked: no focused reward.");
            return;
        }

        RewardMapApplyResult result = adapter.Apply(new RewardMapApplyCommand(
            choice.ChoiceId,
            choice.FocusedCard.RewardId,
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
    }

    public void ContinueAfterReward()
    {
        if (!rewardApplied)
        {
            SetText(statusText, "Continue blocked: select one reward before leaving this screen.");
            return;
        }

        string rewardLabel = lastApplyResult != null && lastApplyResult.Reward != null
            ? lastApplyResult.Reward.Title
            : "no reward applied yet";
        SetText(statusText, "Continue selected. " + rewardLabel + " is committed for the next run node.");
    }

    private void RefreshChoice(string statusMessage)
    {
        choice = adapter.BuildChoice(
            new RewardMapChoiceRequest(
                runId,
                stageIndex,
                runGold,
                currentArmy,
                new RewardMapBattleResultSummary("Stage " + stageIndex + " - Road Battle", "Victory", 12, 120)),
            focusedRewardId);

        focusedRewardId = choice.FocusedCard == null ? focusedRewardId : choice.FocusedCard.RewardId;
        RenderChoice(statusMessage);
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
        BindCommandButtons(false);
    }

    private void BindRewardCards()
    {
        for (int i = 0; i < rewardCards.Length; i++)
        {
            RewardMapRewardCardView cardView = rewardCards[i];
            RewardMapCardViewData card = choice.Cards != null && i < choice.Cards.Count ? choice.Cards[i] : null;
            if (cardView == null)
            {
                continue;
            }

            cardView.Bind(card, card != null && card.RewardId == focusedRewardId);
            if (cardView.Button != null)
            {
                cardView.Button.onClick.RemoveAllListeners();
                cardView.Button.interactable = card != null && card.Legal && !rewardApplied;
                if (card != null && card.Legal && !rewardApplied)
                {
                    string rewardId = card.RewardId;
                    cardView.Button.onClick.AddListener(delegate { FocusReward(rewardId); });
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
        BindCommandButtons(card != null && preview != null && preview.Error == RewardMapError.None && !rewardApplied);
    }

    private void BindCommandButtons(bool canSelect)
    {
        if (selectCommandButton != null)
        {
            selectCommandButton.Bind("Select", canSelect, true);
        }

        if (continueCommandButton != null)
        {
            continueCommandButton.Bind("Continue", true, false);
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
        return "Inventory: " + Mathf.Max(0, sampleInventorySupplies) + " reward supplies";
    }

    private RewardMapArmySnapshot BuildSampleArmy()
    {
        List<RewardMapStackSnapshot> stacks = new List<RewardMapStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", 40, 12, Skill("Chope", true), Skill("Rush", false)),
            Stack("stack-thrower", "Thrower", 28, 0, Skill("Range_Stance_Barb", true), Skill("Double_Throw", false)),
            Stack("stack-healer", "Healer", 20, 3, Skill("Tough_Skin", true), Skill("Defence_Ritual", false)),
            Stack("stack-wisp", "Wisp", 22, 0, Skill("Blind_by_light", true)),
            Stack("stack-stonegolem", "StoneGolem", 30, 0, Skill("Stone_Throw", true)),
            Stack("stack-fireelemental", "FireElemental", 12, 0, Skill("Fire_Ball", true))
        };

        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            total += stacks[i].CombatValue;
        }

        return new RewardMapArmySnapshot("mock-post-battle-army", total, stacks);
    }

    private RewardMapStackSnapshot Stack(string stackId, string unitId, int amount, int lost, params RewardMapSkillState[] skills)
    {
        RunShopUnitDefinition unit = new RewardMapDataMapperUnitSource(dataMapper).FindUnit(unitId);
        int unlockedSkillValue = 0;
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null && skills[i].Unlocked)
            {
                unlockedSkillValue += 18;
            }
        }

        return new RewardMapStackSnapshot(
            stackId,
            unitId,
            unit == null ? unitId : unit.DisplayName,
            unit == null ? "I" : unit.Tier,
            1,
            amount,
            lost,
            amount * (unit == null ? 0 : unit.Cost) + unlockedSkillValue,
            new List<RewardMapSkillState>(skills));
    }

    private static RewardMapSkillState Skill(string skillId, bool unlocked)
    {
        return new RewardMapSkillState(skillId, unlocked);
    }

    private void WireStaticButtons()
    {
        if (selectCommandButton != null && selectCommandButton.Button != null && selectCommandButton.Button.onClick.GetPersistentEventCount() == 0)
        {
            selectCommandButton.Button.onClick.AddListener(SelectFocusedReward);
        }

        if (continueCommandButton != null && continueCommandButton.Button != null && continueCommandButton.Button.onClick.GetPersistentEventCount() == 0)
        {
            continueCommandButton.Button.onClick.AddListener(ContinueAfterReward);
        }
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
