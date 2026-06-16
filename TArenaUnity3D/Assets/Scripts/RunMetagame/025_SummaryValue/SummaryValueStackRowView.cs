using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SummaryValueStackRowView : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI skillsText;

    public void Bind(SummaryValueStackSnapshot stack)
    {
        bool hasStack = stack != null;
        gameObject.SetActive(hasStack);
        if (!hasStack)
        {
            return;
        }

        RunMetagameRepresentationBinder.BindStack(
            gameObject,
            RunMetagameDisplayInfoFactory.FromSummaryValue(stack, DataMapper.Instance),
            unitIcon,
            tierText,
            nameText,
            amountText,
            valueText,
            null,
            null,
            null,
            skillsText);

        SetText(nameText, stack.DisplayName);
        SetText(tierText, "Tier " + stack.Tier + " / Level " + stack.Level);
        SetText(amountText, "x" + stack.Amount.ToString("N0"));
        SetText(valueText, stack.CombatValue.ToString("N0") + " value");
        SetText(skillsText, BuildSkillText(stack));
    }

    private static string BuildSkillText(SummaryValueStackSnapshot stack)
    {
        if (stack.Skills == null || stack.Skills.Count == 0)
        {
            return "No skills";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            SummaryValueSkillState skill = stack.Skills[i];
            if (skill == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(skill.Unlocked ? skill.SkillId : "[" + skill.SkillId + "]");
        }

        return builder.ToString();
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
