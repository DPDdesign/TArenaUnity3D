using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SummaryValueCommandButtonKind
{
    PrimaryAction,
    Return
}

[DisallowMultipleComponent]
public class SummaryValueCommandButtonView : MonoBehaviour
{
    [SerializeField] private SummaryValueScreenController screenController;
    [SerializeField] private SummaryValueCommandButtonKind commandKind;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;

    public Button Button
    {
        get { return button; }
    }

    public void SetController(SummaryValueScreenController controller)
    {
        screenController = controller;
    }

    public void Bind(string label, bool interactable)
    {
        SetText(labelText, label);
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void OnClicked()
    {
        if (screenController == null)
        {
            return;
        }

        if (commandKind == SummaryValueCommandButtonKind.Return)
        {
            screenController.OnReturnClicked();
            return;
        }

        screenController.OnPrimaryActionClicked();
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
