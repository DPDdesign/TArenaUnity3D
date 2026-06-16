# [TARENA] PRD030: Offline Mode Database And Persistence Foundation

- Status: draft-for-review
- Type: Parent PRD / Architecture Plan
- Area: Offline Mode, SQLite, Persistence, Run State, Saved Armies
- Label: needs-grill
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Related:
  - `_codex/tasks/020_PRD019_StartRun.md`
  - `_codex/tasks/021_PRD019_RunMap.md`
  - `_codex/tasks/022_PRD019_RunBattle.md`
  - `_codex/tasks/023_PRD019_RewardMap.md`
  - `_codex/tasks/024_PRD019_RunShop.md`
  - `_codex/tasks/025_PRD019_SummaryValue.md`
  - `_codex/tasks/026_PRD019_SavedArmies.md`
  - `_codex/tasks/027_PRD019_BattleResult.md`
- Blocked by: Product approval of this PRD030 task split

## Product Owner Summary

PRD030 turns the PRD019 run metagame prototype into durable Offline Mode state.

The target product outcome:

- remove production dependency on all PRD019 `InMemory...Store` state,
- create one shared army snapshot model used by runs, rewards, shops, summary,
  and saved armies,
- create one local Offline Mode database as the source of truth for metagame
  state,
- let later tasks plug Start Run, Run Map, Battle, Reward, Shop, Summary,
  Saved Armies, and Battle Result into that database one slice at a time.

The database is local-only for now. It is not Online Mode, not PlayFab, not PUN,
not Photon, and not cloud sync.

## Product Goal

When a player starts an Offline Mode run, the run should exist as durable local
state. The player should be able to leave a screen, reload a screen, or restart
the app and still have recoverable run, route, army, reward, shop, saved-army,
and account-progress data where the flow supports it.

The database must also make the architecture cleaner:

- UI displays state but does not own state.
- Domain services own rules.
- Persistence adapters save and load records.
- Authored catalogs remain authored catalogs unless a later task explicitly
  moves them.

## Current Context

PRD019 tasks 020-027 built separate Offline Mode slices under:

`TArenaUnity3D/Assets/Scripts/RunMetagame`

The current implementation is intentionally prototype-friendly:

- Start Run stores created runs in `InMemoryStartRunRecordStore`.
- Run Map stores route state in `InMemoryRunMapStore`.
- Run Battle stores prepared battles and completions in
  `InMemoryRunBattleStore`.
- Reward Map stores generated reward choices in `InMemoryRewardMapChoiceStore`.
- Run Shop stores visits and purchases in `InMemoryRunShopVisitStore`.
- Summary Value stores save candidates in `InMemorySummaryValueRosterStore`.
- Saved Armies stores roster and history in
  `InMemorySavedArmiesRosterStore` and
  `InMemorySavedArmiesAttackHistoryStore`.
- Battle Result stores async results in `InMemoryBattleResultStore`.

Those stores were useful for vertical slices. They are not the product state
model.

The code also has multiple army snapshot types:

- `RunArmySnapshot`
- `RunBattleArmySnapshot`
- `RewardMapArmySnapshot`
- `RunShopArmySnapshot`
- `SummaryValueArmySnapshot`
- `SavedArmy`

PRD030 should stop this drift. There should be one shared Offline Mode army
snapshot shape, with adapters only where older slice DTOs still need them.

## Source Of Truth Rules

### Runtime State

The Offline Mode database owns durable runtime state:

- active and completed runs,
- route progress,
- run events,
- current army snapshots,
- battle launch/completion records,
- reward choices and selected rewards,
- shop visits and purchases,
- run summaries,
- saved armies,
- local account progress,
- async/offence/defence battle results where simulated locally.

### Authored Catalogs

The database does not become the authored content catalog in PRD030.

Keep these as authored data/code/assets for now:

- unit definitions from the current unit catalog/DataMapper path,
- legal unit skill lists,
- skill execution ids,
- skill presentation catalog,
- route path catalog,
- reward template catalog,
- shop offer generation rules,
- encounter catalog.

The database stores references and runtime choices:

- `unit_id`,
- `skill_id`,
- `template_id`,
- `node_id`,
- `encounter_id`,
- quantities,
- unlock state,
- selected/applied state,
- snapshots and records.

This prevents SQLite from becoming a second source of truth for unit/skill
catalogs.

### Identifier Rules

Local SQLite records use plain integer ids:

- primary keys are `integer primary key`,
- foreign keys to local runtime records are integer ids,
- do not use Guid/string/composite ids for local DB records in V1.

Authored/catalog references stay as text ids:

