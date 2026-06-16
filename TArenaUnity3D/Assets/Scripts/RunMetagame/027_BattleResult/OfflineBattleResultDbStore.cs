using System;
using System.Data;
using UnityEngine;

public sealed class OfflineBattleResultDbStore : IBattleResultStore
{
    private readonly string databasePath;

    public OfflineBattleResultDbStore(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public void Save(BattleResultViewData result)
    {
        if (result == null || !result.Success || string.IsNullOrEmpty(result.AsyncBattleResultId))
        {
            return;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            EnsureDetailTable(connection, transaction);
            int accountId = OfflineDatabaseAccountBootstrap.EnsureDefaultAccount(connection, transaction, "offline-player");
            int attackerSavedArmyId = EnsureSavedArmyAnchor(connection, transaction, accountId, result.AttackerArmy);
            int defenderSavedArmyId = EnsureSavedArmyAnchor(connection, transaction, accountId, result.DefenderArmy);
            int asyncBattleResultId = UpsertResult(connection, transaction, accountId, attackerSavedArmyId, defenderSavedArmyId, result);

            UpsertDetails(connection, transaction, asyncBattleResultId, result);
            UpdateOfflineAccount(connection, transaction, accountId, result);
            SyncUnlockProgress(connection, transaction, accountId, result.AccountXpAfter);
            DeactivateExistingHistory(connection, transaction, asyncBattleResultId);
            InsertHistoryIfApplicable(connection, transaction, attackerSavedArmyId, asyncBattleResultId, result, true);
            InsertHistoryIfApplicable(connection, transaction, defenderSavedArmyId, asyncBattleResultId, result, false);

            transaction.Commit();
        }
    }

    public BattleResultViewData Find(string asyncBattleResultIdText)
    {
        int asyncBattleResultId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(asyncBattleResultIdText);
        if (asyncBattleResultId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            EnsureDetailTable(connection, transaction);
            BattleResultRow row = LoadRow(connection, transaction, asyncBattleResultId);
            transaction.Commit();
            return row == null ? null : BuildResult(row);
        }
    }

    private static void EnsureDetailTable(IDbConnection connection, IDbTransaction transaction)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
CREATE TABLE IF NOT EXISTS async_battle_result_details (
    async_battle_result_id INTEGER PRIMARY KEY,
    attacker_army_json TEXT NOT NULL,
    defender_army_json TEXT NOT NULL,
    opponent_json TEXT,
    preservation_json TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (async_battle_result_id) REFERENCES async_battle_results(async_battle_result_id)
);",
            transaction);
    }

    private static int EnsureSavedArmyAnchor(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        BattleResultSavedArmySnapshot army)
    {
        int requestedSavedArmyId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SavedArmyId);
        if (requestedSavedArmyId > 0 && SavedArmyExists(connection, transaction, requestedSavedArmyId))
        {
            return requestedSavedArmyId;
        }

        OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
        OfflineArmySnapshotRecord snapshot = OfflineArmySnapshotMapper.FromBattleResult(army, accountId, requestedSavedArmyId);
        snapshot.AccountId = accountId;
        snapshot.RunId = 0;
        snapshot.NodeId = 0;
        snapshot.CreatedAtUtc = OfflineDatabaseSql.UtcNowText();
        int snapshotId = snapshotRepository.SaveSnapshot(connection, transaction, snapshot);
        string now = OfflineDatabaseSql.UtcNowText();

        if (requestedSavedArmyId > 0)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO saved_armies (
    saved_army_id,
    account_id,
    snapshot_id,
    created_from_run_id,
    active,
    replaced_by_saved_army_id,
    created_at_utc,
    deactivated_at_utc,
    is_active
) VALUES (
    @savedArmyId,
    @accountId,
    @snapshotId,
    NULL,
    1,
    NULL,
    @createdAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@savedArmyId", requestedSavedArmyId),
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
        }
        else
        {
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
    NULL,
    1,
    NULL,
    @createdAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@snapshotId", snapshotId),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            requestedSavedArmyId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE army_snapshots
SET saved_army_id = @savedArmyId
WHERE snapshot_id = @snapshotId;",
            transaction,
            new OfflineDatabaseSqlParameter("@savedArmyId", requestedSavedArmyId),
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));

        return requestedSavedArmyId;
    }

    private static int UpsertResult(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        int attackerSavedArmyId,
        int defenderSavedArmyId,
        BattleResultViewData result)
    {
        int requestedResultId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(result.AsyncBattleResultId);
        bool hasExisting = requestedResultId > 0 && ResultExists(connection, transaction, requestedResultId);

        if (!hasExisting && requestedResultId > 0)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO async_battle_results (
    async_battle_result_id,
    account_id,
    attacker_saved_army_id,
    defender_saved_army_id,
    opponent_id,
    opponent_name,
    result_kind_id,
    rank_before,
    rank_after,
    rank_delta,
    account_xp_before,
    account_xp_after,
    account_xp_gained,
    next_unlock_preview,
    preservation_record,
    result_source,
    recorded_at_utc,
    is_active
) VALUES (
    @asyncBattleResultId,
    @accountId,
    @attackerSavedArmyId,
    @defenderSavedArmyId,
    @opponentId,
    @opponentName,
    @resultKindId,
    @rankBefore,
    @rankAfter,
    @rankDelta,
    @accountXpBefore,
    @accountXpAfter,
    @accountXpGained,
    @nextUnlockPreview,
    @preservationRecord,
    @resultSource,
    @recordedAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", requestedResultId),
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@attackerSavedArmyId", attackerSavedArmyId),
                new OfflineDatabaseSqlParameter("@defenderSavedArmyId", defenderSavedArmyId),
                new OfflineDatabaseSqlParameter("@opponentId", result.Opponent == null ? string.Empty : result.Opponent.OpponentId),
                new OfflineDatabaseSqlParameter("@opponentName", result.Opponent == null ? string.Empty : result.Opponent.DisplayName),
                new OfflineDatabaseSqlParameter("@resultKindId", ToDbResultKindId(result.ResultKind)),
                new OfflineDatabaseSqlParameter("@rankBefore", result.RankBefore),
                new OfflineDatabaseSqlParameter("@rankAfter", result.RankAfter),
                new OfflineDatabaseSqlParameter("@rankDelta", result.RankDelta),
                new OfflineDatabaseSqlParameter("@accountXpBefore", result.AccountXpBefore),
                new OfflineDatabaseSqlParameter("@accountXpAfter", result.AccountXpAfter),
                new OfflineDatabaseSqlParameter("@accountXpGained", result.AccountXpGained),
                new OfflineDatabaseSqlParameter("@nextUnlockPreview", result.NextUnlockPreview),
                new OfflineDatabaseSqlParameter("@preservationRecord", result.PreservationRecord == null ? string.Empty : result.PreservationRecord.Message),
                new OfflineDatabaseSqlParameter("@resultSource", result.AuthoritySource.ToString()),
                new OfflineDatabaseSqlParameter("@recordedAtUtc", OfflineDatabaseSql.UtcNowText()));
            return requestedResultId;
        }

        if (hasExisting)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE async_battle_results
