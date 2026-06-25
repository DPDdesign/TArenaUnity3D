using System;

[Serializable]
public sealed class SkillDefinitionSpec
{
    public string SkillName = string.Empty;
    public string Type = string.Empty;
    public string Info = string.Empty;
    public string Flags = string.Empty;
    public ActivationRuleData ActivationRule = new ActivationRuleData();
    public TargetingRuleData TargetingRule = new TargetingRuleData();
    public ResolutionRuleData ResolutionRule = new ResolutionRuleData();
    public SkillEffect[] Effects = new SkillEffect[0];

    public static SkillDefinitionSpec FromAsset(SkillDefinitionAsset asset)
    {
        if (asset == null)
        {
            return null;
        }

        return new SkillDefinitionSpec
        {
            SkillName = asset.SkillName ?? string.Empty,
            Type = asset.Type ?? string.Empty,
            Info = asset.Info ?? string.Empty,
            Flags = asset.Flags ?? string.Empty,
            ActivationRule = asset.ActivationRule == null ? new ActivationRuleData() : asset.ActivationRule.Clone(),
            TargetingRule = asset.TargetingRule == null ? new TargetingRuleData() : asset.TargetingRule.Clone(),
            ResolutionRule = asset.ResolutionRule == null ? new ResolutionRuleData() : asset.ResolutionRule.Clone(),
            Effects = SkillEffect.CloneArray(asset.Effects)
        };
    }
}