- `unit_id`,
- `skill_id`,
- `template_id`,
- `encounter_id`,
- authored route/path/template ids.

Future online/sync identifiers should be added as explicit optional external
fields, for example `external_account_id` or `sync_id`, not by changing local
primary keys.

### Foreign Key And Soft Delete Rules

SQLite V1 should use real foreign keys:

- enable `PRAGMA foreign_keys = ON` for opened connections,
- local record references should be declared as foreign keys where SQLite can
  enforce them,
- do not use aggressive cascade delete for run history, snapshots, events,
  rewards, purchases, saved armies, or battle results.

Normal product flow does not hard-delete database records. Local runtime tables
include:

- `is_active` integer not null default 1

Deactivation, replacement, hiding, or logical removal should set
`is_active = 0` and update the relevant status enum where one exists. Destructive
delete is reserved for explicit dev/test reset tooling or schema migration
maintenance, not gameplay flow.

## Architecture Direction

### Deep Modules To Create

1. Offline Mode Database Module

   One Module owns the local database file, schema version, migrations,
   connection lifecycle, transaction helpers, and persistence errors.

   The Interface should be small: open/load database, ensure schema, expose
   persistence adapters, and reset dev/test data when explicitly allowed.

2. Shared Army Snapshot Module

   One Module owns the runtime army snapshot shape:

   - army snapshot id,
   - owner run or saved-army relation,
   - account id,
   - route node id where the snapshot was created,
   - stacks,
   - per-stack unit id,
   - amount,
   - formation slot,
   - per-stack assigned skills.

   Existing slice-specific DTOs may temporarily adapt to/from this shared
   shape, but database tables should not encode six incompatible army formats.

3. Offline Run State Module

   One Module owns active run state:

   - created run,
   - current route map,
   - current node,
   - current army snapshot,
   - current run gold,
   - run status,
   - next screen/flow state where needed.

4. Saved Army Roster Module

   One Module owns saved-army slots, immutable saved-army snapshots,
   active/inactive armies, current defence, and saved-army history.

5. Account Progress Module

   One Module owns local account XP, unlocked saved-army slots, unlocked maps,
   unlocked units, and unlocked skills for future runs.

### Adapter Rule

Each PRD019 screen should eventually use an Offline database adapter at the
same seam where it currently uses an `InMemory...Store`.

Do not make UI controllers query SQLite directly.

Do not move reward/shop/battle rules into SQL.

## Database Technology Decision

Target technology: SQLite for Offline Mode.

Current repository note from 2026-06-15 analysis:

- no SQLite package/reference was found in project scripts or manifest,
- no `Mono.Data.Sqlite`, `Microsoft.Data.Sqlite`, or SQLite plugin reference
  was found,
- adding the physical SQLite package/plugin is therefore an explicit
  implementation task, not an assumed dependency.

Approved V1 direction for planning:

- design the schema as SQLite,
- add the SQLite dependency only in the implementation task that owns the
  database foundation,
- keep all gameplay rules outside SQL.

Recommended database file location:

- `Application.persistentDataPath/TArenaOffline.db`

This is a recommendation and should be confirmed during the HITL gate.

## Draft SQLite Schema

This is a product/database draft. Exact C# DTO names and SQL column types can be
adjusted during implementation, but the concepts should remain stable.

### `schema_version`

Purpose: tracks database schema version.

Columns:

- `id` integer primary key
- `version` integer not null
- `applied_at_utc` text not null
- `notes` text

### `offline_accounts`

Purpose: one local account/player profile for Offline Mode.

Columns:

- `account_id` integer primary key
- `external_account_id` text
- `display_name` text
- `created_at_utc` text not null
- `updated_at_utc` text not null
- `account_xp` integer not null default 0
- `rank_value` integer not null default 1000
- `unlocked_saved_army_slots` integer not null default 2

Notes:

- `unlocked_saved_army_slots` controls gameplay availability.
- UI may still display 8 physical slots.

### `account_unlocks`

Purpose: generic local unlock records.

Columns:

- `unlock_id` integer primary key
- `account_id` integer not null
- `unlock_type` text not null
- `target_id` text not null
- `unlocked_at_utc` text not null

Allowed `unlock_type` examples:

- `map`
- `unit`
- `skill`
- `saved_army_slot`

### `offline_runs`

Purpose: durable run record created by Start Run.

Columns:

