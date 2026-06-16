using System;
using System.Collections.Generic;

public enum BattleResultGameMode
{
    Offline,
    Online
}

public enum BattleResultAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum BattleResultKind
{
    OffenceWin,
    OffenceLoss,
    DefenceWin,
    DefenceLoss
}

public enum BattleResultError
{
    None,
    MissingAttacker,
    MissingDefender,
    MissingResult
}

[Serializable]
public class BattleResultSkillState
{
    public string SkillId;
    public bool Unlocked;

    public BattleResultSkillState(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class BattleResultStackSnapshot
{
    public string StackId;
    public string UnitId;
    public string DisplayName;
    public int Amount;
    public int CombatValue;
    public List<BattleResultSkillState> Skills;

    public BattleResultStackSnapshot(string stackId, string unitId, string displayName, int amount, int combatValue, List<BattleResultSkillState> skills)
    {
        StackId = stackId;
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Amount = Math.Max(0, amount);
        CombatValue = Math.Max(0, combatValue);
        Skills = skills ?? new List<BattleResultSkillState>();
    }
}

[Serializable]
public class BattleResultSavedArmySnapshot
{
    public string SavedArmyId;
    public string SnapshotId;
    public string DisplayName;
    public int ArmyValue;
    public List<BattleResultStackSnapshot> Stacks;

    public BattleResultSavedArmySnapshot(string savedArmyId, string snapshotId, string displayName, int armyValue, List<BattleResultStackSnapshot> stacks)
    {
        SavedArmyId = savedArmyId;
        SnapshotId = snapshotId;
        DisplayName = displayName;
        ArmyValue = Math.Max(0, armyValue);
        Stacks = stacks ?? new List<BattleResultStackSnapshot>();
    }
}

[Serializable]
public class BattleResultOpponentMetadata
{
    public string OpponentId;
    public string DisplayName;
    public int RankBefore;
    public int ArmyValue;
    public bool SimulatedOfflineOpponent;

    public BattleResultOpponentMetadata(string opponentId, string displayName, int rankBefore, int armyValue, bool simulatedOfflineOpponent)
    {
        OpponentId = opponentId;
        DisplayName = displayName;
        RankBefore = Math.Max(0, rankBefore);
        ArmyValue = Math.Max(0, armyValue);
        SimulatedOfflineOpponent = simulatedOfflineOpponent;
    }
}

[Serializable]
public class BattleResultRecordRequest
{
    public string AsyncBattleResultId;
    public BattleResultKind ResultKind;
    public BattleResultSavedArmySnapshot AttackerArmy;
    public BattleResultSavedArmySnapshot DefenderArmy;
    public BattleResultOpponentMetadata Opponent;
    public int PlayerRankBefore;
    public int AccountXpBefore;

    public BattleResultRecordRequest(string asyncBattleResultId, BattleResultKind resultKind, BattleResultSavedArmySnapshot attackerArmy, BattleResultSavedArmySnapshot defenderArmy, BattleResultOpponentMetadata opponent, int playerRankBefore, int accountXpBefore)
    {
        AsyncBattleResultId = asyncBattleResultId;
        ResultKind = resultKind;
        AttackerArmy = attackerArmy;
        DefenderArmy = defenderArmy;
        Opponent = opponent;
        PlayerRankBefore = Math.Max(0, playerRankBefore);
        AccountXpBefore = Math.Max(0, accountXpBefore);
    }
}

[Serializable]
public class BattleResultPreservationRecord
{
    public string AttackerSavedArmyId;
    public string DefenderSavedArmyId;
    public bool AttackerPreserved;
    public bool DefenderPreserved;
    public string Message;

    public BattleResultPreservationRecord(string attackerSavedArmyId, string defenderSavedArmyId, bool attackerPreserved, bool defenderPreserved, string message)
    {
        AttackerSavedArmyId = attackerSavedArmyId;
        DefenderSavedArmyId = defenderSavedArmyId;
        AttackerPreserved = attackerPreserved;
        DefenderPreserved = defenderPreserved;
        Message = message;
    }
}

[Serializable]
public class BattleResultAccountProgress
{
    public int Level;
    public int XpIntoLevel;
    public int XpForNextLevel;
    public float Progress01;
    public int NextUnlockAtTotalXp;
    public string NextUnlockPreview;

    public BattleResultAccountProgress(int level, int xpIntoLevel, int xpForNextLevel, float progress01, int nextUnlockAtTotalXp, string nextUnlockPreview)
    {
        Level = Math.Max(1, level);
        XpIntoLevel = Math.Max(0, xpIntoLevel);
        XpForNextLevel = Math.Max(1, xpForNextLevel);
        Progress01 = Math.Max(0f, Math.Min(1f, progress01));
        NextUnlockAtTotalXp = Math.Max(0, nextUnlockAtTotalXp);
        NextUnlockPreview = nextUnlockPreview;
    }

    public static BattleResultAccountProgress FromTotalXp(int totalXp, string nextUnlockPreview)
    {
        int clampedTotalXp = Math.Max(0, totalXp);
        const int xpPerLevel = 250;
        int level = (clampedTotalXp / xpPerLevel) + 1;
        int levelStartXp = (level - 1) * xpPerLevel;
        int xpIntoLevel = clampedTotalXp - levelStartXp;
        int nextLevelTotalXp = level * xpPerLevel;
        float progress = (float)xpIntoLevel / xpPerLevel;

        return new BattleResultAccountProgress(
            level,
            xpIntoLevel,
            xpPerLevel,
            progress,
            nextLevelTotalXp,
            nextUnlockPreview);
    }
}

[Serializable]
public class BattleResultViewData
{
    public string AsyncBattleResultId;
    public BattleResultGameMode GameMode;
    public BattleResultAuthoritySource AuthoritySource;
    public BattleResultKind ResultKind;
    public BattleResultSavedArmySnapshot AttackerArmy;
    public BattleResultSavedArmySnapshot DefenderArmy;
    public BattleResultOpponentMetadata Opponent;
    public int RankBefore;
    public int RankAfter;
    public int RankDelta;
    public int AccountXpBefore;
    public int AccountXpGained;
    public int AccountXpAfter;
    public string NextUnlockPreview;
    public BattleResultAccountProgress AccountProgress;
    public BattleResultPreservationRecord PreservationRecord;
    public bool Success;
    public BattleResultError Error;
    public string Message;

    public BattleResultViewData(string asyncBattleResultId, BattleResultGameMode gameMode, BattleResultAuthoritySource authoritySource, BattleResultKind resultKind, BattleResultSavedArmySnapshot attackerArmy, BattleResultSavedArmySnapshot defenderArmy, BattleResultOpponentMetadata opponent, int rankBefore, int rankAfter, int rankDelta, int accountXpBefore, int accountXpGained, int accountXpAfter, string nextUnlockPreview, BattleResultPreservationRecord preservationRecord, bool success, BattleResultError error, string message, BattleResultAccountProgress accountProgress = null)
    {
        AsyncBattleResultId = asyncBattleResultId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        ResultKind = resultKind;
        AttackerArmy = attackerArmy;
        DefenderArmy = defenderArmy;
        Opponent = opponent;
        RankBefore = Math.Max(0, rankBefore);
        RankAfter = Math.Max(0, rankAfter);
        RankDelta = rankDelta;
        AccountXpBefore = Math.Max(0, accountXpBefore);
        AccountXpGained = Math.Max(0, accountXpGained);
        AccountXpAfter = Math.Max(0, accountXpAfter);
        NextUnlockPreview = nextUnlockPreview;
        AccountProgress = accountProgress ?? BattleResultAccountProgress.FromTotalXp(AccountXpAfter, NextUnlockPreview);
        PreservationRecord = preservationRecord;
        Success = success;
        Error = error;
        Message = message;
    }
}
