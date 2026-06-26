public sealed class TacticalAISkillMetadata
{
    public string SkillId = string.Empty;
    public bool IsPassive;
    public bool CanUseAfterMove;
    public bool CanMoveAfterSkill;
    public bool IsRepeatableToggle;
}

public interface ITacticalAISkillMetadataProvider
{
    bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata);
}

public interface ITacticalAISkillDefinitionProvider
{
    bool TryGetSkillDefinition(string skillId, out SkillDefinitionAsset definition);
}

public interface ITacticalAISkillSpecProvider
{
    bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec);
}

public sealed class TacticalAIDataMapperSkillMetadataProvider :
    ITacticalAISkillMetadataProvider,
    ITacticalAISkillDefinitionProvider,
    ITacticalAISkillSpecProvider
{
    static TacticalAIDataMapperSkillMetadataProvider instance;

    public static TacticalAIDataMapperSkillMetadataProvider Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TacticalAIDataMapperSkillMetadataProvider();
            }

            return instance;
        }
    }

    TacticalAIDataMapperSkillMetadataProvider()
    {
    }

    public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
    {
        SkillDefinitionAsset definition;
        TryGetSkillDefinition(skillId, out definition);
        ActivationRuleData activation = definition != null ? definition.ActivationRule : new ActivationRuleData();
        metadata = new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsPassive = activation.activationKind == SkillActivationKind.Passive,
            CanUseAfterMove = activation.canUseAfterMove,
            CanMoveAfterSkill = activation.canMoveAfterUse,
            IsRepeatableToggle = activation.repeatableInTurn || BattleActionSkillUtility.IsRepeatableToggleSkillId(skillId)
        };
        return true;
    }

    public bool TryGetSkillDefinition(string skillId, out SkillDefinitionAsset definition)
    {
        definition = DataMapper.Instance != null ? DataMapper.Instance.FindSkillAsset(skillId) : null;
        return definition != null;
    }

    public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
    {
        SkillDefinitionAsset definition;
        if (TryGetSkillDefinition(skillId, out definition) == false)
        {
            spec = null;
            return false;
        }

        spec = SkillDefinitionSpec.FromAsset(definition);
        return spec != null;
    }
}
