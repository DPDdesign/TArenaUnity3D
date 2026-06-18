using System;
using System.Collections.Generic;

public enum RewardMapGameMode
{
    Offline,
    Online
}

public enum RewardMapAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum RewardMapFamily
{
    Mass,
    Quality,
    Width,
    Skill,
    Recovery,
    Economy
}

public enum RewardMapIntention
{
    Stabilize,
    Strengthen,
    Pivot
}

public enum RewardMapRarity
{
    Common,
    Uncommon,
    Rare
}

public enum RewardMapOperationType
{
    AddUnits,
    PromoteStack,
    AddStack,
    TeachSkill,
    RecoverLosses,
    GainCurrency,
    DowngradeStack
}

public enum RewardMapError
{
    None,
    MissingChoice,
    MissingReward,
    MissingArmy,
    InvalidTarget,
    NoLegalTarget,
    AlreadyApplied
}

[Serializable]
public class RewardMapSkillState
{
    public string SkillId;
    public bool Unlocked;

    public RewardMapSkillState(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class RewardMapStackSnapshot
{
    public string StackId;
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Level;
    public int Amount;
    public int Lost;
    public int CombatValue;
    public List<RewardMapSkillState> Skills;

    public RewardMapStackSnapshot(string stackId, string unitId, string displayName, string tier, int level, int amount, int lost, int combatValue, List<RewardMapSkillState> skills)
    {
        StackId = stackId;
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Level = Math.Max(1, level);
        Amount = Math.Max(0, amount);
        Lost = Math.Max(0, lost);
        CombatValue = Math.Max(0, combatValue);
        Skills = skills ?? new List<RewardMapSkillState>();
    }
}

[Serializable]
public class RewardMapArmySnapshot
{
    public string SnapshotId;
    public int TotalArmyValue;
    public List<RewardMapStackSnapshot> Stacks;

    public RewardMapArmySnapshot(string snapshotId, int totalArmyValue, List<RewardMapStackSnapshot> stacks)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = Math.Max(0, totalArmyValue);
        Stacks = stacks ?? new List<RewardMapStackSnapshot>();
    }
}

[Serializable]
public class RewardMapBattleResultSummary
{
    public string BattleResultId;
    public string ResultLabel;
    public int Losses;
    public int RunGoldGained;

    public RewardMapBattleResultSummary(string battleResultId, string resultLabel, int losses, int runGoldGained)
    {
        BattleResultId = battleResultId;
        ResultLabel = resultLabel;
        Losses = Math.Max(0, losses);
        RunGoldGained = Math.Max(0, runGoldGained);
    }
}

[Serializable]
public class RewardMapOperation
{
    public RewardMapOperationType Type;
    public string StackId;
    public string UnitId;
    public string ToUnitId;
    public string SkillId;
    public string NewStackId;
    public int Amount;
    public int CurrencyDelta;

    public RewardMapOperation(RewardMapOperationType type, string stackId, string unitId, string toUnitId, string skillId, string newStackId, int amount, int currencyDelta)
    {
        Type = type;
        StackId = stackId;
        UnitId = unitId;
        ToUnitId = toUnitId;
        SkillId = skillId;
        NewStackId = newStackId;
        Amount = Math.Max(0, amount);
        CurrencyDelta = currencyDelta;
    }
}

[Serializable]
public class RewardMapTemplate
{
    public string TemplateId;
    public RewardMapFamily Family;
    public RewardMapIntention Intention;
    public RewardMapRarity Rarity;
    public string Verb;
    public string Title;
    public string Detail;
    public RewardMapOperation Operation;

    public RewardMapTemplate(string templateId, RewardMapFamily family, RewardMapIntention intention, RewardMapRarity rarity, string verb, string title, string detail, RewardMapOperation operation)
    {
        TemplateId = templateId;
        Family = family;
        Intention = intention;
        Rarity = rarity;
        Verb = verb;
        Title = title;
        Detail = detail;
        Operation = operation;
    }
}

[Serializable]
public class RewardMapCardViewData
{
    public string RewardId;
    public string TemplateId;
    public RewardMapFamily Family;
    public RewardMapIntention Intention;
    public RewardMapRarity Rarity;
    public string Verb;
    public string Title;
    public string Detail;
    public string BeforeText;
    public string AfterText;
    public string AffectedStackId;
    public bool Legal;
    public RewardMapError Error;
    public RewardMapOperation Operation;

