using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopOfferCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI previewText;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject disabledState;
    [SerializeField] private GameObject purchasedState;

    public Button Button
    {
        get { return button; }
    }

    public void Bind(RunShopOfferViewData offer, bool selected)
    {
        bool hasOffer = offer != null;
        gameObject.SetActive(hasOffer);
        if (!hasOffer)
        {
            return;
        }

        SetText(titleText, offer.Title);
        SetText(categoryText, offer.Category.ToString());
        SetText(costText, offer.Cost + " RUN GOLD");
        SetText(detailText, offer.Detail);
        SetText(previewText, offer.BeforeText + " -> " + offer.AfterText);

        if (button != null)
        {
            button.interactable = offer.Available && !offer.Purchased;
        }

        SetActive(selectedState, selected);
        SetActive(disabledState, !offer.Affordable || !offer.Available);
        SetActive(purchasedState, offer.Purchased);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
