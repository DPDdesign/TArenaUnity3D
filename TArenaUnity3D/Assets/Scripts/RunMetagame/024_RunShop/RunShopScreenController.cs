using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopScreenController : MonoBehaviour
{
    [SerializeField] private string runId = "offline-run";
    [SerializeField] private string routeNodeId = "offline-shop-node";
    [SerializeField] private int startingRunCurrency = 120;
    [SerializeField] private RunShopOfferCardView[] offerCards = new RunShopOfferCardView[0];
    [SerializeField] private RunShopStackRowView[] currentArmyRows = new RunShopStackRowView[0];
    [SerializeField] private RunShopStackRowView[] previewArmyRows = new RunShopStackRowView[0];
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private TextMeshProUGUI selectedOfferTitleText;
    [SerializeField] private TextMeshProUGUI selectedOfferPreviewText;
    [SerializeField] private TextMeshProUGUI resultMessageText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button leaveButton;

    private OfflineRunShopAdapter adapter;
    private DataMapper dataMapper;
    private RunShopVisitViewData visit;
    private RunShopArmySnapshot currentArmy;
    private int runCurrency;
    private string visitId;
    private string focusedOfferId;

    private void Awake()
    {
        adapter = new OfflineRunShopAdapter();
        dataMapper = DataMapper.Instance;
        runCurrency = Mathf.Max(0, startingRunCurrency);
        currentArmy = BuildMockArmy();
        WireStaticButtons();
        Refresh();
    }

    public void Refresh()
    {
        visit = adapter.BuildVisit(
            new RunShopVisitRequest(visitId, runId, routeNodeId, runCurrency, currentArmy),
            focusedOfferId);

        visitId = visit.VisitId;
        focusedOfferId = visit.FocusedOffer == null ? focusedOfferId : visit.FocusedOffer.OfferId;
        BindOfferCards();
        BindArmyRows(currentArmyRows, visit.CurrentArmy);
        BindArmyRows(previewArmyRows, visit.FocusedPreview == null ? visit.CurrentArmy : visit.FocusedPreview.ArmyAfterPurchase);
        SetText(walletText, visit.RunCurrency + " RUN GOLD");
        SetText(selectedOfferTitleText, visit.FocusedOffer == null ? "No offer selected" : visit.FocusedOffer.Title);
        SetText(
            selectedOfferPreviewText,
            visit.FocusedOffer == null ? string.Empty : visit.FocusedOffer.BeforeText + " -> " + visit.FocusedOffer.AfterText);
        SetText(resultMessageText, visit.Message);

        if (buyButton != null)
        {
            buyButton.interactable = visit.CanBuyFocusedOffer;
        }
    }

    public void SelectOffer(string offerId)
    {
        focusedOfferId = offerId;
        Refresh();
    }

    public void BuyFocusedOffer()
    {
        if (visit == null || visit.FocusedOffer == null)
        {
            return;
        }

        RunShopPurchaseResult result = adapter.Purchase(new RunShopPurchaseCommand(
            visit.VisitId,
            visit.FocusedOffer.OfferId,
            runCurrency,
            currentArmy));

        SetText(resultMessageText, result.Message);
        if (result.Success)
        {
            currentArmy = result.ArmyAfterPurchase;
            runCurrency = result.CurrencyAfterPurchase;
            Refresh();
        }
    }

    public void LeaveShop()
    {
        RunShopLeaveResult result = adapter.Leave(new RunShopLeaveCommand(
            visit == null ? visitId : visit.VisitId,
            visit == null || visit.FocusedOffer == null ? focusedOfferId : visit.FocusedOffer.OfferId));

        SetText(resultMessageText, result == null ? "Leave shop failed." : result.Message);
    }

    private void BindOfferCards()
    {
        for (int i = 0; i < offerCards.Length; i++)
        {
            RunShopOfferCardView card = offerCards[i];
            RunShopOfferViewData offer = visit != null && visit.Offers != null && i < visit.Offers.Count
                ? visit.Offers[i]
                : null;

            if (card == null)
            {
                continue;
            }

            card.Bind(offer, offer != null && offer.OfferId == focusedOfferId);
            if (card.Button != null)
            {
                card.Button.onClick.RemoveAllListeners();
                if (offer != null)
                {
                    string offerId = offer.OfferId;
                    card.Button.onClick.AddListener(delegate { SelectOffer(offerId); });
                }
            }
        }
    }

    private void BindArmyRows(RunShopStackRowView[] rows, RunShopArmySnapshot army)
    {
        if (rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            RunShopStackRowView row = rows[i];
            RunShopStackSnapshot stack = army != null && army.Stacks != null && i < army.Stacks.Count
                ? army.Stacks[i]
                : null;

            if (row != null)
            {
                row.Bind(stack, dataMapper);
            }
        }
    }

    private RunShopArmySnapshot BuildMockArmy()
    {
        System.Collections.Generic.List<RunShopStackSnapshot> stacks = new System.Collections.Generic.List<RunShopStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", 28, 5, "Chope", "Rush"),
            Stack("stack-thrower", "Thrower", 10, 0, "Range_Stance_Barb", "Double_Throw", "Axe_Rain"),
            Stack("stack-healer", "Healer", 5, 2, "Tough_Skin", "Defence_Ritual"),
            Stack("stack-wisp", "Wisp", 22, 0, "Blind_by_light", "Unstoppable_Light")
        };

        int totalValue = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            totalValue += stacks[i].CombatValue;
        }

        return new RunShopArmySnapshot(
            "mock-shop-army",
            totalValue,
            stacks);
    }

    private RunShopStackSnapshot Stack(string stackId, string unitId, int amount, int lost, params string[] skillIds)
    {
        RunShopUnitDefinition unit = new DataMapperRunShopUnitSource(dataMapper).FindUnit(unitId);
        System.Collections.Generic.List<RunShopSkillState> skills = new System.Collections.Generic.List<RunShopSkillState>();
        for (int i = 0; i < skillIds.Length; i++)
        {
            skills.Add(new RunShopSkillState(skillIds[i], i == 0));
        }

        return new RunShopStackSnapshot(
            stackId,
            unitId,
            unit == null ? unitId : unit.DisplayName,
            unit == null ? "I" : unit.Tier,
            1,
            amount,
            lost,
            amount * (unit == null ? 0 : unit.Cost),
            skills);
    }

    private void WireStaticButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            if (buyButton.onClick.GetPersistentEventCount() == 0)
            {
                buyButton.onClick.AddListener(BuyFocusedOffer);
            }
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            if (leaveButton.onClick.GetPersistentEventCount() == 0)
            {
                leaveButton.onClick.AddListener(LeaveShop);
            }
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
