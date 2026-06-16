using System;
using System.Collections.Generic;

[Serializable]
public class OfflineArmySnapshotRecord
{
    public int SnapshotId;
    public int AccountId;
    public int RunId;
    public int SavedArmyId;
    public int NodeId;
    public string CreatedAtUtc;
    public bool IsActive;
    public List<OfflineArmySnapshotStackRecord> Stacks;

    public OfflineArmySnapshotRecord(
        int snapshotId,
        int accountId,
        int runId,
        int savedArmyId,
        int nodeId,
        string createdAtUtc,
        bool isActive,
        List<OfflineArmySnapshotStackRecord> stacks)
    {
        SnapshotId = Math.Max(0, snapshotId);
        AccountId = Math.Max(0, accountId);
        RunId = Math.Max(0, runId);
        SavedArmyId = Math.Max(0, savedArmyId);
        NodeId = Math.Max(0, nodeId);
        CreatedAtUtc = createdAtUtc;
        IsActive = isActive;
        Stacks = stacks ?? new List<OfflineArmySnapshotStackRecord>();
    }
}

[Serializable]
public class OfflineArmySnapshotStackRecord
{
    public int SnapshotStackId;
    public string UnitId;
    public int Amount;
    public int FormationSlot;
    public bool IsActive;
    public List<OfflineArmySnapshotStackSkillRecord> Skills;

    public OfflineArmySnapshotStackRecord(
        int snapshotStackId,
        string unitId,
        int amount,
        int formationSlot,
        bool isActive,
        List<OfflineArmySnapshotStackSkillRecord> skills)
    {
        SnapshotStackId = Math.Max(0, snapshotStackId);
        UnitId = unitId;
        Amount = Math.Max(0, amount);
        FormationSlot = Math.Max(0, formationSlot);
        IsActive = isActive;
        Skills = skills ?? new List<OfflineArmySnapshotStackSkillRecord>();
    }
}

[Serializable]
public class OfflineArmySnapshotStackSkillRecord
{
    public int SnapshotStackSkillId;
    public string SkillId;
    public int AcquiredAtRunNodeId;
    public bool IsActive;

    public OfflineArmySnapshotStackSkillRecord(int snapshotStackSkillId, string skillId, int acquiredAtRunNodeId, bool isActive)
    {
        SnapshotStackSkillId = Math.Max(0, snapshotStackSkillId);
        SkillId = skillId;
        AcquiredAtRunNodeId = Math.Max(0, acquiredAtRunNodeId);
        IsActive = isActive;
    }
}

[Serializable]
public class OfflineArmySnapshotStackLoss
{
    public int FormationSlot;
    public string UnitId;
    public int AmountBefore;
    public int AmountAfter;
    public int LostAmount;

    public OfflineArmySnapshotStackLoss(int formationSlot, string unitId, int amountBefore, int amountAfter)
    {
        FormationSlot = Math.Max(0, formationSlot);
        UnitId = unitId;
        AmountBefore = Math.Max(0, amountBefore);
        AmountAfter = Math.Max(0, amountAfter);
        LostAmount = Math.Max(0, AmountBefore - AmountAfter);
    }
}

[Serializable]
public class OfflineArmySnapshotLossDiff
{
    public List<OfflineArmySnapshotStackLoss> Losses;
    public int TotalLostAmount;

    public OfflineArmySnapshotLossDiff(List<OfflineArmySnapshotStackLoss> losses)
    {
        Losses = losses ?? new List<OfflineArmySnapshotStackLoss>();

        int total = 0;
        for (int i = 0; i < Losses.Count; i++)
        {
            total += Losses[i].LostAmount;
        }

        TotalLostAmount = Math.Max(0, total);
    }
}

[Serializable]
public class OfflineArmySnapshotUnitCatalogEntry
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int CombatValue;
    public List<string> SkillIds;

    public OfflineArmySnapshotUnitCatalogEntry(
        string unitId,
        string displayName,
        string tier,
        int combatValue,
        List<string> skillIds)
    {
        UnitId = unitId;
        DisplayName = displayName;
        Tier = tier;
        CombatValue = Math.Max(0, combatValue);
        SkillIds = skillIds ?? new List<string>();
    }
}

public interface IOfflineArmySnapshotCatalogResolver
{
    OfflineArmySnapshotUnitCatalogEntry FindUnit(string unitId);
}
