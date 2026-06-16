using System;
using System.Collections.Generic;

public enum SummaryValueGameMode
{
    Offline,
    Online
}

public enum SummaryValueAuthoritySource
{
    LocalOfflineAdapter,
    BackendAdapter
}

public enum SummaryValueFinalResult
{
    Pending,
    Won,
    Lost
}

public enum SummaryValueSlotState
{
    Locked,
    Empty,
    Taken
}

public enum SummaryValueSaveActionMode
{
    None,
    Save,
    Overwrite
}

public enum SummaryValueError
{
    None,
    MissingRun,
    MissingSnapshot,
    FinalNotWon,
    LockedSlot,
    MissingConfirmation,
    MissingSlot
}

[Serializable]
public class SummaryValueSkillState
{
    public string SkillId;
    public bool Unlocked;

    public SummaryValueSkillState(string skillId, bool unlocked)
    {
        SkillId = skillId;
        Unlocked = unlocked;
    }
}

[Serializable]
public class SummaryValueStackSnapshot
{
    public string StackId;
    public string UnitId;
    public string DisplayName;
    public string Tier;
    public int Level;
    public int Amount;
    public int CombatValue;
    public List<SummaryValueSkillState> Skills;

    public SummaryValueStackSnapshot(string stackId, string unitId, string displayName, string tier, int level, int amount, int combatValue, List<SummaryValueSkillState> skills)
    {
        StackId = stackId;
        UnitId = unitId;
        DisplayName = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        Tier = string.IsNullOrEmpty(tier) ? "I" : tier;
        Level = Math.Max(1, level);
        Amount = Math.Max(0, amount);
        CombatValue = Math.Max(0, combatValue);
        Skills = skills ?? new List<SummaryValueSkillState>();
    }
}

[Serializable]
public class SummaryValueArmySnapshot
{
    public string SnapshotId;
    public int TotalArmyValue;
    public List<SummaryValueStackSnapshot> Stacks;

    public SummaryValueArmySnapshot(string snapshotId, int totalArmyValue, List<SummaryValueStackSnapshot> stacks)
    {
        SnapshotId = snapshotId;
        TotalArmyValue = Math.Max(0, totalArmyValue);
        Stacks = stacks ?? new List<SummaryValueStackSnapshot>();
    }
}

[Serializable]
public class SummaryValueTimelineEntry
{
    public string EntryId;
    public int StageIndex;
    public string Label;
    public string ReceivedText;
    public int ArmyValueAfterStage;
    public int RunGoldAfterStage;

    public SummaryValueTimelineEntry(string entryId, int stageIndex, string label, string receivedText, int armyValueAfterStage, int runGoldAfterStage)
    {
        EntryId = entryId;
        StageIndex = Math.Max(0, stageIndex);
        Label = label;
        ReceivedText = receivedText;
        ArmyValueAfterStage = Math.Max(0, armyValueAfterStage);
        RunGoldAfterStage = Math.Max(0, runGoldAfterStage);
    }
}

[Serializable]
public class SummaryValueAccountProgressReward
{
    public int AccountXp;
    public string NextUnlockPreview;

    public SummaryValueAccountProgressReward(int accountXp, string nextUnlockPreview)
    {
        AccountXp = Math.Max(0, accountXp);
        NextUnlockPreview = nextUnlockPreview;
    }
}

[Serializable]
public class SummaryValueSavedArmyCandidate
{
    public string CandidateId;
    public string CreatedFromRunId;
    public string PreFinalSnapshotId;
    public SummaryValueArmySnapshot ImmutableArmySnapshot;
    public int ArmyValue;

    public SummaryValueSavedArmyCandidate(string candidateId, string createdFromRunId, string preFinalSnapshotId, SummaryValueArmySnapshot immutableArmySnapshot, int armyValue)
    {
        CandidateId = candidateId;
        CreatedFromRunId = createdFromRunId;
        PreFinalSnapshotId = preFinalSnapshotId;
        ImmutableArmySnapshot = immutableArmySnapshot;
        ArmyValue = Math.Max(0, armyValue);
    }
}

[Serializable]
public class SummaryValueSaveSlotViewData
{
    public string SlotId;
    public int PhysicalIndex;
    public SummaryValueSlotState State;
    public string ExistingSavedArmyId;
    public int ExistingArmyValue;
    public bool Selectable;
    public bool Selected;

    public SummaryValueSaveSlotViewData(string slotId, int physicalIndex, SummaryValueSlotState state, string existingSavedArmyId, bool selectable, bool selected, int existingArmyValue = 0)
    {
        SlotId = slotId;
        PhysicalIndex = Math.Max(0, physicalIndex);
        State = state;
        ExistingSavedArmyId = existingSavedArmyId;
        ExistingArmyValue = Math.Max(0, existingArmyValue);
        Selectable = selectable;
        Selected = selected;
    }
}

