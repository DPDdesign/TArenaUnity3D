#if UNITY_EDITOR
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class DataMapperAssetBootstrap
{
    private const string AssetPath = "Assets/Resources/0_Data/DataMapper.asset";
    private const string UnitCatalogPath = "Assets/Resources/0_Data/UnitCatalog.asset";
    private const string SkillCatalogPath = "Assets/Resources/0_Data/SkillCatalog.asset";
    private const string UnitDefinitionsFolder = "Assets/Resources/0_Data/Units";
    private const string UnitsXmlPath = "Assets/Resources/0_Data/Units.xml";

    [InitializeOnLoadMethod]
    private static void EnsureDataMapperAssetExists()
    {
        DataMapper existingAsset = AssetDatabase.LoadAssetAtPath<DataMapper>(AssetPath);
        if (existingAsset == null)
        {
            existingAsset = ScriptableObject.CreateInstance<DataMapper>();
            AssetDatabase.CreateAsset(existingAsset, AssetPath);
        }

        UnitCatalog unitCatalog = EnsureUnitCatalog();
        SkillCatalog skillCatalog = EnsureSkillCatalog();
        AssignCatalogs(existingAsset, unitCatalog, skillCatalog);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("TArena/Data/Rebuild Unit Catalog From XML")]
    private static void RebuildUnitCatalogFromXmlMenu()
    {
        DataMapper dataMapper = AssetDatabase.LoadAssetAtPath<DataMapper>(AssetPath);
        if (dataMapper == null)
        {
            dataMapper = ScriptableObject.CreateInstance<DataMapper>();
            AssetDatabase.CreateAsset(dataMapper, AssetPath);
        }

        UnitCatalog unitCatalog = RebuildUnitCatalogFromXml();
        SkillCatalog skillCatalog = EnsureSkillCatalog();
        AssignCatalogs(dataMapper, unitCatalog, skillCatalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static UnitCatalog EnsureUnitCatalog()
    {
        UnitCatalog unitCatalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(UnitCatalogPath);
        if (unitCatalog == null)
        {
            unitCatalog = RebuildUnitCatalogFromXml();
        }

        return unitCatalog;
    }

    private static SkillCatalog EnsureSkillCatalog()
    {
        return AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
    }

    private static UnitCatalog RebuildUnitCatalogFromXml()
    {
        EnsureFolder("Assets/Resources/0_Data", "Units");

        UnitCatalog unitCatalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(UnitCatalogPath);
        if (unitCatalog == null)
        {
            unitCatalog = ScriptableObject.CreateInstance<UnitCatalog>();
            AssetDatabase.CreateAsset(unitCatalog, UnitCatalogPath);
        }

        TextAsset unitsXml = AssetDatabase.LoadAssetAtPath<TextAsset>(UnitsXmlPath);
        if (unitsXml == null)
        {
            Debug.LogError("Cannot rebuild UnitCatalog. Missing XML migration source: " + UnitsXmlPath);
            return unitCatalog;
        }

        List<UnitDefinitionAsset> unitAssets = ParseUnitAssets(unitsXml.text);
        unitCatalog.SetUnits(unitAssets);
        EditorUtility.SetDirty(unitCatalog);
        return unitCatalog;
    }

    private static List<UnitDefinitionAsset> ParseUnitAssets(string xmlText)
    {
        List<UnitDefinitionAsset> unitAssets = new List<UnitDefinitionAsset>();
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlText);

        XmlNodeList units = xmlDocument.SelectNodes("Units/Unit");
        foreach (XmlNode unitNode in units)
        {
            UnitDefinitionAsset unitAsset = CreateOrUpdateUnitAsset(unitNode);
            if (unitAsset != null)
            {
                unitAssets.Add(unitAsset);
            }
        }

        return unitAssets;
    }

    private static UnitDefinitionAsset CreateOrUpdateUnitAsset(XmlNode unitNode)
    {
        string unitName = ReadChildText(unitNode, "Name");
        if (string.IsNullOrEmpty(unitName))
        {
            return null;
        }

        string assetPath = UnitDefinitionsFolder + "/" + SanitizeAssetName(unitName) + ".asset";
        UnitDefinitionAsset unitAsset = AssetDatabase.LoadAssetAtPath<UnitDefinitionAsset>(assetPath);
        bool createdAsset = false;
        if (unitAsset == null)
        {
            unitAsset = ScriptableObject.CreateInstance<UnitDefinitionAsset>();
            AssetDatabase.CreateAsset(unitAsset, assetPath);
            createdAsset = true;
        }

        int factionId = createdAsset ? UnitFactionResolver.ResolveFactionId(unitName) : unitAsset.FactionId;
        UnitRoleCategory unitRoleCategory = createdAsset ? InferDefaultRoleCategory(unitName) : unitAsset.UnitRoleCategory;

        unitAsset.Configure(
            unitName,
            ReadTier(unitNode, unitName),
            factionId,
            unitRoleCategory,
            ReadChildInt(unitNode, "HP"),
            ReadChildInt(unitNode, "Attack"),
            ReadChildInt(unitNode, "Defense"),
            ReadChildInt(unitNode, "Initiative"),
            ReadChildInt(unitNode, "Speed"),
            ReadChildInt(unitNode, "DamageMinimum"),
            ReadChildInt(unitNode, "DamageMaximum"),
            ReadChildInt(unitNode, "Cost"),
            ReadChildText(unitNode, "Sprite"),
            ReadSkills(unitNode));

        EditorUtility.SetDirty(unitAsset);
        return unitAsset;
    }

    private static UnitRoleCategory InferDefaultRoleCategory(string unitName)
    {
        switch (unitName)
        {
            case "Healer":
                return UnitRoleCategory.Support;
            case "Thrower":
            case "Wisp":
                return UnitRoleCategory.Ranged;
            case "Trapper":
            case "Specialist":
                return UnitRoleCategory.Control;
            case "Tank":
            case "Axeman":
            case "Rusher":
            case "HeavyHitter":
            case "StoneGolem":
            case "FleshGolem":
            case "FireElemental":
                return UnitRoleCategory.Frontline;
            default:
                return UnitRoleCategory.Flexible;
        }
    }

    private static List<string> ReadSkills(XmlNode unitNode)
    {
        List<string> skills = new List<string>();
        XmlNodeList skillNodes = unitNode.SelectNodes("Skills/*");
        foreach (XmlNode skillNode in skillNodes)
        {
            if (!string.IsNullOrEmpty(skillNode.InnerText))
            {
                skills.Add(skillNode.InnerText);
            }
        }

        return skills;
    }

    private static string ReadTier(XmlNode unitNode, string unitName)
    {
        string tier = ReadChildText(unitNode, "Tier");
        if (!string.IsNullOrEmpty(tier))
        {
            return tier;
        }

        return InferLegacyTier(unitName);
    }

    private static string InferLegacyTier(string unitName)
    {
        switch (unitName)
        {
            case "Axeman":
            case "Specialist":
            case "StoneGolem":
            case "FireElemental":
                return "II";
            case "HeavyHitter":
            case "Tank":
            case "FleshGolem":
                return "III";
            default:
                return "I";
        }
    }

    private static void AssignCatalogs(DataMapper dataMapper, UnitCatalog unitCatalog, SkillCatalog skillCatalog)
    {
        if (dataMapper == null)
        {
            return;
        }

        SerializedObject serializedDataMapper = new SerializedObject(dataMapper);
        serializedDataMapper.FindProperty("unitCatalog").objectReferenceValue = unitCatalog;
        serializedDataMapper.FindProperty("skillCatalog").objectReferenceValue = skillCatalog;
        serializedDataMapper.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dataMapper);
    }

    private static void EnsureFolder(string parentFolder, string childFolder)
    {
        string folderPath = parentFolder + "/" + childFolder;
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, childFolder);
        }
    }

    private static string ReadChildText(XmlNode parentNode, string childName)
    {
        if (parentNode == null)
        {
            return string.Empty;
        }

        XmlNode childNode = parentNode.SelectSingleNode(childName);
        return childNode == null ? string.Empty : childNode.InnerText;
    }

    private static int ReadChildInt(XmlNode parentNode, string childName)
    {
        int parsedValue;
        int.TryParse(ReadChildText(parentNode, childName), out parsedValue);
        return parsedValue;
    }

    private static string SanitizeAssetName(string rawName)
    {
        string safeName = string.IsNullOrEmpty(rawName) ? "UnitDefinition" : rawName.Trim();
        char[] invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
        {
            safeName = safeName.Replace(invalidCharacters[i], '_');
        }

        return safeName;
    }
}
#endif
