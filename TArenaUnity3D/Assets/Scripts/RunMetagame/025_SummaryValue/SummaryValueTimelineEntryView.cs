using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SummaryValueTimelineEntryView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI receivedText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI goldText;

    public void Bind(SummaryValueTimelineEntry entry)
    {
        bool hasEntry = entry != null;
        gameObject.SetActive(hasEntry);
        if (!hasEntry)
        {
            return;
        }

        SetText(stageText, entry.StageIndex.ToString("00"));
        SetText(labelText, entry.Label);
        SetText(receivedText, entry.ReceivedText);
        SetText(valueText, "Value " + entry.ArmyValueAfterStage.ToString("N0"));
        SetText(goldText, entry.RunGoldAfterStage > 0 ? entry.RunGoldAfterStage.ToString("N0") + " gold" : "spent/clear");
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
