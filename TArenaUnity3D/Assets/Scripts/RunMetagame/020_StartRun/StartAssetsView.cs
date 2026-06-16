using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class StartAssetsView : MonoBehaviour
{
    [SerializeField] private TMP_Text runStartingGoldText;
    [SerializeField] private TMP_Text runRollTokensText;
    [SerializeField] private TMP_Text battleSkipTokensText;

    public void Bind(StartRunStartingAssetsViewData assets)
    {
        int runStartingGold = assets == null ? 0 : assets.RunStartingGold;
        int runRollTokens = assets == null ? 0 : assets.RunRollTokens;
        int battleSkipTokens = assets == null ? 0 : assets.BattleSkipTokens;

        Bind(runStartingGold, runRollTokens, battleSkipTokens);
    }

    public void Bind(int runStartingGold, int runRollTokens, int battleSkipTokens)
    {
        SetText(runStartingGoldText, runStartingGold + " RUN GOLD");
        SetText(runRollTokensText, runRollTokens + " ROLL TOKENS");
        SetText(battleSkipTokensText, battleSkipTokens + " BATTLE SKIP TOKENS");
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