- `run_id` integer primary key
- `account_id` integer not null
- `game_mode_id` integer not null
- `authority_source_id` integer not null
- `run_status_id` integer not null
- `starting_army_template_id` text not null
- `starting_army_variant_id` text not null
- `selected_starting_army_id` text not null
- `selected_route_choice_id` text not null
- `route_map_id` integer
- `current_node_id` integer
- `current_army_snapshot_id` integer
- `start_army_snapshot_id` integer
- `pre_final_army_snapshot_id` integer
- `current_run_gold` integer not null default 0
- `stage_progress` integer not null default 0
- `route_progress` integer not null default 0
- `next_screen` text
- `created_at_utc` text not null
- `updated_at_utc` text not null

Allowed `run_status_id` enum meanings:

- `Created`
- `InProgress`
- `AwaitingBattle`
- `AwaitingReward`
- `InShop`
- `AwaitingFinal`
- `Won`
- `Lost`
- `Abandoned`

### `army_snapshots`

Purpose: one shared immutable or historical army snapshot.

Columns:

- `snapshot_id` integer primary key
- `account_id` integer not null
- `run_id` integer
- `saved_army_id` integer
- `node_id` integer
- `created_at_utc` text not null

Notes:

- Snapshots are records. Do not mutate a saved-army snapshot after creation.
- A new gameplay change creates a new snapshot.
- `node_id` replaces earlier `snapshot_kind` / `source` fields. The route node
  supplies battle, reward, shop, final, or other run context.
- `node_id` is a globally unique integer from the canonical `route_nodes`
  table. Do not build composite/string node ids for V1.
- A final snapshot should point at the final node.
- Test/seed snapshots that do not come from a normal run should be created by
  explicit seed/setup flow, not by importing legacy Arena data.

### `army_snapshot_stacks`

Purpose: stacks inside an army snapshot.

Columns:

- `snapshot_stack_id` integer primary key
- `snapshot_id` integer not null
- `unit_id` text not null
- `amount` integer not null default 0
- `formation_slot` integer not null default 0

Notes:

- `unit_id` points to the authored unit catalog.
- Do not store display name, tier, unit cost, combat value, level, experience,
  temporary power, or lost amount in V1 snapshots.
- Losses should be derived by comparing before/after snapshots.
- Stack ordering should be derived from `formation_slot`.

### `army_snapshot_stack_skills`

Purpose: skills assigned to a stack in that snapshot.

Columns:

- `snapshot_stack_skill_id` integer primary key
- `snapshot_stack_id` integer not null
- `skill_id` text not null
- `acquired_at_run_node_id` integer

Notes:

- `skill_id` uses the current stable skill string.
- If a skill record exists for a snapshot stack, that skill is available to the
  stack in that snapshot.
- Metaprogress unlocks decide what may appear in future runs. They live in
  account progress tables, not in army snapshots.

### `route_maps`

Purpose: one route map for a run.

Columns:

- `route_map_id` integer primary key
- `run_id` integer not null
- `selected_route_choice_id` text not null
- `created_from_catalog_id` text
- `created_at_utc` text not null
- `updated_at_utc` text not null

### `route_paths`

Purpose: paths shown inside a route map.

Columns:

- `route_path_id` integer primary key
- `route_map_id` integer not null
- `path_id` text not null
- `display_name` text not null
- `bias_description` text
- `sort_order` integer not null default 0

### `route_nodes`

Purpose: durable node state.

Columns:

- `node_id` integer primary key
- `route_map_id` integer not null
- `route_path_id` integer not null
- `node_type_id` integer not null
- `node_state_id` integer not null
- `stage_index` integer not null default 0
- `display_name` text not null
- `possible_reward_hint` text
- `expected_risk_hint` text
- `encounter_id` text
- `shop_visit_id` integer
- `next_node_id` integer
- `completed_at_utc` text

Notes:

- `route_nodes` is the one canonical table for run nodes.
- `node_id` is a plain integer id, unique across seeded run nodes.
- Other tables reference this integer directly instead of storing route node
  strings or duplicate node keys.

Allowed `node_type_id` enum meanings:

- `Start`
- `Battle`
- `Shop`
- `RecruitReward`
- `FinalBoss`

Allowed `node_state_id` enum meanings:

- `Locked`
- `Available`
- `Selected`
- `Completed`

### `run_events`

Purpose: one shared event timeline for a run.

Columns:

- `event_id` integer primary key
- `run_id` integer not null
- `account_id` integer not null
- `node_id` integer
- `event_type_id` integer not null
- `before_snapshot_id` integer
- `after_snapshot_id` integer
- `run_gold_before` integer
- `run_gold_after` integer
- `result` text
- `created_at_utc` text not null

Allowed `event_type_id` enum meanings:

- `StartRun`
- `RouteTravel`
- `Battle`
- `Reward`
- `Purchase`
- `SaveArmy`
- `RunComplete`

Notes:

