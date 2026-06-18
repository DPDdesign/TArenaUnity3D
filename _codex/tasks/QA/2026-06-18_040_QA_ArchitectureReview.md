# [TARENA] QA Architecture Review: PRD 040 Full Encounter

- Task: `_codex/tasks/040_PRD_FullEncounterMaterializationBattleLaunchLoop.md`
- Protocol: `_codex/tasks/QA/2026-06-18_040_CodingCompletion_FullEncounter.md`
- Status: pass-with-notes
- Date: 2026-06-18

## Verdict

PASS.

No blocking architecture findings remain after review.

The implementation follows the intended ownership split:

- Start Run / materialized map owns enemy army materialization at run start.
- `EnemyEncounterRuleCatalog` remains the difficulty-to-rule lookup boundary.
- Run Battle owns prepare/complete and launch payload persistence.
- `GameSceneManager` remains only a scene/screen transition coordinator.
- The legacy battle input bridge is documented as transitional, not source of truth.

## Reviewed Files

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

## Findings

No actionable findings.

## Notes

- Restoring `route_maps`, `route_paths`, and `route_nodes` is justified because active `OfflineRunMapDbStore` still uses those tables. Dropping them while keeping the store unchanged would break Run Map persistence.
- `route_nodes.next_node_id` is correctly set after all route node inserts, avoiding immediate self-FK failure.
- Enemy generation uses `DeterministicRunGenerationCatalog` with an unrestricted unlock context, satisfying the full-pool enemy rule.
- Production Start Run now fails clearly if an enemy unit source is provided but the enemy catalog is missing.
- Old test/direct constructors without both enemy dependencies still create placeholder enemy rows; this is acceptable compatibility, but generated enemy snapshot coverage must use the new constructor path.
- The current battle scene bridge still emits legacy adapter labels. ADR 040 correctly frames those labels as adapter output, not run source of truth.

## Required Follow-Up

None.

## Recommended Tests

- Add EditMode DB coverage for materialized enemy snapshot rows on all battle/final nodes.
- Add EditMode coverage for DB-backed encounter source returning `snapshot-*`.
- Add focused service/controller-level coverage for travel-before-prepare ordering if the current code can isolate `RunMapController` dependencies without scene setup.
- Run Unity Test Runner manually after test authoring.
