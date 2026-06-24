public enum DBGameModeId
{
    Offline = 1,
    Online = 2
}

public enum DBAuthoritySourceId
{
    LocalOfflineAdapter = 1,
    BackendAdapter = 2
}

public enum DBRunStatusId
{
    Created = 1,
    InProgress = 2,
    AwaitingBattle = 3,
    AwaitingReward = 4,
    InShop = 5,
    AwaitingFinal = 6,
    Won = 7,
    Lost = 8,
    Abandoned = 9
}

public enum DBUnlockTypeId
{
    Map = 1,
    Unit = 2,
    Skill = 3,
    SavedArmySlot = 4
}

public enum DBNodeTypeId
{
    Start = 1,
    Battle = 2,
    Shop = 3,
    RecruitReward = 4,
    FinalBoss = 5,
    RandomEvent = 6,
    Empty = 7
}

public enum DBNodeStateId
{
    Locked = 1,
    Available = 2,
    Selected = 3,
    Completed = 4
}

public enum DBEventTypeId
{
    StartRun = 1,
    RouteTravel = 2,
    Battle = 3,
    Reward = 4,
    Purchase = 5,
    SaveArmy = 6,
    RunComplete = 7
}

public enum DBBattleStatusId
{
    Prepared = 1,
    Launched = 2,
    Completed = 3,
    Cancelled = 4
}

public enum DBBattleOutcomeId
{
    Win = 1,
    Loss = 2,
    Cancelled = 3
}

public enum DBChoiceStatusId
{
    Generated = 1,
    Selected = 2,
    Skipped = 3,
    Expired = 4
}

public enum DBRewardOpportunityStateId
{
    Unresolved = 1,
    Resolved = 2,
    Burned = 3
}

public enum DBVisitStatusId
{
    Open = 1,
    Left = 2,
    Completed = 3
}

public enum DBPurchaseResultId
{
    Purchased = 1,
    InsufficientCurrency = 2,
    InvalidTarget = 3,
    UnavailableOffer = 4
}

public enum DBFinalResultId
{
    Pending = 1,
    Won = 2,
    Lost = 3
}

public enum DBResultKindId
{
    OffenceWin = 1,
    OffenceLoss = 2,
    DefenceWin = 3,
    DefenceLoss = 4
}
