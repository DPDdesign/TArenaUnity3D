using TMPro;
using UnityEngine;

public class RewardMapResultGainedPanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI battleText;
    [SerializeField] private TextMeshProUGUI lossesText;
    [SerializeField] private TextMeshProUGUI gainedText;

    public void Bind(RewardMapBattleResultSummary summary, string gainedSummary)
    {
        if (summary == null)
        {
            SetText(resultText, "Battle Result");
            SetText(battleText, "No battle summary");
            SetText(lossesText, string.Empty);
            SetText(gainedText, gainedSummary);
            return;
        }

        SetText(resultText, summary.ResultLabel);
        SetText(battleText, summary.BattleResultId);
        SetText(lossesText, "Losses: " + summary.Losses);
        SetText(gainedText, gainedSummary);

        if (resultText != null)
        {
            resultText.color = string.Equals(summary.ResultLabel, "Victory", System.StringComparison.OrdinalIgnoreCase)
                ? new Color(0.76f, 0.92f, 0.56f, 1f)
                : new Color(0.90f, 0.48f, 0.42f, 1f);
        }

        if (battleText != null)
        {
            battleText.color = new Color(0.96f, 0.84f, 0.65f, 1f);
        }

        if (lossesText != null)
        {
            lossesText.color = summary.Losses > 0
                ? new Color(0.90f, 0.48f, 0.42f, 1f)
                : new Color(0.78f, 0.61f, 0.44f, 1f);
        }

        if (gainedText != null)
        {
            gainedText.color = new Color(0.94f, 0.77f, 0.38f, 1f);
        }
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
