using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopStackRowView : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI skillsText;

    public void Bind(RunShopStackSnapshot stack, DataMapper dataMapper)
    {
        bool hasStack = stack != null;
        gameObject.SetActive(hasStack);
        if (!hasStack)
        {
            return;
        }

        RunMetagameRepresentationBinder.BindStack(
            gameObject,
            RunMetagameDisplayInfoFactory.FromRunShop(stack, dataMapper),
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
        SetText(amountText, "x" + stack.Amount + " / lost " + stack.Lost);
        SetText(valueText, stack.CombatValue + " value");
        SetText(skillsText, BuildSkillText(stack));
    }

    private static string BuildSkillText(RunShopStackSnapshot stack)
    {
        if (stack.Skills == null || stack.Skills.Count == 0)
        {
            return "No skills";
        }

        string result = string.Empty;
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            RunShopSkillState skill = stack.Skills[i];
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
