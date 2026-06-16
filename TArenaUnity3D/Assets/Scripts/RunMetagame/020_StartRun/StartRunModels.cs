using System;
using System.Collections.Generic;

public enum StartRunGameMode
{
    Offline,
    Online
}

public enum StartRunAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum StartRunValidationError
{
    None,
    MissingStartingArmy,
    EmptyArmy,
    InvalidArmy,
    MissingRoute,
    BlockedRunStart
}

[Serializable]
public class StartRunSkillTemplate
{
    public string SkillId;
    public bool Unlocked;

    public StartRunSkillTemplate(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class StartRunStackTemplate
{
    public string UnitId;
    public string Tier;
    public int Level;
    public int Amount;
    public List<StartRunSkillTemplate> Skills;

    public StartRunStackTemplate(string unitId, string tier, int level, int amount, List<StartRunSkillTemplate> skills)
    {
        UnitId = unitId;
        Tier = tier;
        Level = level;
        Amount = amount;
        Skills = skills ?? new List<StartRunSkillTemplate>();
    }
}

[Serializable]
public class StartingArmyTemplate
{
    public string TemplateId;
    public string VariantId;
    public string DisplayName;
    public string Description;
    public int StartingCurrency;
    public List<StartRunStackTemplate> Stacks;

    public StartingArmyTemplate(
        string templateId,
        string variantId,
        string displayName,
        string description,
        int startingCurrency,
        List<StartRunStackTemplate> stacks)
    {
        TemplateId = templateId;
        VariantId = variantId;
        DisplayName = displayName;
        Description = description;
        StartingCurrency = startingCurrency;
        Stacks = stacks ?? new List<StartRunStackTemplate>();
    }
}

[Serializable]
public class RoutePreviewTemplate
{
    public string RouteId;
    public string DisplayName;
    public string Description;
    public int RecommendedArmyValue;

    public RoutePreviewTemplate(string routeId, string displayName, string description, int recommendedArmyValue)
    {
        RouteId = routeId;
        DisplayName = displayName;
        Description = description;
        RecommendedArmyValue = recommendedArmyValue;
    }
}

[Serializable]
public class StartRunUnitDefinition
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Cost;
    public List<string> SkillIds;

    public StartRunUnitDefinition(string unitId, string displayName, string tier, int cost, List<string> skillIds)
    {
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Cost = Math.Max(0, cost);
        SkillIds = skillIds ?? new List<string>();
    }
}

[Serializable]
public class StartRunSkillViewData
{
    public string SkillId;
    public bool Unlocked;

    public StartRunSkillViewData(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class StartRunStackViewData
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Level;
    public int Amount;
    public int CombatValue;
    public List<StartRunSkillViewData> Skills;

    public StartRunStackViewData(
        string unitId,
        string displayName,
        string tier,
        int level,
        int amount,
        int combatValue,
        List<StartRunSkillViewData> skills)
    {
        UnitId = unitId;
        DisplayName = displayName;
        Tier = tier;
        Level = level;
        Amount = amount;
        CombatValue = combatValue;
        Skills = skills ?? new List<StartRunSkillViewData>();
    }
}

[Serializable]
public class StartingArmyOptionViewData
{
    public string TemplateId;
    public string VariantId;
    public string DisplayName;
    public string Description;
    public int StartingCurrency;
    public int TotalArmyValue;
    public bool CanStartRun;
    public StartRunValidationError ValidationError;
    public List<StartRunStackViewData> Stacks;

    public StartingArmyOptionViewData(
        string templateId,
        string variantId,
        string displayName,
        string description,
        int startingCurrency,
        int totalArmyValue,
        bool canStartRun,
        StartRunValidationError validationError,
        List<StartRunStackViewData> stacks)
    {
        TemplateId = templateId;
        VariantId = variantId;
        DisplayName = displayName;
        Description = description;
        StartingCurrency = startingCurrency;
        TotalArmyValue = totalArmyValue;
        CanStartRun = canStartRun;
        ValidationError = validationError;
        Stacks = stacks ?? new List<StartRunStackViewData>();
    }
}

[Serializable]
public class RoutePreviewViewData
{
    public string RouteId;
    public string DisplayName;
    public string Description;
    public int RecommendedArmyValue;
    public int CurrentArmyValue;

