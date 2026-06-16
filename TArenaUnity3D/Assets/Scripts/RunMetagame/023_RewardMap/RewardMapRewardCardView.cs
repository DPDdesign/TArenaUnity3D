using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardMapRewardCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image accentImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private TextMeshProUGUI intentionText;
    [SerializeField] private TextMeshProUGUI familyText;
    [SerializeField] private TextMeshProUGUI verbText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI beforeText;
    [SerializeField] private TextMeshProUGUI afterText;
    [SerializeField] private TextMeshProUGUI legalText;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject disabledState;

    public Button Button
    {
        get { return button; }
    }

    public void Bind(RewardMapCardViewData card, bool selected)
    {
        bool hasCard = card != null;
        gameObject.SetActive(hasCard);
        if (!hasCard)
        {
            return;
        }

        SetText(intentionText, card.Intention.ToString().ToUpperInvariant());
        SetText(familyText, card.Family + " / " + card.Rarity);
        SetText(verbText, card.Verb);
        SetText(titleText, card.Title);
        SetText(detailText, card.Detail);
        SetText(beforeText, card.BeforeText);
        SetText(afterText, card.AfterText);
        SetText(legalText, card.Legal ? "Focus to preview" : card.Error.ToString());

        if (accentImage != null)
        {
            accentImage.color = ColorFor(card.Intention);
        }

        if (frameImage != null)
        {
            frameImage.color = selected
                ? new Color(0.94f, 0.77f, 0.56f, 1f)
                : card.Legal
                    ? new Color(0.50f, 0.31f, 0.18f, 0.96f)
                    : new Color(0.30f, 0.20f, 0.14f, 0.76f);
        }

        if (button != null)
        {
            button.interactable = card.Legal;
        }

        if (afterText != null)
        {
            afterText.color = card.Legal
                ? new Color(0.76f, 0.92f, 0.56f, 1f)
                : new Color(0.62f, 0.54f, 0.46f, 1f);
        }

        if (legalText != null)
        {
            legalText.color = card.Legal
                ? new Color(0.94f, 0.77f, 0.56f, 1f)
                : new Color(0.88f, 0.48f, 0.42f, 1f);
        }

        SetActive(selectedState, selected);
        SetActive(disabledState, !card.Legal);
    }

    private static Color ColorFor(RewardMapIntention intention)
    {
        switch (intention)
        {
            case RewardMapIntention.Stabilize:
                return new Color(0.19f, 0.38f, 0.18f, 1f);
            case RewardMapIntention.Strengthen:
                return new Color(0.56f, 0.31f, 0.15f, 1f);
            case RewardMapIntention.Pivot:
                return new Color(0.45f, 0.20f, 0.20f, 1f);
            default:
                return new Color(0.40f, 0.31f, 0.18f, 1f);
        }
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
