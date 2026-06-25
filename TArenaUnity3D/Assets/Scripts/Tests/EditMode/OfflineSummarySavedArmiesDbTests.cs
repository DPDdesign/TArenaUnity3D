#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class OfflineSummarySavedArmiesDbTests
{
    [Test]
    public void SummaryAndSavedArmyPersistAcrossServiceRecreation()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            CreatedRunRecord createdRun = CreateRun(databasePath, units);

            SummaryValueService firstSummaryService = new SummaryValueService(
                new OfflineSummaryValueDbStore(databasePath, units));
            SummaryValueScreenViewData built = firstSummaryService.BuildSummary(new SummaryValueBuildRequest(
                createdRun.RunId,
                SummaryValueFinalResult.Won,
                CreateSummaryArmy("start-summary", 8, 4),
                CreateSummaryArmy("pre-final-summary", 20, 6),
                CreateSummaryArmy("post-final-summary", 7, 2),
                CreateTimeline(),
                2,
                "slot-01"));

            Assert.That(built.SavedArmyCandidate, Is.Not.Null);
            Assert.That(built.SavedArmyCandidate.ImmutableArmySnapshot.TotalArmyValue, Is.EqualTo(built.PreFinalArmySnapshot.TotalArmyValue));
            Assert.That(built.PostFinalArmySnapshot.TotalArmyValue, Is.Not.EqualTo(built.PreFinalArmySnapshot.TotalArmyValue));

            SummaryValueSaveResult saved = firstSummaryService.Save(new SummaryValueSaveCommand(
                createdRun.RunId,
                built.SavedArmyCandidate.CandidateId,
                "slot-01",
                false));

            Assert.That(saved.Success, Is.True);
            Assert.That(saved.SavedArmyId, Does.StartWith("saved-army-"));

            SummaryValueService reloadedSummaryService = new SummaryValueService(
                new OfflineSummaryValueDbStore(databasePath, units));
            SummaryValueScreenViewData reloaded = reloadedSummaryService.BuildSummary(new SummaryValueBuildRequest(
                createdRun.RunId,
                SummaryValueFinalResult.Pending,
                null,
                null,
                null,
                new List<SummaryValueTimelineEntry>(),
                2,
                "slot-01"));

            Assert.That(reloaded.TimelineEntries.Count, Is.EqualTo(3));
            Assert.That(reloaded.PreFinalArmySnapshot.TotalArmyValue, Is.EqualTo(built.PreFinalArmySnapshot.TotalArmyValue));
            Assert.That(reloaded.PostFinalArmySnapshot.TotalArmyValue, Is.EqualTo(built.PostFinalArmySnapshot.TotalArmyValue));
            Assert.That(reloaded.SaveSlots[0].State, Is.EqualTo(SummaryValueSlotState.Taken));
            Assert.That(reloaded.SaveSlots[2].State, Is.EqualTo(SummaryValueSlotState.Locked));

            AssertPersistedSummary(databasePath, createdRun.RunId, 3);

            OfflineSavedArmiesDbStore rosterStore = new OfflineSavedArmiesDbStore(databasePath);
            SavedArmiesService savedArmiesService = new SavedArmiesService(
                rosterStore,
                new SavedArmiesSeedSnapshotSource(),
                rosterStore,
                units);
            SavedArmiesRosterViewData roster = savedArmiesService.BuildRoster(new SavedArmiesRosterRequest(2, "slot-01", string.Empty));

            Assert.That(roster.SelectedArmy, Is.Not.Null);
            Assert.That(roster.SelectedArmy.SavedArmyId, Is.EqualTo(saved.SavedArmyId));
            Assert.That(FindSavedArmyStack(roster.SelectedArmy, "Rusher").Amount, Is.EqualTo(20));
            Assert.That(FindSavedArmyStack(roster.SelectedArmy, "Thrower").Amount, Is.EqualTo(6));
            Assert.That(roster.CurrentDefenceSavedArmyId, Is.Empty);
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void OverwriteCreatesNewIdentityAndClearsCurrentDefence()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            CreatedRunRecord firstRun = CreateRun(databasePath, units);
            SummaryValueService summaryService = new SummaryValueService(
                new OfflineSummaryValueDbStore(databasePath, units));

            SummaryValueScreenViewData firstSummary = summaryService.BuildSummary(new SummaryValueBuildRequest(
                firstRun.RunId,
                SummaryValueFinalResult.Won,
                CreateSummaryArmy("start-first", 8, 4),
                CreateSummaryArmy("pre-final-first", 16, 5),
                CreateSummaryArmy("post-final-first", 5, 1),
                CreateTimeline(),
                2,
                "slot-01"));
            SummaryValueSaveResult firstSave = summaryService.Save(new SummaryValueSaveCommand(
                firstRun.RunId,
                firstSummary.SavedArmyCandidate.CandidateId,
                "slot-01",
                false));

            OfflineSavedArmiesDbStore rosterStore = new OfflineSavedArmiesDbStore(databasePath);
            SavedArmiesService savedArmiesService = new SavedArmiesService(
                rosterStore,
                new SavedArmiesSeedSnapshotSource(),
                rosterStore,
                units);
            SavedArmyCommandResult defenceResult = savedArmiesService.SetDefence(firstSave.SavedArmyId);
            Assert.That(defenceResult.Success, Is.True);

            CreatedRunRecord secondRun = CreateRun(databasePath, units);
            SummaryValueScreenViewData secondSummary = summaryService.BuildSummary(new SummaryValueBuildRequest(
                secondRun.RunId,
                SummaryValueFinalResult.Won,
                CreateSummaryArmy("start-second", 8, 4),
                CreateSummaryArmy("pre-final-second", 24, 9),
                CreateSummaryArmy("post-final-second", 11, 3),
                CreateTimeline(),
                2,
                "slot-01"));

            SummaryValueSaveResult missingConfirmation = summaryService.Save(new SummaryValueSaveCommand(
                secondRun.RunId,
                secondSummary.SavedArmyCandidate.CandidateId,
                "slot-01",
                false));
            SummaryValueSaveResult overwrite = summaryService.Save(new SummaryValueSaveCommand(
                secondRun.RunId,
                secondSummary.SavedArmyCandidate.CandidateId,
                "slot-01",
                true));

            Assert.That(missingConfirmation.Success, Is.False);
            Assert.That(missingConfirmation.Error, Is.EqualTo(SummaryValueError.MissingConfirmation));
            Assert.That(overwrite.Success, Is.True);
            Assert.That(overwrite.SavedArmyId, Is.Not.EqualTo(firstSave.SavedArmyId));

            SavedArmiesRosterViewData roster = savedArmiesService.BuildRoster(new SavedArmiesRosterRequest(2, "slot-01", string.Empty));
            Assert.That(roster.CurrentDefenceSavedArmyId, Is.Empty);
            Assert.That(roster.SelectedArmy, Is.Not.Null);
            Assert.That(roster.SelectedArmy.SavedArmyId, Is.EqualTo(overwrite.SavedArmyId));
            Assert.That(FindSavedArmyStack(roster.SelectedArmy, "Rusher").Amount, Is.EqualTo(24));
            Assert.That(FindSavedArmyStack(roster.SelectedArmy, "Thrower").Amount, Is.EqualTo(9));

            AssertPersistedOverwrite(databasePath, firstSave.SavedArmyId, overwrite.SavedArmyId);
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static CreatedRunRecord CreateRun(string databasePath, TestUnitCatalog units)
    {
        DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
        StartRunService service = new StartRunService(
            catalog,
            catalog,
            units,
            new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

        StartRunResult result = service.BeginRun(new StartRunCommand(
            "offline-player",
            "barbarian-starter",
            "barbarian-starter-v1",
            "barbarian-starter",
            "iron-line"));

        Assert.That(result.Success, Is.True);
        return result.CreatedRun;
    }

    private static SummaryValueArmySnapshot CreateSummaryArmy(string snapshotId, int rusherAmount, int throwerAmount)
    {
        List<SummaryValueStackSnapshot> stacks = new List<SummaryValueStackSnapshot>
        {
            new SummaryValueStackSnapshot(
                "stack-rusher",
                "Rusher",
                "Rusher",
                "I",
                1,
                rusherAmount,
                rusherAmount * 31,
                new List<SummaryValueSkillState> { new SummaryValueSkillState("Chope", true), new SummaryValueSkillState("Rush", true) }),
            new SummaryValueStackSnapshot(
                "stack-thrower",
                "Thrower",
                "Thrower",
                "I",
                1,
                throwerAmount,
                throwerAmount * 60,
                new List<SummaryValueSkillState> { new SummaryValueSkillState("Range_Stance_Barb", true), new SummaryValueSkillState("Double_Throw", true) })
        };

        return new SummaryValueArmySnapshot(
            snapshotId,
            stacks.Sum(delegate(SummaryValueStackSnapshot stack) { return stack.CombatValue; }),
            stacks);
    }

    private static List<SummaryValueTimelineEntry> CreateTimeline()
    {
        return new List<SummaryValueTimelineEntry>
        {
            new SummaryValueTimelineEntry("summary-entry-1", 0, "Start", "Route selected", 488, 0),
            new SummaryValueTimelineEntry("summary-entry-2", 1, "Battle", "Reward card gained", 920, 40),
            new SummaryValueTimelineEntry("summary-entry-3", 2, "Final", "Victory proof", 980, 60)
        };
    }

    private static SavedArmyStackViewData FindSavedArmyStack(SavedArmyPreviewViewData army, string unitId)
    {
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            SavedArmyStackViewData stack = army.Stacks[i];
            if (stack != null && stack.UnitId == unitId)
            {
                return stack;
            }
        }

        return null;
    }

    private static void AssertPersistedOverwrite(string databasePath, string oldSavedArmyIdText, string newSavedArmyIdText)
    {
        int oldSavedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(oldSavedArmyIdText);
        int newSavedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(newSavedArmyIdText);

        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object slotCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM saved_army_slots;");
            object oldActive = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT active FROM saved_armies WHERE saved_army_id = @savedArmyId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", oldSavedArmyId));
            object oldIsActive = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT is_active FROM saved_armies WHERE saved_army_id = @savedArmyId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", oldSavedArmyId));
            object replacedBy = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT replaced_by_saved_army_id FROM saved_armies WHERE saved_army_id = @savedArmyId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", oldSavedArmyId));
            object slotSavedArmyId = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT saved_army_id FROM saved_army_slots WHERE slot_index = 0 LIMIT 1;");
            object currentDefence = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT current_defence_saved_army_id FROM saved_army_roster_state WHERE account_id = 1 LIMIT 1;");

            Assert.That(OfflineDatabaseSql.ReadInt(slotCount), Is.EqualTo(8));
            Assert.That(OfflineDatabaseSql.ReadInt(oldActive), Is.EqualTo(0));
            Assert.That(OfflineDatabaseSql.ReadInt(oldIsActive), Is.EqualTo(0));
            Assert.That(OfflineDatabaseSql.ReadInt(replacedBy), Is.EqualTo(newSavedArmyId));
            Assert.That(OfflineDatabaseSql.ReadInt(slotSavedArmyId), Is.EqualTo(newSavedArmyId));
            Assert.That(OfflineDatabaseSql.ReadInt(currentDefence), Is.EqualTo(0));
        }
    }

    private static void AssertPersistedSummary(string databasePath, string runIdText, int expectedEntryCount)
    {
        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runIdText);
        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object summaryCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM run_summaries WHERE run_id = @runId AND is_active = 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));
            object entryCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT COUNT(*)
