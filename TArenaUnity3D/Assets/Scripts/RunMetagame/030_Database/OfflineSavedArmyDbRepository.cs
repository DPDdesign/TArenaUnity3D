using System;
using System.Collections.Generic;
using System.Data;

public sealed class OfflineSavedArmySlotRecord
{
    public int SlotIndex;
    public int SavedArmyId;
    public bool Locked;
}

public sealed class OfflineSavedArmyPersistenceResult
{
    public int SavedArmyId;
    public int SnapshotId;
    public int ReplacedSavedArmyId;
    public bool ClearedCurrentDefence;
}

public sealed class OfflineSavedArmyDbRepository
{
    private const int PhysicalSlotCount = 8;

    private readonly string databasePath;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();

    public OfflineSavedArmyDbRepository(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public List<OfflineSavedArmySlotRecord> LoadSlotRecords(int unlockedSlotCount)
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            EnsurePhysicalSlots(connection, transaction, accountId, unlockedSlotCount);
            List<OfflineSavedArmySlotRecord> slots = QuerySlotRecords(connection, transaction);
            transaction.Commit();
            return slots;
        }
    }

    public SavedArmy LoadActiveArmyInSlot(string slotIdText)
    {
        int slotIndex = OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault(slotIdText, -1);
        if (slotIndex < 0 || slotIndex >= PhysicalSlotCount)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT saved_army_id
FROM saved_army_slots
WHERE slot_index = @slotIndex
  AND is_active = 1
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@slotIndex", slotIndex));

            int savedArmyId = OfflineDatabaseSql.ReadInt(result);
            return savedArmyId <= 0 ? null : LoadActiveArmy(connection, savedArmyId);
        }
    }

    public SavedArmy LoadActiveArmy(string savedArmyIdText)
    {
        int savedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(savedArmyIdText);
        if (savedArmyId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return LoadActiveArmy(connection, savedArmyId);
        }
    }

    public OfflineSavedArmyPersistenceResult SaveSnapshotToSlot(
        string slotIdText,
        OfflineArmySnapshotRecord snapshot,
        int createdFromRunId,
        int unlockedSlotCount)
    {
        int slotIndex = OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault(slotIdText, -1);
        if (slotIndex < 0 || slotIndex >= PhysicalSlotCount || snapshot == null)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            int effectiveUnlockedSlotCount = unlockedSlotCount > 0
                ? unlockedSlotCount
                : LoadUnlockedSlotCount(connection, transaction, accountId);
            EnsurePhysicalSlots(connection, transaction, accountId, effectiveUnlockedSlotCount);
            OfflineSavedArmySlotRecord slot = LoadSlotRecord(connection, transaction, slotIndex);
            if (slot == null || slot.Locked)
            {
                transaction.Rollback();
                return null;
            }

            string now = OfflineDatabaseSql.UtcNowText();
            OfflineArmySnapshotRecord snapshotCopy = OfflineArmySnapshotFactory.Clone(snapshot);
            snapshotCopy.AccountId = accountId;
            snapshotCopy.RunId = Math.Max(0, createdFromRunId);
            snapshotCopy.SavedArmyId = 0;
            snapshotCopy.CreatedAtUtc = string.IsNullOrEmpty(snapshotCopy.CreatedAtUtc) ? now : snapshotCopy.CreatedAtUtc;
            snapshotCopy.IsActive = true;

            int snapshotId = snapshotRepository.SaveSnapshot(connection, transaction, snapshotCopy);
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO saved_armies (
    account_id,
    snapshot_id,
    created_from_run_id,
    active,
    replaced_by_saved_army_id,
    created_at_utc,
    deactivated_at_utc,
    is_active
) VALUES (
    @accountId,
    @snapshotId,
    @createdFromRunId,
    1,
    NULL,
    @createdAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@createdFromRunId", createdFromRunId > 0 ? (object)createdFromRunId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            int savedArmyId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE army_snapshots
SET saved_army_id = @savedArmyId
WHERE snapshot_id = @snapshotId;",
                transaction,
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId),
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

            bool clearedCurrentDefence = false;
            if (slot.SavedArmyId > 0)
            {
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
UPDATE saved_armies
SET active = 0,
    replaced_by_saved_army_id = @replacedBySavedArmyId,
    deactivated_at_utc = @deactivatedAtUtc,
    is_active = 0
WHERE saved_army_id = @savedArmyId;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@replacedBySavedArmyId", savedArmyId),
                    new OfflineDatabaseSqlParameter("@deactivatedAtUtc", now),
                    new OfflineDatabaseSqlParameter("@savedArmyId", slot.SavedArmyId));

                int currentDefenceId = LoadCurrentDefenceSavedArmyId(connection, transaction, accountId);
                if (currentDefenceId == slot.SavedArmyId)
                {
                    UpsertCurrentDefence(connection, transaction, accountId, 0, now);
                    clearedCurrentDefence = true;
                }
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE saved_army_slots
SET saved_army_id = @savedArmyId,
    updated_at_utc = @updatedAtUtc
WHERE slot_index = @slotIndex
  AND account_id = @accountId;",
                transaction,
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
                new OfflineDatabaseSqlParameter("@slotIndex", slotIndex),
                new OfflineDatabaseSqlParameter("@accountId", accountId));

            transaction.Commit();
            return new OfflineSavedArmyPersistenceResult
            {
                SavedArmyId = savedArmyId,
                SnapshotId = snapshotId,
                ReplacedSavedArmyId = slot.SavedArmyId,
                ClearedCurrentDefence = clearedCurrentDefence
            };
        }
    }

    public string LoadCurrentDefenceSavedArmyId()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            int savedArmyId = LoadCurrentDefenceSavedArmyId(connection, transaction, accountId);
            transaction.Commit();
            return savedArmyId <= 0 ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(savedArmyId);
        }
    }

    public void SetCurrentDefence(string savedArmyIdText)
    {
        int savedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(savedArmyIdText);
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            string now = OfflineDatabaseSql.UtcNowText();
            UpsertCurrentDefence(connection, transaction, accountId, savedArmyId, now);
            transaction.Commit();
        }
    }

    public void ClearCurrentDefence()
    {
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            UpsertCurrentDefence(connection, transaction, accountId, 0, OfflineDatabaseSql.UtcNowText());
            transaction.Commit();
        }
    }

    public List<SavedArmyAttackHistoryEntry> ListHistory(string savedArmyIdText)
    {
        int savedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(savedArmyIdText);
        if (savedArmyId <= 0)
        {
            return new List<SavedArmyAttackHistoryEntry>();
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            return OfflineDatabaseSql.Query(
                connection,
                @"
SELECT history_id, result_kind_id, opponent_name, attacker_value_at_battle, defender_value_at_battle
FROM saved_army_history
WHERE saved_army_id = @savedArmyId
  AND is_active = 1
ORDER BY history_id;",
                delegate(IDataRecord row)
                {
                    return new SavedArmyAttackHistoryEntry(
                        "history-" + OfflineDatabaseSql.ReadInt(row["history_id"]).ToString(),
                        OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(savedArmyId),
                        ToBattleResultKind(OfflineDatabaseSql.ReadInt(row["result_kind_id"])),
                        OfflineDatabaseSql.ReadText(row["opponent_name"]),
                        OfflineDatabaseSql.ReadInt(row["attacker_value_at_battle"]),
                        OfflineDatabaseSql.ReadInt(row["defender_value_at_battle"]));
                },
                null,
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId));
        }
    }

    public void AddHistory(SavedArmyAttackHistoryEntry entry)
    {
        int savedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(entry == null ? string.Empty : entry.SavedArmyId);
        if (entry == null || savedArmyId <= 0)
        {
            return;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO saved_army_history (
    saved_army_id,
    async_battle_result_id,
    result_kind_id,
    opponent_name,
    attacker_value_at_battle,
    defender_value_at_battle,
    rank_delta,
    recorded_at_utc,
    is_active
) VALUES (
    @savedArmyId,
    NULL,
    @resultKindId,
    @opponentName,
    @attackerValueAtBattle,
    @defenderValueAtBattle,
    0,
    @recordedAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId),
                new OfflineDatabaseSqlParameter("@resultKindId", ToDbResultKindId(entry.ResultKind)),
                new OfflineDatabaseSqlParameter("@opponentName", entry.OpponentName),
                new OfflineDatabaseSqlParameter("@attackerValueAtBattle", entry.AttackerValueAtBattle),
                new OfflineDatabaseSqlParameter("@defenderValueAtBattle", entry.DefenderValueAtBattle),
                new OfflineDatabaseSqlParameter("@recordedAtUtc", OfflineDatabaseSql.UtcNowText()));
            transaction.Commit();
        }
    }

    private SavedArmy LoadActiveArmy(IDbConnection connection, int savedArmyId)
    {
        List<SavedArmyRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT saved_army_id, snapshot_id
FROM saved_armies
WHERE saved_army_id = @savedArmyId
  AND active = 1
  AND is_active = 1
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new SavedArmyRow
                {
                    SavedArmyId = OfflineDatabaseSql.ReadInt(row["saved_army_id"]),
                    SnapshotId = OfflineDatabaseSql.ReadInt(row["snapshot_id"])
                };
            },
            null,
            new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId));

        if (rows.Count == 0)
        {
            return null;
        }

        OfflineArmySnapshotRecord snapshot = snapshotRepository.LoadSnapshot(connection, rows[0].SnapshotId);
        return OfflineArmySnapshotMapper.ToSavedArmy(snapshot, OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(rows[0].SavedArmyId));
    }

    private static List<OfflineSavedArmySlotRecord> QuerySlotRecords(IDbConnection connection, IDbTransaction transaction)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT slot_index, saved_army_id, locked
