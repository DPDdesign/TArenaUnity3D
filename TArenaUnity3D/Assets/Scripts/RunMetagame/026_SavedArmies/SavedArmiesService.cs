using System.Collections.Generic;

public class SavedArmiesService
{
    private readonly ISavedArmiesRosterStore rosterStore;
    private readonly ISavedArmiesSeedSource seedSource;
    private readonly ISavedArmiesAttackHistoryStore attackHistoryStore;
    private readonly ISavedArmiesUnitDefinitionSource unitSource;

    public SavedArmiesService(ISavedArmiesRosterStore rosterStore, ISavedArmiesSeedSource seedSource, ISavedArmiesAttackHistoryStore attackHistoryStore, ISavedArmiesUnitDefinitionSource unitSource)
    {
        this.rosterStore = rosterStore;
        this.seedSource = seedSource;
        this.attackHistoryStore = attackHistoryStore;
        this.unitSource = unitSource;
    }

    public SavedArmiesRosterViewData BuildRoster(SavedArmiesRosterRequest request)
    {
        int unlockedSlotCount = request == null || request.UnlockedSlotCount <= 0 ? 8 : request.UnlockedSlotCount;
        string selectedSlotId = request == null ? "slot-01" : request.SelectedSlotId;
        string selectedArenaArmyId = request == null ? string.Empty : request.SelectedArenaArmyId;
        List<SavedArmySlotViewData> slots = rosterStore == null ? new List<SavedArmySlotViewData>() : rosterStore.ListSlots(unlockedSlotCount, selectedSlotId, rosterStore.CurrentDefenceSavedArmyId, unitSource);
        SavedArmySlotViewData selectedSlot = FindSlot(slots, selectedSlotId);
        if (selectedSlot == null && slots.Count > 0)
        {
            selectedSlot = slots[0];
            selectedSlotId = selectedSlot.SlotId;
            slots = rosterStore == null ? slots : rosterStore.ListSlots(unlockedSlotCount, selectedSlotId, rosterStore.CurrentDefenceSavedArmyId, unitSource);
            selectedSlot = FindSlot(slots, selectedSlotId);
        }

        SavedArmy selectedArmyModel = selectedSlot == null || rosterStore == null ? null : rosterStore.FindActiveArmy(selectedSlot.SavedArmyId);
        SavedArmyPreviewViewData selectedArmy = BuildPreview(selectedArmyModel);
        List<ArenaArmyOptionViewData> arenaOptions = BuildArenaOptions(selectedArenaArmyId);
        ArenaArmyOptionViewData selectedArenaArmy = FindArenaOption(arenaOptions, selectedArenaArmyId);
        if (selectedArenaArmy == null && arenaOptions.Count > 0)
        {
            selectedArenaArmy = arenaOptions[0];
        }

        List<SavedArmyAttackHistoryEntry> history = selectedArmyModel == null || attackHistoryStore == null ? new List<SavedArmyAttackHistoryEntry>() : attackHistoryStore.ListHistory(selectedArmyModel.SavedArmyId);
        bool canSetDefence = CanSetDefence(selectedArmyModel);
        bool requiresOverwrite = selectedSlot != null && selectedSlot.State == SavedArmySlotState.Taken;

        return new SavedArmiesRosterViewData(SavedArmiesGameMode.Offline, SavedArmiesAuthoritySource.LocalOfflineAdapter, slots, selectedSlot, selectedArmy, arenaOptions, selectedArenaArmy, history, rosterStore == null ? string.Empty : rosterStore.CurrentDefenceSavedArmyId, canSetDefence, requiresOverwrite, selectedArmy == null ? "Select a taken slot to preview saved army details." : "Selected army value is calculated from current unit definitions.");
    }

