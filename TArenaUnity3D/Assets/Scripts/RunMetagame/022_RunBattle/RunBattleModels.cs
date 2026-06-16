using System;
using System.Collections.Generic;

public enum RunBattleGameMode
{
    Offline,
    Online
}

public enum RunBattleAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum RunBattleNodeType
{
    Battle,
    Final
}

public enum RunBattleEnemyGoal
{
    TryToWin,
    DealMaximumLosses
}

public enum RunBattleOutcome
{
    Pending,
    Win,
    Loss,
    Escaped,
    Cancelled
}

public enum RunBattleNextScreen
{
    Battle,
    Reward,
    RunLoss,
    FinalSummary
}

public enum RunBattleError
{
    None,
    MissingRun,
    MissingRouteNode,
    MissingEncounter,
    MissingCurrentArmy,
    MissingPreparedBattle,
    MissingCompletionPayload
}

[Serializable]
public class RunBattleSkillState
{
    public string SkillId;
    public bool Unlocked;

    public RunBattleSkillState(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class RunBattleStackSnapshot
{
    public string StackId;
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Level;
    public int Amount;
    public int Lost;
    public int CombatValue;
    public List<RunBattleSkillState> Skills;

    public RunBattleStackSnapshot(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int level,
        int amount,
        int lost,
        int combatValue,
        List<RunBattleSkillState> skills)
    {
        StackId = stackId;
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Level = Math.Max(1, level);
        Amount = Math.Max(0, amount);
        Lost = Math.Max(0, lost);
        CombatValue = Math.Max(0, combatValue);
        Skills = skills ?? new List<RunBattleSkillState>();
    }
}

[Serializable]
public class RunBattleArmySnapshot
{
    public string SnapshotId;
    public int TotalArmyValue;
    public List<RunBattleStackSnapshot> Stacks;

    public RunBattleArmySnapshot(string snapshotId, int totalArmyValue, List<RunBattleStackSnapshot> stacks)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = Math.Max(0, totalArmyValue);
        Stacks = stacks ?? new List<RunBattleStackSnapshot>();
    }
}

[Serializable]
public class RunBattleEncounterDefinition
{
    public string EncounterId;
    public string RouteNodeId;
    public RunBattleNodeType NodeType;
    public string DisplayName;
    public string ExpectedRisk;
    public int RecommendedArmyValue;
    public string EnemyArmySourceId;
    public RunBattleEnemyGoal EnemyGoal;

    public RunBattleEncounterDefinition(
        string encounterId,
        string routeNodeId,
        RunBattleNodeType nodeType,
        string displayName,
        string expectedRisk,
        int recommendedArmyValue,
        string enemyArmySourceId,
        RunBattleEnemyGoal enemyGoal)
    {
        EncounterId = encounterId;
        RouteNodeId = routeNodeId;
        NodeType = nodeType;
        DisplayName = displayName;
        ExpectedRisk = expectedRisk;
        RecommendedArmyValue = Math.Max(0, recommendedArmyValue);
        EnemyArmySourceId = enemyArmySourceId;
        EnemyGoal = enemyGoal;
    }
}

[Serializable]
public class RunBattlePrepareRequest
{
    public string RunId;
    public string RouteNodeId;
    public string EncounterId;
    public int StageIndex;
    public int RunCurrency;
    public RunBattleArmySnapshot CurrentArmy;

    public RunBattlePrepareRequest(
        string runId,
        string routeNodeId,
        string encounterId,
        int stageIndex,
        int runCurrency,
        RunBattleArmySnapshot currentArmy)
    {
        RunId = runId;
        RouteNodeId = routeNodeId;
        EncounterId = encounterId;
        StageIndex = Math.Max(0, stageIndex);
        RunCurrency = Math.Max(0, runCurrency);
        CurrentArmy = currentArmy;
    }
}

[Serializable]
public class RunBattleLaunchPayload
{
    public string RunBattleId;
    public string RunId;
    public string RouteNodeId;
    public string EncounterId;
    public string CurrentArmySnapshotId;
    public string EnemyArmySourceId;
    public RunBattleEnemyGoal EnemyGoal;
    public string ResultSource;

