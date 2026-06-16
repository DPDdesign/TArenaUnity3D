using System.Collections.Generic;

public class OfflineSavedArmiesDbStore : ISavedArmiesRosterStore, ISavedArmiesAttackHistoryStore
{
    private readonly OfflineSavedArmyDbRepository repository;

    public OfflineSavedArmiesDbStore(string databasePath)
    {
        repository = new OfflineSavedArmyDbRepository(databasePath);
    }

    public string CurrentDefenceSavedArmyId
    {
        get { return repository.LoadCurrentDefenceSavedArmyId(); }
    }

    public List<SavedArmySlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId, string currentDefenceSavedArmyId, ISavedArmiesUnitDefinitionSource unitSource)
    {
        List<OfflineSavedArmySlotRecord> records = repository.LoadSlotRecords(unlockedSlotCount);
        string defenceId = string.IsNullOrEmpty(currentDefenceSavedArmyId) ? CurrentDefenceSavedArmyId : currentDefenceSavedArmyId;
        List<SavedArmySlotViewData> slots = new List<SavedArmySlotViewData>();

        for (int i = 0; i < records.Count; i++)
        {
            OfflineSavedArmySlotRecord record = records[i];
            string slotId = OfflineDatabaseLegacyIdentity.ToLegacySlotId(record.SlotIndex);
            string savedArmyId = record.SavedArmyId <= 0 ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(record.SavedArmyId);
            SavedArmy army = string.IsNullOrEmpty(savedArmyId) ? null : repository.LoadActiveArmy(savedArmyId);
            SavedArmySlotState state = record.Locked ? SavedArmySlotState.Locked : army == null ? SavedArmySlotState.Empty : SavedArmySlotState.Taken;
            int value = army == null ? 0 : SavedArmiesValueCalculator.CalculateArmyValue(army.Stacks, unitSource);

            slots.Add(new SavedArmySlotViewData(
                slotId,
                record.SlotIndex + 1,
                state,
                savedArmyId,
                value,
                slotId == selectedSlotId,
                !string.IsNullOrEmpty(savedArmyId) && savedArmyId == defenceId));
        }

        return slots;
    }

    public SavedArmy FindActiveArmyInSlot(string slotId)
    {
        return repository.LoadActiveArmyInSlot(slotId);
    }

    public SavedArmy FindActiveArmy(string savedArmyId)
    {
        return repository.LoadActiveArmy(savedArmyId);
    }

    public SavedArmy SaveArmyToSlot(string slotId, List<SavedArmyStackSnapshot> stacks)
    {
        OfflineArmySnapshotRecord snapshot = OfflineArmySnapshotMapper.FromSavedArmy(
            new SavedArmy(string.Empty, string.Empty, true, CopyStacks(stacks)));
        OfflineSavedArmyPersistenceResult result = repository.SaveSnapshotToSlot(slotId, snapshot, 0, 0);
        return result == null ? null : repository.LoadActiveArmy(OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(result.SavedArmyId));
    }

    public void SetCurrentDefence(string savedArmyId)
    {
        repository.SetCurrentDefence(savedArmyId);
    }

    public void ClearCurrentDefence()
    {
        repository.ClearCurrentDefence();
    }

    public List<SavedArmyAttackHistoryEntry> ListHistory(string savedArmyId)
    {
        return repository.ListHistory(savedArmyId);
    }

    public void AddHistory(SavedArmyAttackHistoryEntry entry)
    {
        repository.AddHistory(entry);
    }

    private static List<SavedArmyStackSnapshot> CopyStacks(List<SavedArmyStackSnapshot> stacks)
    {
        List<SavedArmyStackSnapshot> result = new List<SavedArmyStackSnapshot>();
        if (stacks == null)
        {
            return result;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            SavedArmyStackSnapshot stack = stacks[i];
            if (stack != null)
            {
                result.Add(new SavedArmyStackSnapshot(stack.UnitId, stack.Amount));
            }
        }

        return result;
    }
}