    public RewardMapCardViewData(string rewardId, string templateId, RewardMapFamily family, RewardMapIntention intention, RewardMapRarity rarity, string verb, string title, string detail, string beforeText, string afterText, string affectedStackId, bool legal, RewardMapError error, RewardMapOperation operation)
    {
        RewardId = rewardId;
        TemplateId = templateId;
        Family = family;
        Intention = intention;
        Rarity = rarity;
        Verb = verb;
        Title = title;
        Detail = detail;
        BeforeText = beforeText;
        AfterText = afterText;
        AffectedStackId = affectedStackId;
        Legal = legal;
        Error = error;
        Operation = operation;
    }
}

[Serializable]
public class RewardMapPreviewData
{
    public string RewardId;
    public RewardMapArmySnapshot ArmyAfterReward;
    public int RunGoldAfterReward;
    public RewardMapStackSnapshot AffectedStackPreview;
    public RewardMapError Error;
    public string Message;
    public string ResultSource;

    public RewardMapPreviewData(string rewardId, RewardMapArmySnapshot armyAfterReward, int runGoldAfterReward, RewardMapStackSnapshot affectedStackPreview, RewardMapError error, string message, string resultSource)
    {
        RewardId = rewardId;
        ArmyAfterReward = armyAfterReward;
        RunGoldAfterReward = Math.Max(0, runGoldAfterReward);
        AffectedStackPreview = affectedStackPreview;
        Error = error;
        Message = message;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RewardMapChoiceViewData
{
    public string ChoiceId;
    public string RunId;
    public RewardMapGameMode GameMode;
    public RewardMapAuthoritySource AuthoritySource;
    public RewardMapBattleResultSummary BattleResultSummary;
    public string GainedSummary;
    public int RunGoldBeforeReward;
    public RewardMapArmySnapshot ArmyBeforeReward;
    public List<RewardMapCardViewData> Cards;
    public RewardMapCardViewData FocusedCard;
    public RewardMapPreviewData FocusedPreview;
    public string Message;
    public string SelectedRewardId;

    public RewardMapChoiceViewData(string choiceId, string runId, RewardMapGameMode gameMode, RewardMapAuthoritySource authoritySource, RewardMapBattleResultSummary battleResultSummary, string gainedSummary, int runGoldBeforeReward, RewardMapArmySnapshot armyBeforeReward, List<RewardMapCardViewData> cards, RewardMapCardViewData focusedCard, RewardMapPreviewData focusedPreview, string message)
    {
        ChoiceId = choiceId;
        RunId = runId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        BattleResultSummary = battleResultSummary;
        GainedSummary = gainedSummary;
        RunGoldBeforeReward = Math.Max(0, runGoldBeforeReward);
        ArmyBeforeReward = armyBeforeReward;
        Cards = cards ?? new List<RewardMapCardViewData>();
        FocusedCard = focusedCard;
        FocusedPreview = focusedPreview;
        Message = message;
        SelectedRewardId = string.Empty;
    }
}

[Serializable]
public class RewardMapChoiceRequest
{
    public string RunId;
    public int StageIndex;
    public int RunGoldBeforeReward;
    public RewardMapArmySnapshot ArmyAfterBattle;
    public RewardMapBattleResultSummary BattleResultSummary;

    public RewardMapChoiceRequest(string runId, int stageIndex, int runGoldBeforeReward, RewardMapArmySnapshot armyAfterBattle, RewardMapBattleResultSummary battleResultSummary)
    {
        RunId = runId;
        StageIndex = Math.Max(0, stageIndex);
        RunGoldBeforeReward = Math.Max(0, runGoldBeforeReward);
        ArmyAfterBattle = armyAfterBattle;
        BattleResultSummary = battleResultSummary;
    }
}

[Serializable]
public class RewardMapApplyCommand
{
    public string ChoiceId;
    public string RewardId;
    public int RunGoldBeforeReward;
    public RewardMapArmySnapshot ArmyBeforeReward;

    public RewardMapApplyCommand(string choiceId, string rewardId, int runGoldBeforeReward, RewardMapArmySnapshot armyBeforeReward)
    {
        ChoiceId = choiceId;
        RewardId = rewardId;
        RunGoldBeforeReward = Math.Max(0, runGoldBeforeReward);
        ArmyBeforeReward = armyBeforeReward;
    }
}

[Serializable]
public class RewardMapApplyResult
{
    public bool Success;
    public RewardMapError Error;
    public string Message;
    public RewardMapCardViewData Reward;
    public RewardMapArmySnapshot ArmyAfterReward;
    public int RunGoldAfterReward;
    public string ResultSource;

    public RewardMapApplyResult(bool success, RewardMapError error, string message, RewardMapCardViewData reward, RewardMapArmySnapshot armyAfterReward, int runGoldAfterReward, string resultSource)
    {
        Success = success;
        Error = error;
        Message = message;
        Reward = reward;
        ArmyAfterReward = armyAfterReward;
        RunGoldAfterReward = Math.Max(0, runGoldAfterReward);
        ResultSource = resultSource;
    }
}