    public RoutePreviewViewData(
        string routeId,
        string displayName,
        string description,
        int recommendedArmyValue,
        int currentArmyValue)
    {
        RouteId = routeId;
        DisplayName = displayName;
        Description = description;
        RecommendedArmyValue = recommendedArmyValue;
        CurrentArmyValue = currentArmyValue;
    }
}

[Serializable]
public class StartRunScreenViewData
{
    public List<StartingArmyOptionViewData> StartingArmies;
    public StartingArmyOptionViewData SelectedStartingArmy;
    public List<RoutePreviewViewData> RoutePreviews;
    public RoutePreviewViewData SelectedRoutePreview;
    public bool CanBeginRun;
    public StartRunValidationError ValidationError;
    public string ValidationMessage;

    public StartRunScreenViewData(
        List<StartingArmyOptionViewData> startingArmies,
        StartingArmyOptionViewData selectedStartingArmy,
        List<RoutePreviewViewData> routePreviews,
        RoutePreviewViewData selectedRoutePreview,
        bool canBeginRun,
        StartRunValidationError validationError,
        string validationMessage)
    {
        StartingArmies = startingArmies ?? new List<StartingArmyOptionViewData>();
        SelectedStartingArmy = selectedStartingArmy;
        RoutePreviews = routePreviews ?? new List<RoutePreviewViewData>();
        SelectedRoutePreview = selectedRoutePreview;
        CanBeginRun = canBeginRun;
        ValidationError = validationError;
        ValidationMessage = validationMessage;
    }
}

[Serializable]
public class StartRunCommand
{
    public string AccountPlayerId;
    public string StartingArmyTemplateId;
    public string StartingArmyVariantId;
    public string SelectedStartingArmyId;
    public string RoutePreviewOptionId;

    public StartRunCommand(
        string accountPlayerId,
        string startingArmyTemplateId,
        string startingArmyVariantId,
        string selectedStartingArmyId,
        string routePreviewOptionId)
    {
        AccountPlayerId = accountPlayerId;
        StartingArmyTemplateId = startingArmyTemplateId;
        StartingArmyVariantId = startingArmyVariantId;
        SelectedStartingArmyId = selectedStartingArmyId;
        RoutePreviewOptionId = routePreviewOptionId;
    }
}

[Serializable]
public class RunArmyStackSnapshot
{
    public string UnitId;
    public string Tier;
    public int Level;
    public int Amount;
    public int CombatValue;
    public List<StartRunSkillViewData> Skills;

    public RunArmyStackSnapshot(string unitId, string tier, int level, int amount, int combatValue, List<StartRunSkillViewData> skills)
    {
        UnitId = unitId;
        Tier = tier;
        Level = level;
        Amount = amount;
        CombatValue = combatValue;
        Skills = skills ?? new List<StartRunSkillViewData>();
    }
}

[Serializable]
public class RunArmySnapshot
{
    public string SnapshotId;
    public int TotalArmyValue;
    public List<RunArmyStackSnapshot> Stacks;

    public RunArmySnapshot(string snapshotId, int totalArmyValue, List<RunArmyStackSnapshot> stacks)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = totalArmyValue;
        Stacks = stacks ?? new List<RunArmyStackSnapshot>();
    }
}

[Serializable]
public class CreatedRunRecord
{
    public string RunId;
    public StartRunGameMode GameMode;
    public StartRunAuthoritySource AuthoritySource;
    public string AccountPlayerId;
    public string StartingArmyTemplateId;
    public string StartingArmyVariantId;
    public string SelectedStartingArmyId;
    public string RoutePreviewOptionId;
    public int StartingCurrency;
    public string RunStatus;
    public RunArmySnapshot InitialArmySnapshot;

    public CreatedRunRecord(
        string runId,
        StartRunGameMode gameMode,
        StartRunAuthoritySource authoritySource,
        string accountPlayerId,
        string startingArmyTemplateId,
        string startingArmyVariantId,
        string selectedStartingArmyId,
        string routePreviewOptionId,
        int startingCurrency,
        string runStatus,
        RunArmySnapshot initialArmySnapshot)
    {
        RunId = runId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        AccountPlayerId = accountPlayerId;
        StartingArmyTemplateId = startingArmyTemplateId;
        StartingArmyVariantId = startingArmyVariantId;
        SelectedStartingArmyId = selectedStartingArmyId;
        RoutePreviewOptionId = routePreviewOptionId;
        StartingCurrency = startingCurrency;
        RunStatus = runStatus;
        InitialArmySnapshot = initialArmySnapshot;
    }
}

[Serializable]
public class StartRunResult
{
    public bool Success;
    public StartRunValidationError Error;
    public string Message;
    public CreatedRunRecord CreatedRun;

    public StartRunResult(bool success, StartRunValidationError error, string message, CreatedRunRecord createdRun)
    {
        Success = success;
        Error = error;
        Message = message;
        CreatedRun = createdRun;
    }
}
