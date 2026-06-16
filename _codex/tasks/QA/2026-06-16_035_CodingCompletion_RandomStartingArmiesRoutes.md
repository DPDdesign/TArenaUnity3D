# [TARENA] 035 Coding Completion - Random Starting Armies And Route Maps

- Date: 2026-06-16
- Task: `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
- Status: implemented-manual-test-pending

## Scope

Implemented the first PRD035 runtime slice for deterministic generated Start
Run offers and generated mission route maps through existing Start Run, Run Map,
Run Battle, and Offline DB seams.

## Completed

- Added a deterministic generator-backed catalog that implements existing
  `IStartingArmyTemplateSource`, `IRunRoutePreviewSource`, and
  `IRunMapPathCatalog` boundaries.
- Added generator config models for seed, starting army budget, fallback pool,
  route campaigns, and unlock filtering.
- Generated Start Run options use four stacks, one unlocked legal skill per
  stack, 150 run gold, one reroll token, zero battle skip tokens, and fallback
  early units when unlock filtering is too narrow.
- Extended Start Run option/view data so starting reroll and battle skip token
  counts come from the selected generated army instead of a hardcoded service
  default.
- Added a unit-pool interface and DataMapper-backed implementation so generator
  logic uses UnitCatalog/DataMapper unit ids, tier, cost, and legal skills.
- Added explicit Run Map node types for random events and empty nodes, with
  stable DB enum ids.
- Added multi-next route links to `RunMapNodeDefinition` so a fixed opening
  sequence can branch into safer and riskier paths without changing the
  existing single `next_node_id` DB column.
- Updated Run Map state rules so generated branch nodes and the final node open
  only from the current node's explicit next links.
- Updated Offline DB route map reload to rebuild generated branch links from
  the deterministic catalog while keeping persisted node text, type, risk,
  reward, and encounter data authoritative.
- Switched production Offline Mode composition to use the generated catalog for
  Start Run and Run Map, while leaving legacy authored catalogs in place for
  tests and fixtures.
- Added generated PRD035 encounter-id fallback handling in the existing Run
  Battle encounter catalog.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/DataMapperStartRunUnitSource.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/StartRunService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/OfflineRunMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/PRD19_021_RunMapMockupController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/DefaultRunBattleEncounterCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/DB Enums.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedFactory.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/DeterministicRunGenerationCatalog.cs`

## Validation Performed

- Static code review against Start Run, Run Map, Offline DB, and Run Battle
  seams.
- Checked constructor call sites after adding Start Run asset fields.
- Checked Run Map node type mapping call sites after adding random event and
  empty node types.
- Did not run Unity compilation or EditMode tests automatically.

## Not Run

Unity compilation, EditMode execution, and Play Mode execution were not run in
this Codex pass, in line with the project rule that the user compiles/tests in
Unity unless explicitly allowed.

## Notes

- No Unity assets, prefabs, scenes, materials, controllers, `.asmdef`, or
  `.asmref` files were edited.
- PRD035 seed storage is represented through deterministic generated route ids
  and persisted route node rows in this slice; no schema table/column migration
  was added.
- Start Run displays one reroll token for generated offers, but no new reroll
  button or command was added in this slice because the existing UI contract has
  no reroll command surface.
- Campaign mission progression beyond mission 1 is represented in generator
  config and route id parsing, but account unlock progression for missions 2/3
  was not added.