- Battle, reward, and purchase records must reference `run_events.event_id`.
- `run_events` records what happened in the run.
- Army state lives in snapshots, not duplicated inside event detail records.
- Event detail tables store only details specific to that event type.
- Store event types and persistence statuses in SQLite as integer enum ids.
- Prefer one shared persistence enum file, for example `DB Enums.cs`, so event
  types and database status values do not drift across slices.
- Enum ids must be manually assigned and stable. They must not depend on enum
  declaration order in C#.
- These stable ids should remain usable later for translations, UI labels, and
  import/export or analytics mapping.

### `run_battles`

Purpose: prepared and completed run battle records.

Columns:

- `run_battle_id` integer primary key
- `event_id` integer not null
- `run_id` integer not null
- `node_id` integer not null
- `encounter_id` text not null
- `enemy_goal` text
- `battle_status_id` integer not null
- `pre_battle_snapshot_id` integer
- `post_battle_snapshot_id` integer
- `launch_payload_json` text
- `launch_adapter_surface` text
- `battle_outcome_id` integer
- `result_source` text
- `next_screen` text
- `prepared_at_utc` text
- `completed_at_utc` text

Allowed `battle_status_id` enum meanings:

- `Prepared`
- `Launched`
- `Completed`
- `Cancelled`

### `run_battle_losses`

Purpose: loss records for a completed battle.

Columns:

- `loss_id` integer primary key
- `run_battle_id` integer not null
- `snapshot_stack_id` integer not null
- `unit_id` text not null
- `amount_before` integer not null
- `amount_after` integer not null
- `lost_amount` integer not null

### `reward_choices`

Purpose: generated post-battle reward choice.

Columns:

- `reward_choice_id` integer primary key
- `event_id` integer not null
- `run_id` integer not null
- `run_battle_id` integer
- `node_id` integer
- `army_before_reward_snapshot_id` integer not null
- `focused_reward_id` text
- `selected_reward_id` text
- `run_gold_before` integer not null default 0
- `run_gold_after` integer
- `choice_status_id` integer not null
- `created_at_utc` text not null
- `applied_at_utc` text

Allowed `choice_status` examples:

- `Generated`
- `Selected`
- `Skipped`
- `Expired`

### `reward_cards`

Purpose: the actual reward cards in a choice.

Columns:

- `reward_card_id` integer primary key
- `reward_choice_id` integer not null
- `template_id` text not null
- `family` text not null
- `intention` text not null
- `rarity` text
- `title` text not null
- `verb` text
- `target_snapshot_stack_id` integer
- `operation_json` text not null
- `preview_text_before` text
- `preview_text_after` text
- `preview_snapshot_id` integer
- `applied_snapshot_id` integer
- `sort_order` integer not null default 0

Allowed `family` examples:

- `Mass`
- `Quality`
- `Width`
- `Skill`
- `Recovery`
- `Economy`

Allowed `intention` examples:

- `Stabilize`
- `Strengthen`
- `Pivot`

### `shop_visits`

Purpose: durable shop visit state.

Columns:

- `shop_visit_id` integer primary key
- `run_id` integer not null
- `node_id` integer not null
- `visit_status_id` integer not null
- `army_before_shop_snapshot_id` integer not null
- `current_army_snapshot_id` integer not null
- `run_gold_before` integer not null default 0
- `current_run_gold` integer not null default 0
- `focused_offer_id` text
- `created_at_utc` text not null
- `left_at_utc` text

Allowed `visit_status` examples:

- `Open`
- `Left`
- `Completed`

### `shop_offers`

Purpose: offers generated for a shop visit.

Columns:

- `shop_offer_id` integer primary key
- `shop_visit_id` integer not null
- `offer_category` text not null
- `title` text not null
- `detail` text
- `cost` integer not null default 0
- `available` integer not null default 1
- `purchased` integer not null default 0
- `affected_snapshot_stack_id` integer
- `operation_json` text not null
- `preview_text_before` text
- `preview_text_after` text
- `preview_snapshot_id` integer
- `purchase_snapshot_id` integer
- `sort_order` integer not null default 0

Allowed `offer_category` examples:

- `Recovery`
- `Resurrection`
- `Skill`
- `Stack`
- `UpgradeExchange`
- `Economy`

Note:

- PRD024 V1 decision removed the free economy offer. Do not persist a free
  RUN GOLD offer unless a later task defines a real trade-off.

### `shop_purchases`

Purpose: purchase history for a visit.

Columns:

- `shop_purchase_id` integer primary key
- `event_id` integer not null
- `shop_visit_id` integer not null
- `shop_offer_id` integer not null
- `run_id` integer not null
- `run_gold_before` integer not null
- `run_gold_after` integer not null
- `army_before_purchase_snapshot_id` integer not null
- `army_after_purchase_snapshot_id` integer not null
- `purchase_result_id` integer not null
- `message` text
- `purchased_at_utc` text not null

