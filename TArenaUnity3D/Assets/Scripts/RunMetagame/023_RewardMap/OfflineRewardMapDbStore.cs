using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class OfflineRewardMapDbStore : IMaterializedRewardMapChoiceStore
{
    private readonly string databasePath;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();
    private readonly OfflineRunContextDbWriter runContextWriter = new OfflineRunContextDbWriter();

    public OfflineRewardMapDbStore(string databasePath, IOfflineArmySnapshotCatalogResolver resolver)
    {
        this.databasePath = databasePath;
        this.resolver = resolver;
    }

    public RewardMapChoiceViewData SaveChoice(RewardMapChoiceViewData choice)
    {
        if (choice == null)
        {
            return null;
        }

        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(choice.RunId);
        if (runId <= 0)
        {
            throw new InvalidOperationException("Reward DB store requires a persisted run id.");
        }

        int persistedChoiceId;
        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            RunRow run = LoadRun(connection, transaction, runId);
            int nodeId = run.CurrentNodeId;
            if (nodeId <= 0)
            {
                throw new InvalidOperationException("Reward DB store requires a persisted current route node.");
            }

            string now = OfflineDatabaseSql.UtcNowText();
            OfflineArmySnapshotRecord beforeSnapshot = OfflineArmySnapshotMapper.FromRewardMap(choice.ArmyBeforeReward, run.AccountId, runId, nodeId);
            int beforeSnapshotId = snapshotRepository.SaveSnapshot(connection, transaction, beforeSnapshot);
            Dictionary<int, int> snapshotStackIdsByFormationSlot = snapshotRepository.LoadSnapshotStackIdsByFormationSlot(connection, beforeSnapshotId, transaction);
            Dictionary<string, int> formationSlotBySemanticStackId = BuildFormationSlotLookup(choice.ArmyBeforeReward);

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO run_events (
    run_id,
    account_id,
    node_id,
    event_type_id,
    before_snapshot_id,
    after_snapshot_id,
    run_gold_before,
    run_gold_after,
    result,
    created_at_utc,
    is_active
) VALUES (
    @runId,
    @accountId,
    @nodeId,
    @eventTypeId,
    @beforeSnapshotId,
    NULL,
    @runGoldBefore,
    NULL,
    @result,
    @createdAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@accountId", run.AccountId),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId),
                new OfflineDatabaseSqlParameter("@eventTypeId", (int)DBEventTypeId.Reward),
                new OfflineDatabaseSqlParameter("@beforeSnapshotId", beforeSnapshotId),
                new OfflineDatabaseSqlParameter("@runGoldBefore", choice.RunGoldBeforeReward),
                new OfflineDatabaseSqlParameter("@result", choice.Message),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            int eventId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

            persistedChoiceId = ReadNextId(connection, "reward_choices", "reward_choice_id", transaction);
            int runBattleId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(choice.BattleResultSummary == null ? string.Empty : choice.BattleResultSummary.BattleResultId);
            int focusedRewardSlotIndex = ResolveRewardSlotIndex(choice.FocusedCard, IndexOfRewardCard(choice.Cards, choice.FocusedCard));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO reward_choices (
    reward_choice_id,
    event_id,
    run_id,
    run_battle_id,
    node_id,
    army_before_reward_snapshot_id,
    focused_reward_id,
    focused_reward_slot_index,
    selected_reward_id,
    selected_reward_slot_index,
    run_gold_before,
    run_gold_after,
    choice_status_id,
    created_at_utc,
    applied_at_utc,
    is_active
) VALUES (
    @rewardChoiceId,
    @eventId,
    @runId,
    @runBattleId,
    @nodeId,
    @armyBeforeRewardSnapshotId,
    @focusedRewardId,
    @focusedRewardSlotIndex,
    NULL,
    -1,
    @runGoldBefore,
    NULL,
    @choiceStatusId,
    @createdAtUtc,
    NULL,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@rewardChoiceId", persistedChoiceId),
                new OfflineDatabaseSqlParameter("@eventId", eventId),
                new OfflineDatabaseSqlParameter("@runId", runId),
                new OfflineDatabaseSqlParameter("@runBattleId", runBattleId > 0 ? (object)runBattleId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId),
                new OfflineDatabaseSqlParameter("@armyBeforeRewardSnapshotId", beforeSnapshotId),
                new OfflineDatabaseSqlParameter("@focusedRewardId", choice.FocusedCard == null ? string.Empty : choice.FocusedCard.RewardId),
                new OfflineDatabaseSqlParameter("@focusedRewardSlotIndex", focusedRewardSlotIndex),
                new OfflineDatabaseSqlParameter("@runGoldBefore", choice.RunGoldBeforeReward),
                new OfflineDatabaseSqlParameter("@choiceStatusId", (int)DBChoiceStatusId.Generated),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));

            SaveCards(connection, transaction, persistedChoiceId, run.AccountId, runId, nodeId, beforeSnapshotId, choice, formationSlotBySemanticStackId, snapshotStackIdsByFormationSlot);
            OfflineRewardOpportunityDbStore.MarkResolvedForChoice(connection, transaction, runId, nodeId, persistedChoiceId);

            runContextWriter.UpdateNodeArmyGoldScreen(
                connection,
                transaction,
                runId,
                nodeId,
                beforeSnapshotId,
                choice.RunGoldBeforeReward,
                (int)DBRunStatusId.AwaitingReward,
                "Reward");

            transaction.Commit();
        }

        return FindChoice(OfflineDatabaseLegacyIdentity.ToLegacyRewardChoiceId(persistedChoiceId));
    }

    public RewardMapChoiceViewData FindChoice(string choiceId)
    {
        int parsedChoiceId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(choiceId);
        if (parsedChoiceId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            ChoiceRow choiceRow = LoadChoiceRow(connection, parsedChoiceId);
            if (choiceRow == null)
            {
                return null;
            }

            OfflineArmySnapshotRecord beforeSnapshot = snapshotRepository.LoadSnapshot(connection, choiceRow.BeforeSnapshotId);
            RewardMapArmySnapshot armyBeforeReward = OfflineArmySnapshotMapper.ToRewardMap(beforeSnapshot, resolver);
            List<RewardCardRow> cardRows = LoadCardRows(connection, parsedChoiceId);
            List<RewardMapCardViewData> cards = new List<RewardMapCardViewData>();
            RewardMapCardViewData focusedCard = null;

            for (int i = 0; i < cardRows.Count; i++)
            {
                RewardCardRow cardRow = cardRows[i];
                RewardMapOperation operation = string.IsNullOrEmpty(cardRow.OperationJson)
                    ? null
                    : JsonUtility.FromJson<RewardMapOperation>(cardRow.OperationJson);
                string runtimeStackId = ResolveRuntimeStackId(armyBeforeReward, cardRow.TargetFormationSlot);
                bool legal = cardRow.Legal;
                RewardMapError error = legal
                    ? RewardMapError.None
                    : ToRewardMapError(cardRow.ErrorId, RewardMapError.NoLegalTarget);
                if (legal)
                {
                    NormalizeOperationForLoadedSnapshot(operation, runtimeStackId, armyBeforeReward);
                }

                string affectedStackId = !string.IsNullOrEmpty(cardRow.AffectedStackId)
                    ? cardRow.AffectedStackId
                    : operation != null && !string.IsNullOrEmpty(operation.StackId)
                    ? operation.StackId
                    : (legal ? runtimeStackId : string.Empty);

                RewardMapCardViewData card = new RewardMapCardViewData(
                    BuildRewardId(cardRow),
                    cardRow.TemplateId,
                    ToFamily(cardRow.FamilyId),
                    ToIntention(cardRow.IntentionId),
                    ToRarity(cardRow.RarityId),
                    cardRow.VerbId,
                    cardRow.TitleId,
                    string.Empty,
                    cardRow.PreviewTextBefore,
                    cardRow.PreviewTextAfter,
                    affectedStackId,
                    legal,
                    error,
                    operation);
                card.RewardSlotIndex = cardRow.RewardSlotIndex;
                card.AffectedSlotIndex = cardRow.AffectedSlotIndex;
                card.IsFallback = cardRow.IsFallback;

                cards.Add(card);
                if (CardIdentityMatches(choiceRow.FocusedRewardId, choiceRow.FocusedRewardSlotIndex, card))
                {
                    focusedCard = card;
                }
            }

            if (focusedCard == null && cards.Count > 0)
            {
                focusedCard = cards[0];
            }

            RewardMapPreviewData focusedPreview = BuildFocusedPreview(connection, choiceRow, focusedCard);
            RewardMapChoiceViewData choice = new RewardMapChoiceViewData(
                OfflineDatabaseLegacyIdentity.ToLegacyRewardChoiceId(choiceRow.RewardChoiceId),
                OfflineDatabaseLegacyIdentity.ToLegacyRunId(choiceRow.RunId),
                RewardMapGameMode.Offline,
                RewardMapAuthoritySource.LocalOfflineAdapter,
                BuildBattleSummary(choiceRow.RunBattleId),
                BuildGainedSummary(choiceRow.RunGoldBefore, focusedPreview == null ? choiceRow.RunGoldBefore : focusedPreview.RunGoldAfterReward),
                choiceRow.RunGoldBefore,
                armyBeforeReward,
                cards,
                focusedCard,
                focusedPreview,
                focusedPreview == null ? string.Empty : focusedPreview.Message);
            choice.SelectedRewardId = choiceRow.SelectedRewardId;
            return choice;
        }
    }

    public RewardMapChoiceViewData FindChoiceForRunNode(string runId)
    {
        int parsedRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runId);
        if (parsedRunId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            int nodeId = ReadCurrentNodeId(connection, parsedRunId);
            if (nodeId <= 0)
            {
                return null;
            }

            object choiceId = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT reward_choice_id
FROM reward_choices
WHERE run_id = @runId
  AND node_id = @nodeId
  AND is_active = 1
ORDER BY reward_choice_id DESC
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@runId", parsedRunId),
                new OfflineDatabaseSqlParameter("@nodeId", nodeId));
            int parsedChoiceId = OfflineDatabaseSql.ReadInt(choiceId);
            return parsedChoiceId <= 0
                ? null
                : FindChoice(OfflineDatabaseLegacyIdentity.ToLegacyRewardChoiceId(parsedChoiceId));
        }
    }

    public RewardMapApplyResult SaveAppliedReward(string choiceId, RewardMapApplyResult result)
    {
        int parsedChoiceId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(choiceId);
        if (parsedChoiceId <= 0 || result == null)
        {
            return result;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            ChoiceRow choiceRow = LoadChoiceRow(connection, parsedChoiceId, transaction);
            if (choiceRow == null)
            {
                throw new InvalidOperationException("Reward choice was not found in the database.");
            }

            if (!string.IsNullOrEmpty(choiceRow.SelectedRewardId))
            {
                return new RewardMapApplyResult(false, RewardMapError.AlreadyApplied, "Reward was already applied.", result.Reward, result.ArmyAfterReward, result.RunGoldAfterReward, result.ResultSource);
            }

            int afterSnapshotId = snapshotRepository.SaveSnapshot(
                connection,
                transaction,
                OfflineArmySnapshotMapper.FromRewardMap(result.ArmyAfterReward, choiceRow.AccountId, choiceRow.RunId, choiceRow.NodeId));
            RewardCardIdentity selectedIdentity = ResolveRewardCardIdentity(connection, transaction, parsedChoiceId, result.Reward);
            if (result.Reward != null && selectedIdentity == null)
            {
                throw new InvalidOperationException("Applied reward card was not found in the persisted reward choice.");
            }

            int selectedSlotIndex = selectedIdentity == null
                ? ResolveRewardSlotIndex(result.Reward, -1)
                : selectedIdentity.RewardSlotIndex;
            string selectedRewardId = selectedIdentity == null
                ? (result.Reward == null ? string.Empty : result.Reward.RewardId)
                : selectedIdentity.RewardId;

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE run_events
SET after_snapshot_id = @afterSnapshotId,
    run_gold_after = @runGoldAfter,
    result = @result
WHERE event_id = @eventId;",
                transaction,
                new OfflineDatabaseSqlParameter("@afterSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@runGoldAfter", result.RunGoldAfterReward),
                new OfflineDatabaseSqlParameter("@result", selectedRewardId),
                new OfflineDatabaseSqlParameter("@eventId", choiceRow.EventId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE reward_choices
SET selected_reward_id = @selectedRewardId,
    selected_reward_slot_index = @selectedRewardSlotIndex,
    run_gold_after = @runGoldAfter,
    choice_status_id = @choiceStatusId,
    applied_at_utc = @appliedAtUtc
WHERE reward_choice_id = @rewardChoiceId;",
                transaction,
                new OfflineDatabaseSqlParameter("@selectedRewardId", selectedRewardId),
                new OfflineDatabaseSqlParameter("@selectedRewardSlotIndex", selectedSlotIndex),
                new OfflineDatabaseSqlParameter("@runGoldAfter", result.RunGoldAfterReward),
                new OfflineDatabaseSqlParameter("@choiceStatusId", (int)DBChoiceStatusId.Selected),
                new OfflineDatabaseSqlParameter("@appliedAtUtc", OfflineDatabaseSql.UtcNowText()),
                new OfflineDatabaseSqlParameter("@rewardChoiceId", parsedChoiceId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE reward_cards
SET is_selected = CASE WHEN reward_card_id = @rewardCardId THEN 1 ELSE 0 END,
    applied_snapshot_id = CASE WHEN reward_card_id = @rewardCardId THEN @appliedSnapshotId ELSE applied_snapshot_id END
WHERE reward_choice_id = @rewardChoiceId
  AND is_active = 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@rewardCardId", selectedIdentity == null ? 0 : selectedIdentity.RewardCardId),
                new OfflineDatabaseSqlParameter("@appliedSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@rewardChoiceId", parsedChoiceId));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE map_node_rewards
SET is_selected = CASE WHEN card_reward_id = @selectedRewardId AND reward_slot_index = @selectedRewardSlotIndex THEN 1 ELSE 0 END,
    applied_snapshot_id = CASE WHEN card_reward_id = @selectedRewardId AND reward_slot_index = @selectedRewardSlotIndex THEN @appliedSnapshotId ELSE applied_snapshot_id END
WHERE node_id = @nodeId
  AND (reward_choice_id = @rewardChoiceId OR reward_choice_id = 0)
  AND is_active = 1;",
                transaction,
                new OfflineDatabaseSqlParameter("@selectedRewardId", selectedRewardId),
                new OfflineDatabaseSqlParameter("@selectedRewardSlotIndex", selectedSlotIndex),
                new OfflineDatabaseSqlParameter("@appliedSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@rewardChoiceId", parsedChoiceId),
                new OfflineDatabaseSqlParameter("@nodeId", choiceRow.NodeId));

            runContextWriter.UpdateArmyGoldScreen(
                connection,
                transaction,
                choiceRow.RunId,
                afterSnapshotId,
                result.RunGoldAfterReward,
                (int)DBRunStatusId.InProgress,
                "RunMap");

            transaction.Commit();

            RewardMapArmySnapshot persistedArmy = OfflineArmySnapshotMapper.ToRewardMap(snapshotRepository.LoadSnapshot(connection, afterSnapshotId), resolver);
            return new RewardMapApplyResult(
                true,
                RewardMapError.None,
                result.Message,
                result.Reward,
                persistedArmy,
                result.RunGoldAfterReward,
                result.ResultSource);
        }
    }

    private void SaveCards(
        IDbConnection connection,
        IDbTransaction transaction,
        int rewardChoiceId,
        int accountId,
        int runId,
        int nodeId,
        int beforeSnapshotId,
        RewardMapChoiceViewData choice,
        Dictionary<string, int> formationSlotBySemanticStackId,
        Dictionary<int, int> snapshotStackIdsByFormationSlot)
    {
        if (choice.Cards == null)
        {
            return;
        }

        for (int i = 0; i < choice.Cards.Count; i++)
        {
            RewardMapCardViewData card = choice.Cards[i];
            if (card == null)
            {
                continue;
            }

            int rewardSlotIndex = ResolveRewardSlotIndex(card, i);
            card.RewardSlotIndex = rewardSlotIndex;
            int formationSlot = ResolveFormationSlot(card.AffectedStackId, formationSlotBySemanticStackId, card.Operation, i);
            int affectedSlotIndex = ResolveAffectedSlotIndex(card, formationSlot);
            card.AffectedSlotIndex = affectedSlotIndex;
            int targetSnapshotStackId;
            snapshotStackIdsByFormationSlot.TryGetValue(formationSlot, out targetSnapshotStackId);
            bool isFallback = IsFallback(card);
            card.IsFallback = isFallback;

            int previewSnapshotId = 0;
            if (choice.FocusedPreview != null &&
                choice.FocusedPreview.Error == RewardMapError.None &&
                choice.FocusedCard != null &&
                choice.FocusedCard.RewardId == card.RewardId &&
                choice.FocusedPreview.ArmyAfterReward != null)
            {
                previewSnapshotId = snapshotRepository.SaveSnapshot(
                    connection,
                    transaction,
                    OfflineArmySnapshotMapper.FromRewardMap(choice.FocusedPreview.ArmyAfterReward, accountId, runId, nodeId));
            }

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO reward_cards (
    reward_choice_id,
    reward_id,
    reward_slot_index,
    template_id,
    family_id,
    intention_id,
    rarity_id,
    title_id,
    verb_id,
    affected_stack_id,
    affected_slot_index,
    target_snapshot_stack_id,
    operation_type,
    operation_json,
    legal,
    error_id,
    preview_text_before,
    preview_text_after,
    preview_snapshot_id,
    applied_snapshot_id,
    is_selected,
    is_fallback,
    sort_order,
    is_active
) VALUES (
    @rewardChoiceId,
    @rewardId,
    @rewardSlotIndex,
    @templateId,
    @familyId,
    @intentionId,
    @rarityId,
    @titleId,
    @verbId,
    @affectedStackId,
    @affectedSlotIndex,
    @targetSnapshotStackId,
    @operationType,
    @operationJson,
    @legal,
    @errorId,
    @previewTextBefore,
    @previewTextAfter,
    @previewSnapshotId,
    NULL,
    0,
    @isFallback,
    @rewardSlotIndex,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
                new OfflineDatabaseSqlParameter("@rewardId", card.RewardId),
                new OfflineDatabaseSqlParameter("@rewardSlotIndex", rewardSlotIndex),
                new OfflineDatabaseSqlParameter("@templateId", card.TemplateId),
                new OfflineDatabaseSqlParameter("@familyId", (int)card.Family + 1),
                new OfflineDatabaseSqlParameter("@intentionId", (int)card.Intention + 1),
                new OfflineDatabaseSqlParameter("@rarityId", (int)card.Rarity + 1),
                new OfflineDatabaseSqlParameter("@titleId", card.Title),
                new OfflineDatabaseSqlParameter("@verbId", card.Verb),
                new OfflineDatabaseSqlParameter("@affectedStackId", card.AffectedStackId),
                new OfflineDatabaseSqlParameter("@affectedSlotIndex", affectedSlotIndex),
                new OfflineDatabaseSqlParameter("@targetSnapshotStackId", targetSnapshotStackId > 0 ? (object)targetSnapshotStackId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@operationType", OperationTypeText(card.Operation)),
                new OfflineDatabaseSqlParameter("@operationJson", JsonUtility.ToJson(card.Operation)),
                new OfflineDatabaseSqlParameter("@legal", card.Legal ? 1 : 0),
                new OfflineDatabaseSqlParameter("@errorId", (int)card.Error),
                new OfflineDatabaseSqlParameter("@previewTextBefore", card.BeforeText),
                new OfflineDatabaseSqlParameter("@previewTextAfter", card.AfterText),
                new OfflineDatabaseSqlParameter("@previewSnapshotId", previewSnapshotId > 0 ? (object)previewSnapshotId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@isFallback", isFallback ? 1 : 0));

            SaveMapNodeReward(
                connection,
                transaction,
                rewardChoiceId,
                nodeId,
                rewardSlotIndex,
                beforeSnapshotId,
                targetSnapshotStackId,
                isFallback,
                card);
        }
    }

    private static void SaveMapNodeReward(
        IDbConnection connection,
        IDbTransaction transaction,
        int rewardChoiceId,
        int nodeId,
        int slotIndex,
        int beforeSnapshotId,
        int targetSnapshotStackId,
        bool isFallback,
        RewardMapCardViewData card)
    {
        if (nodeId <= 0 || beforeSnapshotId <= 0 || card == null)
        {
            return;
        }

        RewardMapOperation operation = card.Operation;
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO map_node_rewards (
    node_id,
    reward_choice_id,
    reward_slot_index,
    card_reward_id,
    catalog_entry_id,
    base_snapshot_id,
    target_snapshot_stack_id,
    reward_type,
    unit_id,
    to_unit_id,
    amount,
    currency_delta,
    operation_json,
    legal,
    error_id,
    is_selected,
    applied_snapshot_id,
    is_fallback,
    is_active
) VALUES (
    @nodeId,
    @rewardChoiceId,
    @rewardSlotIndex,
    @cardRewardId,
    @catalogEntryId,
    @baseSnapshotId,
    @targetSnapshotStackId,
    @rewardType,
    @unitId,
    @toUnitId,
    @amount,
    @currencyDelta,
    @operationJson,
    @legal,
    @errorId,
    0,
    NULL,
    @isFallback,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@nodeId", nodeId),
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
            new OfflineDatabaseSqlParameter("@rewardSlotIndex", slotIndex),
            new OfflineDatabaseSqlParameter("@cardRewardId", card.RewardId),
            new OfflineDatabaseSqlParameter("@catalogEntryId", card.TemplateId),
            new OfflineDatabaseSqlParameter("@baseSnapshotId", beforeSnapshotId),
            new OfflineDatabaseSqlParameter("@targetSnapshotStackId", targetSnapshotStackId > 0 ? (object)targetSnapshotStackId : DBNull.Value),
            new OfflineDatabaseSqlParameter("@rewardType", ToRewardType(operation)),
            new OfflineDatabaseSqlParameter("@unitId", operation == null ? string.Empty : operation.UnitId),
            new OfflineDatabaseSqlParameter("@toUnitId", operation == null ? string.Empty : operation.ToUnitId),
            new OfflineDatabaseSqlParameter("@amount", operation == null ? 0 : operation.Amount),
            new OfflineDatabaseSqlParameter("@currencyDelta", operation == null ? 0 : operation.CurrencyDelta),
            new OfflineDatabaseSqlParameter("@operationJson", JsonUtility.ToJson(operation)),
            new OfflineDatabaseSqlParameter("@legal", card.Legal ? 1 : 0),
            new OfflineDatabaseSqlParameter("@errorId", (int)card.Error),
            new OfflineDatabaseSqlParameter("@isFallback", isFallback ? 1 : 0));
    }

    private static Dictionary<string, int> BuildFormationSlotLookup(RewardMapArmySnapshot army)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        if (army == null || army.Stacks == null)
        {
            return result;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack == null || string.IsNullOrEmpty(stack.StackId))
            {
                continue;
            }

            int formationSlot = ParseRewardFormationSlotOrDefault(stack.StackId, i);
            if (!result.ContainsKey(stack.StackId))
            {
                result.Add(stack.StackId, formationSlot);
            }
        }

        return result;
    }

    private static int ResolveFormationSlot(string stackId, Dictionary<string, int> formationSlotBySemanticStackId, RewardMapOperation operation, int fallback)
    {
        if (operation != null &&
            (operation.Type == RewardMapOperationType.AddStack || operation.Type == RewardMapOperationType.GainCurrency))
        {
            return -1;
        }

        int direct = ParseRewardFormationSlotOrDefault(stackId, -1);
        if (direct >= 0)
        {
            return direct;
        }

        int mapped;
        if (!string.IsNullOrEmpty(stackId) && formationSlotBySemanticStackId.TryGetValue(stackId, out mapped))
        {
            return mapped;
        }

        string operationStackId = operation == null ? string.Empty : operation.StackId;
        if (!string.IsNullOrEmpty(operationStackId) && formationSlotBySemanticStackId.TryGetValue(operationStackId, out mapped))
        {
            return mapped;
        }

        return fallback;
    }

    private static void NormalizeOperationForLoadedSnapshot(RewardMapOperation operation, string slotId, RewardMapArmySnapshot army)
    {
        if (operation == null || string.IsNullOrEmpty(slotId))
        {
            return;
        }

        switch (operation.Type)
        {
            case RewardMapOperationType.AddUnits:
            case RewardMapOperationType.PromoteStack:
            case RewardMapOperationType.DowngradeStack:
            case RewardMapOperationType.TeachSkill:
            case RewardMapOperationType.RecoverLosses:
                if (StackExists(army, operation.StackId))
                {
                    return;
                }

                operation.StackId = slotId;
                break;
        }
    }

    private static bool StackExists(RewardMapArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(stackId))
        {
            return false;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.StackId == stackId)
            {
                return true;
            }
        }

        return false;
    }

    private static int ReadCurrentNodeId(IDbConnection connection, int runId)
    {
        object nodeId = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT current_node_id
FROM offline_runs
WHERE run_id = @runId
LIMIT 1;",
            null,
            new OfflineDatabaseSqlParameter("@runId", runId));
        return OfflineDatabaseSql.ReadInt(nodeId);
    }

    private static string ToRewardType(RewardMapOperation operation)
    {
        if (operation == null)
        {
            return string.Empty;
        }

        switch (operation.Type)
        {
            case RewardMapOperationType.AddStack:
                return "AddNewStack";
            case RewardMapOperationType.AddUnits:
                return "IncreaseStack";
            case RewardMapOperationType.PromoteStack:
                return "PromoteUnit";
            case RewardMapOperationType.DowngradeStack:
                return "DowngradeUnit";
            case RewardMapOperationType.GainCurrency:
                return "RunGold";
            default:
                return operation.Type.ToString();
        }
    }

    private static bool IsFallback(RewardMapCardViewData card)
    {
        if (card == null)
        {
            return false;
        }

        if (card.IsFallback)
        {
            return true;
        }

        return string.Equals(card.TemplateId, "prd37-run-gold-fallback-v1", StringComparison.Ordinal);
    }

    private static int IndexOfRewardCard(List<RewardMapCardViewData> cards, RewardMapCardViewData reward)
    {
        if (cards == null || reward == null)
        {
            return -1;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            RewardMapCardViewData card = cards[i];
            if (ReferenceEquals(card, reward))
            {
                return i;
            }

            if (card != null && !string.IsNullOrEmpty(reward.RewardId) && card.RewardId == reward.RewardId)
            {
                return i;
            }
        }

        return -1;
    }

    private static int ResolveRewardSlotIndex(RewardMapCardViewData reward, int fallback)
    {
        if (reward == null)
        {
            return fallback;
        }

        if (reward.RewardSlotIndex >= 0)
        {
            return reward.RewardSlotIndex;
        }

        string rewardId = reward.RewardId ?? string.Empty;
        int separator = rewardId.LastIndexOf('-');
        if (separator < 0 || separator >= rewardId.Length - 1)
        {
            return fallback;
        }

        int parsed;
        return int.TryParse(rewardId.Substring(separator + 1), out parsed) ? parsed : fallback;
    }

    private static int ResolveAffectedSlotIndex(RewardMapCardViewData card, int targetFormationSlot)
    {
        if (card == null)
        {
            return -1;
        }

        if (card.AffectedSlotIndex >= 0)
        {
            return card.AffectedSlotIndex;
        }

        if (targetFormationSlot >= 0)
        {
            return targetFormationSlot;
        }

        int parsed = ParseRewardFormationSlotOrDefault(card.AffectedStackId, -1);
        if (parsed >= 0)
        {
            return parsed;
        }

        RewardMapOperation operation = card.Operation;
        parsed = ParseRewardFormationSlotOrDefault(operation == null ? string.Empty : operation.NewStackId, -1);
        if (parsed >= 0)
        {
            return parsed;
        }

        return ParseRewardFormationSlotOrDefault(operation == null ? string.Empty : operation.StackId, -1);
    }

    private static string OperationTypeText(RewardMapOperation operation)
    {
        return operation == null ? string.Empty : operation.Type.ToString();
    }

    private static RewardMapError ToRewardMapError(int errorId, RewardMapError fallback)
    {
        return errorId >= (int)RewardMapError.None && errorId <= (int)RewardMapError.AlreadyApplied
            ? (RewardMapError)errorId
            : fallback;
    }

    private static bool CardIdentityMatches(string rewardId, int rewardSlotIndex, RewardMapCardViewData card)
    {
        if (card == null || string.IsNullOrEmpty(rewardId) || card.RewardId != rewardId)
        {
            return false;
        }

        return rewardSlotIndex < 0 || card.RewardSlotIndex == rewardSlotIndex;
    }

    private static string ResolveRuntimeStackId(RewardMapArmySnapshot army, int formationSlot)
    {
        if (army == null || army.Stacks == null || formationSlot < 0 || formationSlot >= army.Stacks.Count)
        {
            return string.Empty;
        }

        RewardMapStackSnapshot stack = army.Stacks[formationSlot];
        return stack == null ? string.Empty : stack.StackId;
    }

    private static int ParseRewardFormationSlotOrDefault(string stackId, int fallback)
    {
        if (string.IsNullOrEmpty(stackId))
        {
            return fallback;
        }

        const string prefix = "slot-";
        if (stackId.StartsWith(prefix, StringComparison.Ordinal))
        {
            string suffix = stackId.Substring(prefix.Length);
            int parsed;
            if (int.TryParse(suffix, out parsed))
            {
                bool legacyOneBased = suffix.Length > 1 && suffix[0] == '0';
                return legacyOneBased ? Math.Max(0, parsed - 1) : Math.Max(0, parsed);
            }
        }

        int numeric;
        if (int.TryParse(stackId, out numeric))
        {
            return Math.Max(0, numeric);
        }

        return fallback;
    }

    private RewardMapPreviewData BuildFocusedPreview(IDbConnection connection, ChoiceRow choiceRow, RewardMapCardViewData focusedCard)
    {
        if (focusedCard == null)
        {
            return null;
        }

        if (focusedCard.Error != RewardMapError.None)
        {
            return new RewardMapPreviewData(
                focusedCard.RewardId,
                null,
                choiceRow.RunGoldBefore,
                null,
                focusedCard.Error,
                focusedCard.Error == RewardMapError.NoLegalTarget ? "Reward has no legal target." : "Reward preview unavailable.",
                "offline-local-reward-resolver");
        }

        RewardCardRow focusedCardRow = LoadFocusedCardRow(connection, choiceRow.RewardChoiceId, focusedCard);
        RewardMapArmySnapshot previewArmy = focusedCardRow == null || focusedCardRow.PreviewSnapshotId <= 0
            ? null
            : OfflineArmySnapshotMapper.ToRewardMap(snapshotRepository.LoadSnapshot(connection, focusedCardRow.PreviewSnapshotId), resolver);
        RewardMapStackSnapshot affected = FindStack(previewArmy, focusedCard.AffectedStackId);
        int runGoldAfter = choiceRow.RunGoldBefore;
        if (focusedCard.Operation != null && focusedCard.Operation.Type == RewardMapOperationType.GainCurrency)
        {
            runGoldAfter = Math.Max(0, runGoldAfter + focusedCard.Operation.CurrencyDelta);
        }

        return new RewardMapPreviewData(
            focusedCard.RewardId,
            previewArmy == null ? null : previewArmy,
            runGoldAfter,
            affected,
            RewardMapError.None,
            "Preview army after reward ready.",
            "offline-local-reward-resolver");
    }

    private static RewardMapBattleResultSummary BuildBattleSummary(int runBattleId)
    {
        return runBattleId > 0
            ? new RewardMapBattleResultSummary(OfflineDatabaseLegacyIdentity.ToLegacyRunBattleId(runBattleId), "Victory", 0, 0)
            : null;
    }

    private static string BuildGainedSummary(int beforeGold, int afterGold)
    {
        return "Gained: " + Math.Max(0, afterGold - beforeGold) + " RUN GOLD";
    }

    private static RewardMapStackSnapshot FindStack(RewardMapArmySnapshot army, string stackId)
    {
        if (army == null || army.Stacks == null || string.IsNullOrEmpty(stackId))
        {
            return null;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RewardMapStackSnapshot stack = army.Stacks[i];
            if (stack != null && stack.StackId == stackId)
            {
                return stack;
            }
        }

        return null;
    }

    private static string BuildRewardId(RewardCardRow row)
    {
        if (row != null && !string.IsNullOrEmpty(row.RewardId))
        {
            return row.RewardId;
        }

        string templateId = row == null ? string.Empty : row.TemplateId;
        int slotIndex = row == null ? -1 : row.RewardSlotIndex;
        if (slotIndex < 0 && row != null)
        {
            slotIndex = row.SortOrder;
        }

        return !string.IsNullOrEmpty(templateId) && templateId.StartsWith("prd37-", StringComparison.Ordinal)
            ? "reward-" + templateId + "-" + slotIndex
            : "reward-" + templateId;
    }

    private static RewardMapFamily ToFamily(int familyId)
    {
        return (RewardMapFamily)Mathf.Max(0, familyId - 1);
    }

    private static RewardMapIntention ToIntention(int intentionId)
    {
        return (RewardMapIntention)Mathf.Max(0, intentionId - 1);
    }

    private static RewardMapRarity ToRarity(int rarityId)
    {
        return (RewardMapRarity)Mathf.Max(0, rarityId - 1);
    }

    private static int ReadNextId(IDbConnection connection, string tableName, string keyColumn, IDbTransaction transaction)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT COALESCE(MAX(" + keyColumn + "), 0) + 1 FROM " + tableName + ";",
            transaction);
        return OfflineDatabaseSql.ReadInt(result, 1);
    }

    private static RunRow LoadRun(IDbConnection connection, IDbTransaction transaction, int runId)
    {
        List<RunRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT account_id, current_node_id
FROM offline_runs
WHERE run_id = @runId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new RunRow
                {
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    CurrentNodeId = OfflineDatabaseSql.ReadInt(row["current_node_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Offline run was not found for reward persistence.");
        }

        return rows[0];
    }

    private static ChoiceRow LoadChoiceRow(IDbConnection connection, int rewardChoiceId, IDbTransaction transaction = null)
    {
        List<ChoiceRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT choice.reward_choice_id, choice.event_id, choice.run_id, choice.run_battle_id, choice.node_id,
       choice.army_before_reward_snapshot_id, choice.focused_reward_id, choice.selected_reward_id,
       choice.focused_reward_slot_index, choice.selected_reward_slot_index,
       choice.run_gold_before, runs.account_id
FROM reward_choices choice
INNER JOIN offline_runs runs ON runs.run_id = choice.run_id
WHERE choice.reward_choice_id = @rewardChoiceId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new ChoiceRow
                {
                    RewardChoiceId = OfflineDatabaseSql.ReadInt(row["reward_choice_id"]),
                    EventId = OfflineDatabaseSql.ReadInt(row["event_id"]),
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    RunBattleId = OfflineDatabaseSql.ReadInt(row["run_battle_id"]),
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    BeforeSnapshotId = OfflineDatabaseSql.ReadInt(row["army_before_reward_snapshot_id"]),
                    FocusedRewardId = OfflineDatabaseSql.ReadText(row["focused_reward_id"]),
                    FocusedRewardSlotIndex = OfflineDatabaseSql.ReadInt(row["focused_reward_slot_index"], -1),
                    SelectedRewardId = OfflineDatabaseSql.ReadText(row["selected_reward_id"]),
                    SelectedRewardSlotIndex = OfflineDatabaseSql.ReadInt(row["selected_reward_slot_index"], -1),
                    RunGoldBefore = OfflineDatabaseSql.ReadInt(row["run_gold_before"]),
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId));

        return rows.Count == 0 ? null : rows[0];
    }

    private static List<RewardCardRow> LoadCardRows(IDbConnection connection, int rewardChoiceId)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT cards.template_id, cards.family_id, cards.intention_id, cards.rarity_id, cards.title_id, cards.verb_id,
       cards.reward_id, cards.reward_slot_index, cards.affected_stack_id, cards.affected_slot_index,
       cards.target_snapshot_stack_id, cards.operation_type, cards.operation_json, cards.legal, cards.error_id,
       cards.preview_text_before, cards.preview_text_after, cards.preview_snapshot_id, cards.applied_snapshot_id,
       cards.is_selected, cards.is_fallback, cards.sort_order, stacks.formation_slot
