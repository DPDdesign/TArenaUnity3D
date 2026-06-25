using System;
using System.Collections.Generic;

[Serializable]
public enum TacticalAIActionType
{
    Wait,
    Defend,
    Move,
    MoveAndAttack,
    BasicRangedAttack,
    Skill
}

[Serializable]
public class TacticalAIHexCoordinate
{
    public int C;
    public int R;

    public TacticalAIHexCoordinate()
    {
    }

    public TacticalAIHexCoordinate(int c, int r)
    {
        C = c;
        R = r;
    }
}

[Serializable]
public class TacticalAIActionIntent
{
    public TacticalAIActionType ActionType;
    public string ActorUnitId = string.Empty;
    public TacticalAIHexCoordinate SourceHex = new TacticalAIHexCoordinate();
    public TacticalAIHexCoordinate DestinationHex;
    public string TargetUnitId = string.Empty;
    public TacticalAIHexCoordinate TargetHex;
    public int SkillSlot = -1;
    public string SkillId = string.Empty;
    public SkillCast ValidatedSkillCast;
    public SkillResult PreviewResult;
    public int PredictedPriority;
    public string StableOrderKey = string.Empty;
}

[Serializable]
public class TacticalAICandidateGenerationOptions
{
    public int MaxCandidatesPerActionType = 32;
    public int MaxSkillCandidates = 16;
    public int MaxMoveCandidates = 16;
    public int MaxAttackCandidates = 16;

    public static TacticalAICandidateGenerationOptions Default
    {
        get { return new TacticalAICandidateGenerationOptions(); }
    }
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

[Serializable]
public class TacticalAISkillMetadata
{
    public string SkillId = string.Empty;
    public bool IsPassive;
    public bool CanUseAfterMove;
    public bool CanMoveAfterSkill;
    public bool IsRepeatableToggle;
}

public sealed class TacticalAIDataMapperSkillMetadataProvider : ITacticalAISkillMetadataProvider
    , ITacticalAISkillDefinitionProvider
    , ITacticalAISkillSpecProvider
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

    public bool TryGetSkillMetadata(string skillId, out TacticalAISkillMetadata metadata)
    {
        metadata = new TacticalAISkillMetadata
        {
            SkillId = skillId ?? string.Empty,
            IsRepeatableToggle = TacticalAICandidateGenerator.IsRepeatableToggleSkillId(skillId)
        };

        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        if (DataMapper.Instance == null)
        {
            return false;
        }

        SkillDefinitionAsset skillDefinition = DataMapper.Instance.FindSkillAsset(skillId);
        if (skillDefinition == null)
        {
            return false;
        }

        ActivationRuleData activationRule = skillDefinition.ActivationRule;
        metadata.IsPassive = activationRule.activationKind == SkillActivationKind.Passive;
        metadata.CanUseAfterMove = activationRule.canUseAfterMove;
        metadata.CanMoveAfterSkill = activationRule.canMoveAfterUse;
        metadata.IsRepeatableToggle = activationRule.repeatableInTurn;
        return true;
    }

    public bool TryGetSkillDefinition(string skillId, out SkillDefinitionAsset definition)
    {
        definition = null;
        if (string.IsNullOrEmpty(skillId) || DataMapper.Instance == null)
        {
            return false;
        }

        definition = DataMapper.Instance.FindSkillAsset(skillId);
        return definition != null;
    }

    public bool TryGetSkillSpec(string skillId, out SkillDefinitionSpec spec)
    {
        spec = null;
        SkillDefinitionAsset definition;
        if (TryGetSkillDefinition(skillId, out definition) == false)
        {
            return false;
        }

        spec = SkillDefinitionSpec.FromAsset(definition);
        return spec != null;
    }
}