    public SavedArmyCommandResult LoadSeedArmy(SavedArmyImportCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.SlotId))
        {
            return Fail(SavedArmiesError.MissingSlot, "Select a saved-army slot first.");
        }

        SavedArmySlotState state = SlotState(command.SlotId, CommandUnlockedSlotCount(command.UnlockedSlotCount));
        if (state == SavedArmySlotState.Locked)
        {
            return Fail(SavedArmiesError.LockedSlot, "Locked slots cannot load seed armies.");
        }

        if (state == SavedArmySlotState.Taken && !command.ConfirmOverwrite)
        {
            return Fail(SavedArmiesError.MissingConfirmation, "Overwrite requires confirmation.");
        }

        SavedArmySeedCandidate seedArmy = seedSource == null ? null : seedSource.FindSeedArmy(command.ArenaArmyId);
        if (seedArmy == null)
        {
            return Fail(SavedArmiesError.MissingArenaArmy, "Choose a seed army to load.");
        }

        SavedArmy seededArmy = OfflineArmySnapshotMapper.ToSavedArmy(seedArmy.Snapshot, seedArmy.SeedArmyId);
        List<SavedArmyStackSnapshot> copiedStacks = CopyStacks(seededArmy == null ? null : seededArmy.Stacks);
        if (SavedArmiesValueCalculator.CalculateArmyValue(copiedStacks, unitSource) <= 0)
        {
            return Fail(SavedArmiesError.ZeroValueArmy, "Seed army has no current value.");
        }

        SavedArmy savedArmy = rosterStore == null ? null : rosterStore.SaveArmyToSlot(command.SlotId, copiedStacks);
        return new SavedArmyCommandResult(true, SavedArmiesError.None, state == SavedArmySlotState.Taken ? "Seed army loaded and previous defence cleared if needed." : "Seed army loaded into empty slot.", savedArmy == null ? string.Empty : savedArmy.SavedArmyId);
    }

    public SavedArmyCommandResult ImportFromArena(SavedArmyImportCommand command)
    {
        // Legacy compatibility entry point for existing callers.
        return LoadSeedArmy(command);
    }

    public SavedArmyCommandResult SaveRunArmy(SavedArmySaveRunCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.SlotId))
        {
            return Fail(SavedArmiesError.MissingSlot, "Select a saved-army slot first.");
        }

        SavedArmySlotState state = SlotState(command.SlotId, CommandUnlockedSlotCount(command.UnlockedSlotCount));
        if (state == SavedArmySlotState.Locked)
        {
            return Fail(SavedArmiesError.LockedSlot, "Locked slots cannot save run-created armies.");
        }

        if (state == SavedArmySlotState.Taken && !command.ConfirmOverwrite)
        {
            return Fail(SavedArmiesError.MissingConfirmation, "Overwrite requires confirmation.");
        }

        if (SavedArmiesValueCalculator.CalculateArmyValue(command.Stacks, unitSource) <= 0)
        {
            return Fail(SavedArmiesError.ZeroValueArmy, "Run-created army has no current value.");
        }

        SavedArmy savedArmy = rosterStore == null ? null : rosterStore.SaveArmyToSlot(command.SlotId, command.Stacks);
        return new SavedArmyCommandResult(true, SavedArmiesError.None, state == SavedArmySlotState.Taken ? "Run-created army overwrote the slot." : "Run-created army saved.", savedArmy == null ? string.Empty : savedArmy.SavedArmyId);
    }

    public SavedArmyCommandResult SetDefence(string savedArmyId)
    {
        if (rosterStore == null || string.IsNullOrEmpty(savedArmyId))
        {
            return Fail(SavedArmiesError.MissingSavedArmy, "Select a saved army before setting defence.");
        }

        SavedArmy army = rosterStore.FindActiveArmy(savedArmyId);
        if (army == null || !army.IsValid)
        {
            return Fail(SavedArmiesError.InvalidArmy, "Only valid taken armies can be set as defence.");
        }

        int value = SavedArmiesValueCalculator.CalculateArmyValue(army.Stacks, unitSource);
        if (value <= 0)
        {
            return Fail(SavedArmiesError.ZeroValueArmy, "Zero-value armies cannot be set as defence.");
        }

        rosterStore.SetCurrentDefence(savedArmyId);
        return new SavedArmyCommandResult(true, SavedArmiesError.None, "Current defence set.", savedArmyId);
    }

    private SavedArmySlotState SlotState(string slotId, int unlockedSlotCount)
    {
        List<SavedArmySlotViewData> slots = rosterStore == null ? new List<SavedArmySlotViewData>() : rosterStore.ListSlots(unlockedSlotCount, slotId, rosterStore.CurrentDefenceSavedArmyId, unitSource);
        SavedArmySlotViewData slot = FindSlot(slots, slotId);
        return slot == null ? SavedArmySlotState.Locked : slot.State;
    }

    private SavedArmyPreviewViewData BuildPreview(SavedArmy army)
    {
        if (army == null)
        {
            return null;
        }

        List<SavedArmyStackViewData> stacks = new List<SavedArmyStackViewData>();
        if (army.Stacks != null)
        {
            for (int i = 0; i < army.Stacks.Count; i++)
            {
                SavedArmyStackSnapshot stack = army.Stacks[i];
                if (stack == null)
                {
                    continue;
                }

                SavedArmiesUnitDefinition unit = SavedArmiesValueCalculator.ResolveUnit(stack.UnitId, unitSource);
                int stackValue = SavedArmiesValueCalculator.CalculateStackValue(stack, unitSource);
                stacks.Add(new SavedArmyStackViewData(stack.UnitId, unit.DisplayName, unit.Tier, stack.Amount, unit.Cost, stackValue));
            }
        }

        return new SavedArmyPreviewViewData(army.SavedArmyId, army.SnapshotId, army.IsValid, SavedArmiesValueCalculator.CalculateArmyValue(army.Stacks, unitSource), stacks);
    }

    private List<ArenaArmyOptionViewData> BuildArenaOptions(string selectedArenaArmyId)
    {
        List<ArenaArmyOptionViewData> options = new List<ArenaArmyOptionViewData>();
        List<SavedArmySeedCandidate> candidates = seedSource == null ? new List<SavedArmySeedCandidate>() : seedSource.ListSeedArmies();
        for (int i = 0; i < candidates.Count; i++)
        {
            SavedArmySeedCandidate candidate = candidates[i];
            if (candidate != null)
            {
                SavedArmy savedArmy = OfflineArmySnapshotMapper.ToSavedArmy(candidate.Snapshot, candidate.SeedArmyId);
                options.Add(new ArenaArmyOptionViewData(candidate.SeedArmyId, candidate.DisplayName, SavedArmiesValueCalculator.CalculateArmyValue(savedArmy == null ? null : savedArmy.Stacks, unitSource), candidate.SeedArmyId == selectedArenaArmyId));
            }
        }

        return options;
    }

    private bool CanSetDefence(SavedArmy army)
    {
        return army != null && army.IsValid && SavedArmiesValueCalculator.CalculateArmyValue(army.Stacks, unitSource) > 0;
    }

    private static int CommandUnlockedSlotCount(int unlockedSlotCount)
    {
        return unlockedSlotCount <= 0 ? 8 : unlockedSlotCount;
    }

    private static SavedArmySlotViewData FindSlot(List<SavedArmySlotViewData> slots, string slotId)
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

    private static ArenaArmyOptionViewData FindArenaOption(List<ArenaArmyOptionViewData> options, string arenaArmyId)
    {
        if (options == null || string.IsNullOrEmpty(arenaArmyId))
        {
            return null;
        }

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].ArenaArmyId == arenaArmyId)
            {
                return options[i];
            }
        }

        return null;
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

    private static SavedArmyCommandResult Fail(SavedArmiesError error, string message)
    {
        return new SavedArmyCommandResult(false, error, message, string.Empty);
    }
}
