using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunBattleStackRowView : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI lossText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI skillsText;

    public void Configure(
        Image unitIcon,
        TextMeshProUGUI nameText,
        TextMeshProUGUI amountText,
        TextMeshProUGUI lossText,
        TextMeshProUGUI valueText,
        TextMeshProUGUI skillsText)
    {
        this.unitIcon = unitIcon;
        this.nameText = nameText;
        this.amountText = amountText;
        this.lossText = lossText;
        this.valueText = valueText;
        this.skillsText = skillsText;
    }

    public void Bind(RunBattleStackSnapshot stack, DataMapper dataMapper)
    {
        bool hasStack = stack != null;
        gameObject.SetActive(hasStack);
        if (!hasStack)
        {
            return;
        }

        SetText(nameText, stack.DisplayName);
        SetText(amountText, "x" + stack.Amount + " / Tier " + stack.Tier + " / Level " + stack.Level);
        SetText(lossText, "Lost " + stack.Lost);
        SetText(valueText, stack.CombatValue + " value");
        SetText(skillsText, BuildSkillText(stack));

        if (unitIcon != null && dataMapper != null)
        {
            unitIcon.sprite = dataMapper.LoadUnitSprite(stack.UnitId);
            unitIcon.color = unitIcon.sprite == null ? new Color(1f, 1f, 1f, 0.18f) : Color.white;
        }
    }

    private static string BuildSkillText(RunBattleStackSnapshot stack)
    {
        if (stack.Skills == null || stack.Skills.Count == 0)
        {
            return "No skills";
        }

        string result = string.Empty;
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            RunBattleSkillState skill = stack.Skills[i];
            if (skill == null)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result += " / ";
            }

            result += skill.Unlocked ? skill.SkillId : "[" + skill.SkillId + "]";
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
}
