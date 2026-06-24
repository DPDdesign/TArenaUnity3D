# [TARENA] Current State

Status: living document
Last updated: 2026-06-24
Owner: Project Director

## Purpose

This document is the return-from-vacation state file for TArenaUnity3D.
Update it with user answers and Unity-side verification results.

Source priority used for this pass:

- `_codex/agents/project-director-agent.md`
- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `_codex/Context/02_Current_State.md`
- PRD/task files 019, archived 021, 035, 037, 038, 039, 040
- matching QA reports from 2026-06-16 through 2026-06-18
- code under `TArenaUnity3D/Assets/Scripts/RunMetagame/`

## Executive Summary

TArenaUnity3D is still a legacy recovery project, but the Offline Mode run
metagame has a substantial vertical-slice architecture in place.

The strongest implemented area is PRD019/PRD030: Start Run, Run Map, Run Battle,
Reward Map, Run Shop, Summary Value, Saved Armies, Battle Result, shared army
snapshots, and SQLite-backed Offline Mode services exist as code slices.

The newest run-map direction is:

- generated starting armies are in code and use `ArmyGeneratorRuleSet`;
- generated route maps are in code inside `DeterministicRunGenerationCatalog`;
- generated runtime map data is materialized into SQLite tables;
- UI node binding was cleaned up through `RunMapNodeRepresentation`;
- enemy encounter difficulty has a separate ScriptableObject catalog;
- the actual map definition itself is not yet a ScriptableObject.

Project Director correction from 2026-06-24:

- Do not use the older hand-authored `DefaultStartRunCatalog`,
  `DefaultRunMapPathCatalog`, or `DefaultRunBattleEncounterCatalog` as current
  run-progression design truth.
- Current army design work should tune generator rule sets, value bands, tier
  caps, faction mix, unlock pools, starting resources, and enemy difficulty
  bands.
- Exact fixed army stack lists are allowed only as future predefined-army design
  or explanatory examples, not as the current Start Run source.

Primary return target after vacation:

```text
Create an authored Run Map / Mission Map ScriptableObject:
Node1, type, difficulty, connects to...
Then use that asset as generator input and materialize it into map_nodes /
map_node_connections / map_node_enemies.
```

User-confirmed short gameplay status on 2026-06-18:

1. A run can be started with a starting army.
2. The player can move to a Battle node and play a battle.
3. Battle view works, but its UI and lighting need improvement.
4. Starting army currently comes from the `Mock_startingArmyRuleSet` catalog.
5. Enemy armies currently come from `EnemyArmyGenerator`.
6. Encounters currently come from `Mock_EnemyEncounters`.
7. Rewards were later moved to materialized DB-backed reward flow; Unity
   Play Mode validation remains manual.
8. Random Events are not in the playable flow yet.

## What Exists Now

### Project Recovery Baseline

Status: active recovery.

Known from `_codex/Context/02_Current_State.md`:

- Project goal is still legacy recovery, not broad feature growth.
- Main technical debt remains PlayFab, PUN, Photon, old local files,
  PlayerPrefs, scene coupling, and legacy singleton access.
- Core battle prototype is a hex tactics stack battle.
- Current mechanics documentation still needs Unity/code verification before it
  becomes final truth.

### PRD019 Offline Run Loop

Status: implemented in slices, manual Unity validation still required.

Implemented code folders exist for:

- `020_StartRun`
- `021_RunMap`
- `022_RunBattle`
- `023_RewardMap`
- `024_RunShop`
- `025_SummaryValue`
- `026_SavedArmies`
- `027_BattleResult`

Current architecture shape:

- screen controllers use adapters/services;
- DB stores own persistence;
- `OfflineRunContextDbReader` and `OfflineRunContextDbWriter` own shared run
  context reads/writes;
- shared army snapshots are persisted through `OfflineArmySnapshotDbRepository`;
- production composition is centralized in `OfflineModeDatabaseComposition`.

Important caveat:

- Many task files report implementation and QA pass, but Unity compilation,
  EditMode tests, and Play Mode checks are still user-run/manual unless a task
  explicitly says otherwise.

### PRD030 Offline Database

Status: substantial SQLite foundation implemented.

Existing schema includes:

- `offline_accounts`
- `account_unlocks`
- `offline_runs`
- `army_snapshots`
- `army_snapshot_stacks`
- `army_snapshot_stack_skills`
- materialized PRD037 tables: `map_nodes`, `map_node_connections`,
  `map_node_rewards`, `map_node_enemies`