FROM reward_cards cards
LEFT JOIN army_snapshot_stacks stacks ON stacks.snapshot_stack_id = cards.target_snapshot_stack_id
WHERE cards.reward_choice_id = @rewardChoiceId AND cards.is_active = 1
ORDER BY cards.reward_slot_index, cards.sort_order, cards.reward_card_id;",
            delegate(IDataRecord row)
            {
                return new RewardCardRow
                {
                    RewardId = OfflineDatabaseSql.ReadText(row["reward_id"]),
                    RewardSlotIndex = OfflineDatabaseSql.ReadInt(row["reward_slot_index"]),
                    TemplateId = OfflineDatabaseSql.ReadText(row["template_id"]),
                    FamilyId = OfflineDatabaseSql.ReadInt(row["family_id"]),
                    IntentionId = OfflineDatabaseSql.ReadInt(row["intention_id"]),
                    RarityId = OfflineDatabaseSql.ReadInt(row["rarity_id"]),
                    TitleId = OfflineDatabaseSql.ReadText(row["title_id"]),
                    VerbId = OfflineDatabaseSql.ReadText(row["verb_id"]),
                    AffectedStackId = OfflineDatabaseSql.ReadText(row["affected_stack_id"]),
                    AffectedSlotIndex = OfflineDatabaseSql.ReadInt(row["affected_slot_index"], -1),
                    OperationType = OfflineDatabaseSql.ReadText(row["operation_type"]),
                    OperationJson = OfflineDatabaseSql.ReadText(row["operation_json"]),
                    Legal = OfflineDatabaseSql.ReadBool(row["legal"], true),
                    ErrorId = OfflineDatabaseSql.ReadInt(row["error_id"]),
                    PreviewTextBefore = OfflineDatabaseSql.ReadText(row["preview_text_before"]),
                    PreviewTextAfter = OfflineDatabaseSql.ReadText(row["preview_text_after"]),
                    PreviewSnapshotId = OfflineDatabaseSql.ReadInt(row["preview_snapshot_id"]),
                    AppliedSnapshotId = OfflineDatabaseSql.ReadInt(row["applied_snapshot_id"]),
                    IsSelected = OfflineDatabaseSql.ReadBool(row["is_selected"]),
                    IsFallback = OfflineDatabaseSql.ReadBool(row["is_fallback"]),
                    TargetFormationSlot = row["formation_slot"] == DBNull.Value ? -1 : OfflineDatabaseSql.ReadInt(row["formation_slot"]),
                    SortOrder = OfflineDatabaseSql.ReadInt(row["sort_order"])
                };
            },
            null,
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId));
    }

    private static RewardCardIdentity ResolveRewardCardIdentity(
        IDbConnection connection,
        IDbTransaction transaction,
        int rewardChoiceId,
        RewardMapCardViewData reward)
    {
        if (connection == null || rewardChoiceId <= 0 || reward == null)
        {
            return null;
        }

        int rewardSlotIndex = ResolveRewardSlotIndex(reward, -1);
        string rewardId = reward.RewardId ?? string.Empty;
        RewardCardIdentity identity = null;
        if (!string.IsNullOrEmpty(rewardId))
        {
            identity = FindRewardCardIdentity(
                connection,
                transaction,
                @"
SELECT reward_card_id, reward_id, reward_slot_index, template_id
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND reward_id = @rewardId
  AND reward_slot_index = @rewardSlotIndex
  AND is_active = 1
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
                new OfflineDatabaseSqlParameter("@rewardId", rewardId),
                new OfflineDatabaseSqlParameter("@rewardSlotIndex", rewardSlotIndex));
            if (identity != null)
            {
                return identity;
            }

            identity = FindRewardCardIdentity(
                connection,
                transaction,
                @"
SELECT reward_card_id, reward_id, reward_slot_index, template_id
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND reward_id = @rewardId
  AND is_active = 1
ORDER BY reward_slot_index, sort_order, reward_card_id
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
                new OfflineDatabaseSqlParameter("@rewardId", rewardId));
            if (identity != null)
            {
                return identity;
            }
        }

        if (rewardSlotIndex >= 0)
        {
            identity = FindRewardCardIdentity(
                connection,
                transaction,
                @"
SELECT reward_card_id, reward_id, reward_slot_index, template_id
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND reward_slot_index = @rewardSlotIndex
  AND is_active = 1
ORDER BY reward_card_id
LIMIT 1;",
                new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
                new OfflineDatabaseSqlParameter("@rewardSlotIndex", rewardSlotIndex));
            if (identity != null)
            {
                return identity;
            }
        }

        return FindRewardCardIdentity(
            connection,
            transaction,
            @"
SELECT reward_card_id, reward_id, reward_slot_index, template_id
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND template_id = @templateId
  AND is_active = 1
ORDER BY reward_slot_index, sort_order, reward_card_id
LIMIT 1;",
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
            new OfflineDatabaseSqlParameter("@templateId", reward.TemplateId ?? string.Empty));
    }

    private static RewardCardIdentity FindRewardCardIdentity(
        IDbConnection connection,
        IDbTransaction transaction,
        string sql,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        List<RewardCardIdentity> rows = OfflineDatabaseSql.Query(
            connection,
            sql,
            delegate(IDataRecord row)
            {
                return new RewardCardIdentity
                {
                    RewardCardId = OfflineDatabaseSql.ReadInt(row["reward_card_id"]),
                    RewardId = OfflineDatabaseSql.ReadText(row["reward_id"]),
                    RewardSlotIndex = OfflineDatabaseSql.ReadInt(row["reward_slot_index"], -1),
                    TemplateId = OfflineDatabaseSql.ReadText(row["template_id"])
                };
            },
            transaction,
            parameters);

        return rows.Count == 0 ? null : rows[0];
    }

    private static RewardCardRow LoadFocusedCardRow(IDbConnection connection, int rewardChoiceId, RewardMapCardViewData focusedCard)
    {
        if (focusedCard == null)
        {
            return null;
        }

        RewardCardIdentity identity = ResolveRewardCardIdentity(connection, null, rewardChoiceId, focusedCard);
        if (identity == null)
        {
            return null;
        }

        List<RewardCardRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT preview_snapshot_id
FROM reward_cards
WHERE reward_choice_id = @rewardChoiceId
  AND reward_card_id = @rewardCardId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new RewardCardRow
                {
                    PreviewSnapshotId = OfflineDatabaseSql.ReadInt(row["preview_snapshot_id"])
                };
            },
            null,
            new OfflineDatabaseSqlParameter("@rewardChoiceId", rewardChoiceId),
            new OfflineDatabaseSqlParameter("@rewardCardId", identity.RewardCardId));

        return rows.Count == 0 ? null : rows[0];
    }

    private sealed class RunRow
    {
        public int AccountId;
        public int CurrentNodeId;
    }

    private sealed class ChoiceRow
    {
        public int RewardChoiceId;
        public int EventId;
        public int RunId;
        public int RunBattleId;
        public int NodeId;
        public int BeforeSnapshotId;
        public string FocusedRewardId;
        public int FocusedRewardSlotIndex;
        public string SelectedRewardId;
        public int SelectedRewardSlotIndex;
        public int RunGoldBefore;
        public int AccountId;
    }

    private sealed class RewardCardRow
    {
        public string RewardId;
        public int RewardSlotIndex;
        public string TemplateId;
        public int FamilyId;
        public int IntentionId;
        public int RarityId;
        public string TitleId;
        public string VerbId;
        public string AffectedStackId;
        public int AffectedSlotIndex;
        public string OperationType;
        public string OperationJson;
        public bool Legal;
        public int ErrorId;
        public string PreviewTextBefore;
        public string PreviewTextAfter;
        public int PreviewSnapshotId;
        public int AppliedSnapshotId;
        public bool IsSelected;
        public bool IsFallback;
        public int TargetFormationSlot;
        public int SortOrder;
    }

    private sealed class RewardCardIdentity
    {
        public int RewardCardId;
        public string RewardId;
        public int RewardSlotIndex;
        public string TemplateId;
    }
}
