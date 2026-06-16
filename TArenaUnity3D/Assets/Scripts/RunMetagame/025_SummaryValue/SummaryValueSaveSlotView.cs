using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SummaryValueSaveSlotView : MonoBehaviour
{
    [SerializeField] private SummaryValueScreenController screenController;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI armyIdText;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject takenState;
    [SerializeField] private GameObject emptyState;

    private SummaryValueSaveSlotViewData currentData;

    public Button Button
    {
        get { return button; }
    }

    public void SetController(SummaryValueScreenController controller)
    {
        screenController = controller;
    }

    public void Bind(SummaryValueSaveSlotViewData data)
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

        SetText(slotNumberText, data.PhysicalIndex.ToString());
        SetText(stateText, BuildStateText(data));
        SetText(valueText, BuildValueText(data));
        SetText(armyIdText, BuildArmyText(data));
        SetActive(selectedState, data.Selected);
        SetActive(lockedOverlay, data.State == SummaryValueSlotState.Locked);
        SetActive(takenState, data.State == SummaryValueSlotState.Taken);
        SetActive(emptyState, data.State == SummaryValueSlotState.Empty);
    }

    public void OnClicked()
    {
        if (screenController == null || currentData == null)
        {
            return;
        }

        screenController.SelectSlot(currentData.SlotId);
    }

    private static string BuildStateText(SummaryValueSaveSlotViewData data)
    {
        if (data.State == SummaryValueSlotState.Locked)
        {
            return "Locked";
        }

        return data.State == SummaryValueSlotState.Taken ? "Taken" : "Empty";
    }

    private static string BuildValueText(SummaryValueSaveSlotViewData data)
    {
        if (data.State == SummaryValueSlotState.Locked)
        {
            return "Unlock later";
        }

        if (data.State == SummaryValueSlotState.Empty)
        {
            return "Ready to save";
        }

        return "Value " + data.ExistingArmyValue.ToString("N0");
    }

    private static string BuildArmyText(SummaryValueSaveSlotViewData data)
    {
        if (data.State == SummaryValueSlotState.Taken)
        {
            return string.IsNullOrEmpty(data.ExistingSavedArmyId) ? "Saved army" : data.ExistingSavedArmyId;
        }

        return data.State == SummaryValueSlotState.Locked ? "Account unlock" : "No army saved";
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
