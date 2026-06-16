using System;
using System.Collections.Generic;

public enum SavedArmiesGameMode
{
    Offline,
    Online
}

public enum SavedArmiesAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum SavedArmySlotState
{
    Locked,
    Empty,
    Taken
}

public enum SavedArmyBattleResultKind
{
    OffenceWin,
    OffenceLoss,
    DefenceWin,
    DefenceLoss
}

public enum SavedArmiesError
{
    None,
    MissingSlot,
    LockedSlot,
    MissingArenaArmy,
    MissingSavedArmy,
    MissingConfirmation,
    InvalidArmy,
    ZeroValueArmy
}

[Serializable]
public class SavedArmiesUnitDefinition
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Cost;

    public SavedArmiesUnitDefinition(string unitId, string displayName, string tier, int cost)
    {
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Cost = Math.Max(0, cost);
    }
}

[Serializable]
public class SavedArmyStackSnapshot
{
    public string UnitId;
    public int Amount;

    public SavedArmyStackSnapshot(string unitId, int amount)
    {
        UnitId = unitId;
        Amount = Math.Max(0, amount);
    }
}

[Serializable]
public class SavedArmy
{
    public string SavedArmyId;
    public string SnapshotId;
    public bool IsValid;
    public List<SavedArmyStackSnapshot> Stacks;

    public SavedArmy(string savedArmyId, string snapshotId, bool isValid, List<SavedArmyStackSnapshot> stacks)
    {
        SavedArmyId = savedArmyId;
        SnapshotId = snapshotId;
        IsValid = isValid;
        Stacks = stacks ?? new List<SavedArmyStackSnapshot>();
    }
}

[Serializable]
public class SavedArmySlotViewData
{
    public string SlotId;
    public int PhysicalIndex;
    public SavedArmySlotState State;
    public string SavedArmyId;
    public string DisplayName;
    public int CurrentArmyValue;
    public int StackCount;
    public bool Selectable;
    public bool Selected;
    public bool IsCurrentDefence;

    public SavedArmySlotViewData(string slotId, int physicalIndex, SavedArmySlotState state, string savedArmyId, int currentArmyValue, bool selected, bool isCurrentDefence)
    {
        SlotId = slotId;
        PhysicalIndex = Math.Max(0, physicalIndex);
        State = state;
        SavedArmyId = savedArmyId;
        DisplayName = savedArmyId;
        CurrentArmyValue = Math.Max(0, currentArmyValue);
        StackCount = 0;
        Selectable = state != SavedArmySlotState.Locked;
        Selected = selected;
        IsCurrentDefence = isCurrentDefence;
    }
}

[Serializable]
public class SavedArmyStackViewData
{
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Amount;
    public int UnitValue;
    public int StackValue;

    public SavedArmyStackViewData(string unitId, string displayName, string tier, int amount, int unitValue, int stackValue)
    {
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Amount = Math.Max(0, amount);
        UnitValue = Math.Max(0, unitValue);
        StackValue = Math.Max(0, stackValue);
    }
}

[Serializable]
public class SavedArmyPreviewViewData
{
    public string SavedArmyId;
    public string SnapshotId;
    public bool IsValid;
    public int CurrentArmyValue;
    public List<SavedArmyStackViewData> Stacks;

    public SavedArmyPreviewViewData(string savedArmyId, string snapshotId, bool isValid, int currentArmyValue, List<SavedArmyStackViewData> stacks)
    {
        SavedArmyId = savedArmyId;
        SnapshotId = snapshotId;
        IsValid = isValid;
        CurrentArmyValue = Math.Max(0, currentArmyValue);
        Stacks = stacks ?? new List<SavedArmyStackViewData>();
    }
}

[Serializable]
public class ArenaArmyImportCandidate
{
    public string ArenaArmyId;
    public string DisplayName;
    public List<SavedArmyStackSnapshot> Stacks;

    public ArenaArmyImportCandidate(string arenaArmyId, string displayName, List<SavedArmyStackSnapshot> stacks)
    {
        ArenaArmyId = arenaArmyId;
        DisplayName = string.IsNullOrEmpty(displayName) ? arenaArmyId : displayName;
        Stacks = stacks ?? new List<SavedArmyStackSnapshot>();
    }
}

[Serializable]
public class SavedArmySeedCandidate
{
    public string SeedArmyId;
    public string DisplayName;
    public OfflineArmySnapshotRecord Snapshot;

    public SavedArmySeedCandidate(string seedArmyId, string displayName, OfflineArmySnapshotRecord snapshot)
    {
        SeedArmyId = seedArmyId;
        DisplayName = string.IsNullOrEmpty(displayName) ? seedArmyId : displayName;
        Snapshot = snapshot == null ? OfflineArmySnapshotFactory.Create(0, 0, 0, 0, new List<OfflineArmySnapshotStackRecord>()) : OfflineArmySnapshotFactory.Clone(snapshot);
    }
}

[Serializable]
public class ArenaArmyOptionViewData
{
    public string ArenaArmyId;
    public string DisplayName;
    public int CurrentArmyValue;
    public bool Selected;

