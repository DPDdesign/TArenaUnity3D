using TMPro;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardMapRewardCardView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image accentImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private TextMeshProUGUI intentionText;
    [SerializeField] private TextMeshProUGUI familyText;
    [SerializeField] private TextMeshProUGUI verbText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI legalText;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject disabledState;

    public Button Button
    {
        get { return button; }
    }

    public Action<string> FocusRequested;
    public Action<string> ApplyRequested;

    private string rewardId;
    private bool legal;
    private static readonly string[] LegacyPreviewChildNames =
    {
        "Text_BeforeLabel",
        "Text_Before",
        "Preview_BeforeStack",
        "Text_Arrow",
        "Text_AfterLabel",
        "Text_After",
        "Preview_AfterStack",
        "Inset_Before",
        "Inset_After"
    };

    public void Bind(RewardMapCardViewData card, bool selected)
    {
        HideLegacyPreviewBlocks();
        bool hasCard = card != null;
        gameObject.SetActive(hasCard);
        if (!hasCard)
        {
            rewardId = string.Empty;
            legal = false;
            return;
        }

        SetText(intentionText, card.Intention.ToString().ToUpperInvariant());
        SetText(familyText, card.Family + " / " + card.Rarity);
        SetText(verbText, card.Verb);
        SetText(titleText, card.Title);
        SetText(detailText, card.Detail);
        SetText(legalText, card.Legal ? "Hover to preview, click to apply" : card.Error.ToString());
        rewardId = card.RewardId;
        legal = card.Legal;

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

        if (legalText != null)
        {
            legalText.color = card.Legal
                ? new Color(0.94f, 0.77f, 0.56f, 1f)
                : new Color(0.88f, 0.48f, 0.42f, 1f);
        }

        SetActive(selectedState, selected);
        SetActive(disabledState, !card.Legal);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (legal && FocusRequested != null)
        {
            FocusRequested(rewardId);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (legal && ApplyRequested != null)
        {
            ApplyRequested(rewardId);
        }
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

    private void HideLegacyPreviewBlocks()
    {
        for (int i = 0; i < LegacyPreviewChildNames.Length; i++)
        {
            Transform child = FindChildRecursive(transform, LegacyPreviewChildNames[i]);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
