# [TARENA] PRD019/PRD030 Run Metagame Code Map

Status: active
Last updated: 2026-06-17

## Purpose

This map is the agent entry point for PRD019 Run Metagame and PRD030 Offline
Database work.

Use it when moving logic between Start Run, Run Map, Reward Map, Run Shop,
Summary Value, Saved Armies, Battle Result, and the Offline Mode database.

## Source Set

Primary PRDs:

- `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`

PRD019 child tasks:

- `_codex/tasks/020_PRD019_StartRun.md`
- `_codex/tasks/021_PRD019_RunMap.md`
- `_codex/tasks/022_PRD019_RunBattle.md`
- `_codex/tasks/023_PRD019_RewardMap.md`
- `_codex/tasks/024_PRD019_RunShop.md`
- `_codex/tasks/025_PRD019_SummaryValue.md`
- `_codex/tasks/026_PRD019_SavedArmies.md`
- `_codex/tasks/027_PRD019_BattleResult.md`

PRD030 child tasks:

- `_codex/tasks/030_1_PRD030_DatabaseFoundation.md`
- `_codex/tasks/030_2_PRD030_SharedArmySnapshotAndPersistenceModels.md`
- `_codex/tasks/030_3_PRD030_IntegerIdentityMigration.md`
- `_codex/tasks/030_4_PRD030_RemovePRD26ArenaImport.md`
- `_codex/tasks/030_5_PRD030_StartRun_RunMap_DBIntegration.md`
- `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
- `_codex/tasks/030_7_PRD030_RunShop_DBIntegration.md`
- `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`
- `_codex/tasks/030_9_PRD030_AccountProgress_BattleResult_DBIntegration.md`
- `_codex/tasks/030_10_PRD030_RemoveInMemoryProductionUsage_FinalAudit.md`

Current implementation root:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/`

Current EditMode tests:

- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/`

## Product Shape

PRD019 defines the Offline Mode run loop:

1. Start Run selects a weaker starting army and route.
2. Run Map advances through node-route choices.
3. Run Battle prepares/completes the existing tactical battle through an adapter.
4. Reward Map gives a 1-of-3 reward choice.
5. Run Shop offers constrained in-run purchases.
6. Summary Value turns a won final into a pre-final saved-army candidate.
7. Saved Armies stores immutable armies, defence choice, and history.
8. Battle Result records local async offence/defence rank and XP outcomes.

PRD030 turns that prototype loop into durable local Offline Mode state:

- one SQLite database,
- one shared persisted army snapshot shape,
- integer local DB ids,
- screen adapters and services, not UI-owned state,
- in-memory stores only as explicit test doubles.

## Current Status Reading

Several PRD030 child files still say `Status: draft` in their header while
their implementation sections and code show completed DB integration work.
For planning, treat the implementation sections and code as stronger evidence
than the stale status header.

Unity compilation and Play Mode validation remain manual unless a future task
explicitly allows running Unity tests outside the editor.

## Architecture Layers

### Composition Layer

Start here before wiring any production screen:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`

Responsibilities:

- opens/ensures the default DB,
- creates DB-backed services and adapters,
- centralizes all `Offline...DbStore` construction,
- provides the shared DataMapper-backed snapshot resolver.

Rule:

- production code should not construct `new InMemory...Store()`.
- production code should not construct `new Offline...DbStore()` outside
  `OfflineModeDatabaseComposition`.
- Default adapter constructors such as `new OfflineStartRunAdapter()` are
  acceptable only because they delegate to the composition point.

Guard test:

- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineModeProductionCompositionTests.cs`

### Database Foundation

Key files:

- `030_Database/OfflineDatabaseModule.cs` - open/create, schema version, PRAGMA
  foreign keys, migration transaction.
- `030_Database/OfflineDatabaseProvider.cs` - resolves `Microsoft.Data.Sqlite`
  or `Mono.Data.Sqlite`.
- `030_Database/OfflineDatabaseSql.cs` - small SQL helper API.
- `030_Database/OfflineDatabaseSchemaV1.cs` - schema table definitions.
- `030_Database/DB Enums.cs` - stable manually assigned DB enum ids.
- `030_Database/OfflineDatabaseAccountBootstrap.cs` - default local account.
- `030_Database/OfflineDatabaseLegacyIdentity.cs` - legacy string label to
  integer id conversion.
- `030_Database/OfflineRunContextDbReader.cs` - shared read side for persisted
  run context, `next_screen` lookup, Start Run-created record reload, current
  snapshots, summary lookup, and latest async battle result id.
- `030_Database/OfflineRunContextDbWriter.cs` - shared write side for
  `offline_runs` creation and run-context updates.

Database file:

- `Application.persistentDataPath/TArenaOffline.db`

Run context rule:

- `OfflineRunContextDbWriter` is the only runtime code surface that should
  contain `INSERT INTO offline_runs` or `UPDATE offline_runs`.
- Slice DB stores own their detail tables, but update `offline_runs` through
  the writer.
- UI controllers read persisted run state via adapters/services and
  `OfflineRunContextDbReader`, not serialized sample ids or direct SQLite.

### Shared Army Snapshot Module

Key files:

- `030_Database/OfflineArmySnapshotModels.cs`
- `030_Database/OfflineArmySnapshotFactory.cs`
- `030_Database/OfflineArmySnapshotMapper.cs`
- `030_Database/OfflineArmySnapshotDiff.cs`
- `030_Database/OfflineArmySnapshotDbRepository.cs`
- `030_Database/DataMapperOfflineArmySnapshotCatalogResolver.cs`

Persistent truth:

- snapshot id,
- account/run/saved-army/node references,
- stack `unit_id`,
- stack `amount`,
- stack `formation_slot`,
- assigned stack `skill_id`.

Not persistent V1 truth:

- display name,
- tier,
- combat value,
- level,
- lost amount,
- experience,
- temporary power,
- skill `Unlocked` UI flag.

Those can exist in screen DTOs but must be derived/adapted.

Known risk:

- Some reward/battle runtime stack ids are reconstructed from persisted
  snapshot data. Duplicate stacks of the same unit type may need stricter
  runtime stack identity later.

## Slice Map

### 020 Start Run

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/`

Main code:

- `StartRunModels.cs` - starting army, route preview, run-start DTOs.
- `StartRunContracts.cs` - catalog/unit/store interfaces and in-memory test
  store.
- `DefaultStartRunCatalog.cs` - authored V1 starting armies and route previews.
- `DataMapperStartRunUnitSource.cs` - current unit definition bridge.
- `StartRunService.cs` - screen data and begin-run command.
- `OfflineStartRunAdapter.cs` - UI/service facade.
- `OfflineStartRunDbStore.cs` - persists Start Run detail flow through shared
  run context writer, shared snapshot repository, `map_nodes`,
  `map_node_connections`, and `map_node_enemies`.
- `StartRunScreenController.cs` - Unity UI controller.
- `StartRunArmyCardView.cs` - starting-army card view; binds one
  `StartingArmyOptionViewData` into the instantiated card and displays its
  stacks through `StackRepresentation` children under its `stackRowsParent`.

DB handoff:

- creates durable `run_id` through `OfflineRunContextDbWriter.InsertStartRun`,
- writes `start_army_snapshot_id` through shared snapshot persistence,
- seeds route map and nodes upfront,
- attaches route map and initial army through
  `OfflineRunContextDbWriter.AttachStartRunRouteAndArmy`,
- reloads the returned `CreatedRunRecord` through
  `OfflineRunContextDbReader.ToStartRunCreatedRecord`,
- returns legacy-facing ids such as `run-<id>` for screen DTO compatibility.
- does not create or write legacy `route_maps`, `route_paths`, or
  `route_nodes` tables.
- selected army details and army cards display live `StartRunStackViewData`
  through the two prefabs selected on `StartRunScreenController`: whole
  `startingArmyPrefab` cards for each starting-army option and
  `armyDetailsPrefab` stack rows for each selected-army stack.

Tests:

- `StartRunServiceTests.cs`
- `OfflineStartRunRunMapDbTests.cs`

### 021 Run Map

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/`

Main code:

- `RunMapModels.cs` - route map, node, travel DTOs.
- `RunMapContracts.cs` - path catalog/store interface and in-memory test store.
- `DefaultRunMapPathCatalog.cs` - current authored route paths.
- `RunMapService.cs` - create/load/travel rules.
- `OfflineRunMapAdapter.cs` - UI/service facade.
- `OfflineRunMapDbStore.cs` - loads and updates persisted route state.
- `RunMapController.cs` - Unity controller backed by
  `OfflineModeDatabaseComposition.CreateRunMapAdapter()` and
  `OfflineRunContextDbReader`.
- `RunMetagameStackListPresenter.cs` - shared runtime helper for displaying
  stack DTOs by instantiating a prefab that contains `StackRepresentation`.

DB handoff:

- reads by durable `run_id`,
- controller creates `RunMapCreateRequest` from latest persisted `RunMap`
  context, route choice, current gold, and current army snapshot summary,
- controller renders the army panel from hydrated current snapshot stacks into
  `StackRepresentation` row prefab instances,
- updates `map_nodes.node_state_id`,
- updates `offline_runs.current_node_id`, `stage_progress`, and
  `route_progress` through `OfflineRunContextDbWriter.UpdateRunMapState`.

Tests:

- `RunMapServiceTests.cs`
- `OfflineStartRunRunMapDbTests.cs`

### 022 Run Battle

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/`

