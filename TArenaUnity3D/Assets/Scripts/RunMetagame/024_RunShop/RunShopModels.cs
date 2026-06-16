using System;
using System.Collections.Generic;

public enum RunShopGameMode
{
    Offline,
    Online
}

public enum RunShopAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum RunShopOfferCategory
{
    Recovery,
    Resurrection,
    Skill,
    Stack,
    UpgradeExchange,
    Economy
}

public enum RunShopOperationType
{
    RecoverLosses,
    TeachSkill,
    AddStack,
    UpgradeStack,
    GainCurrency
}

public enum RunShopPurchaseError
{
    None,
    MissingVisit,
    MissingOffer,
    AlreadyPurchased,
    InsufficientCurrency,
    InvalidTarget,
    UnavailableOffer
}

[Serializable]
public class RunShopSkillState
{
    public string SkillId;
    public bool Unlocked;

    public RunShopSkillState(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class RunShopStackSnapshot
{
    public string StackId;
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Level;
    public int Amount;
    public int Lost;
    public int CombatValue;
    public List<RunShopSkillState> Skills;

    public RunShopStackSnapshot(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int level,
        int amount,
        int lost,
        int combatValue,
        List<RunShopSkillState> skills)
    {
        StackId = stackId;
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Level = Math.Max(1, level);
        Amount = Math.Max(0, amount);
        Lost = Math.Max(0, lost);
        CombatValue = Math.Max(0, combatValue);
        Skills = skills ?? new List<RunShopSkillState>();
    }
}

[Serializable]
public class RunShopArmySnapshot
{
    public string SnapshotId;
    public int TotalArmyValue;
    public List<RunShopStackSnapshot> Stacks;

    public RunShopArmySnapshot(string snapshotId, int totalArmyValue, List<RunShopStackSnapshot> stacks)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = Math.Max(0, totalArmyValue);
        Stacks = stacks ?? new List<RunShopStackSnapshot>();
    }
}

[Serializable]
public class RunShopUnitDefinition
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Cost;
    public List<string> SkillIds;

    public RunShopUnitDefinition(string unitId, string displayName, string tier, int cost, List<string> skillIds)
    {
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Cost = Math.Max(0, cost);
        SkillIds = skillIds ?? new List<string>();
    }
}

[Serializable]
public class RunShopOperation
{
    public RunShopOperationType Type;
    public string StackId;
    public string UnitId;
    public string ToUnitId;
    public string SkillId;
    public string NewStackId;
    public int Amount;
    public int CurrencyDelta;

    public RunShopOperation(
        RunShopOperationType type,
        string stackId,
        string unitId,
        string toUnitId,
        string skillId,
        string newStackId,
        int amount,
        int currencyDelta)
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
public class RunShopOfferViewData
{
    public string OfferId;
    public RunShopOfferCategory Category;
    public string Title;
    public string Detail;
    public int Cost;
    public bool Available;
    public bool Purchased;
    public bool Affordable;
    public string BeforeText;
    public string AfterText;
    public string AffectedStackId;
    public RunShopOperation Operation;

    public RunShopOfferViewData(
        string offerId,
        RunShopOfferCategory category,
        string title,
        string detail,
        int cost,
        bool available,
        bool purchased,
        bool affordable,
        string beforeText,
        string afterText,
        string affectedStackId,
        RunShopOperation operation)
    {
        OfferId = offerId;
        Category = category;
        Title = title;
        Detail = detail;
        Cost = Math.Max(0, cost);
        Available = available;
        Purchased = purchased;
        Affordable = affordable;
        BeforeText = beforeText;
        AfterText = afterText;
        AffectedStackId = affectedStackId;
        Operation = operation;
    }
}

[Serializable]
public class RunShopPreviewData
{
    public string OfferId;
    public RunShopArmySnapshot ArmyAfterPurchase;
    public RunShopStackSnapshot AffectedStackPreview;
    public int CurrencyAfterPurchase;
    public RunShopPurchaseError Error;
    public string Message;
    public string ResultSource;