### `run_summaries`

Purpose: summary record for final run outcome.

Columns:

- `run_summary_id` integer primary key
- `run_id` integer not null
- `final_result_id` integer not null
- `start_snapshot_id` integer
- `pre_final_snapshot_id` integer
- `post_final_snapshot_id` integer
- `saved_army_candidate_snapshot_id` integer
- `account_xp_awarded` integer not null default 0
- `next_unlock_preview` text
- `created_at_utc` text not null

### `run_summary_entries`

Purpose: timeline entries shown on Summary Value.

Columns:

- `summary_entry_id` integer primary key
- `run_summary_id` integer not null
- `entry_type` text not null
- `title` text not null
- `detail` text
- `run_gold_delta` integer not null default 0
- `snapshot_id` integer
- `sort_order` integer not null default 0

### `saved_army_slots`

Purpose: physical saved-army slots.

Columns:

- `slot_id` integer primary key
- `account_id` integer not null
- `slot_index` integer not null
- `saved_army_id` integer
- `locked` integer not null default 0
- `updated_at_utc` text not null

Notes:

- There are 8 physical slots.
- Whether a slot is usable depends on `offline_accounts.unlocked_saved_army_slots`
  or derived account unlock state.

### `saved_armies`

Purpose: immutable saved armies produced by won runs or explicit database seed
data.

Columns:

- `saved_army_id` integer primary key
- `account_id` integer not null
- `snapshot_id` integer not null
- `created_from_run_id` integer
- `active` integer not null default 1
- `replaced_by_saved_army_id` integer
- `created_at_utc` text not null
- `deactivated_at_utc` text

Notes:

- Overwrite creates a new `saved_army_id`.
- The old saved army becomes inactive instead of being mutated.
- PRD030 does not import legacy Arena/custom armies. If seed data is needed,
  create explicit seed snapshots in the new database.

### `saved_army_roster_state`

Purpose: account-level saved-army roster state.

Columns:

- `account_id` integer primary key
- `current_defence_saved_army_id` integer
- `updated_at_utc` text not null

Notes:

- No current defence is valid.
- Overwriting the current defence army clears this value.

### `saved_army_history`

Purpose: offence/defence history by saved army id.

Columns:

- `history_id` integer primary key
- `saved_army_id` integer not null
- `async_battle_result_id` integer
- `result_kind_id` integer not null
- `opponent_name` text
- `attacker_value_at_battle` integer not null default 0
- `defender_value_at_battle` integer not null default 0
- `rank_delta` integer not null default 0
- `recorded_at_utc` text not null

Allowed `result_kind_id` enum meanings:

- `OffenceWin`
- `OffenceLoss`
- `DefenceWin`
- `DefenceLoss`

### `async_battle_results`

Purpose: local simulated offence/defence results from PRD027.

Columns:

- `async_battle_result_id` integer primary key
- `account_id` integer not null
- `attacker_saved_army_id` integer not null
- `defender_saved_army_id` integer not null
- `opponent_id` text
- `opponent_name` text
- `result_kind_id` integer not null
- `rank_before` integer not null
- `rank_after` integer not null
- `rank_delta` integer not null
- `account_xp_before` integer not null
- `account_xp_after` integer not null
- `account_xp_gained` integer not null
- `next_unlock_preview` text
- `preservation_record` text
- `result_source` text not null
- `recorded_at_utc` text not null

Notes:

- Offline simulated results must not pretend to be backend-authoritative online
  records.
- Attacker and defender saved armies are preserved.

## Data Flow Draft

### Start Run

1. Player chooses starting army and route preview.
2. Start Run creates `offline_runs`.
3. Start Run creates a `start` army snapshot.
4. `offline_runs.start_army_snapshot_id` and
   `offline_runs.current_army_snapshot_id` point to that snapshot.
5. Run Map receives durable `run_id`.

### Run Map

1. Run Map loads `offline_runs` by `run_id`.
2. If missing, it seeds the full run map upfront by creating `route_maps`,
   `route_paths`, and all `route_nodes` from authored route path catalog data.
3. Travel updates route node states and `offline_runs.current_node_id`.
4. Battle/shop/reward/final screens receive integer node ids from persisted
   state.

### Run Battle

1. Battle preparation creates a `run_events` record of type `Battle`.
2. `run_battles` stores battle-specific details and references that `event_id`.
3. Existing battle launch remains an adapter surface.
4. Battle completion creates post-battle snapshot and updates the `Battle`
   event before/after snapshot references.