- `run_events`
- `run_battles`
- `reward_choices`, `reward_cards`
- `shop_visits`, `shop_offers`, `shop_purchases`
- `run_summaries`, `run_summary_entries`
- `saved_army_slots`, `saved_armies`, `saved_army_roster_state`,
  `saved_army_history`
- `async_battle_results`

Current DB direction:

- `offline_runs.run_seed` and `offline_runs.run_seed_version` exist.
- `map_nodes` and `map_node_connections` are now the runtime map truth.
- Legacy `route_maps`, `route_paths`, and `route_nodes` table creation and
  runtime SQL have been removed from code.
- `offline_runs.route_map_id` remains as a facade/context id for the current
  screen contracts; it now points at the materialized map identity rather than
  a `route_maps` table.

Known DB caveats:

- Existing local Offline DB files may need rebuild/reset after PRD037 schema
  changes.
- `async_battle_result_details` is documented as lazily created by Battle Result
  code and should become formal migration work if schema versioning advances.
- Some old reward tables still exist beside materialized map reward rows.
- Default local DB was deleted on 2026-06-18 so Unity can rebuild it without
  legacy `route_*` tables.

### PRD035 Random Starting Armies And Routes

Status: pass-manual-test-pending per QA on 2026-06-16.

What exists:

- `ArmyGeneratorRuleSet` is a ScriptableObject for starting army generation
  tuning.
- `RunGenerationSession` requires a scene instance with a Starting Army RuleSet.
- `DeterministicRunGenerationCatalog` generates starting army offers and route
  paths.
- Starting assets are set by rule/config: 150 run gold, 1 reroll token,
  0 battle skip tokens.
- `RunMapNodeType` includes `RandomEvent` and `Empty`.
- Tests exist in `PRD35RunGenerationTests`.

What is not done:

- The route/map topology itself is hardcoded in
  `DeterministicRunGenerationCatalog.BuildPaths`.
- There is no authored `MissionMapDefinition` / `RunMapDefinition`
  ScriptableObject for nodes and connections.
- Real reroll command and explicit long-term seed UX remain future work.

### PRD037 Materialized Run Generation

Status: QA pass on 2026-06-17; Unity Play Mode validation still manual.

What exists:

- `offline_runs.run_seed` and `run_seed_version`.
- `map_nodes`, `map_node_connections`, `map_node_rewards`,
  `map_node_enemies`.
- `OfflineMaterializedRunMapDbStore` writes materialized map nodes,
  connections, and enemy placeholders.
- Reward rows are materialized after battle completion.
- Reward Map loads persisted/materialized rewards instead of rerolling.
- Reward click applies immediately; old Select/Continue serialized fields were
  removed from `RewardMapScreenController`.
- Tests exist in `PRD37MaterializedRunGenerationTests`.

Open caveats:

- Legacy `route_*` tables have been removed from runtime code; Unity compile
  and Play Mode validation are still pending.
- Local DB reset/rebuild is needed for old databases.

### PRD038 Run Map Node Representation Binding

Status: QA pass on 2026-06-18.

What exists:

- `RunMapController` now uses ordered `RunMapNodeRepresentation[]`.
- Node UI internals are owned by `RunMapNodeRepresentation`.
- Hover focuses node details.
- Click attempts travel when `CanTravel` is true.
- Extra node representations are hidden; too few representations warn instead
  of crashing.
- `RunMapNodeTypeIconCatalog` is a ScriptableObject for node icons.
- Tests exist in `RunMapNodeRepresentationTests` and
  `RunMapControllerBindingTests`.

Manual Unity task still needed:

- Place/map node GameObjects manually in the Run Map screen.
- Assign `RunMapController.routeNodeRepresentations` in generated-node order.
- Run Play Mode smoke checks.

### PRD039 Enemy Encounter Rule Catalog

Status: QA pass on 2026-06-18.

What exists:

- `EnemyEncounterRuleCatalog` ScriptableObject.
- `EnemyEncounterDifficulty`: `Low`, `Medium`, `High`, `Boss`.
- Rule resolution:
  - non-empty `PredefinedEnemyId` wins;
  - empty `PredefinedEnemyId` uses assigned `ArmyGeneratorRuleSet`;
  - missing ruleset for generated mode fails clearly.
- Tests exist in `EnemyEncounterRuleCatalogTests`.

Current follow-up:

- The catalog contract is wired by PRD040 for generated enemy materialization.
- A usable Unity scene still needs an assigned `RunGenerationSession`
  `EnemyEncounterRuleCatalog` asset with Low, Medium, High, and Boss entries.
- Predefined enemy ids remain reserved until a predefined army catalog exists.

