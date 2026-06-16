using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "TArena/Skills/Skill Definition")]
public class SkillDefinitionAsset : ScriptableObject
{
    [SerializeField] private string skillName;
    [SerializeField] private string type;
    [TextArea(2, 6)]
    [SerializeField] private string info;
    [SerializeField] private string flags;

    public string SkillName { get { return skillName; } }
    public string Type { get { return type; } }
    public string Info { get { return info; } }
    public string Flags { get { return flags; } }

    public DataMapper.SkillDefinition ToSkillDefinition()
    {
        return new DataMapper.SkillDefinition(skillName, type, info, flags);
    }

#if UNITY_EDITOR
    public void Configure(string newSkillName, string newType, string newInfo, string newFlags)
    {
        skillName = newSkillName;
        type = newType;
        info = newInfo;
        flags = newFlags;
    }
#endif
}
