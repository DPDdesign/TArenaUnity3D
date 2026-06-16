using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultXpProgressPanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI gainedText;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI unlockText;
    [SerializeField] private Slider progressSlider;

    public void Bind(BattleResultViewData result)
    {
        bool hasResult = result != null && result.Success;
        gameObject.SetActive(hasResult);
        if (!hasResult)
        {
            return;
        }

        BattleResultAccountProgress progress = result.AccountProgress ?? BattleResultAccountProgress.FromTotalXp(result.AccountXpAfter, result.NextUnlockPreview);
        SetText(levelText, "Level " + progress.Level);
        SetText(gainedText, "+" + result.AccountXpGained + " XP");
        SetText(totalText, result.AccountXpAfter.ToString("N0") + " total XP");
        SetText(progressText, progress.XpIntoLevel + " / " + progress.XpForNextLevel);
        SetText(unlockText, result.NextUnlockPreview + "\nUnlock checkpoint at " + progress.NextUnlockAtTotalXp.ToString("N0") + " XP");

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = Mathf.Clamp01(progress.Progress01);
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
