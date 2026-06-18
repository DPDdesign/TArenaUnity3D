using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArmyGeneratorRuleSetAssetBuilder
{
    private const string BuildSessionKey = "TArena.PRD035.MockArmyGeneratorRuleSetBuilt.v1";
    private const string AssetFolder = "Assets/Resources/0_Data/ArmyRulesets";
    private const string AssetPath = AssetFolder + "/Mock_ArmyGeneratorRuleSet.asset";

    static ArmyGeneratorRuleSetAssetBuilder()
    {
        EditorApplication.delayCall += CreateOnceAfterCompile;
    }

    [MenuItem("TArena/Run Metagame/Create Mock Army Generator Rule Set")]
    public static void CreateFromMenu()
    {
        CreateOrUpdateAsset(true);
    }

    private static void CreateOnceAfterCompile()
    {
        if (SessionState.GetBool(BuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(BuildSessionKey, true);
        CreateOrUpdateAsset(false);
    }

    private static void CreateOrUpdateAsset(bool overwriteExisting)
    {
        EnsureFolder(AssetFolder);

        ArmyGeneratorRuleSet asset = AssetDatabase.LoadAssetAtPath<ArmyGeneratorRuleSet>(AssetPath);
        if (asset != null && !overwriteExisting)
        {
            return;
        }

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ArmyGeneratorRuleSet>();
            asset.ConfigureMockDefaults();
            AssetDatabase.CreateAsset(asset, AssetPath);
        }
        else
        {
            asset.ConfigureMockDefaults();
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetPath);
        Debug.Log("Created mock ArmyGeneratorRuleSet at " + AssetPath + ".");
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
