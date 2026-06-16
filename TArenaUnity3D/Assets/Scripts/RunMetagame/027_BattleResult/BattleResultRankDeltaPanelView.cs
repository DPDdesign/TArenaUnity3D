using TMPro;
using UnityEngine;

public class BattleResultRankDeltaPanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI deltaText;
    [SerializeField] private TextMeshProUGUI beforeText;
    [SerializeField] private TextMeshProUGUI afterText;
    [SerializeField] private TextMeshProUGUI opponentText;
    [SerializeField] private TextMeshProUGUI sourceText;

    public void Bind(BattleResultViewData result)
    {
        bool hasResult = result != null && result.Success;
        gameObject.SetActive(hasResult);
        if (!hasResult)
        {
            return;
        }

        SetText(resultText, BuildResultLabel(result.ResultKind));
        SetText(deltaText, FormatDelta(result.RankDelta));
        SetText(beforeText, "Current " + result.RankBefore);
        SetText(afterText, "New " + result.RankAfter);
        SetText(opponentText, BuildOpponentText(result));
        SetText(sourceText, result.GameMode + " / " + result.AuthoritySource);
    }

    private static string BuildResultLabel(BattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case BattleResultKind.OffenceWin:
                return "OFFENCE VICTORY";
            case BattleResultKind.OffenceLoss:
                return "OFFENCE DEFEAT";
            case BattleResultKind.DefenceWin:
                return "DEFENCE HELD";
            case BattleResultKind.DefenceLoss:
                return "DEFENCE LOST";
            default:
                return "BATTLE RESULT";
        }
    }

    private static string BuildOpponentText(BattleResultViewData result)
    {
        if (result.Opponent == null)
        {
            return "Opponent rank " + result.RankBefore;
        }

        return result.Opponent.DisplayName + " rank " + result.Opponent.RankBefore;
    }

    private static string FormatDelta(int value)
    {
        return value >= 0 ? "+" + value : value.ToString();
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