FROM saved_army_slots
WHERE is_active = 1
ORDER BY slot_index;",
            delegate(IDataRecord row)
            {
                return new OfflineSavedArmySlotRecord
                {
                    SlotIndex = OfflineDatabaseSql.ReadInt(row["slot_index"]),
                    SavedArmyId = OfflineDatabaseSql.ReadInt(row["saved_army_id"]),
                    Locked = OfflineDatabaseSql.ReadBool(row["locked"])
                };
            },
            transaction);
    }

    private static OfflineSavedArmySlotRecord LoadSlotRecord(IDbConnection connection, IDbTransaction transaction, int slotIndex)
    {
        List<OfflineSavedArmySlotRecord> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT slot_index, saved_army_id, locked
FROM saved_army_slots
WHERE slot_index = @slotIndex
  AND is_active = 1
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new OfflineSavedArmySlotRecord
                {
                    SlotIndex = OfflineDatabaseSql.ReadInt(row["slot_index"]),
                    SavedArmyId = OfflineDatabaseSql.ReadInt(row["saved_army_id"]),
                    Locked = OfflineDatabaseSql.ReadBool(row["locked"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@slotIndex", slotIndex));

        return rows.Count == 0 ? null : rows[0];
    }

    private static void EnsurePhysicalSlots(IDbConnection connection, IDbTransaction transaction, int accountId, int unlockedSlotCount)
    {
        int clampedUnlocked = Math.Max(0, Math.Min(PhysicalSlotCount, unlockedSlotCount));
        string now = OfflineDatabaseSql.UtcNowText();

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_accounts
SET unlocked_saved_army_slots = @unlockedSavedArmySlots,
    updated_at_utc = @updatedAtUtc
WHERE account_id = @accountId;",
            transaction,
            new OfflineDatabaseSqlParameter("@unlockedSavedArmySlots", clampedUnlocked),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
            new OfflineDatabaseSqlParameter("@accountId", accountId));

        for (int slotIndex = 0; slotIndex < PhysicalSlotCount; slotIndex++)
        {
            object existing = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT slot_id
FROM saved_army_slots
WHERE account_id = @accountId
  AND slot_index = @slotIndex
LIMIT 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@slotIndex", slotIndex));

            int locked = slotIndex >= clampedUnlocked ? 1 : 0;
            if (existing == null || existing == DBNull.Value)
            {
                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
INSERT INTO saved_army_slots (
    account_id,
    slot_index,
    saved_army_id,
    locked,
    updated_at_utc,
    is_active
) VALUES (
    @accountId,
    @slotIndex,
    NULL,
    @locked,
    @updatedAtUtc,
    1
);",
                    transaction,
                    new OfflineDatabaseSqlParameter("@accountId", accountId),
                    new OfflineDatabaseSqlParameter("@slotIndex", slotIndex),
                    new OfflineDatabaseSqlParameter("@locked", locked),
                    new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
                continue;
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE saved_army_slots
SET locked = @locked,
    updated_at_utc = @updatedAtUtc,
    is_active = 1
WHERE account_id = @accountId
  AND slot_index = @slotIndex;",
                transaction,
                new OfflineDatabaseSqlParameter("@locked", locked),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@slotIndex", slotIndex));
        }
    }

    private static int LoadCurrentDefenceSavedArmyId(IDbConnection connection, IDbTransaction transaction, int accountId)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT current_defence_saved_army_id
FROM saved_army_roster_state
WHERE account_id = @accountId
  AND is_active = 1
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId));
        return OfflineDatabaseSql.ReadInt(result);
    }

    private static int LoadUnlockedSlotCount(IDbConnection connection, IDbTransaction transaction, int accountId)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT unlocked_saved_army_slots
