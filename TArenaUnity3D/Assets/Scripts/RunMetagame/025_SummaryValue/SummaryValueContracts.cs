using System;
using System.Collections.Generic;

public interface ISummaryValueRosterStore
{
    List<SummaryValueSaveSlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId);
    string SaveCandidate(string slotId, SummaryValueSavedArmyCandidate candidate);
    SummaryValueSavedArmyCandidate FindCandidate(string candidateId);
}

public interface ISummaryValuePersistenceStore
{
    SummaryValuePersistedState PersistAndLoad(SummaryValueBuildRequest request);
}

public class InMemorySummaryValueRosterStore : ISummaryValueRosterStore
{
    private readonly Dictionary<string, PersistedCandidate> savedBySlot = new Dictionary<string, PersistedCandidate>();
    private readonly Dictionary<string, SummaryValueSavedArmyCandidate> candidates = new Dictionary<string, SummaryValueSavedArmyCandidate>();

    public List<SummaryValueSaveSlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId)
    {
        List<SummaryValueSaveSlotViewData> slots = new List<SummaryValueSaveSlotViewData>();
        for (int i = 0; i < 8; i++)
        {
            string slotId = "slot-" + (i + 1).ToString("00");
            bool unlocked = i < unlockedSlotCount;
            PersistedCandidate saved;
            bool taken = savedBySlot.TryGetValue(slotId, out saved);
            SummaryValueSlotState state = !unlocked ? SummaryValueSlotState.Locked : taken ? SummaryValueSlotState.Taken : SummaryValueSlotState.Empty;
            slots.Add(new SummaryValueSaveSlotViewData(
                slotId,
                i + 1,
                state,
                saved == null ? string.Empty : saved.SavedArmyId,
                unlocked,
                slotId == selectedSlotId,
                saved == null || saved.Candidate == null ? 0 : saved.Candidate.ArmyValue));
        }

        return slots;
    }

    public string SaveCandidate(string slotId, SummaryValueSavedArmyCandidate candidate)
    {
        if (string.IsNullOrEmpty(slotId) || candidate == null)
        {
            return string.Empty;
        }

        SummaryValueSavedArmyCandidate copy = CloneCandidate(candidate);
        string savedArmyId = "saved-army-" + Guid.NewGuid().ToString("N");
        savedBySlot[slotId] = new PersistedCandidate(savedArmyId, copy);
        candidates[candidate.CandidateId] = copy;
        return savedArmyId;
    }

    public SummaryValueSavedArmyCandidate FindCandidate(string candidateId)
    {
        SummaryValueSavedArmyCandidate candidate;
        return candidates.TryGetValue(candidateId, out candidate) ? candidate : null;
    }

    private static SummaryValueSavedArmyCandidate CloneCandidate(SummaryValueSavedArmyCandidate candidate)
    {
        if (candidate == null)
        {
            return null;
        }

        return new SummaryValueSavedArmyCandidate(
            candidate.CandidateId,
            candidate.CreatedFromRunId,
            candidate.PreFinalSnapshotId,
            CloneArmy(candidate.ImmutableArmySnapshot),
            candidate.ArmyValue);
    }

    private static SummaryValueArmySnapshot CloneArmy(SummaryValueArmySnapshot army)
    {
        List<SummaryValueStackSnapshot> stacks = new List<SummaryValueStackSnapshot>();
        if (army != null && army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                SummaryValueStackSnapshot stack = army.Stacks[i];
                if (stack == null)
                {
                    continue;
                }

                List<SummaryValueSkillState> skills = new List<SummaryValueSkillState>();
                if (stack.Skills != null)
                {
                    for (int skillIndex = 0; skillIndex < stack.Skills.Count; skillIndex++)
                    {
                        SummaryValueSkillState skill = stack.Skills[skillIndex];
                        if (skill != null)
                        {
                            skills.Add(new SummaryValueSkillState(skill.SkillId, skill.Unlocked));
                        }
                    }
                }

                stacks.Add(new SummaryValueStackSnapshot(
                    stack.StackId,
                    stack.UnitId,
                    stack.DisplayName,
                    stack.Tier,
                    stack.Level,
                    stack.Amount,
                    stack.CombatValue,
                    skills));
            }
        }

        return new SummaryValueArmySnapshot(
            army == null ? string.Empty : army.SnapshotId,
            army == null ? 0 : army.TotalArmyValue,
            stacks);
    }

    private sealed class PersistedCandidate
    {
        public string SavedArmyId;
        public SummaryValueSavedArmyCandidate Candidate;

        public PersistedCandidate(string savedArmyId, SummaryValueSavedArmyCandidate candidate)
        {
            SavedArmyId = savedArmyId;
            Candidate = candidate;
        }
    }
}
