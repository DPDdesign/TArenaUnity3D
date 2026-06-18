# [TARENA] PRD030 Offline Database Map

Status: active
Last updated: 2026-06-17

## Purpose

This document maps the PRD030 Offline Mode SQLite database to code ownership,
table ownership, and screen flows.

Use it together with:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`

## Database Rules

Database file:

- `Application.persistentDataPath/TArenaOffline.db`

Provider resolution:

- `Microsoft.Data.Sqlite` first,
- `Mono.Data.Sqlite` fallback,
- handled by `OfflineDatabaseProvider`.

Open/migration:

- `OfflineDatabaseModule.OpenOrCreate()`,
- enables `PRAGMA foreign_keys = ON`,
- creates `schema_version`,
- applies schema V1 in a transaction.

Identity:

- local runtime primary keys are integer ids,
- local runtime foreign keys are integer ids,
- authored/catalog references stay text ids,
- UI-facing legacy labels like `run-12` and `saved-army-3` are facade labels
  converted by `OfflineDatabaseLegacyIdentity`.

Deletion:

- normal gameplay should not hard-delete runtime rows,
- use `is_active = 0` and/or domain flags such as `active = 0`,
- hard delete is only for explicit dev/test reset or migration maintenance.

## Ownership Map

### Foundation

Code owners:

- `OfflineDatabaseModule.cs`
- `OfflineDatabaseProvider.cs`
- `OfflineDatabaseSql.cs`
- `OfflineDatabaseSchemaV1.cs`
- `DB Enums.cs`
- `OfflineDatabaseAccountBootstrap.cs`
- `OfflineDatabaseHandler.cs`

Tables:

- `schema_version`

Responsibilities:

- open/create DB,
- provider selection,
- migration,
- schema version,
- SQL helpers,
- default account bootstrap.

### Account And Unlocks

Tables:

- `offline_accounts`
- `account_unlocks`

Primary owners:

- `OfflineDatabaseAccountBootstrap.cs`
- `OfflineBattleResultDbStore.cs`
- `OfflineSavedArmyDbRepository.cs`

Data:

- local account id,
- display name,
- account XP,
- rank value,
- unlocked saved-army slot count,
- map/unit/skill/saved-army-slot unlock records.

Notes:

- default account bootstrap seeds the baseline Start Run unit unlocks used by
  the generator, so later progression unlock rows extend the roster instead of
  becoming the entire allowed roster,
- default unlocked saved-army slots is 2,
- physical roster capacity can still be 8 slots,
- Battle Result currently updates XP/rank and writes unlock progress.

### Runs And Route State

Tables:

- `offline_runs`
- `map_nodes`
- `map_node_connections`

Primary owners:

- `OfflineRunContextDbReader.cs`
- `OfflineRunContextDbWriter.cs`
- `OfflineStartRunDbStore.cs`
- `OfflineRunMapDbStore.cs`
- `OfflineRouteMapSeedFactory.cs`
- `OfflineRouteMapSeedModels.cs`
- `OfflineMaterializedRunMapDbStore.cs`

Data:

- active/completed run record,
- selected starting army and route choice,
- current army snapshot,
- start/pre-final snapshot references,
- current node,
- stage and route progress,
- materialized route paths, route nodes, and node connections.

Run-context read/write rule:

- `OfflineRunContextDbWriter` is the only runtime code surface that should
  insert or update `offline_runs`.
- `OfflineRunContextDbReader` is the shared read side for active run context,
  `next_screen` lookup, current/start/pre-final snapshots, Start Run record
  reload, summary lookup, and latest async battle result id lookup.
- Slice stores may own their detail tables, but they must update
  `offline_runs` through the writer.
- UI controllers must not query SQLite directly; they should use services,
  adapters, and `OfflineRunContextDbReader`.
- Run Map UI loads the latest persisted `RunMap` context and builds
  `RunMapCreateRequest` from DB run id, selected route choice, current gold,
  and current army snapshot summary. Its army panel renders row data from the
  same hydrated `current_army_snapshot` by instantiating a configured prefab
  containing `StackRepresentation` for each stack.

Important relations:

```text
offline_accounts.account_id
  -> offline_runs.account_id
offline_runs.run_id
  -> map_nodes.run_id