5. `offline_runs.current_army_snapshot_id` points to post-battle snapshot.
6. `offline_runs.next_screen` becomes `Reward`, `RunLoss`, or `FinalSummary`.

### Reward Map

1. Reward choice creates a `run_events` record of type `Reward`.
2. Reward-specific details are saved as `reward_choices` and `reward_cards`
   referencing that `event_id`.
3. Focused reward can create preview snapshot records.
4. Applying a reward creates an applied snapshot and updates the event
   before/after snapshot references.
5. Run gold and current army update on `offline_runs`.

### Run Shop

1. Shop visit is created or loaded by run id and integer node id.
2. Offers are generated once and persisted.
3. Purchased offers remain purchased when the visit is reopened.
4. Each purchase creates a `run_events` record of type `Purchase`.
5. `shop_purchases` stores purchase-specific details and references that
   `event_id`.
6. Each purchase creates a new army snapshot and updates the event before/after
   snapshot references.
7. Leaving shop updates route/run flow state.

### Summary Value

1. Summary loads start, pre-final, and post-final snapshots.
2. Won final creates a saved-army candidate snapshot from pre-final snapshot.
3. Summary entries are stored for timeline display.
4. Saving the candidate creates a `saved_armies` record and assigns a
   `saved_army_slots` slot.

### Saved Armies

1. Roster loads 8 physical slots.
2. Slot availability is derived from unlocked saved-army slot count.
3. Test/dev saved armies, if needed, come from explicit seed snapshots in the
   new database.
4. Overwrite marks the old saved army inactive and stores a new saved army id.
5. Setting defence updates `saved_army_roster_state`.

### Battle Result

1. Local simulated async result creates `async_battle_results`.
2. Account rank and XP update on `offline_accounts`.
3. Saved-army history entries are written for relevant saved army ids.
4. No saved army is stolen, destroyed, or mutated by a result.

## PRD019 Fit Audit

This audit compares PRD030 against implemented PRD019 tasks 020-027 and the
current `Assets/Scripts/RunMetagame` code.

### Fit By Slice

- 020 Start Run fits `offline_runs`, start `army_snapshots`,
  `army_snapshot_stacks`, and `army_snapshot_stack_skills`. Current string/Guid
  ids and `Unlocked` skill DTOs are adapter details. The DB stores integer local
  ids and only the skills actually assigned in a runtime snapshot.
- 021 Run Map fits `route_maps`, `route_paths`, `route_nodes`, and
  `offline_runs.current_node_id`. Route nodes should be seeded upfront for the
  run. Authored node strings from current catalogs may exist during seeding, but
  runtime relations use integer `node_id`.
- 022 Run Battle fits `run_events`, `run_battles`, pre/post battle snapshots,
  and optional loss-cache records. Battle losses are derived from before/after
  snapshots; `run_battle_losses` must not become the source of truth for army
  state.
- 023 Reward Map fits `run_events`, `reward_choices`, `reward_cards`, preview
  snapshots, and applied snapshots. Reward templates remain authored catalog
  data.
- 024 Run Shop fits `shop_visits`, `shop_offers`, `shop_purchases`, purchase
  events, and after-purchase snapshots. Purchased/unavailable offers should be
  status/flag updates, not deletes. The free no-trade-off economy offer remains
  out of V1.
- 025 Summary Value fits `run_summaries`, `run_summary_entries`, pre-final
  snapshots, and saved-army candidate flow. The key rule remains: saved army is
  based on the pre-final snapshot, not post-final losses.
- 026 Saved Armies fits `saved_army_slots`, `saved_armies`,
  `saved_army_roster_state`, and `saved_army_history`. PRD030 intentionally
  overrides the PRD026 temporary Arena import helper: clean DB V1 uses explicit
  seed snapshots if test data is needed, not legacy import.
- 027 Battle Result fits `async_battle_results`, `saved_army_history`,
  `offline_accounts`, and `account_unlocks`. Results update rank, XP, and
  history without mutating saved armies.

### Architecture Diagnosis

Current code has the expected PRD019 prototype shape:

- production adapters instantiate `InMemory...Store` implementations,
- each slice has its own army snapshot DTO,
- ids are currently string/Guid-style values,
- slice DTOs include UI/helper fields such as `Level`, `Lost`, `CombatValue`,
  and `Unlocked`.

These are not PRD019 defects. They are the exact seams PRD030 should replace.

Deepening opportunities for PRD030:

1. Offline Database Module: one deep Module owns SQLite connection lifecycle,
   migrations, foreign key setup, transactions, and persistence errors.
2. Shared Army Snapshot Module: one deep Module owns durable snapshot records,
   stack records, skill records, and adapters to existing slice DTOs.
