using System;
using System.Collections.Generic;
using System.Data;

public static class OfflineRewardOpportunityDbStore
{
    public static void SaveUnresolvedPlanForNode(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int nodeId,
        int runSeed,
        int seedVersion)
    {
        if (connection == null || transaction == null || runId <= 0 || nodeId <= 0)
        {
            return;
        }

        RewardMapOperationType[] plannedTypes = RewardMapMaterializedGenerator.PlanNormalOperationTypes(runSeed, nodeId, seedVersion);
        string now = OfflineDatabaseSql.UtcNowText();
        for (int slotIndex = 0; slotIndex < plannedTypes.Length; slotIndex++)
        {
            RewardMapOperationType plannedType = plannedTypes[slotIndex];
            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT OR IGNORE INTO reward_opportunities (
    run_id,
    node_id,
    reward_slot_index,
    planned_operation_type,
    catalog_entry_id,
    run_seed,
    seed_version,
    opportunity_state_id,
    reward_choice_id,
    resolved_reward_card_id,
    resolved_card_reward_id,
    created_at_utc,
    resolved_at_utc,
    is_active
) VALUES (
    @runId,
    @nodeId,
    @rewardSlotIndex,
    @plannedOperationType,
    @catalogEntryId,
    @runSeed,
    @seedVersion,
    @opportunityStateId,
    0,
    NULL,
    '',
    @createdAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId),
                new OfflineDatabaseSqlParameter("@rewardSlotIndex", slotIndex),
                new OfflineDatabaseSqlParameter("@plannedOperationType", plannedType.ToString()),
                new OfflineDatabaseSqlParameter("@catalogEntryId", RewardMapMaterializedGenerator.CatalogEntryIdFor(plannedType)),
                new OfflineDatabaseSqlParameter("@runSeed", runSeed),
                new OfflineDatabaseSqlParameter("@seedVersion", seedVersion),
                new OfflineDatabaseSqlParameter("@opportunityStateId", (int)DBRewardOpportunityStateId.Unresolved),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
        }
    }

    public static List<RewardMapOperationType> LoadPlannedOperationTypes(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int nodeId,
        int seedVersion)
    {
        List<RewardMapOperationType> result = new List<RewardMapOperationType>();
        if (connection == null || runId <= 0 || nodeId <= 0 || seedVersion <= 0)
        {
            return result;
        }

        List<OpportunityRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT reward_slot_index, planned_operation_type
FROM reward_opportunities
WHERE run_id = @runId
  AND node_id = @nodeId
  AND seed_version = @seedVersion
  AND is_active = 1
ORDER BY reward_slot_index; ",
            delegate(IDataRecord row)
            {
                return new OpportunityRow
                {
                    SlotIndex = OfflineDatabaseSql.ReadInt(row["reward_slot_index"], -1),
                    PlannedOperationType = OfflineDatabaseSql.ReadText(row["planned_operation_type"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@nodeId", nodeId),
            new OfflineDatabaseSqlParameter("@seedVersion", seedVersion));

        if (rows.Count < RewardMapMaterializedGenerator.RewardSlotCount)
        {
            return result;
        }

        for (int expectedSlot = 0; expectedSlot < RewardMapMaterializedGenerator.RewardSlotCount; expectedSlot++)
        {
            OpportunityRow row = rows[expectedSlot];
            RewardMapOperationType parsed;
            if (row == null ||
                row.SlotIndex != expectedSlot ||
                !Enum.TryParse(row.PlannedOperationType, true, out parsed) ||
                parsed == RewardMapOperationType.GainCurrency)
            {
                result.Clear();
                return result;
            }

            result.Add(parsed);
        }

        return result;
    }

    public static void MarkResolvedForChoice(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int nodeId,
        int rewardChoiceId)
    {
        if (connection == null || transaction == null || runId <= 0 || nodeId <= 0 || rewardChoiceId <= 0)
        {
            return;
        }

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE reward_opportunities
SET opportunity_state_id = @resolvedStateId,
    reward_choice_id = @rewardChoiceId,
    resolved_reward_card_id = (
        SELECT reward_card_id
        FROM reward_cards cards
        WHERE cards.reward_choice_id = @rewardChoiceId
          AND cards.reward_slot_index = reward_opportunities.reward_slot_index
          AND cards.is_fallback = 0
          AND cards.is_active = 1
        ORDER BY cards.reward_card_id
        LIMIT 1
    ),
    resolved_card_reward_id = COALESCE((
        SELECT reward_id
        FROM reward_cards cards
        WHERE cards.reward_choice_id = @rewardChoiceId
          AND cards.reward_slot_index = reward_opportunities.reward_slot_index
          AND cards.is_fallback = 0
          AND cards.is_active = 1
        ORDER BY cards.reward_card_id
        LIMIT 1
    ), ''),
    resolved_at_utc = @resolvedAtUtc
WHERE run_id = @runId
  AND node_id = @nodeId
  AND reward_slot_index < @normalSlotCount
  AND is_active = 1
  AND EXISTS (
      SELECT 1
      FROM reward_cards cards
      WHERE cards.reward_choice_id = @rewardChoiceId
        AND cards.reward_slot_index = reward_opportunities.reward_slot_index
        AND cards.is_fallback = 0
        AND cards.is_active = 1
  );",
            transaction,
            new OfflineDatabaseSqlParameter("@resolvedStateId", (int)DBRewardOpportunityStateId.Resolved),
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
            new OfflineDatabaseSqlParameter("@resolvedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@nodeId", nodeId),
            new OfflineDatabaseSqlParameter("@normalSlotCount", RewardMapMaterializedGenerator.RewardSlotCount));
    }

    private sealed class OpportunityRow
    {
        public int SlotIndex;
        public string PlannedOperationType;
    }
}