    public RunBattleLaunchPayload(
        string runBattleId,
        string runId,
        string routeNodeId,
        string encounterId,
        string currentArmySnapshotId,
        string enemyArmySourceId,
        RunBattleEnemyGoal enemyGoal,
        string resultSource)
    {
        RunBattleId = runBattleId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        EncounterId = encounterId;
        CurrentArmySnapshotId = currentArmySnapshotId;
        EnemyArmySourceId = enemyArmySourceId;
        EnemyGoal = enemyGoal;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunBattleLaunchRecord
{
    public string BattleLaunchRecordId;
    public string RunBattleId;
    public string LegacyPlayerArmyAdapterId;
    public string LegacyEnemyArmyAdapterId;
    public string AdapterSurface;
    public string ResultSource;

    public RunBattleLaunchRecord(
        string battleLaunchRecordId,
        string runBattleId,
        string legacyPlayerArmyAdapterId,
        string legacyEnemyArmyAdapterId,
        string adapterSurface,
        string resultSource)
    {
        BattleLaunchRecordId = battleLaunchRecordId;
        RunBattleId = runBattleId;
        LegacyPlayerArmyAdapterId = legacyPlayerArmyAdapterId;
        LegacyEnemyArmyAdapterId = legacyEnemyArmyAdapterId;
        AdapterSurface = adapterSurface;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunBattleLaunchViewData
{
    public string RunBattleId;
    public string RunId;
    public string RouteNodeId;
    public int StageIndex;
    public int RunCurrency;
    public RunBattleGameMode GameMode;
    public RunBattleAuthoritySource AuthoritySource;
    public RunBattleEncounterDefinition Encounter;
    public RunBattleArmySnapshot CurrentArmy;
    public RunBattleLaunchPayload LaunchPayload;
    public RunBattleLaunchRecord LaunchRecord;
    public bool CanLaunch;
    public RunBattleError Error;
    public string Message;

    public RunBattleLaunchViewData(
        string runBattleId,
        string runId,
        string routeNodeId,
        int stageIndex,
        int runCurrency,
        RunBattleGameMode gameMode,
        RunBattleAuthoritySource authoritySource,
        RunBattleEncounterDefinition encounter,
        RunBattleArmySnapshot currentArmy,
        RunBattleLaunchPayload launchPayload,
        RunBattleLaunchRecord launchRecord,
        bool canLaunch,
        RunBattleError error,
        string message)
    {
        RunBattleId = runBattleId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        StageIndex = Math.Max(0, stageIndex);
        RunCurrency = Math.Max(0, runCurrency);
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        Encounter = encounter;
        CurrentArmy = currentArmy;
        LaunchPayload = launchPayload;
        LaunchRecord = launchRecord;
        CanLaunch = canLaunch;
        Error = error;
        Message = message;
    }
}

[Serializable]
public class RunBattleCompletionPayload
{
    public string RunBattleId;
    public RunBattleOutcome Outcome;
    public RunBattleArmySnapshot PlayerArmyAfterBattle;
    public int RunGoldGained;
    public string CompletionPayloadId;
    public string ResultSource;

    public RunBattleCompletionPayload(
        string runBattleId,
        RunBattleOutcome outcome,
        RunBattleArmySnapshot playerArmyAfterBattle,
        int runGoldGained,
        string completionPayloadId,
        string resultSource)
    {
        RunBattleId = runBattleId;
        Outcome = outcome;
        PlayerArmyAfterBattle = playerArmyAfterBattle;
        RunGoldGained = Math.Max(0, runGoldGained);
        CompletionPayloadId = completionPayloadId;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunBattleStackLossRecord
{
    public string StackId;
    public string UnitId;
    public int AmountBefore;
    public int AmountAfter;
    public int LostAmount;

    public RunBattleStackLossRecord(string stackId, string unitId, int amountBefore, int amountAfter)
    {
        StackId = stackId;
        UnitId = unitId;
        AmountBefore = Math.Max(0, amountBefore);
        AmountAfter = Math.Max(0, amountAfter);
        LostAmount = Math.Max(0, AmountBefore - AmountAfter);
    }
}

[Serializable]
public class RunBattleCompletionRecord
{
    public string CompletionRecordId;
    public string RunBattleId;
    public string RunId;
    public string RouteNodeId;
    public string EncounterId;
    public RunBattleOutcome Outcome;
    public RunBattleNextScreen NextScreen;
    public RunBattleArmySnapshot ArmyBeforeBattle;
    public RunBattleArmySnapshot ArmyAfterBattle;
    public List<RunBattleStackLossRecord> Losses;
    public int TotalLosses;
    public int RunGoldGained;
    public string CompletionPayloadId;
    public string ResultSource;

    public RunBattleCompletionRecord(
        string completionRecordId,
        string runBattleId,
        string runId,
        string routeNodeId,
        string encounterId,
        RunBattleOutcome outcome,
        RunBattleNextScreen nextScreen,
        RunBattleArmySnapshot armyBeforeBattle,
        RunBattleArmySnapshot armyAfterBattle,
        List<RunBattleStackLossRecord> losses,
        int totalLosses,
        int runGoldGained,
        string completionPayloadId,
        string resultSource)
    {
        CompletionRecordId = completionRecordId;
        RunBattleId = runBattleId;
        RunId = runId;
        RouteNodeId = routeNodeId;
        EncounterId = encounterId;
        Outcome = outcome;
        NextScreen = nextScreen;
        ArmyBeforeBattle = armyBeforeBattle;
        ArmyAfterBattle = armyAfterBattle;
        Losses = losses ?? new List<RunBattleStackLossRecord>();
        TotalLosses = Math.Max(0, totalLosses);
        RunGoldGained = Math.Max(0, runGoldGained);
        CompletionPayloadId = completionPayloadId;
        ResultSource = resultSource;
    }
}

[Serializable]
public class RunBattleCompletionResult
{
    public bool Success;
    public RunBattleError Error;
    public string Message;
    public RunBattleCompletionRecord CompletionRecord;

    public RunBattleCompletionResult(
        bool success,
        RunBattleError error,
        string message,
        RunBattleCompletionRecord completionRecord)
    {
        Success = success;
        Error = error;
        Message = message;
        CompletionRecord = completionRecord;
    }
}
