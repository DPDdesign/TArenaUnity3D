using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UnityEngine;

public class OfflineRunShopDbStore : IRunShopVisitStore
{
    private readonly string databasePath;
    private readonly IOfflineArmySnapshotCatalogResolver resolver;
    private readonly IRunMapPathCatalog pathCatalog;
    private readonly OfflineArmySnapshotDbRepository snapshotRepository = new OfflineArmySnapshotDbRepository();

    public OfflineRunShopDbStore(string databasePath, IOfflineArmySnapshotCatalogResolver resolver, IRunMapPathCatalog pathCatalog = null)
    {
        this.databasePath = databasePath;
        this.resolver = resolver;
        this.pathCatalog = pathCatalog ?? new DefaultRunMapPathCatalog();
    }

    public void SaveVisit(RunShopVisitViewData visit)
    {
        if (visit == null)
        {
            return;
        }

        int runId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(visit.RunId);
        if (runId <= 0)
        {
            throw new InvalidOperationException("Run shop DB store requires a persisted run id.");
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            RunContext run = LoadRunContext(connection, transaction, runId);
            if (run == null)
            {
                throw new InvalidOperationException("Offline run was not found for run shop persistence.");
            }

            int nodeId = ResolveNodeId(connection, transaction, run, visit.RouteNodeId);
            if (nodeId <= 0)
            {
                throw new InvalidOperationException("Run shop DB store requires a persisted route node.");
            }

            VisitRow existing = LoadVisitRowByVisitId(
                connection,
                transaction,
                OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(visit.VisitId));
            if (existing == null)
            {
                existing = LoadVisitRowByRunNode(connection, transaction, runId, nodeId);
            }

            if (existing == null)
            {
                InsertVisit(connection, transaction, run, nodeId, visit);
            }
            else
            {
                UpdateVisit(connection, transaction, run, existing, visit);
            }

            transaction.Commit();
        }
    }

    public RunShopVisitViewData FindVisit(string visitId)
    {
        int shopVisitId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(visitId);
        if (shopVisitId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            VisitRow row = LoadVisitRowByVisitId(connection, null, shopVisitId);
            return row == null ? null : BuildVisit(connection, row);
        }
    }

    public RunShopVisitViewData FindVisit(string runId, string routeNodeId)
    {
        int parsedRunId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(runId);
        if (parsedRunId <= 0)
        {
            return null;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            RunContext run = LoadRunContext(connection, null, parsedRunId);
            if (run == null)
            {
                return null;
            }

            int nodeId = ResolveNodeId(connection, null, run, routeNodeId);
            if (nodeId <= 0)
            {
                return null;
            }

            VisitRow row = LoadVisitRowByRunNode(connection, null, parsedRunId, nodeId);
            return row == null ? null : BuildVisit(connection, row);
        }
    }

    public bool HasPurchasedOffer(string visitId, string offerId)
    {
        int shopVisitId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(visitId);
        if (shopVisitId <= 0 || string.IsNullOrEmpty(offerId))
        {
            return false;
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        {
            object result = OfflineDatabaseSql.ExecuteScalar(
                connection,
                @"
SELECT purchased
FROM shop_offers
WHERE shop_visit_id = @shopVisitId
  AND offer_id = @offerId
  AND is_active = 1
LIMIT 1;",
                null,
                new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId),
                new OfflineDatabaseSqlParameter("@offerId", offerId));
            return OfflineDatabaseSql.ReadBool(result, false);
        }
    }