offline_runs.current_node_id
  -> map_nodes.node_id
map_nodes.node_id
  -> map_node_connections.from_node_id
map_nodes.node_id
  -> map_node_connections.to_node_id
```

Runtime node rule:

- `map_nodes.node_id` is the canonical integer node id for persisted runtime
  flow.
- authored node ids are seed/catalog input, not the DB relation authority.
- UI-facing route node labels can be reconstructed from catalog/seed context,
  but DB writes should persist integer `node_id`.
- `route_maps`, `route_paths`, and `route_nodes` are no longer schema/runtime
  tables. If they exist in an old local DB, reset/rebuild the Offline DB.

### Army Snapshots

Tables:

- `army_snapshots`
- `army_snapshot_stacks`
- `army_snapshot_stack_skills`

Primary owners:

- `OfflineArmySnapshotDbRepository.cs`
- `OfflineArmySnapshotModels.cs`
- `OfflineArmySnapshotMapper.cs`
- `OfflineArmySnapshotFactory.cs`
- `OfflineArmySnapshotDiff.cs`

Data:

- snapshot owner context: account, run, saved army, node,
- stack unit id,
- amount,
- formation slot,
- assigned skills.

Important relations:

```text
offline_accounts.account_id
  -> army_snapshots.account_id
offline_runs.run_id
  -> army_snapshots.run_id
army_snapshots.snapshot_id
  -> army_snapshot_stacks.snapshot_id
army_snapshot_stacks.snapshot_stack_id
  -> army_snapshot_stack_skills.snapshot_stack_id
```

Snapshot rules:

- a gameplay change creates a new snapshot,
- saved-army snapshots are immutable after creation,
- lost amounts are derived by comparing before/after snapshots,
- display fields are derived from DataMapper/catalogs.

### Event Journal

Tables:

- `run_events`

Detail tables:

- `run_battles`
- `reward_choices`
- `shop_purchases`

Primary owners:

- `OfflineRunBattleDbStore.cs`
- `OfflineRewardMapDbStore.cs`
- `OfflineRunShopDbStore.cs`

Data:

- what happened in a run,
- node,
- event type,
- before/after snapshot ids,
- before/after run gold,
- result text.

Event type ids:

- `StartRun = 1`
- `RouteTravel = 2`
- `Battle = 3`
- `Reward = 4`
- `Purchase = 5`
- `SaveArmy = 6`
- `RunComplete = 7`

Important relation:

```text
offline_runs.run_id
  -> run_events.run_id
map_nodes.node_id
  -> run_events.node_id
run_events.event_id
  -> run_battles.event_id
run_events.event_id
  -> reward_choices.event_id
run_events.event_id
  -> shop_purchases.event_id
```

Current caveat:

- `run_events` is the target shared journal. Some screen-specific persistence
  is already integrated, but any new movement between screens should check
  whether a matching event row is being created and linked.

### Run Battle

Tables:

- `run_battles`
- `run_battle_losses`

Primary owner:

- `OfflineRunBattleDbStore.cs`

Writes:

- battle event,
- prepared battle details,
- pre-battle snapshot,
- post-battle snapshot,
- launch adapter surface,
- outcome,
- next screen,
- loss rows.

Relations:

```text
run_events.event_id
  -> run_battles.event_id
run_battles.run_battle_id
  -> run_battle_losses.run_battle_id
army_snapshot_stacks.snapshot_stack_id
  -> run_battle_losses.snapshot_stack_id
```

Rule:

- `run_battle_losses` is a cached/detail record. Army state remains in snapshots.

### Reward Map

Tables:

- `reward_choices`
- `reward_cards`

Primary owner:

- `OfflineRewardMapDbStore.cs`

Writes:

- materialized generated reward choices for the run/current node,
- reward event,
- choice status,
- focused reward id,
- selected reward id,
- generated cards,
- operation JSON,
- preview snapshot id,
- applied snapshot id.

Relations:

```text
run_events.event_id
  -> reward_choices.event_id
reward_choices.reward_choice_id
  -> reward_cards.reward_choice_id
army_snapshot_stacks.snapshot_stack_id
  -> reward_cards.target_snapshot_stack_id
