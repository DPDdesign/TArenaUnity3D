using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SavedArmiesSlotView : MonoBehaviour
{
    [SerializeField] private SavedArmiesScreenController screenController;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI savedArmyIdText;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject defenceMarker;

    private SavedArmySlotViewData currentData;

    public void SetController(SavedArmiesScreenController controller)
    {
        screenController = controller;
    }

    public void Bind(SavedArmySlotViewData data)
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
            button.interactable = data.State != SavedArmySlotState.Locked;
        }

        SetText(slotNumberText, data.PhysicalIndex.ToString());
        SetText(stateText, data.State.ToString());
        SetText(valueText, BuildValueText(data));
        SetText(savedArmyIdText, BuildArmyIdText(data));
        SetActive(selectedState, data.Selected);
        SetActive(lockedOverlay, data.State == SavedArmySlotState.Locked);
        SetActive(defenceMarker, data.IsCurrentDefence);
    }

    public void OnClicked()
    {
        if (screenController != null && currentData != null)
        {
            screenController.SelectSlot(currentData.SlotId);
        }
    }

    private static string BuildValueText(SavedArmySlotViewData data)
    {
        if (data.State == SavedArmySlotState.Locked)
        {
            return "Locked";
        }

        if (data.State == SavedArmySlotState.Empty)
        {
            return "Empty";
        }

        return "Value " + data.CurrentArmyValue.ToString("N0");
    }

    private static string BuildArmyIdText(SavedArmySlotViewData data)
    {
        if (data.State == SavedArmySlotState.Taken)
        {
            return ShortId(data.SavedArmyId);
        }

        return data.State == SavedArmySlotState.Locked ? "Future unlock" : "Ready for import";
    }

    private static string ShortId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 18)
        {
            return value;
        }

        return value.Substring(0, 18);
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
