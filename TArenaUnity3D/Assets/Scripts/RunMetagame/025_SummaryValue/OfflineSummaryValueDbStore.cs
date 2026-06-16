using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public sealed class SummaryValuePersistedState
{
    public SummaryValueBuildRequest Request;
    public int UnlockedSlotCount;
}

public class OfflineSummaryValueDbStore : ISummaryValueRosterStore, ISummaryValuePersistenceStore
{
    private readonly string databasePath;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
    private readonly OfflineSavedArmyDbRepository savedArmyRepository;

    public OfflineSummaryValueDbStore(string databasePath, IOfflineArmySnapshotCatalogResolver resolver)
    {
        this.databasePath = databasePath;
        this.resolver = resolver;
        savedArmyRepository = new OfflineSavedArmyDbRepository(databasePath);
    }

    public SummaryValuePersistedState PersistAndLoad(SummaryValueBuildRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.RunId))
        {
            return null;
        }

        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(request.RunId);
        if (runId <= 0)
        {
            throw new InvalidOperationException("Summary DB store requires a persisted run id.");
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            RunRow run = LoadRun(connection, transaction, runId);
            int unlockedSlotCount = request.UnlockedSlotCount > 0
                ? Math.Max(0, Math.Min(8, request.UnlockedSlotCount))
                : Math.Max(0, Math.Min(8, run.UnlockedSavedArmySlots));

            if (HasPersistableData(request))
            {
                int startSnapshotId = ResolveOrSaveSnapshot(connection, transaction, request.StartArmySnapshot, run.AccountId, runId);
                int preFinalSnapshotId = ResolveOrSaveSnapshot(connection, transaction, request.PreFinalArmySnapshot, run.AccountId, runId);
                int postFinalSnapshotId = ResolveOrSaveSnapshot(connection, transaction, request.PostFinalArmySnapshot, run.AccountId, runId);
                int candidateSnapshotId = request.FinalResult == SummaryValueFinalResult.Won ? preFinalSnapshotId : 0;
                string now = OfflineDatabaseSql.UtcNowText();
                int summaryId = UpsertSummary(
                    connection,
                    transaction,
                    runId,
                    startSnapshotId,
                    preFinalSnapshotId,
                    postFinalSnapshotId,
                    candidateSnapshotId,
                    request.FinalResult,
                    request.FinalResult == SummaryValueFinalResult.Won ? 100 : 0,
                    request.FinalResult == SummaryValueFinalResult.Won
                        ? "Next unlock: saved army slot progress"
                        : "No final victory reward",
                    now);

                ReplaceEntries(connection, transaction, summaryId, request.TimelineEntries);

                OfflineDatabaseSql.ExecuteNonQuery(
                    connection,
                    @"
UPDATE offline_runs
SET pre_final_army_snapshot_id = @preFinalArmySnapshotId,
    current_army_snapshot_id = @currentArmySnapshotId,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
                    transaction,
                    new OfflineDatabaseSqlParameter("@preFinalArmySnapshotId", preFinalSnapshotId > 0 ? (object)preFinalSnapshotId : DBNull.Value),
                    new OfflineDatabaseSqlParameter("@currentArmySnapshotId", postFinalSnapshotId > 0 ? (object)postFinalSnapshotId : DBNull.Value),
                    new OfflineDatabaseSqlParameter("@updatedAtUtc", now),
                    new OfflineDatabaseSqlParameter("@runId", runId));
            }

            SummaryValuePersistedState state = LoadPersistedState(connection, transaction, runId, unlockedSlotCount, request.SelectedSlotId);
            transaction.Commit();
            return state;
        }
    }

    public List<SummaryValueSaveSlotViewData> ListSlots(int unlockedSlotCount, string selectedSlotId)
    {
        List<OfflineSavedArmySlotRecord> records = savedArmyRepository.LoadSlotRecords(unlockedSlotCount);
        List<SummaryValueSaveSlotViewData> slots = new List<SummaryValueSaveSlotViewData>();

        for (int i = 0; i < records.Count; i++)
        {
            OfflineSavedArmySlotRecord record = records[i];
            string slotId = OfflineDatabaseLegacyIdentity.ToLegacySlotId(record.SlotIndex);
            string savedArmyId = record.SavedArmyId <= 0 ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(record.SavedArmyId);
            SavedArmy army = string.IsNullOrEmpty(savedArmyId) ? null : savedArmyRepository.LoadActiveArmy(savedArmyId);
            SummaryValueSlotState state = record.Locked ? SummaryValueSlotState.Locked : army == null ? SummaryValueSlotState.Empty : SummaryValueSlotState.Taken;
            int armyValue = CalculateSavedArmyValue(army);

            slots.Add(new SummaryValueSaveSlotViewData(
                slotId,
                record.SlotIndex + 1,
                state,
                savedArmyId,
                !record.Locked,
                slotId == selectedSlotId,
                armyValue));
        }

        return slots;
    }

    public string SaveCandidate(string slotId, SummaryValueSavedArmyCandidate candidate)
    {
        if (candidate == null || candidate.ImmutableArmySnapshot == null)
        {
            return string.Empty;
        }

        int createdFromRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(candidate.CreatedFromRunId);
        OfflineArmySnapshotRecord snapshot = OfflineArmySnapshotMapper.FromSummaryValue(candidate.ImmutableArmySnapshot, 0, createdFromRunId);
        OfflineSavedArmyPersistenceResult result = savedArmyRepository.SaveSnapshotToSlot(slotId, snapshot, createdFromRunId, 0);
        return result == null ? string.Empty : OfflineDatabaseLegacyIdentity.ToLegacySavedArmyId(result.SavedArmyId);
    }

    public SummaryValueSavedArmyCandidate FindCandidate(string candidateId)
    {
        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(candidateId);
        if (runId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            List<SummaryRow> rows = OfflineDatabaseSql.Query(
                connection,
                @"
SELECT run_id, saved_army_candidate_snapshot_id
FROM run_summaries
WHERE run_id = @runId
  AND is_active = 1
LIMIT 1;",
                delegate(IDataRecord row)
                {
                    return new SummaryRow
                    {
                        RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                        SavedArmyCandidateSnapshotId = OfflineDatabaseSql.ReadInt(row["saved_army_candidate_snapshot_id"])
                    };
                },
                null,
                new OfflineDatabaseSqlParameter("@runId", runId));

            if (rows.Count == 0 || rows[0].SavedArmyCandidateSnapshotId <= 0)
            {
                return null;
            }

            OfflineArmySnapshotRecord snapshot = snapshotRepository.LoadSnapshot(connection, rows[0].SavedArmyCandidateSnapshotId);
            SummaryValueArmySnapshot army = OfflineArmySnapshotMapper.ToSummaryValue(snapshot, resolver);
            return new SummaryValueSavedArmyCandidate(
                BuildCandidateId(OfflineDatabaseLegacyIdentity.ToLegacyRunId(rows[0].RunId)),
                OfflineDatabaseLegacyIdentity.ToLegacyRunId(rows[0].RunId),
                army == null ? string.Empty : army.SnapshotId,
                army,
                army == null ? 0 : army.TotalArmyValue);
        }
    }

    private SummaryValuePersistedState LoadPersistedState(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int unlockedSlotCount,
        string selectedSlotId)
    {
        List<SummaryRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT run_summary_id, run_id, final_result_id, start_snapshot_id, pre_final_snapshot_id, post_final_snapshot_id,
       saved_army_candidate_snapshot_id, account_xp_awarded, next_unlock_preview
FROM run_summaries
WHERE run_id = @runId
  AND is_active = 1
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new SummaryRow
                {
                    RunSummaryId = OfflineDatabaseSql.ReadInt(row["run_summary_id"]),
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    FinalResultId = OfflineDatabaseSql.ReadInt(row["final_result_id"]),
                    StartSnapshotId = OfflineDatabaseSql.ReadInt(row["start_snapshot_id"]),
                    PreFinalSnapshotId = OfflineDatabaseSql.ReadInt(row["pre_final_snapshot_id"]),
                    PostFinalSnapshotId = OfflineDatabaseSql.ReadInt(row["post_final_snapshot_id"]),
                    SavedArmyCandidateSnapshotId = OfflineDatabaseSql.ReadInt(row["saved_army_candidate_snapshot_id"]),
                    AccountXpAwarded = OfflineDatabaseSql.ReadInt(row["account_xp_awarded"]),
                    NextUnlockPreview = OfflineDatabaseSql.ReadText(row["next_unlock_preview"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        if (rows.Count == 0)
        {
            return new SummaryValuePersistedState
            {
                UnlockedSlotCount = unlockedSlotCount,
                Request = new SummaryValueBuildRequest(
                    OfflineDatabaseLegacyIdentity.ToLegacyRunId(runId),
                    SummaryValueFinalResult.Pending,
                    null,
                    null,
                    null,
                    new List<SummaryValueTimelineEntry>(),
                    unlockedSlotCount,
                    selectedSlotId)
            };
        }

        SummaryRow row = rows[0];
        SummaryValueArmySnapshot start = LoadSummaryArmy(connection, row.StartSnapshotId);
        SummaryValueArmySnapshot preFinal = LoadSummaryArmy(connection, row.PreFinalSnapshotId);
        SummaryValueArmySnapshot postFinal = LoadSummaryArmy(connection, row.PostFinalSnapshotId);
        List<SummaryValueTimelineEntry> entries = LoadEntries(connection, row.RunSummaryId);

        return new SummaryValuePersistedState
        {
            UnlockedSlotCount = unlockedSlotCount,
            Request = new SummaryValueBuildRequest(
                OfflineDatabaseLegacyIdentity.ToLegacyRunId(row.RunId),
                ToFinalResult(row.FinalResultId),
                start,
                preFinal,
                postFinal,
                entries,
                unlockedSlotCount,
                selectedSlotId)
        };
    }

    private int ResolveOrSaveSnapshot(
        IDbConnection connection,
        IDbTransaction transaction,
        SummaryValueArmySnapshot snapshot,
        int accountId,
        int runId)
    {
        int snapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(snapshot == null ? string.Empty : snapshot.SnapshotId);
        if (snapshotId > 0)
        {
            OfflineArmySnapshotRecord existing = snapshotRepository.LoadSnapshot(connection, snapshotId, transaction);
            if (existing != null)
            {
                return snapshotId;
            }
        }

        if (snapshot == null)
        {
            return 0;
        }

        return snapshotRepository.SaveSnapshot(
            connection,
            transaction,
            OfflineArmySnapshotMapper.FromSummaryValue(snapshot, accountId, runId));
    }

    private static int UpsertSummary(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int startSnapshotId,
        int preFinalSnapshotId,
        int postFinalSnapshotId,
        int savedArmyCandidateSnapshotId,
        SummaryValueFinalResult finalResult,
        int accountXpAwarded,
        string nextUnlockPreview,
        string now)
    {
        object existing = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT run_summary_id
FROM run_summaries
WHERE run_id = @runId
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        if (existing == null || existing == DBNull.Value)
        {
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_summaries (
    run_id,
    final_result_id,
    start_snapshot_id,
    pre_final_snapshot_id,
    post_final_snapshot_id,
    saved_army_candidate_snapshot_id,
    account_xp_awarded,
    next_unlock_preview,
    created_at_utc,
    is_active
) VALUES (
    @runId,
    @finalResultId,
    @startSnapshotId,
    @preFinalSnapshotId,
    @postFinalSnapshotId,
    @savedArmyCandidateSnapshotId,
    @accountXpAwarded,
    @nextUnlockPreview,
    @createdAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@finalResultId", ToDbFinalResultId(finalResult)),
                new OfflineDatabaseSqlParameter("@startSnapshotId", startSnapshotId > 0 ? (object)startSnapshotId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@preFinalSnapshotId", preFinalSnapshotId > 0 ? (object)preFinalSnapshotId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@postFinalSnapshotId", postFinalSnapshotId > 0 ? (object)postFinalSnapshotId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@savedArmyCandidateSnapshotId", savedArmyCandidateSnapshotId > 0 ? (object)savedArmyCandidateSnapshotId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@accountXpAwarded", accountXpAwarded),
                new OfflineDatabaseSqlParameter("@nextUnlockPreview", nextUnlockPreview),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            return (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);
        }

        int runSummaryId = OfflineDatabaseSql.ReadInt(existing);
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE run_summaries
SET final_result_id = @finalResultId,
    start_snapshot_id = @startSnapshotId,
    pre_final_snapshot_id = @preFinalSnapshotId,
    post_final_snapshot_id = @postFinalSnapshotId,
    saved_army_candidate_snapshot_id = @savedArmyCandidateSnapshotId,
    account_xp_awarded = @accountXpAwarded,
    next_unlock_preview = @nextUnlockPreview,
    created_at_utc = @createdAtUtc,
    is_active = 1
WHERE run_summary_id = @runSummaryId;",
            transaction,
            new OfflineDatabaseSqlParameter("@finalResultId", ToDbFinalResultId(finalResult)),
            new OfflineDatabaseSqlParameter("@startSnapshotId", startSnapshotId > 0 ? (object)startSnapshotId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@preFinalSnapshotId", preFinalSnapshotId > 0 ? (object)preFinalSnapshotId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@postFinalSnapshotId", postFinalSnapshotId > 0 ? (object)postFinalSnapshotId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@savedArmyCandidateSnapshotId", savedArmyCandidateSnapshotId > 0 ? (object)savedArmyCandidateSnapshotId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@accountXpAwarded", accountXpAwarded),
            new OfflineDatabaseSqlParameter("@nextUnlockPreview", nextUnlockPreview),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now),
            new OfflineDatabaseSqlParameter("@runSummaryId", runSummaryId));
        return runSummaryId;
    }

    private static void ReplaceEntries(IDbConnection connection, IDbTransaction transaction, int runSummaryId, List<SummaryValueTimelineEntry> entries)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE run_summary_entries
SET is_active = 0
WHERE run_summary_id = @runSummaryId;",
            transaction,
            new OfflineDatabaseSqlParameter("@runSummaryId", runSummaryId));

        if (entries == null)
        {
            return;
        }

        int previousGold = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            SummaryValueTimelineEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            int goldDelta = i == 0 ? entry.RunGoldAfterStage : entry.RunGoldAfterStage - previousGold;
            previousGold = entry.RunGoldAfterStage;

            SummaryEntryPayload payload = new SummaryEntryPayload
            {
                ReceivedText = entry.ReceivedText,
                ArmyValueAfterStage = entry.ArmyValueAfterStage,
                RunGoldAfterStage = entry.RunGoldAfterStage
            };

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_summary_entries (
    run_summary_id,
    entry_type_id,
    title_id,
    detail_id,
    run_gold_delta,
    snapshot_id,
    sort_order,
    is_active
) VALUES (
    @runSummaryId,
    @entryTypeId,
    @titleId,
    @detailId,
    @runGoldDelta,
    NULL,
    @sortOrder,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runSummaryId", runSummaryId),
                new OfflineDatabaseSqlParameter("@entryTypeId", 1),
                new OfflineDatabaseSqlParameter("@titleId", entry.Label),
                new OfflineDatabaseSqlParameter("@detailId", JsonUtility.ToJson(payload)),
                new OfflineDatabaseSqlParameter("@runGoldDelta", goldDelta),
                new OfflineDatabaseSqlParameter("@sortOrder", entry.StageIndex));
        }
    }

    private List<SummaryValueTimelineEntry> LoadEntries(IDbConnection connection, int runSummaryId)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT summary_entry_id, title_id, detail_id, sort_order
