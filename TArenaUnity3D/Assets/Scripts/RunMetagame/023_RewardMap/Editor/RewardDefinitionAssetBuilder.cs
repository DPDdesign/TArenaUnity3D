using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RewardDefinitionAssetBuilder
{
    private const string BuildSessionKey = "TArena.PRD035.MockRewardDefinitionsBuilt.v1";
    private const string AssetFolder = "Assets/Resources/0_Data/Rewards";

    static RewardDefinitionAssetBuilder()
    {
        EditorApplication.delayCall += CreateOnceAfterCompile;
    }

    [MenuItem("TArena/Run Metagame/Create Mock Reward Definition Assets")]
    public static void CreateFromMenu()
    {
        CreateOrUpdateAssets(true);
    }

    private static void CreateOnceAfterCompile()
    {
        if (SessionState.GetBool(BuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(BuildSessionKey, true);
        CreateOrUpdateAssets(false);
    }

    private static void CreateOrUpdateAssets(bool overwriteExisting)
    {
        EnsureFolder(AssetFolder);

        List<RewardMapTemplate> templates = new DefaultRewardMapTemplateCatalog().ListTemplates();
        for (int i = 0; i < templates.Count; i++)
        {
            RewardMapTemplate template = templates[i];
            if (template == null || string.IsNullOrEmpty(template.TemplateId))
            {
                continue;
            }

            string assetPath = AssetFolder + "/" + ToAssetFileName(template.TemplateId) + ".asset";
            RewardDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<RewardDefinitionAsset>(assetPath);
            if (asset != null && !overwriteExisting)
            {
                continue;
            }

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RewardDefinitionAsset>();
                asset.Configure(template);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                asset.Configure(template);
                EditorUtility.SetDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created mock reward definition assets under " + AssetFolder + ".");
    }

    private static string ToAssetFileName(string rewardCatalogId)
    {
        char[] buffer = new char[rewardCatalogId.Length];
        int length = 0;
        for (int i = 0; i < rewardCatalogId.Length; i++)
        {
            char value = rewardCatalogId[i];
            buffer[length++] = char.IsLetterOrDigit(value) || value == '-' || value == '_' ? value : '_';
        }

        return new string(buffer, 0, length);
    }

    private static void EnsureFolder(string folder)
    {
        string normalized = folder.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