3. Run Event Journal Module: one deep Module owns `run_events` plus detail table
   references for battle, reward, purchase, save-army, and run-complete events.
4. Route Map Persistence Module: one deep Module seeds full route maps upfront
   and converts authored route/node strings into integer runtime node ids.
5. Account And Saved Army Module: one deep Module owns local account progress,
   slot availability, active/inactive saved armies, current defence, and saved
   army history.

Diagnostic conclusion:

- The PRD030 schema direction fits all PRD019 tasks.
- The main risk is implementation drift if each PRD019 slice writes its own DB
  adapter and DTO mapping independently.
- The mitigation is to implement `030_1`, `030_2`, and `030_3` first as shared
  modules and identity rules, then wire each slice through those modules instead
  of duplicating persistence per screen.

## Child Task Files

Approved child task files:

1. `_codex/tasks/030_1_PRD030_DatabaseFoundation.md`
2. `_codex/tasks/030_2_PRD030_SharedArmySnapshotAndPersistenceModels.md`
3. `_codex/tasks/030_3_PRD030_IntegerIdentityMigration.md`
4. `_codex/tasks/030_4_PRD030_RemovePRD26ArenaImport.md`
5. `_codex/tasks/030_5_PRD030_StartRun_RunMap_DBIntegration.md`
6. `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
7. `_codex/tasks/030_7_PRD030_RunShop_DBIntegration.md`
8. `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`
9. `_codex/tasks/030_9_PRD030_AccountProgress_BattleResult_DBIntegration.md`
10. `_codex/tasks/030_10_PRD030_RemoveInMemoryProductionUsage_FinalAudit.md`

## Original Draft Child Task Breakdown

This older 030A-030H breakdown is kept as historical context. The active work is
tracked in the child task files above.

### 030A - Database Foundation And SQLite Adapter

- Type: HITL
- Blocked by: approval of SQLite dependency/plugin choice

What to build:

- Add the Offline Mode Database Module.
- Add SQLite dependency/plugin if approved.
- Create/open `TArenaOffline.db`.
- Implement schema version table and V1 migration runner.
- Add persistence error handling and test/dev reset hook if approved.

Acceptance criteria:

- database file can be created and opened locally,
- schema version is written and read,
- missing/corrupt database returns controlled errors,
- no PRD019 screen is required to use it yet,
- no Unity scenes/prefabs/assets are edited.

### 030B - Shared Army Snapshot Persistence

- Type: AFK after 030A
- Blocked by: 030A

What to build:

- Add shared army snapshot model and persistence.
- Add mapping adapters from existing PRD019 army DTOs.
- Persist and load stacks plus per-stack assigned skill records.

Acceptance criteria:

- one shared snapshot shape can represent Start Run, Battle, Reward, Shop,
  Summary, and Saved Armies data,
- snapshot round-trip tests cover stacks, formation slots, amounts, and assigned
  skills,
- existing authored unit/skill catalogs remain the source for legal content,
- no gameplay values are changed.

### 030C - Start Run And Run Map Database Integration

- Type: AFK after 030A/030B
- Blocked by: 030A, 030B

What to build:

- Replace Start Run created-run in-memory authority with database persistence.
- Replace Run Map route-state in-memory authority with database persistence.
- Create/load route map by durable `run_id`.

Acceptance criteria:

- Begin Run creates durable `run_id`,
- Run Map can load that run id after adapter recreation,
- route progress survives reload,
- current run gold and current army snapshot are loaded from database,
- UI still calls services/adapters, not SQLite directly.

### 030D - Run Battle And Reward Database Integration

- Type: AFK after 030C
- Blocked by: 030C

What to build:

- Persist prepared battle records, launch metadata, completion payloads,
  losses, post-battle snapshots, reward choices, reward cards, reward previews,
  and applied reward results.

Acceptance criteria:

- Run Battle receives route node/encounter ids from persisted route state,
- battle completion writes post-battle snapshot and next-screen decision,
- Reward Map reloads generated choices by choice id,
- selected reward updates current army snapshot and run gold,
- preview-vs-apply consistency remains tested.

### 030E - Run Shop Database Integration

- Type: AFK after 030C
- Blocked by: 030C

What to build:

- Persist shop visits, offers, purchase records, purchased-offer state, current
  run gold, and army-after-purchase snapshots.

Acceptance criteria:

- reopening a shop visit preserves purchased offers,
- each purchase creates a durable new current army snapshot,
- current run gold survives adapter recreation,
- free no-trade-off economy offer is not persisted in V1,
- leaving shop updates route/run flow state.

### 030F - Summary Value And Saved Army Roster Database Integration

- Type: AFK after 030B
- Blocked by: 030B

What to build:

- Persist run summaries, summary timeline entries, saved-army candidate
  snapshots, saved-army slots, saved armies, active/inactive saved-army state,
  and current defence state.

Acceptance criteria:

- won final can save a pre-final snapshot as a saved army,
- failed run cannot create a saved army,
- overwrite creates a new saved army id and invalidates the old one,
- overwriting current defence clears defence,
- 8 physical slots and unlocked-slot count are both represented,
- Saved Armies and Summary Value use the same roster data.

### 030G - Account Progress And Battle Result Persistence

- Type: AFK after 030F
- Blocked by: 030F

What to build:

- Persist local account XP, rank value, unlock records, async battle results,
  saved-army battle history, and no-steal/no-destroy preservation records.

Acceptance criteria:

- local Battle Result records survive reload,
- rank and account XP update local account state,
- next unlock preview can be loaded from persisted account progress,
- saved-army history is queried by saved army id,
- saved armies are not mutated by battle results.

### 030H - Remove PRD019 InMemory Production Usage

- Type: AFK after 030C-030G
- Blocked by: 030C, 030D, 030E, 030F, 030G

What to build:

- Remove or demote PRD019 `InMemory...Store` usage from production Offline Mode
  paths.
- Keep test doubles only where useful for EditMode tests.
- Make UI/prototype controllers receive database-backed adapters through one
  Offline Mode composition point.

Acceptance criteria:

- production Offline Mode no longer creates fresh in-memory stores in screen
  controllers,
- in-memory stores are either removed or clearly test-only,
- one Offline Mode database composition path creates the adapters,
- PRD019 flow can be reasoned about from database state.

## Out Of Scope

- Online Mode.
- Backend authority.
- PlayFab, Photon, PUN, cloud sync, matchmaking, or account login.
- Migration of old legacy `buildN.d` files into PRD030.
- Import/helper flow from legacy Arena/custom armies.
- Moving authored unit, skill, route, reward, shop, or encounter catalogs into
  SQLite.
- Rebalancing unit stats, skill values, reward values, shop prices, ranking
  formula, or gameplay floats.
- Rewriting battle gameplay.
- Editing Unity scenes, prefabs, materials, controllers, `.inputactions`,
  `.asmdef`, or `.asmref` unless a specific child task explicitly permits it.

## Testing Requirements

Add EditMode tests around persistence interfaces and adapters:

- schema creation and version read/write,
- run create/reload by `run_id`,
- shared army snapshot round-trip,
- route progress round-trip,
- battle completion and loss round-trip,
- reward choice and applied reward round-trip,
- shop visit and purchased-offer round-trip,
- summary and saved-army save/overwrite round-trip,
- account progress and battle result round-trip,
- missing/corrupt record error handling.

Manual Unity validation remains required after screen wiring tasks:

- Start Run creates durable run id.
- Run Map resumes route progress.
- Reward/Shop reload current army and run gold.
- Saved Armies reloads roster and current defence.

## Parent Acceptance Criteria

PRD030 as a whole is complete when:

- Offline Mode has one local database source of truth,
- production PRD019 state no longer depends on `InMemory...Store`,
- one shared army snapshot shape is used for durable run and saved-army state,
- Start Run creates a durable run,
- Run Map loads and advances durable route state,
- Run Battle, Reward Map, Run Shop, Summary Value, Saved Armies, and Battle
  Result persist their state through the same Offline Mode database,
- account progress and unlocked saved-army slots persist locally,
- UI does not own database state or query SQL directly,
- authored unit and skill catalogs remain the legal content source,
- tests cover the main persistence round trips,
- no backend SDK, online transport, or gameplay rebalance is introduced.

## Open Questions For Grill

1. Is SQLite approved as the actual V1 implementation, including adding a Unity
   SQLite dependency/plugin?
2. Is `Application.persistentDataPath/TArenaOffline.db` approved as the local DB
   file path?
3. Should PRD030 include explicit seed data for one or more starter saved-army
   snapshots, or should seed data wait for a later dev tooling task?
4. Should all 8 saved-army physical slots exist in the DB immediately, with
   unlocked count deciding availability?
5. Should dev/test reset tooling be allowed from an Editor menu command, a
   hidden debug button, or tests only?
6. Are the active child task files `030_1` through `030_10` the right
   granularity?
7. Should persistence-facing enums and statuses be centralized in one code file,
   for example `DB Enums.cs`, while being stored as enum ids in SQLite?
8. Should persistence enum ids be manually assigned and treated as stable
   contract values across code, UI labels, and future translations?