SET account_id = @accountId,
    attacker_saved_army_id = @attackerSavedArmyId,
    defender_saved_army_id = @defenderSavedArmyId,
    opponent_id = @opponentId,
    opponent_name = @opponentName,
    result_kind_id = @resultKindId,
    rank_before = @rankBefore,
    rank_after = @rankAfter,
    rank_delta = @rankDelta,
    account_xp_before = @accountXpBefore,
    account_xp_after = @accountXpAfter,
    account_xp_gained = @accountXpGained,
    next_unlock_preview = @nextUnlockPreview,
    preservation_record = @preservationRecord,
    result_source = @resultSource,
    recorded_at_utc = @recordedAtUtc,
    is_active = 1
WHERE async_battle_result_id = @asyncBattleResultId;",
                transaction,
                new OfflineDatabaseSqlParameter("@accountId", accountId),
                new OfflineDatabaseSqlParameter("@attackerSavedArmyId", attackerSavedArmyId),
                new OfflineDatabaseSqlParameter("@defenderSavedArmyId", defenderSavedArmyId),
                new OfflineDatabaseSqlParameter("@opponentId", result.Opponent == null ? string.Empty : result.Opponent.OpponentId),
                new OfflineDatabaseSqlParameter("@opponentName", result.Opponent == null ? string.Empty : result.Opponent.DisplayName),
                new OfflineDatabaseSqlParameter("@resultKindId", ToDbResultKindId(result.ResultKind)),
                new OfflineDatabaseSqlParameter("@rankBefore", result.RankBefore),
                new OfflineDatabaseSqlParameter("@rankAfter", result.RankAfter),
                new OfflineDatabaseSqlParameter("@rankDelta", result.RankDelta),
                new OfflineDatabaseSqlParameter("@accountXpBefore", result.AccountXpBefore),
                new OfflineDatabaseSqlParameter("@accountXpAfter", result.AccountXpAfter),
                new OfflineDatabaseSqlParameter("@accountXpGained", result.AccountXpGained),
                new OfflineDatabaseSqlParameter("@nextUnlockPreview", result.NextUnlockPreview),
                new OfflineDatabaseSqlParameter("@preservationRecord", result.PreservationRecord == null ? string.Empty : result.PreservationRecord.Message),
                new OfflineDatabaseSqlParameter("@resultSource", result.AuthoritySource.ToString()),
                new OfflineDatabaseSqlParameter("@recordedAtUtc", OfflineDatabaseSql.UtcNowText()),
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", requestedResultId));
            return requestedResultId;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO async_battle_results (
    account_id,
    attacker_saved_army_id,
    defender_saved_army_id,
    opponent_id,
    opponent_name,
    result_kind_id,
    rank_before,
    rank_after,
    rank_delta,
    account_xp_before,
    account_xp_after,
    account_xp_gained,
    next_unlock_preview,
    preservation_record,
    result_source,
    recorded_at_utc,
    is_active
) VALUES (
    @accountId,
    @attackerSavedArmyId,
    @defenderSavedArmyId,
    @opponentId,
    @opponentName,
    @resultKindId,
    @rankBefore,
    @rankAfter,
    @rankDelta,
    @accountXpBefore,
    @accountXpAfter,
    @accountXpGained,
    @nextUnlockPreview,
    @preservationRecord,
    @resultSource,
    @recordedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@attackerSavedArmyId", attackerSavedArmyId),
            new OfflineDatabaseSqlParameter("@defenderSavedArmyId", defenderSavedArmyId),
            new OfflineDatabaseSqlParameter("@opponentId", result.Opponent == null ? string.Empty : result.Opponent.OpponentId),
            new OfflineDatabaseSqlParameter("@opponentName", result.Opponent == null ? string.Empty : result.Opponent.DisplayName),
            new OfflineDatabaseSqlParameter("@resultKindId", ToDbResultKindId(result.ResultKind)),
            new OfflineDatabaseSqlParameter("@rankBefore", result.RankBefore),
            new OfflineDatabaseSqlParameter("@rankAfter", result.RankAfter),
            new OfflineDatabaseSqlParameter("@rankDelta", result.RankDelta),
            new OfflineDatabaseSqlParameter("@accountXpBefore", result.AccountXpBefore),
            new OfflineDatabaseSqlParameter("@accountXpAfter", result.AccountXpAfter),
            new OfflineDatabaseSqlParameter("@accountXpGained", result.AccountXpGained),
            new OfflineDatabaseSqlParameter("@nextUnlockPreview", result.NextUnlockPreview),
            new OfflineDatabaseSqlParameter("@preservationRecord", result.PreservationRecord == null ? string.Empty : result.PreservationRecord.Message),
            new OfflineDatabaseSqlParameter("@resultSource", result.AuthoritySource.ToString()),
            new OfflineDatabaseSqlParameter("@recordedAtUtc", OfflineDatabaseSql.UtcNowText()));
        return (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
    }

    private static void UpsertDetails(IDbConnection connection, IDbTransaction transaction, int asyncBattleResultId, BattleResultViewData result)
    {
        bool hasExisting = OfflineDatabaseSql.ReadInt(
            OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM async_battle_result_details WHERE async_battle_result_id = @asyncBattleResultId;",
                transaction,
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId))) > 0;

        string attackerJson = JsonUtility.ToJson(result.AttackerArmy ?? new BattleResultSavedArmySnapshot(string.Empty, string.Empty, string.Empty, 0, new System.Collections.Generic.List<BattleResultStackSnapshot>()));
        string defenderJson = JsonUtility.ToJson(result.DefenderArmy ?? new BattleResultSavedArmySnapshot(string.Empty, string.Empty, string.Empty, 0, new System.Collections.Generic.List<BattleResultStackSnapshot>()));
        string opponentJson = result.Opponent == null ? string.Empty : JsonUtility.ToJson(result.Opponent);
        string preservationJson = result.PreservationRecord == null ? string.Empty : JsonUtility.ToJson(result.PreservationRecord);

        if (!hasExisting)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO async_battle_result_details (
    async_battle_result_id,
    attacker_army_json,
    defender_army_json,
    opponent_json,
    preservation_json,
    is_active
) VALUES (
    @asyncBattleResultId,
    @attackerArmyJson,
    @defenderArmyJson,
    @opponentJson,
    @preservationJson,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId),
                new OfflineDatabaseSqlParameter("@attackerArmyJson", attackerJson),
                new OfflineDatabaseSqlParameter("@defenderArmyJson", defenderJson),
                new OfflineDatabaseSqlParameter("@opponentJson", opponentJson),
                new OfflineDatabaseSqlParameter("@preservationJson", preservationJson));
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE async_battle_result_details
SET attacker_army_json = @attackerArmyJson,
    defender_army_json = @defenderArmyJson,
    opponent_json = @opponentJson,
    preservation_json = @preservationJson,
    is_active = 1
