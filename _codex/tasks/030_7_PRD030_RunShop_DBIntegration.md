# [TARENA] PRD030_7: Run Shop DB Integration

- Status: draft
- Type: Integration Task
- Area: Run Shop, Purchases, Events, Offline DB
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related: `_codex/tasks/024_PRD019_RunShop.md`
- Blocked by: `_codex/tasks/030_5_PRD030_StartRun_RunMap_DBIntegration.md`

## Goal

Persist run shop visits, offers, and purchases through the Offline Mode
database.

## What To Build

- Shop visit is created or loaded by integer `run_id` and integer `node_id`.
- `shop_visits` stores visit state, run gold, and current army snapshot.
- `shop_offers` stores generated offers once per visit.
- Purchased offers update status/flags instead of disappearing.
- Each purchase creates a `run_events` row of type `Purchase`.
- `shop_purchases` stores purchase detail and references `event_id`.
- Each successful purchase creates a new army snapshot.
- Leaving shop updates run/route flow state.

## Acceptance Criteria

- Reopening a shop visit preserves offers and purchased state.
- Purchase affordability and target validation remain domain logic, not SQL.
- Each purchase updates current run gold and current army snapshot durably.
- Free no-trade-off economy offer is not persisted in V1.
- No hard delete is used for offers or purchases in normal flow.
- UI still uses Run Shop adapter/service.