    public void SavePurchase(RunShopPurchaseRecord record, RunShopVisitViewData updatedVisit)
    {
        if (record == null || updatedVisit == null)
        {
            return;
        }

        int shopVisitId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(record.VisitId);
        if (shopVisitId <= 0)
        {
            throw new InvalidOperationException("Run shop purchase requires a persisted visit id.");
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            VisitRow existing = LoadVisitRowByVisitId(connection, transaction, shopVisitId);
            if (existing == null)
            {
                throw new InvalidOperationException("Run shop visit was not found in the database.");
            }

            RunContext run = LoadRunContext(connection, transaction, existing.RunId);
            if (run == null)
            {
                throw new InvalidOperationException("Offline run was not found for run shop purchase persistence.");
            }

            int shopOfferId = LoadShopOfferId(connection, transaction, shopVisitId, record.OfferId);
            if (shopOfferId <= 0)
            {
                throw new InvalidOperationException("Run shop offer was not found in the database.");
            }

            int afterSnapshotId = ResolveSnapshotId(
                connection,
                transaction,
                updatedVisit.CurrentArmy,
                run.AccountId,
                existing.RunId,
                existing.NodeId,
                existing.CurrentArmySnapshotId);
            string now = OfflineDatabaseSql.UtcNowText();

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
    @afterSnapshotId,
    @runGoldBefore,
    @runGoldAfter,
    @result,
    @createdAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@runId", existing.RunId),
                new OfflineDatabaseSqlParameter("@accountId", run.AccountId),
                new OfflineDatabaseSqlParameter("@nodeId", existing.NodeId),
                new OfflineDatabaseSqlParameter("@eventTypeId", (int)DBEventTypeId.Purchase),
                new OfflineDatabaseSqlParameter("@beforeSnapshotId", existing.CurrentArmySnapshotId),
                new OfflineDatabaseSqlParameter("@afterSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@runGoldBefore", record.CurrencyBefore),
                new OfflineDatabaseSqlParameter("@runGoldAfter", record.CurrencyAfter),
                new OfflineDatabaseSqlParameter("@result", record.OfferId),
                new OfflineDatabaseSqlParameter("@createdAtUtc", now));
            int eventId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO shop_purchases (
    event_id,
    shop_visit_id,
    shop_offer_id,
    run_id,
    run_gold_before,
    run_gold_after,
    army_before_purchase_snapshot_id,
    army_after_purchase_snapshot_id,
    purchase_result_id,
    message,
    purchased_at_utc,
    is_active
) VALUES (
    @eventId,
    @shopVisitId,
    @shopOfferId,
    @runId,
    @runGoldBefore,
    @runGoldAfter,
    @armyBeforePurchaseSnapshotId,
    @armyAfterPurchaseSnapshotId,
    @purchaseResultId,
    @message,
    @purchasedAtUtc,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@eventId", eventId),
                new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId),
                new OfflineDatabaseSqlParameter("@shopOfferId", shopOfferId),
                new OfflineDatabaseSqlParameter("@runId", existing.RunId),
                new OfflineDatabaseSqlParameter("@runGoldBefore", record.CurrencyBefore),
                new OfflineDatabaseSqlParameter("@runGoldAfter", record.CurrencyAfter),
                new OfflineDatabaseSqlParameter("@armyBeforePurchaseSnapshotId", existing.CurrentArmySnapshotId),
                new OfflineDatabaseSqlParameter("@armyAfterPurchaseSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@purchaseResultId", (int)DBPurchaseResultId.Purchased),
                new OfflineDatabaseSqlParameter("@message", updatedVisit.Message),
                new OfflineDatabaseSqlParameter("@purchasedAtUtc", now));

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
UPDATE shop_offers
SET purchased = 1,
    available = 0,
    purchase_snapshot_id = @purchaseSnapshotId
WHERE shop_offer_id = @shopOfferId;",
                transaction,
                new OfflineDatabaseSqlParameter("@purchaseSnapshotId", afterSnapshotId),
                new OfflineDatabaseSqlParameter("@shopOfferId", shopOfferId));

            UpdateVisitState(connection, transaction, existing, afterSnapshotId, record.CurrencyAfter, record.OfferId, (int)DBVisitStatusId.Open, null);
            UpdateRunState(connection, transaction, existing.RunId, existing.NodeId, afterSnapshotId, record.CurrencyAfter, (int)DBRunStatusId.InShop, "RunShop");

            transaction.Commit();
        }
    }

    public RunShopLeaveResult LeaveVisit(RunShopLeaveCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.VisitId))
        {
            return new RunShopLeaveResult(false, string.Empty, string.Empty, string.Empty, 0, null, "RunMap", "Missing run shop visit.");
        }

        int shopVisitId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(command.VisitId);
        if (shopVisitId <= 0)
        {
            return new RunShopLeaveResult(false, command.VisitId, string.Empty, string.Empty, 0, null, "RunMap", "Run shop visit was not found.");
        }

        using (IDbConnection connection = OfflineDatabaseSql.OpenConnection(databasePath))
        using (IDbTransaction transaction = connection.BeginTransaction())
        {
            VisitRow row = LoadVisitRowByVisitId(connection, transaction, shopVisitId);
            if (row == null)
            {
                return new RunShopLeaveResult(false, command.VisitId, string.Empty, string.Empty, 0, null, "RunMap", "Run shop visit was not found.");
            }

            string now = OfflineDatabaseSql.UtcNowText();
            UpdateVisitState(
                connection,
                transaction,
                row,
                row.CurrentArmySnapshotId,
                row.CurrentRunGold,
                command.FocusedOfferId,
                (int)DBVisitStatusId.Left,
                now);
            UpdateRunState(connection, transaction, row.RunId, row.NodeId, row.CurrentArmySnapshotId, row.CurrentRunGold, (int)DBRunStatusId.InProgress, "RunMap");
            transaction.Commit();
        }

        RunShopVisitViewData visit = FindVisit(command.VisitId);
        if (visit == null)
        {
            return new RunShopLeaveResult(false, command.VisitId, string.Empty, string.Empty, 0, null, "RunMap", "Leave shop failed.");
        }

        return new RunShopLeaveResult(true, visit.VisitId, visit.RunId, visit.RouteNodeId, visit.RunCurrency, visit.CurrentArmy, "RunMap", "Leave shop accepted.");
    }

    private void InsertVisit(IDbConnection connection, IDbTransaction transaction, RunContext run, int nodeId, RunShopVisitViewData visit)
    {
        int initialSnapshotId = ResolveSnapshotId(connection, transaction, visit.CurrentArmy, run.AccountId, run.RunId, nodeId, 0);
        string now = OfflineDatabaseSql.UtcNowText();

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
INSERT INTO shop_visits (
    run_id,
    node_id,
    visit_status_id,
    army_before_shop_snapshot_id,
    current_army_snapshot_id,
    run_gold_before,
    current_run_gold,
    focused_offer_id,
    created_at_utc,
    left_at_utc,
    is_active
) VALUES (
    @runId,
    @nodeId,
    @visitStatusId,
    @armyBeforeShopSnapshotId,
    @currentArmySnapshotId,
    @runGoldBefore,
    @currentRunGold,
    @focusedOfferId,
    @createdAtUtc,
    NULL,
    1
);",
            transaction,
            new OfflineDatabaseSqlParameter("@runId", run.RunId),
            new OfflineDatabaseSqlParameter("@nodeId", nodeId),
            new OfflineDatabaseSqlParameter("@visitStatusId", (int)DBVisitStatusId.Open),
            new OfflineDatabaseSqlParameter("@armyBeforeShopSnapshotId", initialSnapshotId),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", initialSnapshotId),
            new OfflineDatabaseSqlParameter("@runGoldBefore", visit.RunCurrency),
            new OfflineDatabaseSqlParameter("@currentRunGold", visit.RunCurrency),
            new OfflineDatabaseSqlParameter("@focusedOfferId", visit.FocusedOffer == null ? string.Empty : visit.FocusedOffer.OfferId),
            new OfflineDatabaseSqlParameter("@createdAtUtc", now));
        int shopVisitId = (int)OfflineDatabaseSql.ReadLastInsertRowId(connection, transaction);

        Dictionary<int, int> snapshotStackIdsByFormationSlot = snapshotRepository.LoadSnapshotStackIdsByFormationSlot(connection, initialSnapshotId, transaction);
        Dictionary<string, int> formationSlotByStackId = BuildFormationSlotLookup(visit.CurrentArmy);
        SaveOffers(connection, transaction, shopVisitId, visit.Offers, formationSlotByStackId, snapshotStackIdsByFormationSlot);

        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE route_nodes
