using System;
using System.Collections.Generic;

public class SummaryValueService
{
    private readonly ISummaryValueRosterStore rosterStore;
    private SummaryValueSavedArmyCandidate lastCandidate;
    private SummaryValueScreenViewData lastScreen;

    public SummaryValueService(ISummaryValueRosterStore rosterStore)
    {
        this.rosterStore = rosterStore;
    }

    public SummaryValueScreenViewData BuildSummary(SummaryValueBuildRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.RunId))
        {
            return Empty("Missing run.");
        }

        SummaryValueBuildRequest effectiveRequest = request;
        ISummaryValuePersistenceStore persistenceStore = rosterStore as ISummaryValuePersistenceStore;
        if (persistenceStore != null)
        {
            SummaryValuePersistedState persisted = persistenceStore.PersistAndLoad(request);
            if (persisted != null && persisted.Request != null)
            {
                effectiveRequest = persisted.Request;
            }
        }

        SummaryValueSavedArmyCandidate candidate = BuildCandidate(effectiveRequest);

        List<SummaryValueSaveSlotViewData> slots = rosterStore == null
            ? new List<SummaryValueSaveSlotViewData>()
            : rosterStore.ListSlots(effectiveRequest.UnlockedSlotCount, effectiveRequest.SelectedSlotId);
        SummaryValueSaveSlotViewData selectedSlot = FindSlot(slots, effectiveRequest.SelectedSlotId);
        SummaryValueSaveActionMode actionMode = DetermineActionMode(selectedSlot);
        bool canSave = candidate != null && (actionMode == SummaryValueSaveActionMode.Save || actionMode == SummaryValueSaveActionMode.Overwrite);

        SummaryValueScreenViewData screen = new SummaryValueScreenViewData(
            "run-summary-" + Guid.NewGuid().ToString("N"),
            effectiveRequest.RunId,
            SummaryValueGameMode.Offline,
            SummaryValueAuthoritySource.LocalOfflineAdapter,
            effectiveRequest.FinalResult,
            effectiveRequest.StartArmySnapshot,
            effectiveRequest.PreFinalArmySnapshot,
            effectiveRequest.PostFinalArmySnapshot,
            effectiveRequest.TimelineEntries,
            effectiveRequest.FinalResult == SummaryValueFinalResult.Won ? new SummaryValueAccountProgressReward(100, "Next unlock: saved army slot progress") : new SummaryValueAccountProgressReward(0, "No final victory reward"),
            candidate,
            slots,
            selectedSlot,
            actionMode,
            canSave,
            candidate == null ? "Final victory is required before saving an army." : "Saved army candidate is based on the pre-final snapshot.");

        lastScreen = screen;
        return screen;
    }

    public SummaryValueSaveResult Save(SummaryValueSaveCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.RunId))
        {
            return Fail(SummaryValueError.MissingRun, "Missing run.", SummaryValueSaveActionMode.None);
        }

        SummaryValueSavedArmyCandidate candidate = lastCandidate;
        if (candidate == null || candidate.CandidateId != command.CandidateId)
        {
            candidate = rosterStore == null ? null : rosterStore.FindCandidate(command.CandidateId);
        }

        if (candidate == null)
        {
            return Fail(SummaryValueError.MissingSnapshot, "Saved army candidate was not found.", SummaryValueSaveActionMode.None);
        }

        SummaryValueSaveSlotViewData slot = lastScreen == null ? null : FindSlot(lastScreen.SaveSlots, command.SlotId);
        if (slot == null)
        {
            return Fail(SummaryValueError.MissingSlot, "Save slot was not found.", SummaryValueSaveActionMode.None);
        }

        if (slot.State == SummaryValueSlotState.Locked)
        {
            return Fail(SummaryValueError.LockedSlot, "Locked slots cannot save or overwrite armies.", SummaryValueSaveActionMode.None);
        }

        SummaryValueSaveActionMode actionMode = DetermineActionMode(slot);
        if (actionMode == SummaryValueSaveActionMode.Overwrite && !command.ConfirmOverwrite)
        {
            return Fail(SummaryValueError.MissingConfirmation, "Overwrite requires confirmation.", actionMode);
        }

        if (rosterStore != null)
        {
            string savedArmyId = rosterStore.SaveCandidate(command.SlotId, candidate);
            return new SummaryValueSaveResult(true, SummaryValueError.None, actionMode == SummaryValueSaveActionMode.Overwrite ? "Saved army overwritten." : "Saved army stored.", savedArmyId, actionMode);
        }

        return new SummaryValueSaveResult(true, SummaryValueError.None, actionMode == SummaryValueSaveActionMode.Overwrite ? "Saved army overwritten." : "Saved army stored.", string.Empty, actionMode);
    }

    private SummaryValueSaveActionMode DetermineActionMode(SummaryValueSaveSlotViewData slot)
    {
        if (slot == null || slot.State == SummaryValueSlotState.Locked)
        {
            return SummaryValueSaveActionMode.None;
        }

        return slot.State == SummaryValueSlotState.Taken ? SummaryValueSaveActionMode.Overwrite : SummaryValueSaveActionMode.Save;
    }

    private SummaryValueSavedArmyCandidate BuildCandidate(SummaryValueBuildRequest request)
    {
        if (request.FinalResult != SummaryValueFinalResult.Won || request.PreFinalArmySnapshot == null)
        {
            lastCandidate = null;
            return null;
        }

        if (lastCandidate != null
            && lastCandidate.CreatedFromRunId == request.RunId
            && lastCandidate.PreFinalSnapshotId == request.PreFinalArmySnapshot.SnapshotId)
        {
            return lastCandidate;
        }

        lastCandidate = new SummaryValueSavedArmyCandidate(
            BuildCandidateId(request.RunId),
            request.RunId,
            request.PreFinalArmySnapshot.SnapshotId,
            CloneArmy(request.PreFinalArmySnapshot, "immutable-" + request.PreFinalArmySnapshot.SnapshotId),
            request.PreFinalArmySnapshot.TotalArmyValue);

        return lastCandidate;
    }

    private SummaryValueSaveSlotViewData FindSlot(List<SummaryValueSaveSlotViewData> slots, string slotId)
    {
        if (slots == null || string.IsNullOrEmpty(slotId))
        {
            return null;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].SlotId == slotId)
            {
                return slots[i];
            }
        }

        return null;
    }

    private SummaryValueArmySnapshot CloneArmy(SummaryValueArmySnapshot army, string snapshotId)
    {
        List<SummaryValueStackSnapshot> stacks = new List<SummaryValueStackSnapshot>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                SummaryValueStackSnapshot stack = army.Stacks[i];
                if (stack != null)
                {
                    stacks.Add(new SummaryValueStackSnapshot(stack.StackId, stack.UnitId, stack.DisplayName, stack.Tier, stack.Level, stack.Amount, stack.CombatValue, CloneSkills(stack.Skills)));
                }
            }
        }

        return new SummaryValueArmySnapshot(snapshotId, army == null ? 0 : army.TotalArmyValue, stacks);
    }

    private List<SummaryValueSkillState> CloneSkills(List<SummaryValueSkillState> skills)
    {
        List<SummaryValueSkillState> result = new List<SummaryValueSkillState>();
        if (skills == null)
        {
            return result;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                result.Add(new SummaryValueSkillState(skills[i].SkillId, skills[i].Unlocked));
            }
        }

        return result;
    }

    private SummaryValueScreenViewData Empty(string message)
    {
        return new SummaryValueScreenViewData(
            string.Empty,
            string.Empty,
            SummaryValueGameMode.Offline,
            SummaryValueAuthoritySource.LocalOfflineAdapter,
            SummaryValueFinalResult.Pending,
            null,
            null,
            null,
            new List<SummaryValueTimelineEntry>(),
            new SummaryValueAccountProgressReward(0, string.Empty),
            null,
            new List<SummaryValueSaveSlotViewData>(),
            null,
            SummaryValueSaveActionMode.None,
            false,
            message);
    }

    private SummaryValueSaveResult Fail(SummaryValueError error, string message, SummaryValueSaveActionMode mode)
    {
        return new SummaryValueSaveResult(false, error, message, string.Empty, mode);
    }

    private static string BuildCandidateId(string runId)
    {
        return "saved-candidate-" + (string.IsNullOrEmpty(runId) ? "run-unsaved" : runId);
    }
}