    public RunShopPreviewData(
        string offerId,
        RunShopArmySnapshot armyAfterPurchase,
        RunShopStackSnapshot affectedStackPreview,
        int currencyAfterPurchase,
        RunShopPurchaseError error,
        string message,
        string resultSource)
    {
        OfferId = offerId;
        ArmyAfterPurchase = armyAfterPurchase;
        AffectedStackPreview = affectedStackPreview;
        CurrencyAfterPurchase = currencyAfterPurchase;
        Error = error;
        Message = message;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunShopVisitRequest
{
    public string VisitId;
    public string RunId;
    public string RouteNodeId;
    public int RunCurrency;
    public RunShopArmySnapshot CurrentArmy;

    public RunShopVisitRequest(string runId, string routeNodeId, int runCurrency, RunShopArmySnapshot currentArmy)
        : this(string.Empty, runId, routeNodeId, runCurrency, currentArmy)
    {
    }

    public RunShopVisitRequest(string visitId, string runId, string routeNodeId, int runCurrency, RunShopArmySnapshot currentArmy)
    {
        VisitId = visitId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        RunCurrency = Math.Max(0, runCurrency);
        CurrentArmy = currentArmy;
    }
}

[Serializable]
public class RunShopVisitViewData
{
    public string VisitId;
    public string RunId;
    public string RouteNodeId;
    public RunShopGameMode GameMode;
    public RunShopAuthoritySource AuthoritySource;
    public int RunCurrency;
    public RunShopArmySnapshot CurrentArmy;
    public List<RunShopOfferViewData> Offers;
    public RunShopOfferViewData FocusedOffer;
    public RunShopPreviewData FocusedPreview;
    public bool CanBuyFocusedOffer;
    public string Message;

    public RunShopVisitViewData(
        string visitId,
        string runId,
        string routeNodeId,
        RunShopGameMode gameMode,
        RunShopAuthoritySource authoritySource,
        int runCurrency,
        RunShopArmySnapshot currentArmy,
        List<RunShopOfferViewData> offers,
        RunShopOfferViewData focusedOffer,
        RunShopPreviewData focusedPreview,
        bool canBuyFocusedOffer,
        string message)
    {
        VisitId = visitId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        RunCurrency = Math.Max(0, runCurrency);
        CurrentArmy = currentArmy;
        Offers = offers ?? new List<RunShopOfferViewData>();
        FocusedOffer = focusedOffer;
        FocusedPreview = focusedPreview;
        CanBuyFocusedOffer = canBuyFocusedOffer;
        Message = message;
    }
}

[Serializable]
public class RunShopPurchaseCommand
{
    public string VisitId;
    public string OfferId;
    public int RunCurrency;
    public RunShopArmySnapshot CurrentArmy;

    public RunShopPurchaseCommand(string visitId, string offerId, int runCurrency, RunShopArmySnapshot currentArmy)
    {
        VisitId = visitId;
        OfferId = offerId;
        RunCurrency = Math.Max(0, runCurrency);
        CurrentArmy = currentArmy;
    }
}

[Serializable]
public class RunShopPurchaseRecord
{
    public string PurchaseId;
    public string VisitId;
    public string OfferId;
    public RunShopOfferCategory Category;
    public int Cost;
    public int CurrencyBefore;
    public int CurrencyAfter;
    public string ResultSource;

    public RunShopPurchaseRecord(
        string purchaseId,
        string visitId,
        string offerId,
        RunShopOfferCategory category,
        int cost,
        int currencyBefore,
        int currencyAfter,
        string resultSource)
    {
        PurchaseId = purchaseId;
        VisitId = visitId;
        OfferId = offerId;
        Category = category;
        Cost = Math.Max(0, cost);
        CurrencyBefore = Math.Max(0, currencyBefore);
        CurrencyAfter = Math.Max(0, currencyAfter);
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunShopPurchaseResult
{
    public bool Success;
    public RunShopPurchaseError Error;
    public string Message;
    public RunShopOfferViewData Offer;
    public RunShopArmySnapshot ArmyAfterPurchase;
    public int CurrencyAfterPurchase;
    public RunShopPurchaseRecord PurchaseRecord;

    public RunShopPurchaseResult(
        bool success,
        RunShopPurchaseError error,
        string message,
        RunShopOfferViewData offer,
        RunShopArmySnapshot armyAfterPurchase,
        int currencyAfterPurchase,
        RunShopPurchaseRecord purchaseRecord)
    {
        Success = success;
        Error = error;
        Message = message;
        Offer = offer;
        ArmyAfterPurchase = armyAfterPurchase;
        CurrencyAfterPurchase = Math.Max(0, currencyAfterPurchase);
        PurchaseRecord = purchaseRecord;
    }
}

[Serializable]
public class RunShopLeaveCommand
{
    public string VisitId;
    public string FocusedOfferId;

    public RunShopLeaveCommand(string visitId, string focusedOfferId)
    {
        VisitId = visitId;
        FocusedOfferId = focusedOfferId;
    }
}

[Serializable]
public class RunShopLeaveResult
{
    public bool Success;
    public string VisitId;
    public string RunId;
    public string RouteNodeId;
    public int RunCurrency;
    public RunShopArmySnapshot CurrentArmy;
    public string NextScreen;
    public string Message;

    public RunShopLeaveResult(
        bool success,
        string visitId,
        string runId,
        string routeNodeId,
        int runCurrency,
        RunShopArmySnapshot currentArmy,
        string nextScreen,
        string message)
    {
        Success = success;
        VisitId = visitId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        RunCurrency = Math.Max(0, runCurrency);
        CurrentArmy = currentArmy;
        NextScreen = nextScreen;
        Message = message;
    }
}