FROM offline_accounts
WHERE account_id = @accountId
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId));
        return OfflineDatabaseSql.ReadInt(result, PhysicalSlotCount);
    }

    private static void UpsertCurrentDefence(IDbConnection connection, IDbTransaction transaction, int accountId, int savedArmyId, string now)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT account_id
FROM saved_army_roster_state
WHERE account_id = @accountId
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId));

        if (existing == null || existing == DBNull.Value)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO saved_army_roster_state (
    account_id,
    current_defence_saved_army_id,
    updated_at_utc,
    is_active
) VALUES (
    @accountId,
    @savedArmyId,
    @updatedAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId > 0 ? (object)savedArmyId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@updatedAtUtc", now));
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE saved_army_roster_state
SET current_defence_saved_army_id = @savedArmyId,
    updated_at_utc = @updatedAtUtc,
    is_active = 1
WHERE account_id = @accountId;",
            transaction,
            new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId > 0 ? (object)savedArmyId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
            new OfflineDatabaseSqlParameter("@accountId", accountId));
    }

    private static SavedArmyBattleResultKind ToBattleResultKind(int dbResultKindId)
    {
        switch (dbResultKindId)
        {
            case (int)DBResultKindId.OffenceWin:
                return SavedArmyBattleResultKind.OffenceWin;
            case (int)DBResultKindId.OffenceLoss:
                return SavedArmyBattleResultKind.OffenceLoss;
            case (int)DBResultKindId.DefenceWin:
                return SavedArmyBattleResultKind.DefenceWin;
            default:
                return SavedArmyBattleResultKind.DefenceLoss;
        }
    }

    private static int ToDbResultKindId(SavedArmyBattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case SavedArmyBattleResultKind.OffenceWin:
                return (int)DBResultKindId.OffenceWin;
            case SavedArmyBattleResultKind.OffenceLoss:
                return (int)DBResultKindId.OffenceLoss;
            case SavedArmyBattleResultKind.DefenceWin:
                return (int)DBResultKindId.DefenceWin;
            default:
                return (int)DBResultKindId.DefenceLoss;
        }
    }

    private sealed class SavedArmyRow
    {
        public int SavedArmyId;
        public int SnapshotId;
    }
}
