using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "DataMapper", menuName = "TArena/Data Mapper")]
public class DataMapper : ScriptableObject
{
    private const string MapperResourcePath = "0_Data/DataMapper";

    private static DataMapper instance;

    [SerializeField] private UnitCatalog unitCatalog;
    [SerializeField] private SkillCatalog skillCatalog;
    [SerializeField] private string unitIconsFolderPath = "UI/Unit_Icons";
    [SerializeField] private string skillIconsFolderPath = "Skill_Icons";
    [SerializeField] private string unitPrefabsFolderPath = "1_TosterModels";
    [SerializeField] private string infoPagesFolderPath = "Z_LEGACY/Info_Pages";
    [SerializeField] private string buildFilePrefix = "build";
    [SerializeField] private string buildFileExtension = ".d";

    private Dictionary<string, UnitDefinition> unitLookup;
    private List<UnitDefinition> unitDefinitions;
    private Dictionary<string, SkillDefinition> skillLookup;
    private List<SkillDefinition> skillDefinitions;

    public static DataMapper Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<DataMapper>(MapperResourcePath);
                if (instance == null)
                {
                    instance = CreateInstance<DataMapper>();
                    instance.hideFlags = HideFlags.HideAndDontSave;
                    Debug.LogWarning("DataMapper asset not found. Using in-memory defaults.");
                }
            }

            return instance;
        }
    }

    private void OnEnable()
    {
        ClearCache();
        if (instance == null || (instance.hideFlags & HideFlags.HideAndDontSave) != 0)
        {
            instance = this;
        }
    }

    private void OnValidate()
    {
        ClearCache();
    }

    public UnitDefinition FindUnit(string unitName)
    {
        EnsureUnitCache();

        UnitDefinition definition;
        if (string.IsNullOrEmpty(unitName) || !unitLookup.TryGetValue(unitName, out definition))
        {
            return null;
        }

        return definition;
    }

    public UnitCatalog UnitCatalog
    {
        get { return unitCatalog; }
    }

    public SkillCatalog SkillCatalog
    {
        get { return skillCatalog; }
    }

    public SkillDefinition FindSkill(string skillName)
    {
        EnsureSkillCache();

        SkillDefinition definition;
        if (string.IsNullOrEmpty(skillName) || !skillLookup.TryGetValue(skillName, out definition))
        {
            return null;
        }

        return definition;
    }

    public List<UnitDefinition> GetAllUnits()
    {
        EnsureUnitCache();
        return new List<UnitDefinition>(unitDefinitions);
    }

    public List<string> GetAllUnitNames()
    {
        EnsureUnitCache();
        List<string> names = new List<string>();
        foreach (UnitDefinition definition in unitDefinitions)
        {
            names.Add(definition.Name);
        }

        return names;
    }

    public List<string> GetAllUnitSpriteReferences()
    {
        EnsureUnitCache();
        List<string> spriteReferences = new List<string>();
        foreach (UnitDefinition definition in unitDefinitions)
        {
            spriteReferences.Add(definition.SpritePath);
        }

        return spriteReferences;
    }

    public Sprite LoadUnitSprite(string spriteReference)
    {
        string resourcePath = ResolveUnitSpriteResourcePath(spriteReference);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    public Sprite LoadSkillIcon(string skillName)
    {
        string resourcePath = CombineResourcePath(skillIconsFolderPath, ExtractLeafName(skillName));
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    public Sprite LoadInfoPageSprite(string entryName)
    {
        string resourcePath = CombineResourcePath(infoPagesFolderPath, ExtractLeafName(entryName));
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    public GameObject LoadUnitPrefab(string unitName)
    {
        string resourcePath = CombineResourcePath(unitPrefabsFolderPath, ExtractLeafName(unitName));
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<GameObject>(resourcePath);
    }

    public string GetBuildFilePath(int buildNumber)
    {
        return GetBuildFilePath(buildNumber.ToString());
    }

    public string GetBuildFilePath(string buildNumber)
    {
        string safeBuildNumber = string.IsNullOrEmpty(buildNumber) ? "0" : buildNumber;
        return Path.Combine(Application.persistentDataPath, buildFilePrefix + safeBuildNumber + buildFileExtension);
    }

    public string ResolveUnitSpriteResourcePath(string spriteReference)
    {
        string spriteName = ExtractLeafName(spriteReference);
        if (string.IsNullOrEmpty(spriteName))
        {
            return string.Empty;
        }

        return CombineResourcePath(unitIconsFolderPath, spriteName);
    }

    private void EnsureUnitCache()
    {
        if (unitLookup != null && unitDefinitions != null)
        {
            return;
        }

        unitLookup = new Dictionary<string, UnitDefinition>();
        unitDefinitions = new List<UnitDefinition>();

        if (unitCatalog == null)
        {
            Debug.LogError("UnitCatalog is not assigned on DataMapper. Unit XML fallback is disabled.");
            return;
        }

        List<UnitDefinitionAsset> units = unitCatalog.GetUnits();
        foreach (UnitDefinitionAsset unitAsset in units)
        {
            UnitDefinition definition = unitAsset == null ? null : unitAsset.ToUnitDefinition();
            if (definition == null || string.IsNullOrEmpty(definition.Name))
            {
                continue;
            }

            unitDefinitions.Add(definition);
            if (!unitLookup.ContainsKey(definition.Name))
            {
                unitLookup.Add(definition.Name, definition);
            }
        }
    }

    private void EnsureSkillCache()
    {
        if (skillLookup != null && skillDefinitions != null)
        {
            return;
        }

        skillLookup = new Dictionary<string, SkillDefinition>();
        skillDefinitions = new List<SkillDefinition>();

        if (skillCatalog == null)
        {
            Debug.LogError("SkillCatalog is not assigned on DataMapper. Skill XML fallback is disabled.");
            return;
        }

        List<SkillDefinitionAsset> skills = skillCatalog.GetSkills();
        foreach (SkillDefinitionAsset skillAsset in skills)
        {
            SkillDefinition definition = skillAsset == null ? null : skillAsset.ToSkillDefinition();
            if (definition == null || string.IsNullOrEmpty(definition.Name))
            {
                continue;
            }

            skillDefinitions.Add(definition);
            if (!skillLookup.ContainsKey(definition.Name))
            {
                skillLookup.Add(definition.Name, definition);
            }
        }
    }

    private void ClearCache()
    {
        unitLookup = null;
        unitDefinitions = null;
        skillLookup = null;
        skillDefinitions = null;
    }

    private static string CombineResourcePath(string folderPath, string entryName)
    {
        string safeFolderPath = string.IsNullOrEmpty(folderPath) ? string.Empty : folderPath.Trim().TrimEnd('/', '\\');
        string safeEntryName = string.IsNullOrEmpty(entryName) ? string.Empty : entryName.Trim().TrimStart('/', '\\');

        if (string.IsNullOrEmpty(safeFolderPath))
        {
            return safeEntryName;
        }

        if (string.IsNullOrEmpty(safeEntryName))
        {
            return safeFolderPath;
        }

        return safeFolderPath + "/" + safeEntryName;
    }

    private static string ExtractLeafName(string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName))
        {
            return string.Empty;
        }

        string normalizedPath = pathOrName.Replace('\\', '/').Trim();
        int separatorIndex = normalizedPath.LastIndexOf('/');
        string leafName = separatorIndex >= 0 ? normalizedPath.Substring(separatorIndex + 1) : normalizedPath;

        int extensionIndex = leafName.LastIndexOf('.');
        if (extensionIndex > 0)
        {
            leafName = leafName.Substring(0, extensionIndex);
        }

        return leafName;
    }

    public class UnitDefinition
    {
        public string Name { get; private set; }
        public string Tier { get; private set; }
        public int HP { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int Initiative { get; private set; }
        public int Speed { get; private set; }
        public int DamageMinimum { get; private set; }
        public int DamageMaximum { get; private set; }
        public int Cost { get; private set; }
        public string SpritePath { get; private set; }
        public List<string> SkillNames { get; private set; }

        public UnitDefinition(
            string name,
            string tier,
            int hp,
            int attack,
            int defense,
            int initiative,
            int speed,
            int damageMinimum,
            int damageMaximum,
            int cost,
            string spritePath,
            List<string> skillNames)
        {
            Name = name;
            Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
            HP = hp;
            Attack = attack;
            Defense = defense;
            Initiative = initiative;
            Speed = speed;
            DamageMinimum = damageMinimum;
            DamageMaximum = damageMaximum;
            Cost = cost;
            SpritePath = spritePath;
            SkillNames = skillNames ?? new List<string>();
        }
    }

    public class SkillDefinition
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Info { get; private set; }
        public string Flags { get; private set; }

        public SkillDefinition(string name, string type, string info, string flags)
        {
            Name = name;
            Type = type;
            Info = info;
            Flags = flags;
        }

        public bool HasFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || string.IsNullOrEmpty(Flags))
            {
                return false;
            }

            string[] parts = Flags.Split(new char[] { ' ', ',', ';', '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == flag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
