using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultArmySummaryCardView : MonoBehaviour
{
    [SerializeField] private Button focusButton;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI armyNameText;
    [SerializeField] private TextMeshProUGUI ownerText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI stackSummaryText;
    [SerializeField] private TextMeshProUGUI preservedText;
    [SerializeField] private Image[] stackIcons = new Image[0];
    [SerializeField] private GameObject selectedState;

    private BattleResultSavedArmySnapshot boundArmy;

    public Button FocusButton
    {
        get { return focusButton; }
    }

    public BattleResultSavedArmySnapshot BoundArmy
    {
        get { return boundArmy; }
    }

    public void Bind(BattleResultSavedArmySnapshot army, string roleLabel, string ownerLabel, bool selected)
    {
        boundArmy = army;
        bool hasArmy = army != null;
        gameObject.SetActive(hasArmy);
        if (!hasArmy)
        {
            return;
        }

        SetText(roleText, roleLabel);
        SetText(armyNameText, string.IsNullOrEmpty(army.DisplayName) ? army.SavedArmyId : army.DisplayName);
        SetText(ownerText, ownerLabel);
        SetText(powerText, army.ArmyValue.ToString("N0") + " power");
        SetText(stackSummaryText, BuildStackSummary(army.Stacks));
        SetText(preservedText, "Saved army preserved");
        SetActive(selectedState, selected);

        if (focusButton != null)
        {
            focusButton.interactable = true;
        }

        BindStackIcons(army.Stacks);
    }

    public void SetSelected(bool selected)
    {
        SetActive(selectedState, selected);
    }

    private void BindStackIcons(List<BattleResultStackSnapshot> stacks)
    {
        for (int i = 0; i < stackIcons.Length; i++)
        {
            Image icon = stackIcons[i];
            if (icon == null)
            {
                continue;
            }

            bool hasStack = stacks != null && i < stacks.Count && stacks[i] != null;
            icon.gameObject.SetActive(hasStack);
            if (!hasStack)
            {
                continue;
            }

            Sprite sprite = LoadUnitSprite(stacks[i]);
            icon.sprite = sprite;
            icon.color = sprite == null ? new Color(0.40f, 0.30f, 0.18f, 1f) : Color.white;
            icon.preserveAspect = true;
        }
    }

    private static Sprite LoadUnitSprite(BattleResultStackSnapshot stack)
    {
        StackInfoData stackInfo = RunMetagameDisplayInfoFactory.FromBattleResult(stack, DataMapper.Instance);
        if (stackInfo == null || stackInfo.Unit == null || string.IsNullOrEmpty(stackInfo.Unit.SpriteReference))
        {
            return null;
        }

        return DataMapper.Instance.LoadUnitSprite(stackInfo.Unit.SpriteReference);
    }

    private static string BuildStackSummary(List<BattleResultStackSnapshot> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return "No stack data";
        }

        string result = string.Empty;
        int count = Mathf.Min(4, stacks.Count);
        for (int i = 0; i < count; i++)
        {
            BattleResultStackSnapshot stack = stacks[i];
            if (stack == null)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result += "\n";
            }

            StackInfoData stackInfo = RunMetagameDisplayInfoFactory.FromBattleResult(stack, DataMapper.Instance);
            string displayName = stackInfo != null && stackInfo.Unit != null ? stackInfo.Unit.DisplayName : stack.DisplayName;
            int amount = stackInfo == null ? stack.Amount : stackInfo.Count;
            int value = stackInfo == null ? stack.CombatValue : stackInfo.StackValue;
            result += displayName + " x" + amount + " / " + value.ToString("N0");
        }

        if (stacks.Count > count)
        {
            result += "\n+" + (stacks.Count - count) + " more stacks";
        }

        return result;
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
