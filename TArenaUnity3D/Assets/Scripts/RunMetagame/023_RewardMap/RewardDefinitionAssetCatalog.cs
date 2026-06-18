using System.Collections.Generic;
using UnityEngine;

public class RewardDefinitionAssetCatalog : IRewardMapTemplateCatalog
{
    private const string DefaultResourcePath = "0_Data/Rewards";

    private readonly string resourcePath;

    public RewardDefinitionAssetCatalog()
        : this(DefaultResourcePath)
    {
    }

    public RewardDefinitionAssetCatalog(string resourcePath)
    {
        this.resourcePath = string.IsNullOrEmpty(resourcePath) ? DefaultResourcePath : resourcePath;
    }

    public List<RewardMapTemplate> ListTemplates()
    {
        RewardDefinitionAsset[] assets = Resources.LoadAll<RewardDefinitionAsset>(resourcePath);
        List<RewardDefinitionAsset> sortedAssets = new List<RewardDefinitionAsset>(assets);
        sortedAssets.Sort(delegate(RewardDefinitionAsset left, RewardDefinitionAsset right)
        {
            string leftId = left == null ? string.Empty : left.RewardCatalogId;
            string rightId = right == null ? string.Empty : right.RewardCatalogId;
            return string.CompareOrdinal(leftId, rightId);
        });

        List<RewardMapTemplate> templates = new List<RewardMapTemplate>();
        for (int i = 0; i < sortedAssets.Count; i++)
        {
            RewardDefinitionAsset asset = sortedAssets[i];
            if (asset != null && !string.IsNullOrEmpty(asset.RewardCatalogId))
            {
                templates.Add(asset.ToTemplate());
            }
        }

        return templates;
    }
}