```

Rule:

- reward ruleset/catalog configuration remains authored code/assets data,
- reward choices are generated upfront when the run begins and stored as
  runtime rows keyed by run/node/catalog position identity,
- Reward Map loads the materialized runtime choice; it must not roll fallback
  screen-time rewards in production,
- DB stores the generated runtime choice and selected/applied result,
- applying the same choice twice must be rejected.

### Run Shop

Tables:

- `shop_visits`
- `shop_offers`
- `shop_purchases`

Primary owner:

- `OfflineRunShopDbStore.cs`

Writes:

- shop visit state,
- generated offers,
- focused offer,
- purchased flags,
- purchase event,
- purchase detail,
- after-purchase snapshot,
- run gold changes.

Relations:

```text
offline_runs.run_id
  -> shop_visits.run_id
map_nodes.node_id
  -> shop_visits.node_id
shop_visits.shop_visit_id
  -> shop_offers.shop_visit_id
run_events.event_id
  -> shop_purchases.event_id
shop_visits.shop_visit_id
  -> shop_purchases.shop_visit_id
shop_offers.shop_offer_id
  -> shop_purchases.shop_offer_id
```

Rule:

- offers persist and become purchased/unavailable; they should not disappear
  through normal-flow delete.
- no free no-trade-off Economy offer in V1.

### Summary Value

Tables:

- `run_summaries`
- `run_summary_entries`

Primary owner:

- `OfflineSummaryValueDbStore.cs`

Writes:

- final result,
- start snapshot,
- pre-final snapshot,
- post-final snapshot,
- saved-army candidate snapshot,
- account XP awarded preview,
- timeline entries.

Relations:

```text
offline_runs.run_id
  -> run_summaries.run_id
run_summaries.run_summary_id
  -> run_summary_entries.run_summary_id
army_snapshots.snapshot_id
  -> run_summary_entries.snapshot_id
```

Rule:

- won final creates a saved-army candidate from the pre-final snapshot.
- failed run cannot create a saved-army candidate.

Implementation note:

- `OfflineSummaryValueDbStore.ReplaceEntries()` soft-deactivates old entries
  with `is_active = 0` before inserting replacement timeline rows.

### Saved Armies

Tables:

- `saved_army_slots`
- `saved_armies`
- `saved_army_roster_state`
- `saved_army_history`

Primary owners:

- `OfflineSavedArmyDbRepository.cs`
- `OfflineSavedArmiesDbStore.cs`
- `OfflineSummaryValueDbStore.cs`
- `OfflineBattleResultDbStore.cs`

Writes:

- 8 physical saved-army slots,
- locked/unlocked slot availability,
- immutable saved-army identity,
- current defence,
- active/inactive replacement history,
- offence/defence history.

Relations:

```text
offline_accounts.account_id
  -> saved_army_slots.account_id
offline_accounts.account_id
  -> saved_armies.account_id
army_snapshots.snapshot_id
  -> saved_armies.snapshot_id
offline_runs.run_id
  -> saved_armies.created_from_run_id
saved_armies.saved_army_id
  -> saved_armies.replaced_by_saved_army_id
offline_accounts.account_id
  -> saved_army_roster_state.account_id
saved_armies.saved_army_id
  -> saved_army_roster_state.current_defence_saved_army_id
saved_armies.saved_army_id
  -> saved_army_history.saved_army_id
```

Rules:

- overwrite creates a new `saved_army_id`,
- previous saved army gets `active = 0`, `is_active = 0`,
  `replaced_by_saved_army_id = new id`,
- overwriting current defence clears current defence,
- history is keyed by saved army id, not slot id,
- no current defence is valid.

PRD030 override:

- legacy Arena/custom army import is not the product persistence path.
- seed snapshots or run-created snapshots should feed saved armies.

### Async Battle Result

Tables:

- `async_battle_results`
- `saved_army_history`
- `offline_accounts`
- `account_unlocks`

Lazy companion table:

- `async_battle_result_details`

Primary owner:

- `OfflineBattleResultDbStore.cs`

Writes:

- local async result,
- attacker/defender saved army ids,
- rank before/after/delta,
- account XP before/after/gained,
- preservation record,
- saved-army history rows,
- unlock progress.

Relations:

```text
offline_accounts.account_id
  -> async_battle_results.account_id