FROM run_summary_entries entries
INNER JOIN run_summaries summaries ON summaries.run_summary_id = entries.run_summary_id
WHERE summaries.run_id = @runId
  AND entries.is_active = 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));

            Assert.That(OfflineDatabaseSql.ReadInt(summaryCount), Is.EqualTo(1));
            Assert.That(OfflineDatabaseSql.ReadInt(entryCount), Is.EqualTo(expectedEntryCount));
        }
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_SummarySavedArmies_" + Guid.NewGuid().ToString("N") + ".db");
    }

    private static void TryDelete(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
        catch
        {
        }
    }

    private sealed class TestUnitCatalog : IStartRunUnitDefinitionSource, IOfflineArmySnapshotCatalogResolver, ISavedArmiesUnitDefinitionSource
    {
        private readonly Dictionary<string, StartRunUnitDefinition> startRunUnits = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", CreateStartRunUnit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", CreateStartRunUnit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw", "Axe_Rain") },
            { "Healer", CreateStartRunUnit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", CreateStartRunUnit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", CreateStartRunUnit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap") },
            { "Specialist", CreateStartRunUnit("Specialist", "Specialist", "II", 129, "Force_Pull", "Stone_Stance") },
            { "StoneGolem", CreateStartRunUnit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw", "Stone_Skin") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return startRunUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        OfflineArmySnapshotUnitCatalogEntry IOfflineArmySnapshotCatalogResolver.FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            if (!startRunUnits.TryGetValue(unitId, out unit))
            {
                return null;
            }

            return new OfflineArmySnapshotUnitCatalogEntry(
                unit.UnitId,
                unit.DisplayName,
                unit.Tier,
                unit.Cost,
                new List<string>(unit.SkillIds));
        }

        SavedArmiesUnitDefinition ISavedArmiesUnitDefinitionSource.FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            if (!startRunUnits.TryGetValue(unitId, out unit))
            {
                return null;
            }

            return new SavedArmiesUnitDefinition(unit.UnitId, unit.DisplayName, unit.Tier, unit.Cost);
        }

        private static StartRunUnitDefinition CreateStartRunUnit(
            string unitId,
            string displayName,
            string tier,
            int cost,
            params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
#endif
