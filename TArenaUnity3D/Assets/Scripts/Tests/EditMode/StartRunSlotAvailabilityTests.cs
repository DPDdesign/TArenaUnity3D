using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;

public class StartRunSlotAvailabilityTests
{
    [Test]
    public void BuildScreen_GeneratesRequestedSlotsAndAppliesDefaultLocks()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = new DeterministicRunGenerationCatalog(units);
        StartRunService service = new StartRunService(
            catalog,
            catalog,
            units,
            new InMemoryStartRunRecordStore(),
            new FixedAvailabilitySource(1, false));

        StartRunScreenViewData screen = service.BuildScreen(string.Empty, string.Empty, 5, "offline-player");

        Assert.That(screen.StartingArmies.Count, Is.EqualTo(5));
        AssertSlot(screen.StartingArmies[0], 0, false, string.Empty);
        AssertSlot(screen.StartingArmies[1], 1, true, StartRunSlotAvailabilityRules.WinRunReason);
        AssertSlot(screen.StartingArmies[2], 2, true, StartRunSlotAvailabilityRules.ReachLevelReason);
        AssertSlot(screen.StartingArmies[3], 3, true, StartRunSlotAvailabilityRules.DemoReason);
        AssertSlot(screen.StartingArmies[4], 4, true, StartRunSlotAvailabilityRules.ComingSoonReason);