Main code:

- `RunBattleModels.cs` - battle launch, completion, losses, transition DTOs.
- `RunBattleContracts.cs` - encounter source, launch adapter, store interface.
- `DefaultRunBattleEncounterCatalog.cs` - small V1 local encounter catalog.
- `OfflineRunBattleLaunchAdapter.cs` - runtime snapshot battle input label.
- `RunBattleService.cs` - prepare/complete battle behavior.
- `OfflineRunBattleAdapter.cs` - service facade.
- `OfflineRunBattleDbStore.cs` - DB persistence for prepared/completed battles.

DB handoff:

- prepare writes `run_events` type Battle and `run_battles`,
- pre-battle and post-battle armies use shared snapshots,
- completion writes `run_battle_losses`,
- updates `offline_runs.current_army_snapshot_id`, status, current node, and
  run gold through `OfflineRunContextDbWriter`.

Not done here:

- no tactical battle rewrite,
- no scene launch replacement,
- existing `HexMap`/`TeamClass`/PlayerPrefs battle path remains adapter surface.

Tests:

- `RunBattleServiceTests.cs`
- `OfflineRunBattleRewardDbTests.cs`

### 023 Reward Map

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/`

Main code:

- `RewardMapModels.cs` - reward families, intentions, cards, previews, apply
  result.
- `RewardMapContracts.cs` - unit source, template catalog, choice store.
- `DefaultRewardMapTemplateCatalog.cs` - current authored reward templates.
- `RewardMapService.cs` - generate, preview, apply reward rules.
- `OfflineRewardMapAdapter.cs` - service facade.
- `OfflineRewardMapDbStore.cs` - DB persistence for choices/cards/apply.
- `RewardMapScreenController.cs` and view classes - Unity UI.

DB handoff:

- reward choices are generated and materialized upfront when the run begins;
  Reward Map loads the materialized choice for the current run/node instead of
  rolling screen-time fallback rewards,
- choice rows still use `run_events` type Reward, `reward_choices`, and
  `reward_cards` or their future minimal generated-reward successors,
- focused preview can write `preview_snapshot_id`,
- apply writes applied snapshot, selected reward, and run gold/current army,
- handoff to Reward Map uses `offline_runs.next_screen = Reward`,
- second apply returns `RewardMapError.AlreadyApplied`.

Tests:

- `RewardMapServiceTests.cs`
- `OfflineRunBattleRewardDbTests.cs`

### 024 Run Shop

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/024_RunShop/`

Main code:

- `RunShopModels.cs` - visit, offers, operation, purchase, leave DTOs.
- `RunShopContracts.cs` - visit store interface and in-memory test store.
- `RunShopService.cs` - offer build, preview, purchase, leave rules.
- `DataMapperRunShopUnitSource.cs` - current unit bridge.
- `OfflineRunShopAdapter.cs` - service facade.
- `OfflineRunShopDbStore.cs` - DB persistence for visits/offers/purchases.
- `RunShopScreenController.cs` and view classes - Unity UI.

DB handoff:

- visit writes or loads `shop_visits`,
- generated offers persist once in `shop_offers`,
- purchase writes `run_events` type Purchase and `shop_purchases`,
- purchase writes a new current army snapshot and updates run gold,
- leaving updates visit/run/route flow state through
  `OfflineRunContextDbWriter`.

Product rule:

- free no-trade-off Economy offer is removed from V1 shop.

Tests:

- `RunShopServiceTests.cs`
- `OfflineRunShopDbTests.cs`

