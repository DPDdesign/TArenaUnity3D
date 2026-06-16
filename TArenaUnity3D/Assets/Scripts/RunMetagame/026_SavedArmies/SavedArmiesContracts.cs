using System;
using System.Collections.Generic;

public interface ISavedArmiesUnitDefinitionSource
{
    SavedArmiesUnitDefinition FindUnit(string unitId);
}

public interface ISavedArmiesArenaImportSource
{
    List<ArenaArmyImportCandidate> ListArenaArmies();
    ArenaArmyImportCandidate FindArenaArmy(string arenaArmyId);
}

public interface ISavedArmiesSeedSource
{
    List<SavedArmySeedCandidate> ListSeedArmies();
    SavedArmySeedCandidate FindSeedArmy(string seedArmyId);
}

public interface ISavedArmiesRosterStore
{
    List<SavedArmySlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId, string currentDefenceSavedArmyId, ISavedArmiesUnitDefinitionSource unitSource);
    SavedArmy FindActiveArmyInSlot(string slotId);
    SavedArmy FindActiveArmy(string savedArmyId);
    SavedArmy SaveArmyToSlot(string slotId, List<SavedArmyStackSnapshot> stacks);
    string CurrentDefenceSavedArmyId { get; }
    void SetCurrentDefence(string savedArmyId);
    void ClearCurrentDefence();
}

public interface ISavedArmiesAttackHistoryStore
{
    List<SavedArmyAttackHistoryEntry> ListHistory(string savedArmyId);
    void AddHistory(SavedArmyAttackHistoryEntry entry);
}

public class InMemorySavedArmiesRosterStore : ISavedArmiesRosterStore
{
    private readonly Dictionary<string, SavedArmy> activeBySlot = new Dictionary<string, SavedArmy>();
    private readonly Dictionary<string, SavedArmy> allById = new Dictionary<string, SavedArmy>();
    private string currentDefenceSavedArmyId;

    public string CurrentDefenceSavedArmyId
    {
        get { return currentDefenceSavedArmyId; }
    }

