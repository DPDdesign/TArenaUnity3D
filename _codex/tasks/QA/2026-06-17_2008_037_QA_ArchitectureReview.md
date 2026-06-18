# [TARENA] QA Architecture Review - PRD037 Materialized Run Generation

Task: `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`

Protocol reviewed: `_codex/tasks/QA/2026-06-17_2007_037_CodingCompletion_MaterializedRunGeneration.md`

## Verdict

Pass.

No follow-up code changes required after the Coding Agent fixed the compile-level resolver issue found during review (`OfflineArmySnapshotUnitCatalogEntry.CombatValue` vs non-existent `Cost`).

## Reviewed Files

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

## Findings

None remaining.

## Non-Blocking Observations

- Existing `route_*` tables remain as a compatibility layer. This is acceptable for this implementation pass because current Run Map services still own that facade, while PRD037 `map_*` rows are written in the same seeding transaction.
- Existing local DB files without the new `offline_runs.run_seed` columns require a rebuild/reset. This matches the PRD037 task allowance for a clean database rebuild, but manual QA should use a fresh local DB.
- Editor prefab builder scripts can still create legacy command buttons and wire compatibility methods. Runtime `RewardMapScreenController` no longer exposes the obsolete serialized command-button fields, and production card clicks now apply rewards directly.

## Architecture Checks

- `offline_runs` writes remain centralized through `OfflineRunContextDbWriter`.
- Start Run and Run Map seed generated route state through owning DB stores, with PRD037 materialized map rows delegated to `OfflineMaterializedRunMapDbStore`.
- Run Battle completion owns the post-battle materialization trigger, matching the task requirement that rewards are created before Reward Map opens.
- Reward Map production load path uses `IMaterializedRewardMapChoiceStore.FindChoiceForRunNode` for persisted runs, so screen-time generation is not used for DB-backed Reward Map.
- Reward application persists selected/applied state in both legacy `reward_*` compatibility rows and PRD037 `map_node_rewards`.
- UI code uses TextMesh Pro types and does not introduce `UnityEngine.UI.Text`.

## Test Review

Added tests target the right seams:

- `PRD37MaterializedRunGenerationTests.StartRun_PersistsMaterializedMapNodesConnectionsAndSeed`
- `PRD37MaterializedRunGenerationTests.BattleCompletion_MaterializesRewardRowsAndRewardMapDoesNotReroll`

Unity tests were not run automatically per project rule.

## Required Manual Verification

Run in Unity Test Runner:

- `EditMode > PRD37MaterializedRunGenerationTests`
- Adjacent regressions:
  - `OfflineDatabaseSchemaTests`
  - `PRD35RunGenerationTests`
  - `OfflineRunBattleRewardDbTests`
  - `RewardMapServiceTests`
  - `OfflineModeProductionCompositionTests`