## Current Answer To "What Works?"

Use these categories strictly.

### Works As Code Architecture

- PRD019/PRD030 service, adapter, DB-store layering exists.
- SQLite schema and open/create module exist.
- Shared army snapshot persistence exists.
- Start Run and Run Map use generator-backed catalog through composition.
- Run map node UI binding is cleaner and representation-based.
- Reward Map materialized reward direction exists.
- Enemy encounter difficulty catalog contract exists.

### Works In Tests On Paper, But Needs Unity Run

- PRD35 generation tests.
- PRD37 materialized generation tests.
- Run Map representation/controller tests.
- Enemy Encounter Rule Catalog tests.
- Adjacent DB/service tests listed in PRD030 map.

The documents repeatedly say the agent did not run Unity tests automatically.
Treat these as "tests exist, need user-run verification".

### Works Only As Placeholder Or Contract

- Random events and empty nodes: node types exist, but event gameplay is not
  implemented.
- Campaign/mission progression: IDs and route choice shape exist, but there is
  no full campaign progression system.
- Online mode: architecture guardrails exist, no backend implementation.

### Not Built Yet

- Run map / mission map ScriptableObject with authored node list:
  `Node1`, `Type`, `Difficulty`, `ConnectsTo`.
- A converter from that SO into `RunMapPathDefinition` or direct
  materialized DB map rows.
- Predefined armies catalog for authored enemy/boss/special army references.
- Final Unity scene/prefab wiring proof for the full Start Run -> Run Map ->
  Battle -> Reward -> Run Map loop.

## Recommended First Task After Vacation

Task name:

```text
[TARENA] PRD040 Run Map ScriptableObject Definition
```

Goal:

Create an authored mission-map ScriptableObject that defines a map graph in
Unity Inspector and feeds existing run-generation/materialization code.

Proposed asset shape:

```text
RunMapDefinition / MissionMapDefinition
- mapId
- displayName
- campaignId
- missionIndex
- nodes[]

RunMapNodeDefinitionAssetEntry
- nodeId
- displayName
- nodeType
- encounterDifficulty
- possibleRewardHint
- expectedRiskHint
- connectsToNodeIds[]
- optional encounterCatalogEntryId / predefinedEnemyId override
```

Implementation target:

- Replace or wrap the hardcoded topology in
  `DeterministicRunGenerationCatalog.BuildPaths`.
- Keep DB materialization through the existing `map_nodes` and
  `map_node_connections` direction.
- Keep UI binding through `RunMapNodeRepresentation`.
- Do not edit scenes or prefabs unless explicitly allowed.

Acceptance target:

- A test-created SO or in-memory equivalent can define node graph:
  node 1 -> node 2 -> node 3 -> node 4A/4B -> ... -> node 10.
- Conversion preserves node type, difficulty, display text, risk/reward hints,
  and connections.
- `map_nodes` and `map_node_connections` reload without rerolling.
- Missing/duplicate node ids fail clearly.
- Broken `connectsTo` references fail clearly.
- Existing PRD35/037 tests still pass after update.

## Additional Backlog After Vacation

### Predefined Armies Catalog

Status: needed after or alongside Run Map SO work.

Why it is needed:

- `EnemyEncounterRuleCatalog` can already point to a `PredefinedEnemyId`.
- `map_node_enemies` can store encounter/enemy rule references.
- There is not yet a project-owned authored catalog that resolves a predefined
  enemy/boss/special army id into actual stack data or an army snapshot source.

Likely shape:

```text
PredefinedArmyCatalog
- entries[]

PredefinedArmyEntry
- predefinedArmyId
- displayName
- role: Enemy, Boss, SpecialEncounter, Test
- stacks[]

PredefinedArmyStackEntry
- unitId
- amount
- formationSlot
- skillIds[]
```

Integration target:

- `EnemyEncounterRuleCatalog.PredefinedEnemyId` should resolve through this
  catalog.
- Future enemy materialization should turn predefined entries into
  `army_snapshots` and link them from `map_node_enemies.army_snapshot_id`.
- This should stay separate from starting army generation rulesets.

### Runtime Snapshot To Battle Scene Bridge

Status: needed as a transition bridge.

Current direction:

- Runtime run snapshots should become the source of truth for battle launch.
- Do not base the future PRD on manually prepared `PlayerPrefs EnemyArmy` or
  old build-file state.
- Do not rewrite the whole legacy `HexMap` / `TeamClass` battle core yet.
- RunMetagame launch records now use runtime input ids
  (`PlayerArmyInputId`, `EnemyArmyInputId`) and `runtime snapshot battle input`
  instead of `legacy-*` adapter labels.

