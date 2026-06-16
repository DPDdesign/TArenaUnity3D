using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StartRunArmyCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image[] unitIcons = new Image[4];

    public string TemplateId { get; private set; }

    public Button Button
    {
        get { return button; }
    }

    public void Bind(StartingArmyOptionViewData option, bool isSelected, DataMapper dataMapper)
    {
        bool hasData = option != null;
        TemplateId = hasData ? option.TemplateId : string.Empty;

        gameObject.SetActive(hasData);
        if (!hasData)
        {
            return;
        }

        if (nameText != null)
        {
            nameText.text = option.DisplayName.ToUpperInvariant();
        }

        if (valueText != null)
        {
            valueText.text = option.TotalArmyValue.ToString();
        }

        if (statusText != null)
        {
            statusText.text = isSelected ? "SELECTED" : "READY";
            statusText.color = isSelected ? new Color(0.62f, 1f, 0.42f, 1f) : new Color(0.9f, 0.78f, 0.42f, 1f);
        }

        if (background != null)
        {
            background.color = isSelected
                ? new Color(0.72f, 0.34f, 0.13f, 1f)
                : new Color(0.33f, 0.18f, 0.09f, 1f);
        }

        if (button != null)
        {
            button.interactable = option.CanStartRun;
        }

        for (int i = 0; i < unitIcons.Length; i++)
        {
            Image icon = unitIcons[i];
            if (icon == null)
            {
                continue;
            }

            bool hasStack = option.Stacks != null && i < option.Stacks.Count && option.Stacks[i] != null;
            icon.gameObject.SetActive(hasStack);
            if (!hasStack)
            {
                icon.sprite = null;
                continue;
            }

            icon.sprite = StartRunUiSpriteResolver.LoadUnitSprite(dataMapper, option.Stacks[i].UnitId);
            icon.color = icon.sprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        }
    }
}
