# [TARENA] Coding Completion: PRD 040 Full Encounter Materialization And Battle Launch Loop

- Task: `_codex/tasks/040_PRD_FullEncounterMaterializationBattleLaunchLoop.md`
- Status: ready-for-qa
- Type: Coding Completion Protocol
- Date: 2026-06-18

## Scope

Implemented the first full vertical encounter loop across run generation, map persistence, battle preparation, and battle scene entry.

The implementation keeps battle scene internals untouched. It materializes enemy army snapshots at run start, resolves prepared battles to those snapshots through a DB-backed encounter source, and makes Run Map perform travel first, prepare battle second, then call `GameSceneManager.EnterBattle()`.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleEncounterCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseModule.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineMaterializedRunMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedFactory.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbReader.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/DeterministicRunGenerationCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/EnemyEncounterArmyMaterializer.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/RunGenerationSession.cs`
- `_codex/Documentation/ADR_040_ArmySnapshotToLegacyBattleInputBridge.md`

## What Changed

- Added explicit `EncounterDifficulty` to `RunMapNodeDefinition` and `OfflineRouteNodeSeedRecord`.
- Updated generated PRD035 route nodes to assign `Low`, `Medium`, `High`, or `Boss` explicitly.
- Added `EnemyEncounterArmyMaterializer`, which uses `DeterministicRunGenerationCatalog` with the selected enemy `ArmyGeneratorRuleSet` and an unrestricted unlock context.
- Updated materialized map persistence so battle/final nodes can save enemy army snapshots and link them from `map_node_enemies.army_snapshot_id`.
- Kept legacy placeholder behavior only when both enemy unit source and enemy catalog are absent. If production passes a unit source without an enemy catalog, Start Run now fails clearly.
- Restored `route_maps`, `route_paths`, and `route_nodes` schema definitions because `RunMapDbStore` still owns current Run Map flow through those tables.
- Stopped the compatibility pass from dropping `route_*` tables.
- Updated Start Run persistence to seed legacy `route_*` tables with the same node ids as `map_nodes`.
- Added `OfflineRunBattleEncounterCatalog`, which wraps the existing encounter catalog and replaces `EnemyArmySourceId` with the materialized `snapshot-*` from `map_node_enemies` when available.
- Added `OfflineRunContextDbReader.ToRunBattleCurrentArmy(...)`.
- Updated `RunMapController.OnTravelClicked()` so battle/final travel persists first, prepares battle second, and only then calls `GameSceneManager.EnterBattle()`.
- Added ADR 040 documenting the transitional `army_snapshot -> legacy battle input` bridge.

## Design Notes

- `GameSceneManager` remains a scene/UI transition owner only.
- Enemy generation does not use account unlock filtering.
- Predefined enemy ids still fail clearly in materialization because army definitions are not implemented.
- Route and materialized-map node ids are intentionally aligned so `route_nodes.node_id`, `map_nodes.node_id`, and `map_node_enemies.node_id` refer to the same runtime node.
- The bridge still exposes legacy adapter labels, but the battle source of truth is now the prepared run battle payload and persisted snapshots.

## Automatic Tests

No tests were added yet in this pass because the `/implement` workflow writes focused EditMode tests after QA review.

Recommended post-QA tests:

- Start Run with an enemy catalog populates `map_node_enemies.army_snapshot_id` for every battle/final node.
- Unchosen branch battle nodes receive snapshots at run start.
- Missing enemy catalog fails clearly when production materialization receives an enemy unit source.
- DB-backed encounter source returns `snapshot-*` as `EnemyArmySourceId`.
- Run Map battle travel calls travel before prepare and enters battle only after `PrepareBattle` succeeds.

## Manual Unity Validation Needed

- Assign `EnemyEncounterRuleCatalog` on the scene `RunGenerationSession`.
- Ensure the catalog has generated entries for `Low`, `Medium`, `High`, and `Boss`, each with an `ArmyGeneratorRuleSet` unless intentionally testing predefined failure.
- Let Unity recompile.
- Start a run through Offline Mode.
- Open Run Map and click an available battle node.
- Confirm the current node changes first, battle preparation succeeds, and `GameSceneManager` enters battle.
- Resolve battle through the existing completion path and confirm reward/summary/loss routing still follows Run Battle next-screen state.

## Known Risks

- Existing Unity scenes/prefabs are not edited, so `RunGenerationSession.enemyEncounterRuleCatalog` must be assigned manually.
- The tactical battle scene still needs future work to consume snapshots directly instead of legacy adapter labels.
- Existing direct tests that construct `OfflineStartRunDbStore` without enemy catalog still get placeholder enemy rows, not generated enemy snapshots.
- Unity compilation and Play Mode validation were not run automatically per project rules.