### 025 Summary Value

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/`

Main code:

- `SummaryValueModels.cs` - final result, timeline, candidate, slot DTOs.
- `SummaryValueContracts.cs` - roster/persistence interfaces.
- `SummaryValueService.cs` - summary and save/overwrite command logic.
- `OfflineSummaryValueAdapter.cs` - service facade.
- `OfflineSummaryValueDbStore.cs` - DB persistence for summary and save slots.
- `SummaryValueScreenController.cs` and view classes - Unity UI.

DB handoff:

- writes/loads `run_summaries`,
- soft-replaces `run_summary_entries`,
- won final uses pre-final snapshot as `saved_army_candidate_snapshot_id`,
- updates run summary snapshot references through
  `OfflineRunContextDbWriter.UpdateSummarySnapshots`,
- save candidate delegates to saved-army DB repository.

Product rule:

- saved army comes from the pre-final snapshot, not post-final losses.

Tests:

- `SummaryValueServiceTests.cs`
- `OfflineSummarySavedArmiesDbTests.cs`

### 026 Saved Armies

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/026_SavedArmies/`

Main code:

- `SavedArmiesModels.cs` - slot, saved army, seed/import, history DTOs.
- `SavedArmiesContracts.cs` - unit source, seed source, roster/history store.
- `SavedArmiesService.cs` - roster, seed load, overwrite, defence, history.
- `SavedArmiesValueCalculator.cs` - dynamic army value.
- `DataMapperSavedArmiesUnitSource.cs` - current unit bridge.
- `OfflineSavedArmiesAdapter.cs` - service facade.
- `OfflineSavedArmiesDbStore.cs` - DB-backed roster/history store.
- `SavedArmiesScreenController.cs` and view classes - Unity UI.

Shared DB repository:

- `030_Database/OfflineSavedArmyDbRepository.cs`

DB handoff:

- 8 physical slots live in `saved_army_slots`,
- slot availability derives from `offline_accounts.unlocked_saved_army_slots`,
- save/overwrite creates a new `saved_army_id`,
- old army is deactivated, not hard-deleted,
- overwriting current defence clears defence,
- history is keyed by saved army id, not slot id.

PRD030 override:

- PRD26's development `Import from Arena` is not the product DB path.
- PRD030 uses explicit seed snapshots for dev/test data.
- Legacy import-compatible code may still exist adjacent to the flow; do not
  extend it as product persistence.

Tests:

- `SavedArmiesServiceTests.cs`
- `OfflineSummarySavedArmiesDbTests.cs`

### 027 Battle Result

Folder:

- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/`

Main code:

- `BattleResultModels.cs` - async result, rank, XP, preservation DTOs.
- `BattleResultContracts.cs` - result store interface and in-memory test store.
- `BattleResultService.cs` - local deterministic result calculation.
- `OfflineBattleResultAdapter.cs` - service facade.
- `OfflineBattleResultDbStore.cs` - DB persistence for local async result.
- `BattleResultScreenController.cs` and view classes - Unity UI.

DB handoff:

- writes `async_battle_results`,
- updates `offline_accounts.account_xp` and `rank_value`,
- writes `account_unlocks`,
- writes `saved_army_history`,
- keeps attacker and defender armies preserved,
- has a lazy companion table `async_battle_result_details` for exact UI payload
  reload.

Known design debt:

- progression threshold behavior partly lives in `OfflineBattleResultDbStore`
  while some preview text lives in service-level code. Centralize before
  expanding account progression.

Tests:

- `BattleResultServiceTests.cs`
- `OfflineBattleResultDbTests.cs`

## Shared Flow Map

```text
StartRunScreenController
  -> OfflineStartRunAdapter
  -> StartRunService
  -> OfflineStartRunDbStore
  -> OfflineRunContextDbWriter + OfflineArmySnapshotDbRepository
  -> OfflineRunContextDbReader
  -> offline_runs + army_snapshots + map_nodes/map_node_connections/map_node_enemies

RunMapController
  -> OfflineRunMapAdapter
  -> RunMapService
  -> OfflineRunMapDbStore
  -> OfflineRunContextDbWriter
  -> offline_runs + map_nodes/map_node_connections

RunBattle caller/future screen
  -> OfflineRunBattleAdapter
  -> RunBattleService
  -> OfflineRunBattleDbStore
  -> OfflineRunContextDbWriter
  -> run_events + run_battles + run_battle_losses + army_snapshots

RewardMapScreenController
  -> OfflineRunContextDbReader
  -> OfflineRewardMapAdapter
  -> RewardMapService
  -> OfflineRewardMapDbStore
  -> OfflineRunContextDbWriter
  -> run_events + reward_choices + reward_cards + army_snapshots