Recommended bridge:

```text
player army_snapshot_id + enemy army_snapshot_id
-> runtime snapshot loader
-> small battle-input adapter
-> existing battle scene input format
```

Production rule:

- The old battle scene may still internally read legacy surfaces until its
  input layer is replaced.
- The authoritative input should be run snapshot data, not hand-authored
  PlayerPrefs or build-file state.

Future cleanup:

- Remove the legacy battle input path once the battle scene can consume
  runtime snapshot payloads directly.
- Retire manual PlayerPrefs/build-file army handoff from the run PRD path.
- Move Battle Map UI out of the battle scene and into `UI Shared Canvas` like
  the other shared UI components. Treat this as post-vacation work; do not
  touch scene/prefab wiring now.
- Keep this as a narrow replacement task, not a full tactical battle rewrite.

### Route Table Legacy Removal

Status: implemented in code, Unity validation pending.

What changed:

- `OfflineDatabaseSchemaV1` no longer creates `route_maps`, `route_paths`, or
  `route_nodes`.
- `run_events.node_id`, `run_battles.node_id`, `reward_choices.node_id`, and
  `shop_visits.node_id` now reference `map_nodes`.
- Start Run now seeds only `map_nodes`, `map_node_connections`, and
  `map_node_enemies`.
- Run Map and Run Shop reconstruct runtime path/node DTOs from `map_nodes`
  instead of `route_paths` and `route_nodes`.
- Run Battle resolves encounter nodes from `map_nodes`.
- Added `OfflineDatabaseDevReset.RebuildDefaultDatabase()` and Unity menu:
  `TArena/Offline Database/Delete And Rebuild Default DB`.
- Added repeatable reset script:
  `_codex/scripts/reset_offline_db.ps1`.

Manual status:

- The physical default DB at
  `C:\Users\piotr\AppData\LocalLow\DefaultCompany\TArenaUnity3D\TArenaOffline.db`
  was deleted on 2026-06-18.
- Unity has not been run here, so DB rebuild/compile/test validation is still
  pending inside Unity.

## Manual Verification Queue

Run inside Unity when back:

1. Compile/import scripts.
2. Run EditMode tests:
   - `PRD35RunGenerationTests`
   - `PRD37MaterializedRunGenerationTests`
   - `RunMapNodeRepresentationTests`
   - `RunMapControllerBindingTests`
   - `EnemyEncounterRuleCatalogTests`
   - `OfflineDatabaseSchemaTests`
   - `OfflineStartRunRunMapDbTests`
   - `OfflineRunBattleRewardDbTests`
   - `RewardMapServiceTests`
   - `OfflineModeProductionCompositionTests`
3. Reset/rebuild old local Offline DB if testing PRD037 flow.
4. Manual smoke:
   - Start Run
   - choose generated army and mission
   - open Run Map
   - travel through battle/event/branch/shop/final path
   - complete a battle win
   - Reward Map shows three persisted cards
   - hover previews
   - click applies and returns to Run Map

## Questions To Discuss After Vacation

Do not ask these now. Keep them here as a return checklist before creating
PRD040 and the predefined-armies follow-up.

1. Should the map SO define one mission map only, or a campaign with three
   missions inside one asset?
2. Do node ids need to be human-authored strings like `node-1`, `node-4a`, or
   should the asset allow friendly labels and generate stable ids?
3. Should `difficulty` mean enemy difficulty only (`Low/Medium/High/Boss`), or
   route risk/reward difficulty as well?
4. Are `RandomEvent` and `Empty` real V1 node types, or temporary placeholders
   until event design exists?
5. Should a battle node point directly to `EnemyEncounterDifficulty`, to an
   `EnemyEncounterRuleCatalog` entry, or to a predefined encounter id?
6. Should the first SO task create real Unity assets, or only code + tests and
   leave asset authoring/manual wiring to you?
7. Is the target V1 map still exactly the PRD035 10-node mission shape, or do
   you want a shorter/clearer map before full run validation?
8. Can known-seed offline runs create saved armies, or should saved-army export
   be blocked for seeded/dev runs?
9. Should the predefined armies catalog support only enemies/bosses first, or
   also player/dev/test armies?
10. Should predefined army entries store full stack data directly, or point to
   existing unit/army assets if such assets become available?
11. Should the first battle bridge only translate runtime snapshots into the
    current legacy battle input, or should it also start removing specific
    PlayerPrefs/build-file reads from the battle scene?
