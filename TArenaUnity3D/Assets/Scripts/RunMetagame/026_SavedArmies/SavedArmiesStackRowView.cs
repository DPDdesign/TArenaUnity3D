using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SavedArmiesStackRowView : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI valueText;

    public void Bind(SavedArmyStackViewData stack)
    {
        bool hasStack = stack != null;
        gameObject.SetActive(hasStack);
        if (!hasStack)
        {
            return;
        }

        RunMetagameRepresentationBinder.BindStack(
            gameObject,
            RunMetagameDisplayInfoFactory.FromSavedArmies(stack, DataMapper.Instance),
            unitIcon,
            tierText,
            nameText,
            amountText,
            valueText,
            null,
            null,
            null,
            null);

        SetText(nameText, stack.DisplayName);
        SetText(tierText, "Tier " + stack.Tier + " / unit " + stack.UnitValue.ToString("N0"));
        SetText(amountText, "x" + stack.Amount.ToString("N0"));
        SetText(valueText, stack.StackValue.ToString("N0") + " value");
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