WHERE async_battle_result_id = @asyncBattleResultId;",
            transaction,
            new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId),
            new OfflineDatabaseSqlParameter("@attackerArmyJson", attackerJson),
            new OfflineDatabaseSqlParameter("@defenderArmyJson", defenderJson),
            new OfflineDatabaseSqlParameter("@opponentJson", opponentJson),
            new OfflineDatabaseSqlParameter("@preservationJson", preservationJson));
    }

    private static void UpdateOfflineAccount(IDbConnection connection, IDbTransaction transaction, int accountId, BattleResultViewData result)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_accounts
SET account_xp = @accountXp,
    rank_value = @rankValue,
    updated_at_utc = @updatedAtUtc,
    is_active = 1
WHERE account_id = @accountId;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountXp", result.AccountXpAfter),
            new OfflineDatabaseSqlParameter("@rankValue", result.RankAfter),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@accountId", accountId));
    }

    private static void SyncUnlockProgress(IDbConnection connection, IDbTransaction transaction, int accountId, int totalXp)
    {
        UpsertUnlock(connection, transaction, accountId, 250, DBUnlockTypeId.Skill, "skill-rush");
        UpsertUnlock(connection, transaction, accountId, 500, DBUnlockTypeId.SavedArmySlot, "slot-03");
        UpsertUnlock(connection, transaction, accountId, 1000, DBUnlockTypeId.Unit, "unit-specialist");
        UpsertUnlock(connection, transaction, accountId, 1500, DBUnlockTypeId.Map, "map-iron-line-hard");

        int unlockedSlots = 2;
        if (totalXp >= 500)
        {
            unlockedSlots++;
        }

        if (totalXp >= 1000)
        {
            unlockedSlots++;
        }

        if (totalXp >= 1500)
        {
            unlockedSlots++;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_accounts
SET unlocked_saved_army_slots = @unlockedSavedArmySlots,
    updated_at_utc = @updatedAtUtc
WHERE account_id = @accountId;",
            transaction,
            new OfflineDatabaseSqlParameter("@unlockedSavedArmySlots", Math.Max(2, Math.Min(8, unlockedSlots))),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@accountId", accountId));
    }

    private static void UpsertUnlock(
        IDbConnection connection,
        IDbTransaction transaction,
        int accountId,
        int requiredXp,
        DBUnlockTypeId unlockTypeId,
        string targetId)
    {
        object accountXpValue = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT account_xp FROM offline_accounts WHERE account_id = @accountId LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId));
        int accountXp = OfflineDatabaseSql.ReadInt(accountXpValue);
        if (accountXp < requiredXp)
        {
            return;
        }

        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT unlock_id