    public ArenaArmyOptionViewData(string arenaArmyId, string displayName, int currentArmyValue, bool selected)
    {
        ArenaArmyId = arenaArmyId;
        DisplayName = string.IsNullOrEmpty(displayName) ? arenaArmyId : displayName;
        CurrentArmyValue = Math.Max(0, currentArmyValue);
        Selected = selected;
    }
}

[Serializable]
public class SavedArmyAttackHistoryEntry
{
    public string EntryId;
    public string SavedArmyId;
    public SavedArmyBattleResultKind ResultKind;
    public string OpponentName;
    public int AttackerValueAtBattle;
    public int DefenderValueAtBattle;

    public SavedArmyAttackHistoryEntry(string entryId, string savedArmyId, SavedArmyBattleResultKind resultKind, string opponentName, int attackerValueAtBattle, int defenderValueAtBattle)
    {
        EntryId = entryId;
        SavedArmyId = savedArmyId;
        ResultKind = resultKind;
        OpponentName = opponentName;
        AttackerValueAtBattle = Math.Max(0, attackerValueAtBattle);
        DefenderValueAtBattle = Math.Max(0, defenderValueAtBattle);
    }
}

[Serializable]
public class SavedArmiesRosterViewData
{
    public SavedArmiesGameMode GameMode;
    public SavedArmiesAuthoritySource AuthoritySource;
    public List<SavedArmySlotViewData> Slots;
    public SavedArmySlotViewData SelectedSlot;
    public SavedArmyPreviewViewData SelectedArmy;
    public List<ArenaArmyOptionViewData> ArenaArmies;
    public ArenaArmyOptionViewData SelectedArenaArmy;
    public List<SavedArmyAttackHistoryEntry> AttackHistory;
    public string CurrentDefenceSavedArmyId;
    public bool CanSetDefence;
    public bool ImportRequiresOverwriteConfirmation;
    public string Message;

    public SavedArmiesRosterViewData(
        SavedArmiesGameMode gameMode,
        SavedArmiesAuthoritySource authoritySource,
        List<SavedArmySlotViewData> slots,
        SavedArmySlotViewData selectedSlot,
        SavedArmyPreviewViewData selectedArmy,
        List<ArenaArmyOptionViewData> arenaArmies,
        ArenaArmyOptionViewData selectedArenaArmy,
        List<SavedArmyAttackHistoryEntry> attackHistory,
        string currentDefenceSavedArmyId,
        bool canSetDefence,
        bool importRequiresOverwriteConfirmation,
        string message)
    {
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        Slots = slots ?? new List<SavedArmySlotViewData>();
        SelectedSlot = selectedSlot;
        SelectedArmy = selectedArmy;
        ArenaArmies = arenaArmies ?? new List<ArenaArmyOptionViewData>();
        SelectedArenaArmy = selectedArenaArmy;
        AttackHistory = attackHistory ?? new List<SavedArmyAttackHistoryEntry>();
        CurrentDefenceSavedArmyId = currentDefenceSavedArmyId;
        CanSetDefence = canSetDefence;
        ImportRequiresOverwriteConfirmation = importRequiresOverwriteConfirmation;
        Message = message;
    }
}

[Serializable]
public class SavedArmiesRosterRequest
{
    public int UnlockedSlotCount;
    public string SelectedSlotId;
    public string SelectedArenaArmyId;

    public SavedArmiesRosterRequest(int unlockedSlotCount, string selectedSlotId, string selectedArenaArmyId)
    {
        UnlockedSlotCount = Math.Max(0, unlockedSlotCount);
        SelectedSlotId = selectedSlotId;
        SelectedArenaArmyId = selectedArenaArmyId;
    }
}

[Serializable]
public class SavedArmyImportCommand
{
    public string SlotId;
    public string ArenaArmyId;
    public int UnlockedSlotCount;
    public bool ConfirmOverwrite;

    public SavedArmyImportCommand(string slotId, string arenaArmyId, int unlockedSlotCount, bool confirmOverwrite)
    {
        SlotId = slotId;
        ArenaArmyId = arenaArmyId;
        UnlockedSlotCount = Math.Max(0, unlockedSlotCount);
        ConfirmOverwrite = confirmOverwrite;
    }
}

[Serializable]
public class SavedArmySaveRunCommand
{
    public string SlotId;
    public List<SavedArmyStackSnapshot> Stacks;
    public int UnlockedSlotCount;
    public bool ConfirmOverwrite;

    public SavedArmySaveRunCommand(string slotId, List<SavedArmyStackSnapshot> stacks, int unlockedSlotCount, bool confirmOverwrite)
    {
        SlotId = slotId;
        Stacks = stacks ?? new List<SavedArmyStackSnapshot>();
        UnlockedSlotCount = Math.Max(0, unlockedSlotCount);
        ConfirmOverwrite = confirmOverwrite;
    }
}

[Serializable]
public class SavedArmyCommandResult
{
    public bool Success;
    public SavedArmiesError Error;
    public string Message;
    public string SavedArmyId;

    public SavedArmyCommandResult(bool success, SavedArmiesError error, string message, string savedArmyId)
    {
        Success = success;
        Error = error;
        Message = message;
        SavedArmyId = savedArmyId;
    }
}
