using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SavedArmiesHistoryEntryView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI opponentText;
    [SerializeField] private TextMeshProUGUI valuesText;

    public void Bind(SavedArmyAttackHistoryEntry entry)
    {
        bool hasEntry = entry != null;
        gameObject.SetActive(hasEntry);
        if (!hasEntry)
        {
            return;
        }

        SetText(resultText, LabelFor(entry.ResultKind));
        SetText(opponentText, entry.OpponentName);
        SetText(valuesText, "A " + entry.AttackerValueAtBattle.ToString("N0") + " / D " + entry.DefenderValueAtBattle.ToString("N0"));
    }

    private static string LabelFor(SavedArmyBattleResultKind kind)
    {
        switch (kind)
        {
            case SavedArmyBattleResultKind.OffenceWin:
                return "Offence Win";
            case SavedArmyBattleResultKind.OffenceLoss:
                return "Offence Loss";
            case SavedArmyBattleResultKind.DefenceWin:
                return "Defence Win";
            case SavedArmyBattleResultKind.DefenceLoss:
                return "Defence Loss";
            default:
                return "Battle";
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
