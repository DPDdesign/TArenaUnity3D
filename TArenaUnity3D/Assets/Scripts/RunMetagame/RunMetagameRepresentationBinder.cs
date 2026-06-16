using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RunMetagameRepresentationBinder
{
    public static void BindStack(
        GameObject root,
        StackInfoData info,
        Image unitIcon,
        TMP_Text tierText,
        TMP_Text nameText,
        TMP_Text countText,
        TMP_Text stackValueText,
        TMP_Text levelText,
        TMP_Text lostText,
        TMP_Text stackCostText,
        TMP_Text skillsText)
    {
        if (root == null)
        {
            return;
        }

        UnitRepresentation unitRepresentation = EnsureUnitRepresentation(root);
        if (unitRepresentation != null)
        {
            unitRepresentation.Icon = unitRepresentation.Icon == null ? unitIcon : unitRepresentation.Icon;
            unitRepresentation.Tier = unitRepresentation.Tier == null ? tierText : unitRepresentation.Tier;
            unitRepresentation.Name = unitRepresentation.Name == null ? nameText : unitRepresentation.Name;
            unitRepresentation.SkillsText = unitRepresentation.SkillsText == null ? skillsText : unitRepresentation.SkillsText;
        }

        StackRepresentation stackRepresentation = EnsureStackRepresentation(root);
        if (stackRepresentation != null)
        {
            stackRepresentation.Icon = stackRepresentation.Icon == null ? unitIcon : stackRepresentation.Icon;
            stackRepresentation.Tier = stackRepresentation.Tier == null ? tierText : stackRepresentation.Tier;
            stackRepresentation.Name = stackRepresentation.Name == null ? nameText : stackRepresentation.Name;
            stackRepresentation.SkillsText = stackRepresentation.SkillsText == null ? skillsText : stackRepresentation.SkillsText;
            stackRepresentation.Count = stackRepresentation.Count == null ? countText : stackRepresentation.Count;
            stackRepresentation.StackValue = stackRepresentation.StackValue == null ? stackValueText : stackRepresentation.StackValue;
            stackRepresentation.Level = stackRepresentation.Level == null ? levelText : stackRepresentation.Level;
            stackRepresentation.Lost = stackRepresentation.Lost == null ? lostText : stackRepresentation.Lost;
            stackRepresentation.StackCost = stackRepresentation.StackCost == null ? stackCostText : stackRepresentation.StackCost;
            stackRepresentation.DisplayInfo(info);
        }
    }

    static UnitRepresentation EnsureUnitRepresentation(GameObject root)
    {
        UnitRepresentation representation = root.GetComponent<UnitRepresentation>();
        if (representation == null)
        {
            representation = root.GetComponentInChildren<UnitRepresentation>(true);
        }

        return representation == null ? root.AddComponent<UnitRepresentation>() : representation;
    }

    static StackRepresentation EnsureStackRepresentation(GameObject root)
    {
        StackRepresentation representation = root.GetComponent<StackRepresentation>();
        return representation == null ? root.AddComponent<StackRepresentation>() : representation;
    }
}