FROM run_summary_entries
WHERE run_summary_id = @runSummaryId
  AND is_active = 1
ORDER BY sort_order, summary_entry_id;",
            delegate(IDataRecord row)
            {
                SummaryEntryPayload payload = ParsePayload(OfflineDatabaseSql.ReadText(row["detail_id"]));
                return new SummaryValueTimelineEntry(
                    "summary-entry-" + OfflineDatabaseSql.ReadInt(row["summary_entry_id"]).ToString(),
                    OfflineDatabaseSql.ReadInt(row["sort_order"]),
                    OfflineDatabaseSql.ReadText(row["title_id"]),
                    payload.ReceivedText,
                    payload.ArmyValueAfterStage,
                    payload.RunGoldAfterStage);
            },
            null,
            new OfflineDatabaseSqlParameter("@runSummaryId", runSummaryId));
    }

    private SummaryValueArmySnapshot LoadSummaryArmy(IDbConnection connection, int snapshotId)
    {
        if (snapshotId <= 0)
        {
            return null;
        }

        OfflineArmySnapshotRecord snapshot = snapshotRepository.LoadSnapshot(connection, snapshotId);
        return snapshot == null ? null : OfflineArmySnapshotMapper.ToSummaryValue(snapshot, resolver);
    }

    private int CalculateSavedArmyValue(SavedArmy army)
    {
        if (army == null || army.Stacks == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < army.Stacks.Count; i++)
        {
            SavedArmyStackSnapshot stack = army.Stacks[i];
            OfflineArmySnapshotUnitCatalogEntry unit = resolver == null || stack == null ? null : resolver.FindUnit(stack.UnitId);
            total += (stack == null ? 0 : stack.Amount) * (unit == null ? 0 : unit.CombatValue);
        }

        return Math.Max(0, total);
    }

    private static bool HasPersistableData(SummaryValueBuildRequest request)
    {
        return request != null && (
            request.StartArmySnapshot != null ||
            request.PreFinalArmySnapshot != null ||
            request.PostFinalArmySnapshot != null ||
            (request.TimelineEntries != null && request.TimelineEntries.Count > 0));
    }

    private static RunRow LoadRun(IDbConnection connection, IDbTransaction transaction, int runId)
    {
        List<RunRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT runs.account_id, accounts.unlocked_saved_army_slots
FROM offline_runs runs
INNER JOIN offline_accounts accounts ON accounts.account_id = runs.account_id
WHERE runs.run_id = @runId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new RunRow
                {
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    UnlockedSavedArmySlots = OfflineDatabaseSql.ReadInt(row["unlocked_saved_army_slots"], 8)
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Offline run was not found for summary persistence.");
        }

        return rows[0];
    }

    private static SummaryValueFinalResult ToFinalResult(int dbFinalResultId)
    {
        switch (dbFinalResultId)
        {
            case (int)DBFinalResultId.Won:
                return SummaryValueFinalResult.Won;
            case (int)DBFinalResultId.Lost:
                return SummaryValueFinalResult.Lost;
            default:
                return SummaryValueFinalResult.Pending;
        }
    }

    private static int ToDbFinalResultId(SummaryValueFinalResult finalResult)
    {
        switch (finalResult)
        {
            case SummaryValueFinalResult.Won:
                return (int)DBFinalResultId.Won;
            case SummaryValueFinalResult.Lost:
                return (int)DBFinalResultId.Lost;
            default:
                return (int)DBFinalResultId.Pending;
        }
    }

    private static string BuildCandidateId(string runId)
    {
        return "saved-candidate-" + (string.IsNullOrEmpty(runId) ? "run-unsaved" : runId);
    }

    private static SummaryEntryPayload ParsePayload(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new SummaryEntryPayload();
        }

        SummaryEntryPayload payload = JsonUtility.FromJson<SummaryEntryPayload>(text);
        if (payload != null)
        {
            return payload;
        }

        return new SummaryEntryPayload
        {
            ReceivedText = text,
            ArmyValueAfterStage = 0,
            RunGoldAfterStage = 0
        };
    }

    [Serializable]
    private sealed class SummaryEntryPayload
    {
        public string ReceivedText;
        public int ArmyValueAfterStage;
        public int RunGoldAfterStage;
    }

    private sealed class RunRow
    {
        public int AccountId;
        public int UnlockedSavedArmySlots;
    }

    private sealed class SummaryRow
    {
        public int RunSummaryId;
        public int RunId;
        public int FinalResultId;
        public int StartSnapshotId;
        public int PreFinalSnapshotId;
        public int PostFinalSnapshotId;
        public int SavedArmyCandidateSnapshotId;
        public int AccountXpAwarded;
        public string NextUnlockPreview;
    }
}
