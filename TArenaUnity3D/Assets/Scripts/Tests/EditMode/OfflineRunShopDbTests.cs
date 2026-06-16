using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class OfflineRunShopDbTests
{
    [Test]
    public void RunShopVisitPurchaseAndLeave_PersistAcrossOfflineDatabase()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            TestUnitCatalog units = new TestUnitCatalog();
            DefaultStartRunCatalog catalog = new DefaultStartRunCatalog();
            StartRunService startRunService = new StartRunService(
                catalog,
                catalog,
                units,
                new OfflineStartRunDbStore(databasePath, new DefaultRunMapPathCatalog()));

            StartRunResult startRun = startRunService.BeginRun(new StartRunCommand(
                "offline-player",
                "barbarian-starter",
                "barbarian-starter-v1",
                "barbarian-starter",
                "iron-line"));

            Assert.That(startRun.Success, Is.True);

            RunMapService runMapService = new RunMapService(
                new DefaultRunMapPathCatalog(),
                new OfflineRunMapDbStore(databasePath, new DefaultRunMapPathCatalog()));
            RunMapScreenViewData runMap = runMapService.CreateOrLoad(
                new RunMapCreateRequest(startRun.CreatedRun.RunId, startRun.CreatedRun.RoutePreviewOptionId, startRun.CreatedRun.StartingCurrency, null),
                string.Empty);

            Assert.That(runMap.Paths.Count, Is.EqualTo(4));
            Assert.That(runMapService.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-recovery-1")).Success, Is.True);
            Assert.That(runMapService.Travel(new RunMapTravelCommand(startRun.CreatedRun.RunId, "node-recovery-2")).Success, Is.True);

            RunShopService firstShopService = new RunShopService(
                units,
                new OfflineRunShopDbStore(databasePath, units, new DefaultRunMapPathCatalog()));
            RunShopVisitViewData firstVisit = firstShopService.BuildVisit(
                new RunShopVisitRequest(startRun.CreatedRun.RunId, "node-recovery-2", startRun.CreatedRun.StartingCurrency, CreateShopArmy()),
                "shop-teach-skill");

            Assert.That(firstVisit.VisitId, Does.StartWith("shop-visit-"));
            Assert.That(firstVisit.FocusedOffer, Is.Not.Null);
            Assert.That(firstVisit.FocusedOffer.OfferId, Is.EqualTo("shop-teach-skill"));

            RunShopPurchaseResult purchase = firstShopService.Purchase(new RunShopPurchaseCommand(
                firstVisit.VisitId,
                "shop-teach-skill",
                firstVisit.RunCurrency,
                firstVisit.CurrentArmy));

            Assert.That(purchase.Success, Is.True);

            RunShopService reloadedShopService = new RunShopService(
                units,
                new OfflineRunShopDbStore(databasePath, units, new DefaultRunMapPathCatalog()));
            RunShopVisitViewData reloadedVisit = reloadedShopService.BuildVisit(
                new RunShopVisitRequest(startRun.CreatedRun.RunId, "node-recovery-2", 0, null),
                "shop-teach-skill");

            Assert.That(reloadedVisit.VisitId, Is.EqualTo(firstVisit.VisitId));
            Assert.That(reloadedVisit.RunCurrency, Is.EqualTo(purchase.CurrencyAfterPurchase));
            Assert.That(reloadedVisit.CurrentArmy.SnapshotId, Does.StartWith("snapshot-"));
            Assert.That(reloadedVisit.FocusedOffer.Purchased, Is.True);
            Assert.That(HasUnlockedSkill(FindStack(reloadedVisit.CurrentArmy, "stack-rusher"), "Rush"), Is.True);

            RunShopLeaveResult leave = reloadedShopService.LeaveVisit(new RunShopLeaveCommand(
                reloadedVisit.VisitId,
                reloadedVisit.FocusedOffer == null ? string.Empty : reloadedVisit.FocusedOffer.OfferId));

            Assert.That(leave.Success, Is.True);
            Assert.That(leave.NextScreen, Is.EqualTo("RunMap"));

            AssertPersistedState(
                databasePath,
                startRun.CreatedRun.RunId,
                firstVisit.VisitId,
                purchase.CurrencyAfterPurchase);
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static void AssertPersistedState(string databasePath, string runIdText, string visitIdText, int expectedRunGold)
    {
        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runIdText);
        int visitId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(visitIdText);

        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object nextScreen = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT next_screen FROM offline_runs WHERE run_id = @runId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));
            object runStatusId = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT run_status_id FROM offline_runs WHERE run_id = @runId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));
            object currentRunGold = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT current_run_gold FROM offline_runs WHERE run_id = @runId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));
            object visitStatusId = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT visit_status_id FROM shop_visits WHERE shop_visit_id = @shopVisitId LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@shopVisitId", visitId));
            object purchaseCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM shop_purchases WHERE shop_visit_id = @shopVisitId;",
                null,
                new OfflineDatabaseSqlParameter("@shopVisitId", visitId));
            object purchaseEventCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM run_events WHERE run_id = @runId AND event_type_id = @eventTypeId;",
                null,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@eventTypeId", (int)DBEventTypeId.Purchase));

            Assert.That(OfflineDatabaseSql.ReadText(nextScreen), Is.EqualTo("RunMap"));
            Assert.That(OfflineDatabaseSql.ReadInt(runStatusId), Is.EqualTo((int)DBRunStatusId.InProgress));
            Assert.That(OfflineDatabaseSql.ReadInt(currentRunGold), Is.EqualTo(expectedRunGold));
            Assert.That(OfflineDatabaseSql.ReadInt(visitStatusId), Is.EqualTo((int)DBVisitStatusId.Left));
            Assert.That(OfflineDatabaseSql.ReadInt(purchaseCount), Is.EqualTo(1));
            Assert.That(OfflineDatabaseSql.ReadInt(purchaseEventCount), Is.EqualTo(1));
        }
    }

    private static RunShopArmySnapshot CreateShopArmy()
    {
        List<RunShopStackSnapshot> stacks = new List<RunShopStackSnapshot>
        {
            Stack("stack-rusher", "Rusher", "Rusher", "I", 28, 5, 28 * 31, Skill("Chope", true), Skill("Rush", false)),
            Stack("stack-thrower", "Thrower", "Thrower", "I", 10, 0, 10 * 60, Skill("Range_Stance_Barb", true), Skill("Double_Throw", true), Skill("Axe_Rain", false)),
            Stack("stack-healer", "Healer", "Healer", "I", 5, 2, 5 * 60, Skill("Tough_Skin", true), Skill("Defence_Ritual", false)),
            Stack("stack-wisp", "Wisp", "Wisp", "I", 22, 0, 22 * 6, Skill("Blind_by_light", true), Skill("Unstoppable_Light", false))
        };

        int totalValue = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            totalValue += stacks[i].CombatValue;
        }

        return new RunShopArmySnapshot("shop-entry-army", totalValue, stacks);
    }

    private static RunShopStackSnapshot Stack(
        string stackId,
        string unitId,
        string displayName,
        string tier,
        int amount,
        int lost,
        int value,
        params RunShopSkillState[] skills)
    {
        return new RunShopStackSnapshot(
            stackId,
            unitId,
            displayName,
            tier,
            1,
            amount,
            lost,
            value,
            new List<RunShopSkillState>(skills));
    }

    private static RunShopSkillState Skill(string skillId, bool unlocked)
    {
        return new RunShopSkillState(skillId, unlocked);
    }

    private static RunShopStackSnapshot FindStack(RunShopArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null)
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            if (army.Stacks[i] != null && army.Stacks[i].StackId == stackId)
            {
                return army.Stacks[i];
            }
        }

        return null;
    }

    private static bool HasUnlockedSkill(RunShopStackSnapshot stack, string skillId)
    {
        if (stack == null || stack.Skills == null)
        {
            return false;
        }

        for (int i = 0; i < stack.Skills.Count; i++)
        {
            if (stack.Skills[i] != null && stack.Skills[i].SkillId == skillId && stack.Skills[i].Unlocked)
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_RunShop_" + Guid.NewGuid().ToString("N") + ".db");
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

    private sealed class TestUnitCatalog : IStartRunUnitDefinitionSource, IRunShopUnitDefinitionSource, IOfflineArmySnapshotCatalogResolver
    {
        private readonly Dictionary<string, StartRunUnitDefinition> startRunUnits = new Dictionary<string, StartRunUnitDefinition>
        {
            { "Rusher", StartRunUnit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", StartRunUnit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw", "Axe_Rain") },
            { "Healer", StartRunUnit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", StartRunUnit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", StartRunUnit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap", "Rope_Trap") },
            { "Axeman", StartRunUnit("Axeman", "Axeman", "II", 97, "Slash", "Heavy_Fists") },
            { "StoneGolem", StartRunUnit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw", "Stone_Skin") }
        };

        private readonly Dictionary<string, RunShopUnitDefinition> runShopUnits = new Dictionary<string, RunShopUnitDefinition>
        {
            { "Rusher", RunShopUnit("Rusher", "Rusher", "I", 31, "Chope", "Rush") },
            { "Thrower", RunShopUnit("Thrower", "Thrower", "I", 60, "Range_Stance_Barb", "Double_Throw", "Axe_Rain") },
            { "Healer", RunShopUnit("Healer", "Healer", "I", 60, "Tough_Skin", "Defence_Ritual") },
            { "Wisp", RunShopUnit("Wisp", "Wisp", "I", 6, "Blind_by_light", "Unstoppable_Light") },
            { "Trapper", RunShopUnit("Trapper", "Trapper", "I", 45, "Range_Stance_Lizard", "Spike_Trap", "Rope_Trap") },
            { "Axeman", RunShopUnit("Axeman", "Axeman", "II", 97, "Slash", "Heavy_Fists") },
            { "StoneGolem", RunShopUnit("StoneGolem", "Stone Golem", "II", 67, "Stone_Throw", "Stone_Skin") }
        };

        StartRunUnitDefinition IStartRunUnitDefinitionSource.FindUnit(string unitId)
        {
            StartRunUnitDefinition unit;
            return startRunUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        RunShopUnitDefinition IRunShopUnitDefinitionSource.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            return runShopUnits.TryGetValue(unitId, out unit) ? unit : null;
        }

        OfflineArmySnapshotUnitCatalogEntry IOfflineArmySnapshotCatalogResolver.FindUnit(string unitId)
        {
            RunShopUnitDefinition unit;
            if (!runShopUnits.TryGetValue(unitId, out unit))
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

        private static StartRunUnitDefinition StartRunUnit(string unitId, string displayName, string tier, int cost, params string[] skills)
        {
            return new StartRunUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }

        private static RunShopUnitDefinition RunShopUnit(string unitId, string displayName, string tier, int cost, params string[] skills)
        {
            return new RunShopUnitDefinition(unitId, displayName, tier, cost, new List<string>(skills));
        }
    }
}