SET shop_visit_id = @shopVisitId
WHERE node_id = @nodeId;",
            transaction,
            new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId),
            new OfflineDatabaseSqlParameter("@nodeId", nodeId));

        UpdateRunState(connection, transaction, run.RunId, nodeId, initialSnapshotId, visit.RunCurrency, (int)DBRunStatusId.InShop, "RunShop");
    }

    private void UpdateVisit(IDbConnection connection, IDbTransaction transaction, RunContext run, VisitRow existing, RunShopVisitViewData visit)
    {
        int currentSnapshotId = ResolveSnapshotId(
            connection,
            transaction,
            visit.CurrentArmy,
            run.AccountId,
            existing.RunId,
            existing.NodeId,
            existing.CurrentArmySnapshotId);
        UpdateVisitState(
            connection,
            transaction,
            existing,
            currentSnapshotId,
            visit.RunCurrency,
            visit.FocusedOffer == null ? string.Empty : visit.FocusedOffer.OfferId,
            (int)DBVisitStatusId.Open,
            null);
        UpdateRunState(connection, transaction, existing.RunId, existing.NodeId, currentSnapshotId, visit.RunCurrency, (int)DBRunStatusId.InShop, "RunShop");
    }

    private RunShopVisitViewData BuildVisit(IDbConnection connection, VisitRow row)
    {
        RunContext run = LoadRunContext(connection, null, row.RunId);
        OfflineArmySnapshotRecord currentSnapshot = snapshotRepository.LoadSnapshot(connection, row.CurrentArmySnapshotId);
        RunShopArmySnapshot currentArmy = OfflineArmySnapshotMapper.ToRunShop(currentSnapshot, resolver);
        List<OfferRow> offerRows = LoadOfferRows(connection, row.ShopVisitId);
        List<RunShopOfferViewData> offers = new List<RunShopOfferViewData>();

        for (int i = 0; i < offerRows.Count; i++)
        {
            OfferRow offerRow = offerRows[i];
            RunShopOperation operation = string.IsNullOrEmpty(offerRow.OperationJson)
                ? null
                : JsonUtility.FromJson<RunShopOperation>(offerRow.OperationJson);
            string runtimeStackId = ResolveRuntimeStackId(currentArmy, offerRow.TargetFormationSlot);
            NormalizeOperationForLoadedSnapshot(operation, runtimeStackId);

            offers.Add(new RunShopOfferViewData(
                offerRow.OfferId,
                ToOfferCategory(offerRow.OfferCategoryId),
                offerRow.Title,
                offerRow.Detail,
                offerRow.Cost,
                offerRow.Available,
                offerRow.Purchased,
                row.CurrentRunGold >= offerRow.Cost,
                offerRow.PreviewTextBefore,
                offerRow.PreviewTextAfter,
                runtimeStackId,
                operation));
        }

        RunShopOfferViewData focusedOffer = FindOffer(offers, row.FocusedOfferId);
        if (focusedOffer == null && offers.Count > 0)
        {
            focusedOffer = offers[0];
        }

        return new RunShopVisitViewData(
            OfflineDatabaseLegacyIdentity.ToLegacyShopVisitId(row.ShopVisitId),
            OfflineDatabaseLegacyIdentity.ToLegacyRunId(row.RunId),
            ResolveRouteNodeId(connection, run, row.NodeId),
            RunShopGameMode.Offline,
            RunShopAuthoritySource.LocalOfflineAdapter,
            row.CurrentRunGold,
            currentArmy,
            offers,
            focusedOffer,
            null,
            false,
            row.VisitStatusId == (int)DBVisitStatusId.Left ? "Run shop visit was left." : "Run shop ready.");
    }

    private void SaveOffers(
        IDbConnection connection,
        IDbTransaction transaction,
        int shopVisitId,
        List<RunShopOfferViewData> offers,
        Dictionary<string, int> formationSlotByStackId,
        Dictionary<int, int> snapshotStackIdsByFormationSlot)
    {
        if (offers == null)
        {
            return;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            RunShopOfferViewData offer = offers[i];
            if (offer == null)
            {
                continue;
            }

            int formationSlot = ResolveFormationSlot(offer.AffectedStackId, formationSlotByStackId, offer.Operation, i);
            int targetSnapshotStackId;
            snapshotStackIdsByFormationSlot.TryGetValue(formationSlot, out targetSnapshotStackId);

            OfflineDatabaseSql.ExecuteNonQuery(
                connection,
                @"
INSERT INTO shop_offers (
    shop_visit_id,
    offer_id,
    offer_category_id,
    title_id,
    detail_id,
    cost,
    available,
    purchased,
    affected_snapshot_stack_id,
    operation_json,
    preview_text_before,
    preview_text_after,
    preview_snapshot_id,
    purchase_snapshot_id,
    sort_order,
    is_active
) VALUES (
    @shopVisitId,
    @offerId,
    @offerCategoryId,
    @titleId,
    @detailId,
    @cost,
    @available,
    @purchased,
    @affectedSnapshotStackId,
    @operationJson,
    @previewTextBefore,
    @previewTextAfter,
    NULL,
    NULL,
    @sortOrder,
    1
);",
                transaction,
                new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId),
                new OfflineDatabaseSqlParameter("@offerId", offer.OfferId),
                new OfflineDatabaseSqlParameter("@offerCategoryId", ToOfferCategoryId(offer.Category)),
                new OfflineDatabaseSqlParameter("@titleId", offer.Title),
                new OfflineDatabaseSqlParameter("@detailId", offer.Detail),
                new OfflineDatabaseSqlParameter("@cost", offer.Cost),
                new OfflineDatabaseSqlParameter("@available", offer.Available ? 1 : 0),
                new OfflineDatabaseSqlParameter("@purchased", offer.Purchased ? 1 : 0),
                new OfflineDatabaseSqlParameter("@affectedSnapshotStackId", targetSnapshotStackId > 0 ? (object)targetSnapshotStackId : DBNull.Value),
                new OfflineDatabaseSqlParameter("@operationJson", JsonUtility.ToJson(offer.Operation)),
                new OfflineDatabaseSqlParameter("@previewTextBefore", offer.BeforeText),
                new OfflineDatabaseSqlParameter("@previewTextAfter", offer.AfterText),
                new OfflineDatabaseSqlParameter("@sortOrder", i));
        }
    }

    private int ResolveSnapshotId(
        IDbConnection connection,
        IDbTransaction transaction,
        RunShopArmySnapshot army,
        int accountId,
        int runId,
        int nodeId,
        int fallbackSnapshotId)
    {
        int snapshotId = OfflineDatabaseLegacyIdentity.ParseIntIdOrDefault(army == null ? string.Empty : army.SnapshotId);
        if (snapshotId > 0 && SnapshotExists(connection, transaction, snapshotId))
        {
            return snapshotId;
        }

        if (army == null)
        {
            return fallbackSnapshotId;
        }

        return snapshotRepository.SaveSnapshot(
            connection,
            transaction,
            OfflineArmySnapshotMapper.FromRunShop(army, accountId, runId, nodeId));
    }

    private static bool SnapshotExists(IDbConnection connection, IDbTransaction transaction, int snapshotId)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            "SELECT snapshot_id FROM army_snapshots WHERE snapshot_id = @snapshotId LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@snapshotId", snapshotId));
        return result != null && result != DBNull.Value;
    }

    private static void UpdateVisitState(
        IDbConnection connection,
        IDbTransaction transaction,
        VisitRow row,
        int currentSnapshotId,
        int currentRunGold,
        string focusedOfferId,
        int visitStatusId,
        string leftAtUtc)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE shop_visits