FROM account_unlocks
WHERE account_id = @accountId
  AND unlock_type_id = @unlockTypeId
  AND target_id = @targetId
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@unlockTypeId", (int)unlockTypeId),
            new OfflineDatabaseSqlParameter("@targetId", targetId));

        if (existing != null && existing != DBNull.Value)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE account_unlocks
SET is_active = 1
WHERE unlock_id = @unlockId;",
                transaction,
                new OfflineDatabaseSqlParameter("@unlockId", OfflineDatabaseSql.ReadInt(existing)));
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO account_unlocks (
    account_id,
    unlock_type_id,
    target_id,
    unlocked_at_utc,
    is_active
) VALUES (
    @accountId,
    @unlockTypeId,
    @targetId,
    @unlockedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@accountId", accountId),
            new OfflineDatabaseSqlParameter("@unlockTypeId", (int)unlockTypeId),
            new OfflineDatabaseSqlParameter("@targetId", targetId),
            new OfflineDatabaseSqlParameter("@unlockedAtUtc", OfflineDatabaseSql.UtcNowText()));
    }

    private static void InsertHistoryIfApplicable(
        IDbConnection connection,
        IDbTransaction transaction,
        int savedArmyId,
        int asyncBattleResultId,
        BattleResultViewData result,
        bool attackerPerspective)
    {
        if (savedArmyId <= 0)
        {
            return;
        }

        SavedArmyBattleResultKind resultKind = attackerPerspective
            ? ToAttackerHistoryKind(result.ResultKind)
            : ToDefenderHistoryKind(result.ResultKind);
        string opponentName = attackerPerspective
            ? result.DefenderArmy == null ? string.Empty : result.DefenderArmy.DisplayName
            : result.AttackerArmy == null ? string.Empty : result.AttackerArmy.DisplayName;
        int attackerValue = attackerPerspective
            ? result.AttackerArmy == null ? 0 : result.AttackerArmy.ArmyValue
            : result.DefenderArmy == null ? 0 : result.DefenderArmy.ArmyValue;
        int defenderValue = attackerPerspective
            ? result.DefenderArmy == null ? 0 : result.DefenderArmy.ArmyValue
            : result.AttackerArmy == null ? 0 : result.AttackerArmy.ArmyValue;

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
    @asyncBattleResultId,
    @resultKindId,
    @opponentName,
    @attackerValueAtBattle,
    @defenderValueAtBattle,
    @rankDelta,
    @recordedAtUtc,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId),
            new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId),
            new OfflineDatabaseSqlParameter("@resultKindId", ToDbSavedArmyHistoryKindId(resultKind)),
            new OfflineDatabaseSqlParameter("@opponentName", opponentName),
            new OfflineDatabaseSqlParameter("@attackerValueAtBattle", attackerValue),
            new OfflineDatabaseSqlParameter("@defenderValueAtBattle", defenderValue),
            new OfflineDatabaseSqlParameter("@rankDelta", result.RankDelta),
            new OfflineDatabaseSqlParameter("@recordedAtUtc", OfflineDatabaseSql.UtcNowText()));
    }

    private static void DeactivateExistingHistory(IDbConnection connection, IDbTransaction transaction, int asyncBattleResultId)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE saved_army_history
