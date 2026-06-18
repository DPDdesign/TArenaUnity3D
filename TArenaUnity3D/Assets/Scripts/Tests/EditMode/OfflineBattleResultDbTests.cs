using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class OfflineBattleResultDbTests
{
    [Test]
    public void Record_PersistsAcrossAdapterRecreation_AndUpdatesAccountProgress()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            OfflineSavedArmiesDbStore rosterStore = new OfflineSavedArmiesDbStore(databasePath);
            SavedArmy attackerSavedArmy = rosterStore.SaveArmyToSlot("slot-01", new List<SavedArmyStackSnapshot>
            {
                new SavedArmyStackSnapshot("Rusher", 12),
                new SavedArmyStackSnapshot("Thrower", 8)
            });
            SavedArmy defenderSavedArmy = rosterStore.SaveArmyToSlot("slot-02", new List<SavedArmyStackSnapshot>
            {
                new SavedArmyStackSnapshot("Tank", 10),
                new SavedArmyStackSnapshot("Wisp", 14)
            });

            BattleResultService firstService = new BattleResultService(new OfflineBattleResultDbStore(databasePath));
            BattleResultViewData recorded = firstService.Record(new BattleResultRecordRequest(
                "async-battle-result-901",
                BattleResultKind.OffenceWin,
                Army(attackerSavedArmy.SavedArmyId, attackerSavedArmy.SnapshotId, "Attackers", 900, "Rusher", 12, "Thrower", 8),
                Army(defenderSavedArmy.SavedArmyId, defenderSavedArmy.SnapshotId, "Defenders", 1200, "Tank", 10, "Wisp", 14),
                new BattleResultOpponentMetadata("offline-rival-1", "Local Rival", 1040, 1200, true),
                1000,
                460));

            BattleResultService secondService = new BattleResultService(new OfflineBattleResultDbStore(databasePath));
            BattleResultViewData reloaded = secondService.Find(recorded.AsyncBattleResultId);

            Assert.That(recorded.Success, Is.True);
            Assert.That(reloaded, Is.Not.Null);
            Assert.That(reloaded.Success, Is.True);
            Assert.That(reloaded.AsyncBattleResultId, Is.EqualTo("async-battle-result-901"));
            Assert.That(reloaded.AccountXpAfter, Is.EqualTo(recorded.AccountXpAfter));
            Assert.That(reloaded.RankAfter, Is.EqualTo(recorded.RankAfter));
            Assert.That(reloaded.AttackerArmy.DisplayName, Is.EqualTo("Attackers"));
            Assert.That(reloaded.AttackerArmy.Stacks[0].Amount, Is.EqualTo(12));
            Assert.That(reloaded.DefenderArmy.DisplayName, Is.EqualTo("Defenders"));
            Assert.That(reloaded.PreservationRecord.AttackerPreserved, Is.True);
            Assert.That(reloaded.PreservationRecord.DefenderPreserved, Is.True);

            AssertPersistedAccountState(databasePath, recorded.AccountXpAfter, recorded.RankAfter, 3);
            AssertPersistedUnlocks(databasePath);
            AssertPersistedHistory(databasePath, recorded.AsyncBattleResultId, attackerSavedArmy.SavedArmyId, defenderSavedArmy.SavedArmyId);
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    [Test]
    public void Record_UsesIndependentCopiesOfInputArmies()
    {
        string databasePath = BuildTempDatabasePath();
        try
        {
            OfflineSavedArmiesDbStore rosterStore = new OfflineSavedArmiesDbStore(databasePath);
            SavedArmy attackerSavedArmy = rosterStore.SaveArmyToSlot("slot-01", new List<SavedArmyStackSnapshot>
            {
                new SavedArmyStackSnapshot("Rusher", 10)
            });
            SavedArmy defenderSavedArmy = rosterStore.SaveArmyToSlot("slot-02", new List<SavedArmyStackSnapshot>
            {
                new SavedArmyStackSnapshot("Tank", 9)
            });

            BattleResultSavedArmySnapshot attacker = Army(attackerSavedArmy.SavedArmyId, attackerSavedArmy.SnapshotId, "Original Attackers", 700, "Rusher", 10);
            BattleResultSavedArmySnapshot defender = Army(defenderSavedArmy.SavedArmyId, defenderSavedArmy.SnapshotId, "Original Defenders", 880, "Tank", 9);
            BattleResultService service = new BattleResultService(new OfflineBattleResultDbStore(databasePath));

            BattleResultViewData recorded = service.Record(new BattleResultRecordRequest(
                "async-battle-result-902",
                BattleResultKind.OffenceLoss,
                attacker,
                defender,
                new BattleResultOpponentMetadata("offline-rival-2", "Mirror", 980, 880, true),
                1000,
                120));

            attacker.DisplayName = "Mutated Attacker";
            attacker.Stacks[0].Amount = 1;
            attacker.Stacks[0].Skills[0].Unlocked = false;
            defender.DisplayName = "Mutated Defender";
            defender.Stacks[0].Amount = 2;

            BattleResultViewData reloaded = new BattleResultService(new OfflineBattleResultDbStore(databasePath)).Find(recorded.AsyncBattleResultId);

            Assert.That(recorded.AttackerArmy.DisplayName, Is.EqualTo("Original Attackers"));
            Assert.That(recorded.AttackerArmy.Stacks[0].Amount, Is.EqualTo(10));
            Assert.That(recorded.AttackerArmy.Stacks[0].Skills[0].Unlocked, Is.True);
            Assert.That(recorded.DefenderArmy.DisplayName, Is.EqualTo("Original Defenders"));
            Assert.That(recorded.DefenderArmy.Stacks[0].Amount, Is.EqualTo(9));

            Assert.That(reloaded, Is.Not.Null);
            Assert.That(reloaded.AttackerArmy.DisplayName, Is.EqualTo("Original Attackers"));
            Assert.That(reloaded.AttackerArmy.Stacks[0].Amount, Is.EqualTo(10));
            Assert.That(reloaded.AttackerArmy.Stacks[0].Skills[0].Unlocked, Is.True);
            Assert.That(reloaded.DefenderArmy.DisplayName, Is.EqualTo("Original Defenders"));
            Assert.That(reloaded.DefenderArmy.Stacks[0].Amount, Is.EqualTo(9));
        }
        finally
        {
            TryDelete(databasePath);
        }
    }

    private static BattleResultSavedArmySnapshot Army(
        string savedArmyId,
        string snapshotId,
        string displayName,
        int armyValue,
        string firstUnitId,
        int firstAmount,
        string secondUnitId = null,
        int secondAmount = 0)
    {
        List<BattleResultStackSnapshot> stacks = new List<BattleResultStackSnapshot>
        {
            new BattleResultStackSnapshot(
                "stack-" + firstUnitId.ToLowerInvariant(),
                firstUnitId,
                firstUnitId,
                firstAmount,
                armyValue / Math.Max(1, secondUnitId == null ? 1 : 2),
                new List<BattleResultSkillState> { new BattleResultSkillState("Skill-" + firstUnitId, true) })
        };

        if (!string.IsNullOrEmpty(secondUnitId))
        {
            stacks.Add(new BattleResultStackSnapshot(
                "stack-" + secondUnitId.ToLowerInvariant(),
                secondUnitId,
                secondUnitId,
                secondAmount,
                armyValue / 2,
                new List<BattleResultSkillState> { new BattleResultSkillState("Skill-" + secondUnitId, true) }));
        }

        return new BattleResultSavedArmySnapshot(savedArmyId, snapshotId, displayName, armyValue, stacks);
    }

    private static void AssertPersistedAccountState(string databasePath, int expectedXp, int expectedRank, int expectedUnlockedSlots)
    {
        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object xp = OfflineDatabaseSql.ExecuteScalar(connection, "SELECT account_xp FROM offline_accounts WHERE account_id = 1 LIMIT 1;");
            object rank = OfflineDatabaseSql.ExecuteScalar(connection, "SELECT rank_value FROM offline_accounts WHERE account_id = 1 LIMIT 1;");
            object unlockedSlots = OfflineDatabaseSql.ExecuteScalar(connection, "SELECT unlocked_saved_army_slots FROM offline_accounts WHERE account_id = 1 LIMIT 1;");

            Assert.That(OfflineDatabaseSql.ReadInt(xp), Is.EqualTo(expectedXp));
            Assert.That(OfflineDatabaseSql.ReadInt(rank), Is.EqualTo(expectedRank));
            Assert.That(OfflineDatabaseSql.ReadInt(unlockedSlots), Is.EqualTo(expectedUnlockedSlots));
        }
    }

    private static void AssertPersistedUnlocks(string databasePath)
    {
        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object slotUnlockType = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT unlock_type_id FROM account_unlocks WHERE account_id = 1 AND target_id = 'slot-03' LIMIT 1;");
            object skillUnlockType = OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT unlock_type_id FROM account_unlocks WHERE account_id = 1 AND target_id = 'skill-rush' LIMIT 1;");

            Assert.That(OfflineDatabaseSql.ReadInt(slotUnlockType), Is.EqualTo((int)DBUnlockTypeId.SavedArmySlot));
            Assert.That(OfflineDatabaseSql.ReadInt(skillUnlockType), Is.EqualTo((int)DBUnlockTypeId.Skill));
        }
    }

    private static void AssertPersistedHistory(
        string databasePath,
        string asyncBattleResultIdText,
        string attackerSavedArmyIdText,
        string defenderSavedArmyIdText)
    {
        int asyncBattleResultId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(asyncBattleResultIdText);
        int attackerSavedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(attackerSavedArmyIdText);
        int defenderSavedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(defenderSavedArmyIdText);

        using (System.Data.IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object attackerHistoryCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT COUNT(*)
FROM saved_army_history
WHERE saved_army_id = @savedArmyId
  AND async_battle_result_id = @asyncBattleResultId
  AND is_active = 1;",
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", attackerSavedArmyId),
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId));
            object defenderHistoryCount = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT COUNT(*)
FROM saved_army_history
WHERE saved_army_id = @savedArmyId
  AND async_battle_result_id = @asyncBattleResultId
  AND is_active = 1;",
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", defenderSavedArmyId),
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId));

            Assert.That(OfflineDatabaseSql.ReadInt(attackerHistoryCount), Is.EqualTo(1));
            Assert.That(OfflineDatabaseSql.ReadInt(defenderHistoryCount), Is.EqualTo(1));
        }
    }

    private static string BuildTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "TArenaOffline_BattleResult_" + Guid.NewGuid().ToString("N") + ".db");
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
}
