# [TARENA] Coding Completion - PRD037 Materialized Run Generation

Task: `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`

## Summary

Implemented a compatibility migration toward PRD037 materialized run generation:

- Added run-level `run_seed` and `run_seed_version` DB columns.
- Added PRD037 tables: `map_nodes`, `map_node_connections`, `map_node_rewards`, and `map_node_enemies`.
- Mirrored seeded run map topology from the existing `route_*` persistence flow into the new map tables.
- Preserved legacy route/map facade ids for existing UI and service code.
- Added deterministic V1 reward materialization after successful non-final run battle completion.
- Added `map_node_rewards` writes for materialized reward cards and selection/apply state.
- Changed production Reward Map loading so DB-backed Reward Map reads persisted rows for the current run/node instead of rolling screen-time cards.
- Updated Reward Map card interaction to hover-preview and click-apply, with successful apply returning to Run Map through `GameSceneManager.ShowRunMap()`.
- Removed obsolete `selectCommandButton` and `continueCommandButton` serialized fields from `RewardMapScreenController`.
- Added focused EditMode tests for materialized map rows, branch connections, reward materialization, no screen reroll, and selected reward persistence.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbWriter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedFactory.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineMaterializedRunMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/OfflineRunMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapDataMapperUnitSource.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapRewardCardView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD37MaterializedRunGenerationTests.cs`

## Implementation Notes

- `route_*` tables remain in use as compatibility persistence for existing Run Map code.
- `map_nodes` and `map_node_connections` are written in the same transaction when route maps are seeded.
- `OfflineRouteNodeSeedRecord` now carries all outgoing node ids, while `route_nodes.next_node_id` keeps the first link for legacy compatibility.
- `OfflineRunMapDbStore.UpdateNodeStates` mirrors state changes into `map_nodes`.
- Reward materialization is triggered by `OfflineRunBattleDbStore.SaveCompletion` only when the battle next screen is `Reward`.
- Production `RewardMapService.BuildChoice` short-circuits to `IMaterializedRewardMapChoiceStore.FindChoiceForRunNode` for persisted run ids.
- In-memory Reward Map tests still use the existing screen-time generation path.
- `RewardMapScreenController` keeps small public compatibility methods for older UnityEvents, but the obsolete serialized command-button fields were removed.

## Tests Added

- `PRD37MaterializedRunGenerationTests.StartRun_PersistsMaterializedMapNodesConnectionsAndSeed`
  - Verifies materialized map rows, explicit branch connections, enemy placeholder rows, and run seed/version persistence.
- `PRD37MaterializedRunGenerationTests.BattleCompletion_MaterializesRewardRowsAndRewardMapDoesNotReroll`
  - Verifies battle completion writes one persisted reward choice, three `map_node_rewards` rows, Reward Map reload does not reroll, focus can change without creating new rows, and apply marks one selected/applied reward.

## Verification

- Text-level scans were used to check stale removed field references and reward identity references.
- Unity EditMode tests were not run automatically because project rules require the user to run Unity tests manually unless explicitly allowed.

## Manual Unity Test Target

Run in Unity Test Runner:

- `EditMode > PRD37MaterializedRunGenerationTests`
- Recommended adjacent regression tests:
  - `OfflineDatabaseSchemaTests`
  - `PRD35RunGenerationTests`
  - `OfflineRunBattleRewardDbTests`
  - `RewardMapServiceTests`
  - `OfflineModeProductionCompositionTests`