[Serializable]
public class SummaryValueScreenViewData
{
    public string RunSummaryId;
    public string RunId;
    public SummaryValueGameMode GameMode;
    public SummaryValueAuthoritySource AuthoritySource;
    public SummaryValueFinalResult FinalResult;
    public SummaryValueArmySnapshot StartArmySnapshot;
    public SummaryValueArmySnapshot PreFinalArmySnapshot;
    public SummaryValueArmySnapshot PostFinalArmySnapshot;
    public List<SummaryValueTimelineEntry> TimelineEntries;
    public SummaryValueAccountProgressReward AccountProgressReward;
    public SummaryValueSavedArmyCandidate SavedArmyCandidate;
    public List<SummaryValueSaveSlotViewData> SaveSlots;
    public SummaryValueSaveSlotViewData SelectedSlot;
    public SummaryValueSaveActionMode ActionMode;
    public bool CanSave;
    public string Message;

    public SummaryValueScreenViewData(string runSummaryId, string runId, SummaryValueGameMode gameMode, SummaryValueAuthoritySource authoritySource, SummaryValueFinalResult finalResult, SummaryValueArmySnapshot startArmySnapshot, SummaryValueArmySnapshot preFinalArmySnapshot, SummaryValueArmySnapshot postFinalArmySnapshot, List<SummaryValueTimelineEntry> timelineEntries, SummaryValueAccountProgressReward accountProgressReward, SummaryValueSavedArmyCandidate savedArmyCandidate, List<SummaryValueSaveSlotViewData> saveSlots, SummaryValueSaveSlotViewData selectedSlot, SummaryValueSaveActionMode actionMode, bool canSave, string message)
    {
        RunSummaryId = runSummaryId;
        RunId = runId;
        GameMode = gameMode;
        AuthoritySource = authoritySource;
        FinalResult = finalResult;
        StartArmySnapshot = startArmySnapshot;
        PreFinalArmySnapshot = preFinalArmySnapshot;
        PostFinalArmySnapshot = postFinalArmySnapshot;
        TimelineEntries = timelineEntries ?? new List<SummaryValueTimelineEntry>();
        AccountProgressReward = accountProgressReward;
        SavedArmyCandidate = savedArmyCandidate;
        SaveSlots = saveSlots ?? new List<SummaryValueSaveSlotViewData>();
        SelectedSlot = selectedSlot;
        ActionMode = actionMode;
        CanSave = canSave;
        Message = message;
    }
}

[Serializable]
public class SummaryValueBuildRequest
{
    public string RunId;
    public SummaryValueFinalResult FinalResult;
    public SummaryValueArmySnapshot StartArmySnapshot;
    public SummaryValueArmySnapshot PreFinalArmySnapshot;
    public SummaryValueArmySnapshot PostFinalArmySnapshot;
    public List<SummaryValueTimelineEntry> TimelineEntries;
    public int UnlockedSlotCount;
    public string SelectedSlotId;

    public SummaryValueBuildRequest(string runId, SummaryValueFinalResult finalResult, SummaryValueArmySnapshot startArmySnapshot, SummaryValueArmySnapshot preFinalArmySnapshot, SummaryValueArmySnapshot postFinalArmySnapshot, List<SummaryValueTimelineEntry> timelineEntries, int unlockedSlotCount, string selectedSlotId)
    {
        RunId = runId;
        FinalResult = finalResult;
        StartArmySnapshot = startArmySnapshot;
        PreFinalArmySnapshot = preFinalArmySnapshot;
        PostFinalArmySnapshot = postFinalArmySnapshot;
        TimelineEntries = timelineEntries ?? new List<SummaryValueTimelineEntry>();
        UnlockedSlotCount = Math.Max(0, unlockedSlotCount);
        SelectedSlotId = selectedSlotId;
    }
}

[Serializable]
public class SummaryValueSaveCommand
{
    public string RunId;
    public string CandidateId;
    public string SlotId;
    public bool ConfirmOverwrite;

    public SummaryValueSaveCommand(string runId, string candidateId, string slotId, bool confirmOverwrite)
    {
        RunId = runId;
        CandidateId = candidateId;
        SlotId = slotId;
        ConfirmOverwrite = confirmOverwrite;
    }
}

[Serializable]
public class SummaryValueSaveResult
{
    public bool Success;
    public SummaryValueError Error;
    public string Message;
    public string SavedArmyId;
    public SummaryValueSaveActionMode ActionMode;

    public SummaryValueSaveResult(bool success, SummaryValueError error, string message, string savedArmyId, SummaryValueSaveActionMode actionMode)
    {
        Success = success;
        Error = error;
        Message = message;
        SavedArmyId = savedArmyId;
        ActionMode = actionMode;
    }
}