RunShopScreenController
  -> OfflineRunContextDbReader
  -> OfflineRunShopAdapter
  -> RunShopService
  -> OfflineRunShopDbStore
  -> OfflineRunContextDbWriter
  -> shop_visits + shop_offers + run_events + shop_purchases

SummaryValueScreenController
  -> OfflineRunContextDbReader
  -> OfflineSummaryValueAdapter
  -> SummaryValueService
  -> OfflineSummaryValueDbStore / OfflineSavedArmyDbRepository
  -> OfflineRunContextDbWriter
  -> run_summaries + run_summary_entries + saved_army_slots + saved_armies

SavedArmiesScreenController
  -> OfflineSavedArmiesAdapter
  -> SavedArmiesService
  -> OfflineSavedArmiesDbStore / OfflineSavedArmyDbRepository
  -> saved_army_slots + saved_armies + saved_army_roster_state + saved_army_history

BattleResultScreenController
  -> OfflineRunContextDbReader
  -> OfflineBattleResultAdapter
  -> BattleResultService
  -> OfflineBattleResultDbStore
  -> async_battle_results + offline_accounts + account_unlocks + saved_army_history
```

## Screen Movement Rules

When moving data from one screen to another:

- pass persisted ids, not copied UI state, whenever possible;
- prefer `run-<id>`, `node-<id>`, `reward-choice-<id>`, `shop-visit-<id>`,
  `saved-army-<id>` facade labels at UI boundaries only;
- convert to integer ids inside DB stores through `OfflineDatabaseLegacyIdentity`;
- load current army from `offline_runs.current_army_snapshot_id`;
- use `offline_runs.next_screen` values that match code contracts, for example
  `Reward`, `RunShop`, and `RunMap`;
- do not make UI controllers query SQLite directly;
- do not put reward/shop/battle rules in SQL;
- do not move authored catalogs into the database in PRD030.

## Authored Catalogs That Stay Out Of DB

These remain code/assets/catalog references, not SQLite-owned truth:

- starting army catalog,
- route path catalog,
- encounter catalog,
- reward ruleset/catalog configuration,
- run shop offer generation rules,
- current unit catalog/DataMapper path,
- legal unit skill lists,
- skill execution ids,
- skill presentation catalog.

The database stores runtime materialized generated results and references such
as `unit_id`, `skill_id`, `template_id` or catalog position id, reward id,
`encounter_id`, selected/applied state, snapshots, and records. Runtime screens
load these generated rows for the run/node instead of rolling new content.

## Current Manual QA Surface

Relevant PRD019 Unity prefabs/screens live under:

- `TArenaUnity3D/Assets/Resources/UI/PRD_19/020_StartRun/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/021_RunMap/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/023_RewardMap/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/024_RunShop/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/025_SummaryValue/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/026_SavedArmies/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/027_BattleResult/`

Backend-only Run Battle intentionally has no active PRD019 UI prefab.

Manual integration task files:

- `_codex/tasks/RunMetaGame_Tests/020_PRD019_StartRun_ManualIntegrationTest.md`
- `_codex/tasks/RunMetaGame_Tests/024_PRD019_RunShop_ManualIntegrationTest.md`

## Known Risks And Follow-Ups

- Unity tests were not run automatically in the reviewed implementation passes.
- Manual testing still needs a real Start Run -> Run Map flow so screen
  controllers can load persisted `run-*`, `node-*`, and `next_screen` state.
- `async_battle_result_details` is created lazily by Battle Result DB code and
  should be folded into formal schema migration if schema versioning advances.
- Duplicate unit stacks may expose weak runtime stack identity where DTOs
  rebuild stack ids from persisted formation/unit data.
- PRD026 legacy Arena import compatibility code remains adjacent; product DB
  work should use seed snapshots or run-created snapshots instead.
- Account progression thresholds should be centralized before extending unlocks.

## Agent Checklist

Before changing a PRD019/PRD030 screen:

1. Read this map and the matching child PRD.
2. Identify the service, adapter, store, and tables from the slice map.
3. Check whether the change is UI-only, domain-rule, or DB persistence.
4. Keep UI on adapter/service APIs.
5. Keep DB writes in the relevant DB store or shared repository.
6. Use `OfflineRunContextDbReader` / `OfflineRunContextDbWriter` for
   `offline_runs` state.
7. Use shared `OfflineArmySnapshotMapper` and `OfflineArmySnapshotDbRepository`
   for any persisted army state.
8. Add or update EditMode tests at the service/store seam.
9. Leave Unity compilation and Play Mode validation as manual unless explicitly
   allowed.