        for (int i = 0; i < screen.StartingArmies.Count; i++)
        {
            Assert.That(screen.StartingArmies[i].Stacks.Count, Is.EqualTo(4));
        }
    }

    [Test]
    public void BuildScreen_UnlocksWinAndLevelSlotsWhenProgressAllows()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = new DeterministicRunGenerationCatalog(units);
        StartRunService service = new StartRunService(
            catalog,
            catalog,
            units,
            new InMemoryStartRunRecordStore(),
            new FixedAvailabilitySource(5, true));

        StartRunScreenViewData screen = service.BuildScreen(string.Empty, string.Empty, 4, "offline-player");

        AssertSlot(screen.StartingArmies[0], 0, false, string.Empty);
        AssertSlot(screen.StartingArmies[1], 1, false, string.Empty);
        AssertSlot(screen.StartingArmies[2], 2, false, string.Empty);
        AssertSlot(screen.StartingArmies[3], 3, true, StartRunSlotAvailabilityRules.DemoReason);
    }

    [Test]
    public void BeginRun_RejectsLockedSelectionEvenWhenCalledDirectly()
    {
        FakeUnitPoolSource units = new FakeUnitPoolSource();
        DeterministicRunGenerationCatalog catalog = new DeterministicRunGenerationCatalog(units);
        StartRunService service = new StartRunService(
            catalog,
            catalog,
            units,
            new InMemoryStartRunRecordStore(),
            new FixedAvailabilitySource(1, false));
        StartRunScreenViewData screen = service.BuildScreen(string.Empty, string.Empty, 4, "offline-player");
        StartingArmyOptionViewData lockedSlotTwo = screen.StartingArmies[1];

        StartRunResult result = service.BeginRun(new StartRunCommand(
            "offline-player",
            lockedSlotTwo.TemplateId,
            lockedSlotTwo.VariantId,
            lockedSlotTwo.TemplateId,
            screen.SelectedRoutePreview.RouteId,
            4));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(StartRunValidationError.BlockedRunStart));
        Assert.That(result.Message, Is.EqualTo(StartRunSlotAvailabilityRules.WinRunReason));
        Assert.That(result.CreatedRun, Is.Null);
    }

    [Test]
    public void OfflineAvailabilitySource_ReadsAccountLevelAndWonRunFromDatabase()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            OfflineStartRunSlotAvailabilitySource source = new OfflineStartRunSlotAvailabilitySource(databasePath);

            StartRunSlotAvailabilityContext newAccount = source.LoadAvailabilityContext("offline-player");
            Assert.That(newAccount.AccountLevel, Is.EqualTo(1));
            Assert.That(newAccount.HasWonRun, Is.False);

            SeedAccountProgress(databasePath, 1000, true);

            StartRunSlotAvailabilityContext progressed = source.LoadAvailabilityContext("offline-player");
            Assert.That(progressed.AccountLevel, Is.EqualTo(5));
            Assert.That(progressed.HasWonRun, Is.True);
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static void AssertSlot(
        StartingArmyOptionViewData option,
        int visualSlotIndex,
        bool isLocked,
        string lockedReason)
    {
        Assert.That(option.VisualSlotIndex, Is.EqualTo(visualSlotIndex));
        Assert.That(option.IsLocked, Is.EqualTo(isLocked));
        Assert.That(option.LockedReason, Is.EqualTo(lockedReason));
        Assert.That(option.CanStartRun, Is.EqualTo(!isLocked));
    }

    private static void SeedAccountProgress(string databasePath, int accountXp, bool wonRun)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, null, "offline-player");
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                "UPDATE offline_accounts SET account_xp = @accountXp WHERE account_id = @accountId;",
                null,
                new OfflineDatabaseSqlParameter("@accountXp", accountXp),
                new OfflineDatabaseSqlParameter("@accountId", accountId));

            if (wonRun)
            {
                InsertWonRunSummary(connection, accountId);
            }
        }
    }

    private static void InsertWonRunSummary(IDbConnection connection, int accountId)
    {
        string now = OfflineDatabaseSql.UtcNowText();
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO offline_runs (
    account_id,
    game_mode_id,
    authority_source_id,
    run_status_id,
    starting_army_template_id,
    starting_army_variant_id,
    selected_starting_army_id,
    selected_route_choice_id,
    current_run_gold,
    stage_progress,
    route_progress,
    next_screen,
    created_at_utc,
    updated_at_utc,
    is_active
) VALUES (
    @accountId,
    @gameModeId,
    @authoritySourceId,
    @runStatusId,
    @startingArmyTemplateId,
    @startingArmyVariantId,
    @selectedStartingArmyId,
    @selectedRouteChoiceId,
    0,
    0,
    0,
    @nextScreen,
    @createdAtUtc,
    @updatedAtUtc,
    1
);",
            null,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@gameModeId", (int)DBGameModeId.Offline),
            new OfflineDatabaseSqlParameter("@authoritySourceId", (int)DBAuthoritySourceId.LocalOfflineAdapter),
            new OfflineDatabaseSqlParameter("@runStatusId", (int)DBRunStatusId.Won),
            new OfflineDatabaseSqlParameter("@startingArmyTemplateId", "test-start"),
            new OfflineDatabaseSqlParameter("@startingArmyVariantId", "test-start-v1"),
            new OfflineDatabaseSqlParameter("@selectedStartingArmyId", "test-start"),
            new OfflineDatabaseSqlParameter("@selectedRouteChoiceId", "test-route"),
            new OfflineDatabaseSqlParameter("@nextScreen", "SummaryValue"),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
        int runId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection);

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO run_summaries (
    run_id,
    final_result_id,
    account_xp_awarded,
    next_unlock_preview,
    created_at_utc,
    is_active
) VALUES (
    @runId,
    @finalResultId,
    0,
    @nextUnlockPreview,
    @createdAtUtc,
    1
);",
            null,
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@finalResultId", (int)DBFinalResultId.Won),
            new OfflineDatabaseSqlParameter("@nextUnlockPreview", "Test win"),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now));
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_StartRunSlots_" + Guid.NewGuid().ToString("N") + ".db");
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

    private sealed class FixedAvailabilitySource : IStartRunSlotAvailabilitySource
    {
        private readonly int accountLevel;
        private readonly bool hasWonRun;

        public FixedAvailabilitySource(int accountLevel, bool hasWonRun)
        {
            this.accountLevel = accountLevel;
            this.hasWonRun = hasWonRun;
        }

        public StartRunSlotAvailabilityContext LoadAvailabilityContext(string accountPlayerId)
        {
            return new StartRunSlotAvailabilityContext(accountLevel, hasWonRun);
        }
    }

    private sealed class FakeUnitPoolSource : IStartRunUnitPoolSource
    {
        private readonly Dictionary<string, StartRunUnitDefinition> units = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", Unit("Rusher", "I", "Chope", "Rush") },
            { "Thrower", Unit("Thrower", "I", "Range_Stance_Barb", "Double_Throw") },
            { "Healer", Unit("Healer", "I", "Tough_Skin", "Defence_Ritual") },
            { "Wisp", Unit("Wisp", "I", "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", Unit("Trapper", "I", "Spike_Trap", "Rope_Trap") },
            { "Specialist", Unit("Specialist", "II", "Force_Pull", "Stone_Stance") },
            { "StoneGolem", Unit("StoneGolem", "II", "Stone_Throw", "Stone_Skin") },
            { "StoneLord", Unit("StoneLord", "III", "Stone_Throw", "Stone_Skin") },
            { "LizardMage", Unit("LizardMage", "III", "Spike_Trap", "Force_Pull") }
        };

        public StartRunUnitDefinition FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return units.TryGetValue(unitId, out unit) ? unit : null;
        }

        public List<StartRunUnitDefinition> ListUnits()
        {
            return new List<StartRunUnitDefinition>(units.Values);
        }

        private static StartRunUnitDefinition Unit(string unitId, string tier, params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, unitId, tier, 100, new List<string>(skills));
        }
    }
}
