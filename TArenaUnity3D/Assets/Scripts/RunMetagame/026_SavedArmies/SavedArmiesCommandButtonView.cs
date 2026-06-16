using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SavedArmiesCommandButtonKind
{
    ImportFromArena,
    SetDefence,
    Back
}

[DisallowMultipleComponent]
public class SavedArmiesCommandButtonView : MonoBehaviour
{
    [SerializeField] private SavedArmiesScreenController screenController;
    [SerializeField] private SavedArmiesCommandButtonKind commandKind;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;

    public void SetController(SavedArmiesScreenController controller)
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

        if (commandKind == SavedArmiesCommandButtonKind.ImportFromArena)
        {
            screenController.OnImportFromArenaClicked();
            return;
        }

        if (commandKind == SavedArmiesCommandButtonKind.SetDefence)
        {
            screenController.OnSetDefenceClicked();
            return;
        }

        screenController.OnBackClicked();
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