SET is_active = 0
WHERE async_battle_result_id = @asyncBattleResultId;",
            transaction,
            new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId));
    }

    private static BattleResultRow LoadRow(IDbConnection connection, IDbTransaction transaction, int asyncBattleResultId)
    {
        System.Collections.Generic.List<BattleResultRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT result.async_battle_result_id,
       result.opponent_id,
       result.opponent_name,
       result.result_kind_id,
       result.rank_before,
       result.rank_after,
       result.rank_delta,
       result.account_xp_before,
       result.account_xp_after,
       result.account_xp_gained,
       result.next_unlock_preview,
       result.result_source,
       details.attacker_army_json,
       details.defender_army_json,
       details.opponent_json,
       details.preservation_json
FROM async_battle_results result
LEFT JOIN async_battle_result_details details ON details.async_battle_result_id = result.async_battle_result_id
WHERE result.async_battle_result_id = @asyncBattleResultId
  AND result.is_active = 1
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new BattleResultRow
                {
                    AsyncBattleResultId = OfflineDatabaseSql.ReadInt(row["async_battle_result_id"]),
                    OpponentId = OfflineDatabaseSql.ReadText(row["opponent_id"]),
                    OpponentName = OfflineDatabaseSql.ReadText(row["opponent_name"]),
                    ResultKindId = OfflineDatabaseSql.ReadInt(row["result_kind_id"]),
                    RankBefore = OfflineDatabaseSql.ReadInt(row["rank_before"]),
                    RankAfter = OfflineDatabaseSql.ReadInt(row["rank_after"]),
                    RankDelta = OfflineDatabaseSql.ReadInt(row["rank_delta"]),
                    AccountXpBefore = OfflineDatabaseSql.ReadInt(row["account_xp_before"]),
                    AccountXpAfter = OfflineDatabaseSql.ReadInt(row["account_xp_after"]),
                    AccountXpGained = OfflineDatabaseSql.ReadInt(row["account_xp_gained"]),
                    NextUnlockPreview = OfflineDatabaseSql.ReadText(row["next_unlock_preview"]),
                    ResultSource = OfflineDatabaseSql.ReadText(row["result_source"], BattleResultAuthoritySource.LocalOfflineAdapter.ToString()),
                    AttackerArmyJson = OfflineDatabaseSql.ReadText(row["attacker_army_json"]),
                    DefenderArmyJson = OfflineDatabaseSql.ReadText(row["defender_army_json"]),
                    OpponentJson = OfflineDatabaseSql.ReadText(row["opponent_json"]),
                    PreservationJson = OfflineDatabaseSql.ReadText(row["preservation_json"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId));

        return rows.Count == 0 ? null : rows[0];
    }

    private static BattleResultViewData BuildResult(BattleResultRow row)
    {
        BattleResultSavedArmySnapshot attacker = DeserializeOrDefault<BattleResultSavedArmySnapshot>(row.AttackerArmyJson);
        BattleResultSavedArmySnapshot defender = DeserializeOrDefault<BattleResultSavedArmySnapshot>(row.DefenderArmyJson);
        BattleResultOpponentMetadata opponent = DeserializeOrDefault<BattleResultOpponentMetadata>(row.OpponentJson);
        if (opponent == null && (!string.IsNullOrEmpty(row.OpponentId) || !string.IsNullOrEmpty(row.OpponentName)))
        {
            opponent = new BattleResultOpponentMetadata(row.OpponentId, row.OpponentName, 0, defender == null ? 0 : defender.ArmyValue, true);
        }

        BattleResultPreservationRecord preservation = DeserializeOrDefault<BattleResultPreservationRecord>(row.PreservationJson);
        if (preservation == null)
        {
            preservation = new BattleResultPreservationRecord(
                attacker == null ? string.Empty : attacker.SavedArmyId,
                defender == null ? string.Empty : defender.SavedArmyId,
                true,
                true,
                "No saved army is stolen, destroyed, or edited by this result.");
        }

        BattleResultAuthoritySource authoritySource;
        if (!Enum.TryParse(row.ResultSource, out authoritySource))
        {
            authoritySource = BattleResultAuthoritySource.LocalOfflineAdapter;
        }

        return new BattleResultViewData(
            OfflineDatabaseLegacyIdentity.ToLegacyAsyncBattleResultId(row.AsyncBattleResultId),
            BattleResultGameMode.Offline,
            authoritySource,
            ToBattleResultKind(row.ResultKindId),
            attacker,
            defender,
            opponent,
            row.RankBefore,
            row.RankAfter,
            row.RankDelta,
            row.AccountXpBefore,
            row.AccountXpGained,
            row.AccountXpAfter,
            row.NextUnlockPreview,
            preservation,
            true,
            BattleResultError.None,
            "Offline async battle result recorded.",
            BattleResultAccountProgress.FromTotalXp(row.AccountXpAfter, row.NextUnlockPreview));
    }

    private static T DeserializeOrDefault<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private static bool SavedArmyExists(IDbConnection connection, IDbTransaction transaction, int savedArmyId)
    {
        return OfflineDatabaseSql.ReadInt(
            OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM saved_armies WHERE saved_army_id = @savedArmyId LIMIT 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@savedArmyId", savedArmyId))) > 0;
    }

    private static bool ResultExists(IDbConnection connection, IDbTransaction transaction, int asyncBattleResultId)
    {
        return OfflineDatabaseSql.ReadInt(
            OfflineDatabaseSql.ExecuteScalar(
                connection,
                "SELECT COUNT(*) FROM async_battle_results WHERE async_battle_result_id = @asyncBattleResultId LIMIT 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@asyncBattleResultId", asyncBattleResultId))) > 0;
    }

    private static int ToDbResultKindId(BattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case BattleResultKind.OffenceWin:
                return (int)DBResultKindId.OffenceWin;
            case BattleResultKind.OffenceLoss:
                return (int)DBResultKindId.OffenceLoss;
            case BattleResultKind.DefenceWin:
                return (int)DBResultKindId.DefenceWin;
            default:
                return (int)DBResultKindId.DefenceLoss;
        }
    }

    private static BattleResultKind ToBattleResultKind(int dbResultKindId)
    {
        switch (dbResultKindId)
        {
            case (int)DBResultKindId.OffenceWin:
                return BattleResultKind.OffenceWin;
            case (int)DBResultKindId.OffenceLoss:
                return BattleResultKind.OffenceLoss;
            case (int)DBResultKindId.DefenceWin:
                return BattleResultKind.DefenceWin;
            default:
                return BattleResultKind.DefenceLoss;
        }
    }

    private static SavedArmyBattleResultKind ToAttackerHistoryKind(BattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case BattleResultKind.OffenceWin:
                return SavedArmyBattleResultKind.OffenceWin;
            case BattleResultKind.OffenceLoss:
                return SavedArmyBattleResultKind.OffenceLoss;
            case BattleResultKind.DefenceWin:
                return SavedArmyBattleResultKind.OffenceLoss;
            default:
                return SavedArmyBattleResultKind.OffenceWin;
        }
    }

    private static SavedArmyBattleResultKind ToDefenderHistoryKind(BattleResultKind resultKind)
    {
        switch (resultKind)
        {
            case BattleResultKind.OffenceWin:
                return SavedArmyBattleResultKind.DefenceLoss;
            case BattleResultKind.OffenceLoss:
                return SavedArmyBattleResultKind.DefenceWin;
            case BattleResultKind.DefenceWin:
                return SavedArmyBattleResultKind.DefenceWin;
            default:
                return SavedArmyBattleResultKind.DefenceLoss;
        }
    }

    private static int ToDbSavedArmyHistoryKindId(SavedArmyBattleResultKind resultKind)
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

    private sealed class BattleResultRow
    {
        public int AsyncBattleResultId;
        public string OpponentId;
        public string OpponentName;
        public int ResultKindId;
        public int RankBefore;
        public int RankAfter;
        public int RankDelta;
        public int AccountXpBefore;
        public int AccountXpAfter;
        public int AccountXpGained;
        public string NextUnlockPreview;
        public string ResultSource;
        public string AttackerArmyJson;
        public string DefenderArmyJson;
        public string OpponentJson;
        public string PreservationJson;
    }
}
