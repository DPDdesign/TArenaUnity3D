# [TARENA] 030_10 Coding Agent Completion - Remove InMemory Production Usage Final Audit

- Task: `_codex/tasks/030_10_PRD030_RemoveInMemoryProductionUsage_FinalAudit.md`
- Agent: Coding Agent
- Date: 2026-06-15

## Scope

Implemented the final PRD030 production-composition audit for Offline Mode runtime screens and adapters.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
  - Added the single Offline Mode database composition point for DB-backed runtime services and adapters.
  - Opens/migrates the default offline database before constructing DB-backed services.
  - Creates DB-backed composition for Start Run, Run Map, Run Battle, Reward Map, Run Shop, Summary Value, Saved Armies, and Battle Result.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunAdapter.cs`
  - Default constructor now uses `OfflineModeDatabaseComposition.CreateStartRunService()`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/OfflineRunMapAdapter.cs`
  - Default constructor now uses `OfflineModeDatabaseComposition.CreateRunMapService()`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleAdapter.cs`
  - Default constructor now uses `OfflineModeDatabaseComposition.CreateRunBattleService()` instead of `InMemoryRunBattleStore`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapScreenController.cs`
  - Removed production `InMemoryRewardMapChoiceStore` construction.
  - Screen now receives its DB-backed adapter from `OfflineModeDatabaseComposition`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/024_RunShop/OfflineRunShopAdapter.cs`
  - Default constructor now uses `OfflineModeDatabaseComposition.CreateRunShopService()` instead of `InMemoryRunShopVisitStore`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/SummaryValueScreenController.cs`
  - Removed fallback `InMemorySummaryValueRosterStore` production path.
  - Screen now uses the DB-backed summary store and adapter from `OfflineModeDatabaseComposition`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/026_SavedArmies/SavedArmiesScreenController.cs`
  - Screen now uses the shared composition point for saved-army DB store and adapter creation.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/BattleResultScreenController.cs`
  - Removed separate preview database path composition.
  - Screen now uses the default Offline Mode database-backed battle result adapter from `OfflineModeDatabaseComposition`.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineModeProductionCompositionTests.cs`
  - Added focused EditMode source-audit tests for this final cleanup contract.
  - Verifies runtime RunMetagame source does not construct `new InMemory...Store()`.
  - Verifies runtime DB store construction remains owned by `OfflineModeDatabaseComposition`.

## Audit Results

- Runtime `new InMemory...Store()` search outside tests now returns no matches.
- Remaining `InMemory...Store` classes are definitions only and remain available for explicit test doubles.
- Runtime direct SQLite access outside DB stores/repositories/database infrastructure was checked with `rg`; UI controllers are not querying SQLite directly.
- DB stores continue to use `OfflineDatabaseSql.OpenConnection(...)`, which opens/migrates schema and enables `PRAGMA foreign_keys = ON`.
- Existing DB stores for reward, shop, battle, summary, saved armies, and battle result continue to write through `run_events`, detail tables, integer ids, and `is_active` soft-delete/update paths where implemented by prior PRD030 slices.

## Verification

- Ran focused text audits with `rg`:
  - `new InMemory` / default in-memory production construction outside tests.
  - `OfflineDatabaseSql` usage outside DB stores/repositories/database infrastructure.
  - direct DB-backed store creation outside the new composition point.
- Added EditMode tests for manual Unity Test Runner execution; they were not run automatically.
- Did not run Unity, builds, `dotnet`, package restore, or external build scripts per local project rules.

## Manual Unity Focus

- Open a scene containing the PRD019/PRD030 Offline Mode screen controllers.
- Start with `StartRunScreenController` and begin an offline run to create a persisted `run-*` id in the default offline database.
- Use that persisted run id when opening Run Map, Run Battle, Reward Map, Run Shop, Summary Value, Saved Armies, and Battle Result screens.
- Confirm each screen loads through `OfflineModeDatabaseComposition` and persists/loads from the default `TArenaOffline.db`.

## Notes

- No public or serialized field names were renamed.
- No prefabs, scenes, assets, `.asmdef`, `.asmref`, generated files, gameplay float values, or Unity assets were edited.
- In-memory stores were not deleted because EditMode tests still use them as explicit test doubles.
