using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SavedArmiesArenaOptionView : MonoBehaviour
{
    [SerializeField] private SavedArmiesScreenController screenController;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private GameObject selectedState;

    private ArenaArmyOptionViewData currentData;

    public void SetController(SavedArmiesScreenController controller)
    {
        screenController = controller;
    }

    public void Bind(ArenaArmyOptionViewData data)
    {
        currentData = data;
        bool hasData = data != null;
        gameObject.SetActive(hasData);
        if (!hasData)
        {
            return;
        }

        if (button != null)
        {
            button.interactable = true;
        }

        SetText(nameText, data.DisplayName);
        SetText(valueText, "Value " + data.CurrentArmyValue.ToString("N0"));
        SetActive(selectedState, data.Selected);
    }

    public void OnClicked()
    {
        if (screenController != null && currentData != null)
        {
            screenController.SelectArenaArmy(currentData.ArenaArmyId);
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
