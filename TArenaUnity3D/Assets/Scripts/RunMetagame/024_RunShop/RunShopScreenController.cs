using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopScreenController : MonoBehaviour
{
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
    private OfflineRunContextDbReader runContextReader;
    private OfflineRunContext runContext;
    private DataMapper dataMapper;
    private RunShopVisitViewData visit;
    private RunShopArmySnapshot currentArmy;
    private int runCurrency;
    private string visitId;
    private string focusedOfferId;

    private void Awake()
    {
        adapter = OfflineModeDatabaseComposition.CreateRunShopAdapter();
        runContextReader = OfflineModeDatabaseComposition.CreateRunContextReader();
        dataMapper = DataMapper.Instance;
        WireStaticButtons();
        if (!LoadRunState())
        {
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (currentArmy == null && !LoadRunState())
        {
            return;
        }

        visit = adapter.BuildVisit(
            new RunShopVisitRequest(visitId, runContext.RunIdText, runContext.CurrentNodeDatabaseIdText, runCurrency, currentArmy),
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

    private bool LoadRunState()
    {
        runContext = runContextReader == null ? null : runContextReader.LoadLatestRunForNextScreen("RunShop");
        if (runContext == null)
        {
            RenderUnavailable("Run Shop requires a persisted offline run whose next_screen is RunShop.");
            return false;
        }

        currentArmy = runContextReader.ToRunShopCurrentArmy(runContext);
        if (currentArmy == null || currentArmy.Stacks == null || currentArmy.Stacks.Count == 0)
        {
            RenderUnavailable("Run Shop requires current_army_snapshot_id on the active run.");
            return false;
        }

        if (runContext.CurrentNodeId <= 0)
        {
            RenderUnavailable("Run Shop requires current_node_id on the active run.");
            return false;
        }

        runCurrency = Mathf.Max(0, runContext.CurrentRunGold);
        return true;
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

    private void RenderUnavailable(string message)
    {
        visit = null;
        BindOfferCards();
        BindArmyRows(currentArmyRows, null);
        BindArmyRows(previewArmyRows, null);
        SetText(walletText, "0 RUN GOLD");
        SetText(selectedOfferTitleText, "Shop unavailable");
        SetText(selectedOfferPreviewText, string.Empty);
        SetText(resultMessageText, message);
        if (buyButton != null)
        {
            buyButton.interactable = false;
        }
    }
}