    public List<SavedArmySlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId, string currentDefenceId, ISavedArmiesUnitDefinitionSource unitSource)
    {
        List<SavedArmySlotViewData> slots = new List<SavedArmySlotViewData>();
        int clampedUnlocked = Math.Max(0, Math.Min(8, unlockedSlotCount));

        for (int i = 0; i < 8; i++)
        {
            string slotId = SlotIdForIndex(i);
            bool unlocked = i < clampedUnlocked;
            SavedArmy army = FindActiveArmyInSlot(slotId);
            SavedArmySlotState state = !unlocked ? SavedArmySlotState.Locked : army == null ? SavedArmySlotState.Empty : SavedArmySlotState.Taken;
            string savedArmyId = army == null ? string.Empty : army.SavedArmyId;
            int value = army == null ? 0 : SavedArmiesValueCalculator.CalculateArmyValue(army.Stacks, unitSource);

            slots.Add(new SavedArmySlotViewData(
                slotId,
                i + 1,
                state,
                savedArmyId,
                value,
                slotId == selectedSlotId,
                !string.IsNullOrEmpty(savedArmyId) && savedArmyId == currentDefenceId));
        }

        return slots;
    }

    public SavedArmy FindActiveArmyInSlot(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return null;
        }

        SavedArmy army;
        if (!activeBySlot.TryGetValue(slotId, out army) || army == null || !army.IsValid)
        {
            return null;
        }

        return army;
    }

    public SavedArmy FindActiveArmy(string savedArmyId)
    {
        if (string.IsNullOrEmpty(savedArmyId))
        {
            return null;
        }

        SavedArmy army;
        if (!allById.TryGetValue(savedArmyId, out army) || army == null || !army.IsValid)
        {
            return null;
        }

        return army;
    }

    public SavedArmy SaveArmyToSlot(string slotId, List<SavedArmyStackSnapshot> stacks)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return null;
        }

        SavedArmy existing = FindActiveArmyInSlot(slotId);
        if (existing != null)
        {
            existing.IsValid = false;
            if (existing.SavedArmyId == currentDefenceSavedArmyId)
            {
                currentDefenceSavedArmyId = string.Empty;
            }
        }

        string id = "saved-army-" + Guid.NewGuid().ToString("N");
        SavedArmy army = new SavedArmy(id, "snapshot-" + Guid.NewGuid().ToString("N"), true, CopyStacks(stacks));
        activeBySlot[slotId] = army;
        allById[id] = army;
        return army;
    }

    public void SetCurrentDefence(string savedArmyId)
    {
        currentDefenceSavedArmyId = string.IsNullOrEmpty(savedArmyId) ? string.Empty : savedArmyId;
    }

    public void ClearCurrentDefence()
    {
        currentDefenceSavedArmyId = string.Empty;
    }

    private static string SlotIdForIndex(int index)
    {
        return "slot-" + (index + 1).ToString("00");
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

public class InMemorySavedArmiesAttackHistoryStore : ISavedArmiesAttackHistoryStore
{
    private readonly List<SavedArmyAttackHistoryEntry> entries = new List<SavedArmyAttackHistoryEntry>();

    public List<SavedArmyAttackHistoryEntry> ListHistory(string savedArmyId)
    {
        List<SavedArmyAttackHistoryEntry> result = new List<SavedArmyAttackHistoryEntry>();
        if (string.IsNullOrEmpty(savedArmyId))
        {
            return result;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SavedArmyAttackHistoryEntry entry = entries[i];
            if (entry != null && entry.SavedArmyId == savedArmyId)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    public void AddHistory(SavedArmyAttackHistoryEntry entry)
    {
        if (entry != null)
        {
            entries.Add(entry);
        }
    }
}

public class SampleSavedArmiesArenaImportSource : ISavedArmiesArenaImportSource
{
    private readonly SavedArmiesSeedSnapshotSource seedSource = new SavedArmiesSeedSnapshotSource();

    public SampleSavedArmiesArenaImportSource()
    {
    }

    public List<ArenaArmyImportCandidate> ListArenaArmies()
    {
        return SavedArmiesSeedSourceCompatibility.ToArenaCandidates(seedSource.ListSeedArmies());
    }

    public ArenaArmyImportCandidate FindArenaArmy(string arenaArmyId)
    {
        return SavedArmiesSeedSourceCompatibility.ToArenaCandidate(seedSource.FindSeedArmy(arenaArmyId));
    }
}

public class SavedArmiesSeedSnapshotSource : ISavedArmiesSeedSource
{
    private readonly List<SavedArmySeedCandidate> seeds;

    public SavedArmiesSeedSnapshotSource()
    {
        seeds = new List<SavedArmySeedCandidate>
        {
            CreateSeed(
                "seed-army-01",
                "Seed Army A",
                new List<OfflineArmySnapshotStackRecord>
                {
                    new OfflineArmySnapshotStackRecord(0, "Rusher", 64, 0, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "Thrower", 38, 1, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "Tank", 18, 2, true, new List<OfflineArmySnapshotStackSkillRecord>())
                }),
            CreateSeed(
                "seed-army-02",
                "Seed Army B",
                new List<OfflineArmySnapshotStackRecord>
                {
                    new OfflineArmySnapshotStackRecord(0, "Axeman", 42, 0, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "Wisp", 24, 1, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "Fire_Elemental", 9, 2, true, new List<OfflineArmySnapshotStackSkillRecord>())
                }),
            CreateSeed(
                "seed-army-03",
                "Seed Army C",
                new List<OfflineArmySnapshotStackRecord>
                {
                    new OfflineArmySnapshotStackRecord(0, "Trapper", 30, 0, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "Rusher", 36, 1, true, new List<OfflineArmySnapshotStackSkillRecord>()),
                    new OfflineArmySnapshotStackRecord(0, "StoneGolem", 7, 2, true, new List<OfflineArmySnapshotStackSkillRecord>())
                })
        };
    }

    public List<SavedArmySeedCandidate> ListSeedArmies()
    {
        List<SavedArmySeedCandidate> result = new List<SavedArmySeedCandidate>();
        for (int i = 0; i < seeds.Count; i++)
        {
            SavedArmySeedCandidate candidate = seeds[i];
            if (candidate != null)
            {
                result.Add(new SavedArmySeedCandidate(candidate.SeedArmyId, candidate.DisplayName, OfflineArmySnapshotFactory.Clone(candidate.Snapshot)));
            }
        }

        return result;
    }

    public SavedArmySeedCandidate FindSeedArmy(string seedArmyId)
    {
        List<SavedArmySeedCandidate> list = ListSeedArmies();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].SeedArmyId == seedArmyId)
            {
                return list[i];
            }
        }

        return null;
    }

    private static SavedArmySeedCandidate CreateSeed(string seedArmyId, string displayName, List<OfflineArmySnapshotStackRecord> stacks)
    {
        return new SavedArmySeedCandidate(
            seedArmyId,
            displayName,
            OfflineArmySnapshotFactory.Create(1, 0, 0, 0, stacks));
    }
}

public static class SavedArmiesSeedSourceCompatibility
{
    public static List<ArenaArmyImportCandidate> ToArenaCandidates(List<SavedArmySeedCandidate> seeds)
    {
        List<ArenaArmyImportCandidate> result = new List<ArenaArmyImportCandidate>();
        if (seeds == null)
        {
            return result;
        }

        for (int i = 0; i < seeds.Count; i++)
        {
            ArenaArmyImportCandidate candidate = ToArenaCandidate(seeds[i]);
            if (candidate != null)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    public static ArenaArmyImportCandidate ToArenaCandidate(SavedArmySeedCandidate seed)
    {
        if (seed == null)
        {
            return null;
        }

        SavedArmy savedArmy = OfflineArmySnapshotMapper.ToSavedArmy(seed.Snapshot, seed.SeedArmyId);
        return new ArenaArmyImportCandidate(seed.SeedArmyId, seed.DisplayName, CopyStacks(savedArmy == null ? null : savedArmy.Stacks));
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