SET visit_status_id = @visitStatusId,
    current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    focused_offer_id = @focusedOfferId,
    left_at_utc = @leftAtUtc
WHERE shop_visit_id = @shopVisitId;",
            transaction,
            new OfflineDatabaseSqlParameter("@visitStatusId", visitStatusId),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", currentSnapshotId),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@focusedOfferId", string.IsNullOrEmpty(focusedOfferId) ? string.Empty : focusedOfferId),
            new OfflineDatabaseSqlParameter("@leftAtUtc", string.IsNullOrEmpty(leftAtUtc) ? (object)DBNull.Value : leftAtUtc),
            new OfflineDatabaseSqlParameter("@shopVisitId", row.ShopVisitId));
    }

    private static void UpdateRunState(
        IDbConnection connection,
        IDbTransaction transaction,
        int runId,
        int nodeId,
        int currentSnapshotId,
        int currentRunGold,
        int runStatusId,
        string nextScreen)
    {
        OfflineDatabaseSql.ExecuteNonQuery(
            connection,
            @"
UPDATE offline_runs
SET current_node_id = @currentNodeId,
    current_army_snapshot_id = @currentArmySnapshotId,
    current_run_gold = @currentRunGold,
    run_status_id = @runStatusId,
    next_screen = @nextScreen,
    updated_at_utc = @updatedAtUtc
WHERE run_id = @runId;",
            transaction,
            new OfflineDatabaseSqlParameter("@currentNodeId", nodeId),
            new OfflineDatabaseSqlParameter("@currentArmySnapshotId", currentSnapshotId),
            new OfflineDatabaseSqlParameter("@currentRunGold", currentRunGold),
            new OfflineDatabaseSqlParameter("@runStatusId", runStatusId),
            new OfflineDatabaseSqlParameter("@nextScreen", nextScreen),
            new OfflineDatabaseSqlParameter("@updatedAtUtc", OfflineDatabaseSql.UtcNowText()),
            new OfflineDatabaseSqlParameter("@runId", runId));
    }

    private int ResolveNodeId(IDbConnection connection, IDbTransaction transaction, RunContext run, string routeNodeId)
    {
        Dictionary<string, int> dbNodeIdByRuntimeId = new Dictionary<string, int>();
        Dictionary<int, string> runtimeIdByDbNodeId = new Dictionary<int, string>();
        BuildRouteNodeMaps(connection, transaction, run, dbNodeIdByRuntimeId, runtimeIdByDbNodeId);

        int nodeId;
        if (!string.IsNullOrEmpty(routeNodeId) && dbNodeIdByRuntimeId.TryGetValue(routeNodeId, out nodeId))
        {
            return nodeId;
        }

        return TryParseStrictInt(routeNodeId, out nodeId) ? nodeId : 0;
    }

    private string ResolveRouteNodeId(IDbConnection connection, RunContext run, int nodeId)
    {
        Dictionary<string, int> dbNodeIdByRuntimeId = new Dictionary<string, int>();
        Dictionary<int, string> runtimeIdByDbNodeId = new Dictionary<int, string>();
        BuildRouteNodeMaps(connection, null, run, dbNodeIdByRuntimeId, runtimeIdByDbNodeId);

        string runtimeId;
        return runtimeIdByDbNodeId.TryGetValue(nodeId, out runtimeId)
            ? runtimeId
            : nodeId.ToString(CultureInfo.InvariantCulture);
    }

    private void BuildRouteNodeMaps(
        IDbConnection connection,
        IDbTransaction transaction,
        RunContext run,
        Dictionary<string, int> dbNodeIdByRuntimeId,
        Dictionary<int, string> runtimeIdByDbNodeId)
    {
        if (run == null || run.RouteMapId <= 0)
        {
            return;
        }

        List<RunMapPathDefinition> catalogPaths = pathCatalog == null
            ? new List<RunMapPathDefinition>()
            : pathCatalog.BuildPaths(run.SelectedRouteChoiceId);
        List<PersistedPathRow> persistedPaths = LoadPaths(connection, transaction, run.RouteMapId);
        List<PersistedNodeRow> persistedNodes = LoadNodes(connection, transaction, run.RouteMapId);

        for (int pathIndex = 0; pathIndex < persistedPaths.Count; pathIndex++)
        {
            PersistedPathRow path = persistedPaths[pathIndex];
            RunMapPathDefinition catalogPath = FindCatalogPath(catalogPaths, path.PathId);
            List<PersistedNodeRow> nodes = FilterNodesForPath(persistedNodes, path.RoutePathId);

            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                string runtimeNodeId = catalogPath != null &&
                    catalogPath.Nodes != null &&
                    nodeIndex < catalogPath.Nodes.Count &&
                    catalogPath.Nodes[nodeIndex] != null
                    ? catalogPath.Nodes[nodeIndex].NodeId
                    : "node-" + nodes[nodeIndex].NodeId.ToString(CultureInfo.InvariantCulture);

                if (!dbNodeIdByRuntimeId.ContainsKey(runtimeNodeId))
                {
                    dbNodeIdByRuntimeId.Add(runtimeNodeId, nodes[nodeIndex].NodeId);
                }

                if (!runtimeIdByDbNodeId.ContainsKey(nodes[nodeIndex].NodeId))
                {
                    runtimeIdByDbNodeId.Add(nodes[nodeIndex].NodeId, runtimeNodeId);
                }
            }
        }
    }

    private static Dictionary<string, int> BuildFormationSlotLookup(RunShopArmySnapshot army)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        if (army == null || army.Stacks == null)
        {
            return result;
        }

        for (int i = 0; i < army.Stacks.Count; i++)
        {
            RunShopStackSnapshot stack = army.Stacks[i];
            if (stack == null || string.IsNullOrEmpty(stack.StackId))
            {
                continue;
            }

            int formationSlot = OfflineDatabaseLegacyIdentity.ParseSlotIndexOrDefault(stack.StackId, i);
            if (!result.ContainsKey(stack.StackId))
            {
                result.Add(stack.StackId, formationSlot);
            }
        }

        return result;
    }

    private static int ResolveFormationSlot(string stackId, Dictionary<string, int> formationSlotByStackId, RunShopOperation operation, int fallback)
    {
        if (operation != null &&
            (operation.Type == RunShopOperationType.AddStack || operation.Type == RunShopOperationType.GainCurrency))
        {
            return -1;
        }

        int direct;
        if (TryParseStrictFormationSlot(stackId, out direct))
        {
            return direct;
        }

        int mapped;
        if (!string.IsNullOrEmpty(stackId) && formationSlotByStackId.TryGetValue(stackId, out mapped))
        {
            return mapped;
        }

        string operationStackId = operation == null ? string.Empty : operation.StackId;
        if (!string.IsNullOrEmpty(operationStackId) && formationSlotByStackId.TryGetValue(operationStackId, out mapped))
        {
            return mapped;
        }

        return fallback;
    }

    private static string ResolveRuntimeStackId(RunShopArmySnapshot army, int formationSlot)
    {
        if (army == null || army.Stacks == null || formationSlot < 0 || formationSlot >= army.Stacks.Count)
        {
            return string.Empty;
        }

        RunShopStackSnapshot stack = army.Stacks[formationSlot];
        return stack == null ? string.Empty : stack.StackId;
    }

    private static void NormalizeOperationForLoadedSnapshot(RunShopOperation operation, string stackId)
    {
        if (operation == null || string.IsNullOrEmpty(stackId))
        {
            return;
        }

        switch (operation.Type)
        {
            case RunShopOperationType.RecoverLosses:
            case RunShopOperationType.TeachSkill:
            case RunShopOperationType.UpgradeStack:
                operation.StackId = stackId;
                break;
        }
    }

    private static int ToOfferCategoryId(RunShopOfferCategory category)
    {
        return (int)category + 1;
    }

    private static RunShopOfferCategory ToOfferCategory(int offerCategoryId)
    {
        return (RunShopOfferCategory)Mathf.Max(0, offerCategoryId - 1);
    }

    private static bool TryParseStrictInt(string value, out int result)
    {
        result = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    private static bool TryParseStrictFormationSlot(string value, out int result)
    {
        result = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        const string slotPrefix = "slot-";
        if (value.StartsWith(slotPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(
                value.Substring(slotPrefix.Length),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result) && result >= 0;
        }

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
    }

    private static RunContext LoadRunContext(IDbConnection connection, IDbTransaction transaction, int runId)
    {
        List<RunContext> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT run_id, account_id, route_map_id, selected_route_choice_id
FROM offline_runs
WHERE run_id = @runId
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new RunContext
                {
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    AccountId = OfflineDatabaseSql.ReadInt(row["account_id"]),
                    RouteMapId = OfflineDatabaseSql.ReadInt(row["route_map_id"]),
                    SelectedRouteChoiceId = OfflineDatabaseSql.ReadText(row["selected_route_choice_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId));

        return rows.Count == 0 ? null : rows[0];
    }

    private static VisitRow LoadVisitRowByVisitId(IDbConnection connection, IDbTransaction transaction, int shopVisitId)
    {
        if (shopVisitId <= 0)
        {
            return null;
        }

        List<VisitRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT shop_visit_id, run_id, node_id, visit_status_id, army_before_shop_snapshot_id, current_army_snapshot_id,
       run_gold_before, current_run_gold, focused_offer_id
FROM shop_visits
WHERE shop_visit_id = @shopVisitId
  AND is_active = 1
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new VisitRow
                {
                    ShopVisitId = OfflineDatabaseSql.ReadInt(row["shop_visit_id"]),
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    VisitStatusId = OfflineDatabaseSql.ReadInt(row["visit_status_id"]),
                    ArmyBeforeSnapshotId = OfflineDatabaseSql.ReadInt(row["army_before_shop_snapshot_id"]),
                    CurrentArmySnapshotId = OfflineDatabaseSql.ReadInt(row["current_army_snapshot_id"]),
                    RunGoldBefore = OfflineDatabaseSql.ReadInt(row["run_gold_before"]),
                    CurrentRunGold = OfflineDatabaseSql.ReadInt(row["current_run_gold"]),
                    FocusedOfferId = OfflineDatabaseSql.ReadText(row["focused_offer_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId));

        return rows.Count == 0 ? null : rows[0];
    }

    private static VisitRow LoadVisitRowByRunNode(IDbConnection connection, IDbTransaction transaction, int runId, int nodeId)
    {
        List<VisitRow> rows = OfflineDatabaseSql.Query(
            connection,
            @"
SELECT shop_visit_id, run_id, node_id, visit_status_id, army_before_shop_snapshot_id, current_army_snapshot_id,
       run_gold_before, current_run_gold, focused_offer_id
FROM shop_visits
WHERE run_id = @runId
  AND node_id = @nodeId
  AND is_active = 1
ORDER BY shop_visit_id DESC
LIMIT 1;",
            delegate(IDataRecord row)
            {
                return new VisitRow
                {
                    ShopVisitId = OfflineDatabaseSql.ReadInt(row["shop_visit_id"]),
                    RunId = OfflineDatabaseSql.ReadInt(row["run_id"]),
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    VisitStatusId = OfflineDatabaseSql.ReadInt(row["visit_status_id"]),
                    ArmyBeforeSnapshotId = OfflineDatabaseSql.ReadInt(row["army_before_shop_snapshot_id"]),
                    CurrentArmySnapshotId = OfflineDatabaseSql.ReadInt(row["current_army_snapshot_id"]),
                    RunGoldBefore = OfflineDatabaseSql.ReadInt(row["run_gold_before"]),
                    CurrentRunGold = OfflineDatabaseSql.ReadInt(row["current_run_gold"]),
                    FocusedOfferId = OfflineDatabaseSql.ReadText(row["focused_offer_id"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@runId", runId),
            new OfflineDatabaseSqlParameter("@nodeId", nodeId));

        return rows.Count == 0 ? null : rows[0];
    }

    private static int LoadShopOfferId(IDbConnection connection, IDbTransaction transaction, int shopVisitId, string offerId)
    {
        object result = OfflineDatabaseSql.ExecuteScalar(
            connection,
            @"
SELECT shop_offer_id
FROM shop_offers
WHERE shop_visit_id = @shopVisitId
  AND offer_id = @offerId
  AND is_active = 1
LIMIT 1;",
            transaction,
            new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId),
            new OfflineDatabaseSqlParameter("@offerId", offerId));
        return OfflineDatabaseSql.ReadInt(result);
    }

    private static List<OfferRow> LoadOfferRows(IDbConnection connection, int shopVisitId)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT offers.offer_id, offers.offer_category_id, offers.title_id, offers.detail_id, offers.cost,
       offers.available, offers.purchased, offers.operation_json, offers.preview_text_before,
       offers.preview_text_after, offers.sort_order, stacks.formation_slot
FROM shop_offers offers
LEFT JOIN army_snapshot_stacks stacks ON stacks.snapshot_stack_id = offers.affected_snapshot_stack_id
WHERE offers.shop_visit_id = @shopVisitId
  AND offers.is_active = 1
ORDER BY offers.sort_order, offers.shop_offer_id;",
            delegate(IDataRecord row)
            {
                return new OfferRow
                {
                    OfferId = OfflineDatabaseSql.ReadText(row["offer_id"]),
                    OfferCategoryId = OfflineDatabaseSql.ReadInt(row["offer_category_id"]),
                    Title = OfflineDatabaseSql.ReadText(row["title_id"]),
                    Detail = OfflineDatabaseSql.ReadText(row["detail_id"]),
                    Cost = OfflineDatabaseSql.ReadInt(row["cost"]),
                    Available = OfflineDatabaseSql.ReadBool(row["available"], true),
                    Purchased = OfflineDatabaseSql.ReadBool(row["purchased"], false),
                    OperationJson = OfflineDatabaseSql.ReadText(row["operation_json"]),
                    PreviewTextBefore = OfflineDatabaseSql.ReadText(row["preview_text_before"]),
                    PreviewTextAfter = OfflineDatabaseSql.ReadText(row["preview_text_after"]),
                    SortOrder = OfflineDatabaseSql.ReadInt(row["sort_order"]),
                    TargetFormationSlot = row["formation_slot"] == DBNull.Value ? -1 : OfflineDatabaseSql.ReadInt(row["formation_slot"])
                };
            },
            null,
            new OfflineDatabaseSqlParameter("@shopVisitId", shopVisitId));
    }

    private static List<PersistedPathRow> LoadPaths(IDbConnection connection, IDbTransaction transaction, int routeMapId)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT route_path_id, path_id, sort_order
FROM route_paths
WHERE route_map_id = @routeMapId
  AND is_active = 1
ORDER BY sort_order, route_path_id;",
            delegate(IDataRecord row)
            {
                return new PersistedPathRow
                {
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    PathId = OfflineDatabaseSql.ReadText(row["path_id"]),
                    SortOrder = OfflineDatabaseSql.ReadInt(row["sort_order"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
    }

    private static List<PersistedNodeRow> LoadNodes(IDbConnection connection, IDbTransaction transaction, int routeMapId)
    {
        return OfflineDatabaseSql.Query(
            connection,
            @"
SELECT node_id, route_path_id, stage_index
FROM route_nodes
WHERE route_map_id = @routeMapId
  AND is_active = 1
ORDER BY route_path_id, stage_index, node_id;",
            delegate(IDataRecord row)
            {
                return new PersistedNodeRow
                {
                    NodeId = OfflineDatabaseSql.ReadInt(row["node_id"]),
                    RoutePathId = OfflineDatabaseSql.ReadInt(row["route_path_id"]),
                    StageIndex = OfflineDatabaseSql.ReadInt(row["stage_index"])
                };
            },
            transaction,
            new OfflineDatabaseSqlParameter("@routeMapId", routeMapId));
    }

    private static List<PersistedNodeRow> FilterNodesForPath(List<PersistedNodeRow> persistedNodes, int routePathId)
    {
        List<PersistedNodeRow> result = new List<PersistedNodeRow>();
        for (int i = 0; i < persistedNodes.Count; i++)
        {
            if (persistedNodes[i].RoutePathId == routePathId)
            {
                result.Add(persistedNodes[i]);
            }
        }

        return result;
    }

    private static RunMapPathDefinition FindCatalogPath(List<RunMapPathDefinition> catalogPaths, string pathId)
    {
        if (catalogPaths == null)
        {
            return null;
        }

        for (int i = 0; i < catalogPaths.Count; i++)
        {
            if (catalogPaths[i] != null && catalogPaths[i].PathId == pathId)
            {
                return catalogPaths[i];
            }
        }

        return null;
    }

    private static RunShopOfferViewData FindOffer(List<RunShopOfferViewData> offers, string offerId)
    {
        if (offers == null || string.IsNullOrEmpty(offerId))
        {
            return null;
        }

        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].OfferId == offerId)
            {
                return offers[i];
            }
        }

        return null;
    }

    private sealed class RunContext
    {
        public int RunId;
        public int AccountId;
        public int RouteMapId;
        public string SelectedRouteChoiceId;
    }

    private sealed class VisitRow
    {
        public int ShopVisitId;
        public int RunId;
        public int NodeId;
        public int VisitStatusId;
        public int ArmyBeforeSnapshotId;
        public int CurrentArmySnapshotId;
        public int RunGoldBefore;
        public int CurrentRunGold;
        public string FocusedOfferId;
    }

    private sealed class OfferRow
    {
        public string OfferId;
        public int OfferCategoryId;
        public string Title;
        public string Detail;
        public int Cost;
        public bool Available;
        public bool Purchased;
        public string OperationJson;
        public string PreviewTextBefore;
        public string PreviewTextAfter;
        public int SortOrder;
        public int TargetFormationSlot;
    }

    private sealed class PersistedPathRow
    {
        public int RoutePathId;
        public string PathId;
        public int SortOrder;
    }

    private sealed class PersistedNodeRow
    {
        public int NodeId;
        public int RoutePathId;
        public int StageIndex;
    }
}
