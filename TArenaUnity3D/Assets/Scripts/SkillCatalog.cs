using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillCatalog", menuName = "TArena/Skills/Skill Catalog")]
public class SkillCatalog : ScriptableObject
{
    [SerializeField] private List<SkillDefinitionAsset> skills = new List<SkillDefinitionAsset>();

    private Dictionary<string, SkillDefinitionAsset> skillLookup;

    public List<SkillDefinitionAsset> GetSkills()
    {
        return skills == null ? new List<SkillDefinitionAsset>() : new List<SkillDefinitionAsset>(skills);
    }

    public SkillDefinitionAsset FindSkillAsset(string skillName)
    {
        EnsureLookup();

        SkillDefinitionAsset asset;
        if (string.IsNullOrEmpty(skillName) || !skillLookup.TryGetValue(skillName, out asset))
        {
            return null;
        }

        return asset;
    }

    private void OnEnable()
    {
        ClearLookup();
    }

    private void OnValidate()
    {
        ClearLookup();
    }

    private void EnsureLookup()
    {
        if (skillLookup != null)
        {
            return;
        }

        skillLookup = new Dictionary<string, SkillDefinitionAsset>();
        List<SkillDefinitionAsset> sourceSkills = skills ?? new List<SkillDefinitionAsset>();
        for (int i = 0; i < sourceSkills.Count; i++)
        {
            SkillDefinitionAsset skill = sourceSkills[i];
            if (skill == null || string.IsNullOrEmpty(skill.SkillName) || skillLookup.ContainsKey(skill.SkillName))
            {
                continue;
            }

            skillLookup.Add(skill.SkillName, skill);
        }
    }

    private void ClearLookup()
    {
        skillLookup = null;
    }

#if UNITY_EDITOR
    public void SetSkills(List<SkillDefinitionAsset> newSkills)
    {
        skills = newSkills == null ? new List<SkillDefinitionAsset>() : new List<SkillDefinitionAsset>(newSkills);
        ClearLookup();
    }
#endif
}
