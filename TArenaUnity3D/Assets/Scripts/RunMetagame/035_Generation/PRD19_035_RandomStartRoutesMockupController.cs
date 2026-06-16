using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PRD19_035_RandomStartRoutesMockupController : MonoBehaviour
{
    [Serializable]
    public class OfferBinding
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image selectedFrame;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text stackText;
        [SerializeField] private TMP_Text statusText;

        public Button Button { get { return button; } }

        public void Render(int index, bool selected)
        {
            SetText(nameText, index == 0 ? "STONE SPARK" : index == 1 ? "WISP SCREEN" : "IRON PACT");
            SetText(valueText, index == 0 ? "VALUE 1640" : index == 1 ? "VALUE 1660" : "VALUE 1620");
            SetText(stackText, "4 stacks / 1 unlocked skill each");
            SetText(statusText, selected ? "SELECTED" : "READY");
            SetActive(selectedFrame, selected);
            SetImageColor(background, selected ? new Color(0.58f, 0.29f, 0.10f, 1f) : new Color(0.20f, 0.13f, 0.075f, 1f));
        }
    }

    [Serializable]
    public class RouteNodeBinding
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image selectedFrame;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text riskText;

        public Button Button { get { return button; } }

        public void Render(bool selected)
        {
            SetActive(selectedFrame, selected);
            SetImageColor(background, selected ? new Color(0.82f, 0.52f, 0.16f, 1f) : new Color(0.28f, 0.16f, 0.08f, 1f));
        }

        public void SetLabels(string title, string type, string risk)
        {
            SetText(titleText, title);
            SetText(typeText, type);
            SetText(riskText, risk);
        }
    }

    [Header("Offer Preview")]
    [SerializeField] private OfferBinding[] offerBindings = new OfferBinding[0];
    [SerializeField] private TMP_Text selectedOfferText;
    [SerializeField] private TMP_Text startingAssetsText;

    [Header("Route Preview")]
    [SerializeField] private RouteNodeBinding[] routeNodeBindings = new RouteNodeBinding[0];
    [SerializeField] private TMP_Text selectedRouteNodeText;
    [SerializeField] private TMP_Text routeSummaryText;

    [Header("Commands")]
    [SerializeField] private Button rerollPreviewButton;
    [SerializeField] private Button beginPreviewButton;
    [SerializeField] private TMP_Text runtimeMessageText;

    private int selectedOfferIndex;
    private int selectedNodeIndex;

    private void Awake()
    {
        Render();
    }

    public void SelectOffer(int index)
    {
        selectedOfferIndex = Mathf.Clamp(index, 0, Mathf.Max(0, offerBindings.Length - 1));
        Render();
    }

    public void SelectRouteNode(int index)
    {
        selectedNodeIndex = Mathf.Clamp(index, 0, Mathf.Max(0, routeNodeBindings.Length - 1));
        Render();
    }

    public void HandleRerollPreviewClicked()
    {
        if (offerBindings.Length > 0)
        {
            selectedOfferIndex = (selectedOfferIndex + 1) % offerBindings.Length;
        }

        SetText(runtimeMessageText, "Preview reroll: one stable seed variation selected.");
        Render();
    }

    public void HandleBeginPreviewClicked()
    {
        SetText(runtimeMessageText, "Preview begin: selected army snapshot and generated route map persist through Offline DB.");
    }

    private void Render()
    {
        for (int i = 0; i < offerBindings.Length; i++)
        {
            if (offerBindings[i] != null)
            {
                offerBindings[i].Render(i, i == selectedOfferIndex);
            }
        }

        SetText(selectedOfferText, "Selected generated offer " + (selectedOfferIndex + 1).ToString());
        SetText(startingAssetsText, "150 RUN GOLD / 1 REROLL TOKEN / 0 BATTLE SKIP TOKENS");

        for (int i = 0; i < routeNodeBindings.Length; i++)
        {
            if (routeNodeBindings[i] != null)
            {
                routeNodeBindings[i].Render(i == selectedNodeIndex);
            }
        }

        SetText(selectedRouteNodeText, "Focused route point " + (selectedNodeIndex + 1).ToString());
        SetText(routeSummaryText, "Fixed mission route: battle, event, battle, safe/risk branch, shop, final battle.");
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetActive(Graphic graphic, bool active)
    {
        if (graphic != null)
        {
            graphic.gameObject.SetActive(active);
        }
    }

    private static void SetImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }
}
