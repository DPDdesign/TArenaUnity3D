using System.Collections.Generic;
using NUnit.Framework;

public class SavedArmiesServiceTests
{
    [Test]
    public void BuildRoster_DefaultRequestListsEightUnlockedSlots()
    {
        SavedArmiesService service = CreateService(new InMemorySavedArmiesRosterStore(), new SavedArmiesSeedSnapshotSource(), new InMemorySavedArmiesAttackHistoryStore());

        SavedArmiesRosterViewData view = service.BuildRoster(new SavedArmiesRosterRequest(8, "slot-01", "seed-army-01"));

        Assert.That(view.Slots.Count, Is.EqualTo(8));
        Assert.That(view.Slots[0].State, Is.EqualTo(SavedArmySlotState.Empty));
        Assert.That(view.Slots[7].Selectable, Is.True);
        Assert.That(view.ArenaArmies.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void BuildRoster_LockedSlotsRemainRepresentedForFutureUnlocks()
    {
        SavedArmiesService service = CreateService(new InMemorySavedArmiesRosterStore(), new SavedArmiesSeedSnapshotSource(), new InMemorySavedArmiesAttackHistoryStore());

        SavedArmiesRosterViewData view = service.BuildRoster(new SavedArmiesRosterRequest(2, "slot-03", "seed-army-01"));

        Assert.That(view.Slots.Count, Is.EqualTo(8));
        Assert.That(view.Slots[0].State, Is.EqualTo(SavedArmySlotState.Empty));
        Assert.That(view.Slots[1].State, Is.EqualTo(SavedArmySlotState.Empty));
        Assert.That(view.Slots[2].State, Is.EqualTo(SavedArmySlotState.Locked));
        Assert.That(view.Slots[2].Selectable, Is.False);
    }

    [Test]
    public void LoadSeedArmy_CopiesDataIntoNewSavedArmyIdentity()
    {
        MutableSeedSource seedSource = new MutableSeedSource();
        InMemorySavedArmiesRosterStore rosterStore = new InMemorySavedArmiesRosterStore();
        SavedArmiesService service = CreateService(rosterStore, seedSource, new InMemorySavedArmiesAttackHistoryStore());

        SavedArmyCommandResult result = service.LoadSeedArmy(new SavedArmyImportCommand("slot-01", "seed-source", 8, false));
        seedSource.Candidate.Snapshot.Stacks[0].Amount = 999;
        SavedArmiesRosterViewData view = service.BuildRoster(new SavedArmiesRosterRequest(8, "slot-01", "seed-source"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.SavedArmyId, Is.Not.EqualTo("seed-source"));
        Assert.That(view.SelectedArmy.SavedArmyId, Is.EqualTo(result.SavedArmyId));
        Assert.That(view.SelectedArmy.Stacks[0].Amount, Is.EqualTo(10));
    }

    [Test]
    public void LoadSeedArmyIntoTakenSlot_RequiresConfirmationAndClearsCurrentDefenceOnOverwrite()
    {
        InMemorySavedArmiesRosterStore rosterStore = new InMemorySavedArmiesRosterStore();
        SavedArmiesService service = CreateService(rosterStore, new SavedArmiesSeedSnapshotSource(), new InMemorySavedArmiesAttackHistoryStore());
        SavedArmyCommandResult firstImport = service.LoadSeedArmy(new SavedArmyImportCommand("slot-01", "seed-army-01", 8, false));
        service.SetDefence(firstImport.SavedArmyId);

        SavedArmyCommandResult missingConfirm = service.LoadSeedArmy(new SavedArmyImportCommand("slot-01", "seed-army-02", 8, false));
        SavedArmyCommandResult overwrite = service.LoadSeedArmy(new SavedArmyImportCommand("slot-01", "seed-army-02", 8, true));

        Assert.That(firstImport.Success, Is.True);
        Assert.That(missingConfirm.Success, Is.False);
        Assert.That(missingConfirm.Error, Is.EqualTo(SavedArmiesError.MissingConfirmation));
        Assert.That(overwrite.Success, Is.True);
        Assert.That(overwrite.SavedArmyId, Is.Not.EqualTo(firstImport.SavedArmyId));
        Assert.That(rosterStore.CurrentDefenceSavedArmyId, Is.Empty);
    }

    [Test]
    public void SetDefence_AllowsOnlyValidTakenPositiveValueArmy()
    {
        InMemorySavedArmiesRosterStore rosterStore = new InMemorySavedArmiesRosterStore();
        SavedArmiesService service = CreateService(rosterStore, new SavedArmiesSeedSnapshotSource(), new InMemorySavedArmiesAttackHistoryStore());

        SavedArmyCommandResult missingArmy = service.SetDefence("missing-army");
        SavedArmyCommandResult importResult = service.LoadSeedArmy(new SavedArmyImportCommand("slot-01", "seed-army-01", 8, false));
        SavedArmyCommandResult defenceResult = service.SetDefence(importResult.SavedArmyId);

        Assert.That(missingArmy.Success, Is.False);
        Assert.That(missingArmy.Error, Is.EqualTo(SavedArmiesError.InvalidArmy));
        Assert.That(importResult.Success, Is.True);
        Assert.That(defenceResult.Success, Is.True);
        Assert.That(rosterStore.CurrentDefenceSavedArmyId, Is.EqualTo(importResult.SavedArmyId));
    }

    [Test]
    public void History_IsQueriedBySavedArmyIdNotSlot()
    {
        InMemorySavedArmiesAttackHistoryStore historyStore = new InMemorySavedArmiesAttackHistoryStore();
        historyStore.AddHistory(new SavedArmyAttackHistoryEntry("h1", "saved-a", SavedArmyBattleResultKind.OffenceWin, "A", 100, 90));
        historyStore.AddHistory(new SavedArmyAttackHistoryEntry("h2", "saved-b", SavedArmyBattleResultKind.DefenceLoss, "B", 80, 100));
        InMemorySavedArmiesRosterStore rosterStore = new InMemorySavedArmiesRosterStore();
        rosterStore.SaveArmyToSlot("slot-01", new List<SavedArmyStackSnapshot> { new SavedArmyStackSnapshot("Rusher", 10) });
        rosterStore.SaveArmyToSlot("slot-02", new List<SavedArmyStackSnapshot> { new SavedArmyStackSnapshot("Thrower", 10) });
        SavedArmy slotOne = rosterStore.FindActiveArmyInSlot("slot-01");
        SavedArmy slotTwo = rosterStore.FindActiveArmyInSlot("slot-02");
        historyStore.AddHistory(new SavedArmyAttackHistoryEntry("h3", slotOne.SavedArmyId, SavedArmyBattleResultKind.DefenceWin, "C", 100, 80));
        historyStore.AddHistory(new SavedArmyAttackHistoryEntry("h4", slotTwo.SavedArmyId, SavedArmyBattleResultKind.OffenceLoss, "D", 80, 120));
        SavedArmiesService service = CreateService(rosterStore, new SavedArmiesSeedSnapshotSource(), historyStore);

        SavedArmiesRosterViewData first = service.BuildRoster(new SavedArmiesRosterRequest(8, "slot-01", "seed-army-01"));
        SavedArmiesRosterViewData second = service.BuildRoster(new SavedArmiesRosterRequest(8, "slot-02", "seed-army-01"));

        Assert.That(first.AttackHistory.Count, Is.EqualTo(1));
        Assert.That(first.AttackHistory[0].EntryId, Is.EqualTo("h3"));
        Assert.That(second.AttackHistory.Count, Is.EqualTo(1));
        Assert.That(second.AttackHistory[0].EntryId, Is.EqualTo("h4"));
    }

    private static SavedArmiesService CreateService(ISavedArmiesRosterStore rosterStore, ISavedArmiesSeedSource seedSource, ISavedArmiesAttackHistoryStore historyStore)
    {
        return new SavedArmiesService(
            rosterStore,
            seedSource,
            historyStore,
            new TestUnitSource());
    }

    private class TestUnitSource : ISavedArmiesUnitDefinitionSource
    {
        public SavedArmiesUnitDefinition FindUnit(string unitId)
        {
            return new SavedArmiesUnitDefinition(unitId, unitId, "I", 10);
        }
    }

    private class MutableSeedSource : ISavedArmiesSeedSource
    {
        public readonly SavedArmySeedCandidate Candidate = new SavedArmySeedCandidate(
            "seed-source",
            "Mutable Seed",
            OfflineArmySnapshotFactory.Create(
                1,
                0,
                0,
                0,
                new List<OfflineArmySnapshotStackRecord>
                {
                    new OfflineArmySnapshotStackRecord(0, "Rusher", 10, 0, true, new List<OfflineArmySnapshotStackSkillRecord>())
                }));

        public List<SavedArmySeedCandidate> ListSeedArmies()
        {
            return new List<SavedArmySeedCandidate> { Candidate };
        }

        public SavedArmySeedCandidate FindSeedArmy(string seedArmyId)
        {
            return seedArmyId == Candidate.SeedArmyId ? Candidate : null;
        }
    }
}