saved_armies.saved_army_id
  -> async_battle_results.attacker_saved_army_id
saved_armies.saved_army_id
  -> async_battle_results.defender_saved_army_id
async_battle_results.async_battle_result_id
  -> saved_army_history.async_battle_result_id
```

Rule:

- Offline result data is local-authoritative only and must not pretend to be
  future backend-authoritative Online Mode.
- attacker and defender armies are preserved; result recording must not mutate
  saved armies.

Migration caveat:

- `async_battle_result_details` is created lazily by code and is not currently
  part of `OfflineDatabaseSchemaV1.BuildStatements()`. Fold it into a formal
  migration if schema versioning advances.

## End-To-End Flow Writes

### Start Run

Writes:

- `offline_runs`
- `army_snapshots`
- `army_snapshot_stacks`
- `army_snapshot_stack_skills`
- `map_nodes`
- `map_node_connections`
- `map_node_enemies`

Primary code:

- `OfflineStartRunDbStore.SaveCreatedRun(...)`
- `OfflineRunContextDbWriter.InsertStartRun(...)`
- `OfflineRunContextDbWriter.AttachStartRunRouteAndArmy(...)`
- `OfflineRunContextDbReader.ToStartRunCreatedRecord(...)`

Rule:

- Start Run creates the `offline_runs` row through the shared writer.
- Start Run persists the starting army through `OfflineArmySnapshotDbRepository`.
- Start Run reloads the returned `CreatedRunRecord` through
  `OfflineRunContextDbReader` after transaction commit, so the frontend receives
  DB-backed ids and snapshot state.

### Travel On Run Map

Reads/writes:

- `offline_runs`
- `map_nodes`
- `map_node_connections`

Primary code:

- `OfflineRunMapDbStore.Save(...)`
- `OfflineRunMapDbStore.Find(...)`
- `OfflineRunContextDbWriter.UpdateRunMapState(...)`

### Prepare And Complete Battle

Writes:

- `run_events`
- `run_battles`
- `army_snapshots`
- `run_battle_losses`
- `offline_runs`

Primary code:

- `OfflineRunBattleDbStore.SavePreparedBattle(...)`
- `OfflineRunBattleDbStore.SaveCompletion(...)`
- `OfflineRunContextDbWriter.UpdateNodeArmyGoldScreen(...)`
- `OfflineRunContextDbWriter.UpdateArmyGoldScreen(...)`

### Generate And Apply Reward

Writes:

- `run_events`
- `reward_choices`
- `reward_cards`
- `army_snapshots`
- `offline_runs`

Primary code:

- `OfflineRewardMapDbStore.SaveChoice(...)`
- `OfflineRewardMapDbStore.SaveAppliedReward(...)`
- `OfflineRunContextDbWriter.UpdateNodeArmyGoldScreen(...)`
- `OfflineRunContextDbWriter.UpdateArmyGoldScreen(...)`

Rule:

- reward choice/card rows should already exist from Begin Run/run generation
  for reward-producing nodes; Reward Map applies the current materialized
  choice rather than generating a new fallback choice,
- Reward screen handoff uses `offline_runs.next_screen = Reward`, matching
  `RunBattleNextScreen.Reward` and `RewardMapScreenController` lookup.

### Open Shop, Buy, Leave

Writes:

- `shop_visits`
- `shop_offers`
- `run_events`
- `shop_purchases`
- `army_snapshots`
- `offline_runs`
- `map_nodes`

Primary code:

- `OfflineRunShopDbStore.SaveVisit(...)`
- `OfflineRunShopDbStore.SavePurchase(...)`
- `OfflineRunShopDbStore.LeaveVisit(...)`
- `OfflineRunContextDbWriter.UpdateNodeArmyGoldScreen(...)`

### Summary And Save Army

Writes:

- `run_summaries`
- `run_summary_entries`
- `army_snapshots`
- `saved_armies`
- `saved_army_slots`
- `saved_army_roster_state` when replacement clears defence.

Primary code:

- `OfflineSummaryValueDbStore.PersistAndLoad(...)`
- `OfflineSummaryValueDbStore.SaveCandidate(...)`
- `OfflineSavedArmyDbRepository.SaveSnapshotToSlot(...)`
- `OfflineRunContextDbWriter.UpdateSummarySnapshots(...)`

### Saved Armies Roster

Reads/writes:

- `saved_army_slots`
- `saved_armies`
- `saved_army_roster_state`
- `saved_army_history`

Primary code:

- `OfflineSavedArmiesDbStore`
- `OfflineSavedArmyDbRepository`

### Battle Result

Writes:

- `async_battle_results`
- `async_battle_result_details`
- `offline_accounts`
- `account_unlocks`
- `saved_army_history`

Primary code:

- `OfflineBattleResultDbStore.Save(...)`
- `OfflineBattleResultDbStore.Find(...)`

## Table Group Quick Reference

| Group | Tables | Main Code |
| --- | --- | --- |
| Foundation | `schema_version` | `OfflineDatabaseModule`, `OfflineDatabaseSchemaV1` |
| Account | `offline_accounts`, `account_unlocks` | `OfflineDatabaseAccountBootstrap`, `OfflineBattleResultDbStore` |
| Run | `offline_runs` | `OfflineRunContextDbReader`, `OfflineRunContextDbWriter` |
| Route | `map_nodes`, `map_node_connections`, `map_node_enemies` | `OfflineStartRunDbStore`, `OfflineRunMapDbStore`, `OfflineMaterializedRunMapDbStore` |
| Snapshot | `army_snapshots`, `army_snapshot_stacks`, `army_snapshot_stack_skills` | `OfflineArmySnapshotDbRepository`, `OfflineArmySnapshotMapper` |
| Event | `run_events` | battle/reward/shop DB stores |
| Battle | `run_battles`, `run_battle_losses` | `OfflineRunBattleDbStore` |
| Reward | `reward_choices`, `reward_cards` | `OfflineRewardMapDbStore` |
| Shop | `shop_visits`, `shop_offers`, `shop_purchases` | `OfflineRunShopDbStore` |
| Summary | `run_summaries`, `run_summary_entries` | `OfflineSummaryValueDbStore` |
| Saved Armies | `saved_army_slots`, `saved_armies`, `saved_army_roster_state`, `saved_army_history` | `OfflineSavedArmyDbRepository`, `OfflineSavedArmiesDbStore` |
| Async Result | `async_battle_results`, `async_battle_result_details` | `OfflineBattleResultDbStore` |

## Safe Change Rules

- Add new DB behavior in the owning DB store or shared repository.
- Add new `offline_runs` writes only through `OfflineRunContextDbWriter`.
- Add new run-context reads only through `OfflineRunContextDbReader` unless a
  slice store is loading its own detail row as part of the same transaction.
- Add new schema tables through an explicit migration/version plan.
- Use stable enum ids from `DB Enums.cs`; do not depend on enum declaration
  order.
- Do not write direct SQLite queries in UI controllers.
- Do not duplicate `INSERT INTO offline_runs` or `UPDATE offline_runs` SQL in
  screen-specific stores.
- Do not store authored unit/skill/reward/shop/route catalog truth in SQLite
  unless a future PRD explicitly changes the storage model.
- Do not change gameplay balance or float values as part of persistence work.
- Do not edit scenes, prefabs, materials, controllers, `.asmdef`, or `.asmref`
  without explicit permission.

## Verification Targets

EditMode tests to run manually in Unity:

- `OfflineDatabaseSchemaTests`
- `OfflineArmySnapshotMapperTests`
- `OfflineDatabaseLegacyIdentityTests`
- `OfflineStartRunRunMapDbTests`
- `OfflineRunBattleRewardDbTests`
- `OfflineRunShopDbTests`
- `OfflineSummarySavedArmiesDbTests`
- `OfflineBattleResultDbTests`
- `OfflineModeProductionCompositionTests`

Manual flow smoke test:

```text
Start Run
-> Run Map travel
-> Run Battle prepare/complete
-> Reward choose/apply
-> Run Shop buy/leave
-> Summary save pre-final army
-> Saved Armies set defence
-> Battle Result record/reload
```

Watch for:

- missing SQLite provider,
- stale placeholder ids,
- missing persisted run/node ids,
- duplicate-stack identity issues,
- unassigned UI references,
- Unity Console errors after prefab import.
