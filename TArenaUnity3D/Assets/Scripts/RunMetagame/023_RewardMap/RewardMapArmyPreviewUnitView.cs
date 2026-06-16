using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardMapArmyPreviewUnitView : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI skillsText;
    [SerializeField] private GameObject affectedState;

    public void Bind(RewardMapStackSnapshot stack, DataMapper dataMapper, string affectedStackId)
    {
        bool hasStack = stack != null;
        gameObject.SetActive(hasStack);
        if (!hasStack)
        {
            return;
        }

        RunMetagameRepresentationBinder.BindStack(
            gameObject,
            RunMetagameDisplayInfoFactory.FromRewardMap(stack, dataMapper),
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
        SetText(tierText, "Tier " + stack.Tier + " / Lv " + stack.Level);
        SetText(amountText, "x" + stack.Amount + " / lost " + stack.Lost);
        SetText(valueText, stack.CombatValue + " value");
        SetText(skillsText, BuildSkillText(stack));
        SetActive(affectedState, stack.StackId == affectedStackId);

        if (amountText != null)
        {
            amountText.color = stack.Lost > 0
                ? new Color(0.94f, 0.77f, 0.38f, 1f)
                : new Color(0.96f, 0.84f, 0.65f, 1f);
        }

        if (valueText != null)
        {
            valueText.color = stack.StackId == affectedStackId
                ? new Color(0.94f, 0.77f, 0.56f, 1f)
                : new Color(0.84f, 0.66f, 0.40f, 1f);
        }

        if (unitIcon != null)
        {
            unitIcon.preserveAspect = true;
        }
    }

    private static string BuildSkillText(RewardMapStackSnapshot stack)
    {
        if (stack.Skills == null || stack.Skills.Count == 0)
        {
            return "No skills";
        }

        string result = string.Empty;
        for (int i = 0; i < stack.Skills.Count; i++)
        {
            RewardMapSkillState skill = stack.Skills[i];
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
