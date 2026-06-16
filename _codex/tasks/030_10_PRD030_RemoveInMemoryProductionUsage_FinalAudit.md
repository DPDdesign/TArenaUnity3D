# [TARENA] PRD030_10: Remove InMemory Production Usage Final Audit

- Status: draft
- Type: Cleanup / QA Task
- Area: Offline Mode, Persistence Audit, Architecture Cleanup
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Blocked by:
  - `_codex/tasks/030_5_PRD030_StartRun_RunMap_DBIntegration.md`
  - `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
  - `_codex/tasks/030_7_PRD030_RunShop_DBIntegration.md`
  - `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`
  - `_codex/tasks/030_9_PRD030_AccountProgress_BattleResult_DBIntegration.md`

## Goal

Finish PRD030 by removing production dependency on PRD019 in-memory stores and
auditing the Offline Mode database flow end to end.

## What To Build

- Remove production `new InMemory...Store()` creation from Offline Mode paths.
- Keep in-memory stores only as explicit test doubles if still useful.
- Ensure one Offline Mode database composition point creates DB-backed
  adapters.
- Audit all PRD019 slices for direct SQLite access from UI.
- Audit all runtime DB ids for integer PK/FK usage.
- Audit all local deletion paths for `is_active = 0` soft delete behavior.
- Audit all event/detail records for `event_id` references.

## Acceptance Criteria

- Production Offline Mode no longer creates fresh in-memory stores in screen
  controllers or default adapters.
- UI controllers do not query SQLite directly.
- Start Run, Run Map, Run Battle, Reward Map, Run Shop, Summary Value, Saved
  Armies, and Battle Result load from DB-backed adapters.
- Shared army snapshot persistence is used consistently.
- `run_events` is the shared timeline for battle/reward/purchase/save/run
  completion events.
- Foreign keys are enabled.
- Normal flow does not hard delete runtime records.
- PRD030 parent acceptance criteria are ready for final QA.

## Out Of Scope

- Online Mode.
- Backend authority.
- Gameplay balance changes.
- Unity scene/prefab polish unless separately approved.

## Implementation - 2026-06-15

### What Changed

- `OfflineModeDatabaseComposition`: added one DB-backed Offline Mode composition point for Start Run, Run Map, Run Battle, Reward Map, Run Shop, Summary Value, Saved Armies, and Battle Result.
- Default adapters now route through that composition point: `OfflineStartRunAdapter`, `OfflineRunMapAdapter`, `OfflineRunBattleAdapter`, and `OfflineRunShopAdapter`.
- Runtime screen controllers now use DB-backed composition instead of local store creation: `RewardMapScreenController`, `SummaryValueScreenController`, `SavedArmiesScreenController`, and `BattleResultScreenController`.
- `BattleResultScreenController` now uses the default Offline Mode DB instead of a separate preview DB path.
- Added `OfflineModeProductionCompositionTests` to guard against production `new InMemory...Store()` regressions and DB store construction outside `OfflineModeDatabaseComposition`.
- No Inspector fields changed. No public or serialized fields were added, removed, or renamed, so no tuning ranges apply.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineModeProductionCompositionTests.cs`.
- `RuntimeRunMetagameSource_DoesNotCreateInMemoryStores` scans runtime RunMetagame source, excluding `Tests` and `Editor`, and fails if production code constructs `new InMemory...`.
- `RuntimeDbStoreConstruction_StaysInOfflineModeDatabaseComposition` scans runtime source and fails if DB store construction moves outside `OfflineModeDatabaseComposition`.
- These tests do not require scene or prefab setup because they inspect source ownership only.
- They were not run automatically. Run them manually in Unity: `Window > General > Test Runner > EditMode > OfflineModeProductionCompositionTests`. Expected result: 2 passing tests.

### Unity Test

#### Unity Setup

- Open the Offline Mode PRD019/PRD030 scene or prefab test scene.
- Ensure the scene has the relevant screen controller objects wired with their existing TMP labels, buttons, card views, and row views: `StartRunScreenController`, `PRD19_021_RunMapMockupController`, `RewardMapScreenController`, `RunShopScreenController`, `SummaryValueScreenController`, `SavedArmiesScreenController`, and `BattleResultScreenController`.
- Start from `StartRunScreenController` and create a persisted offline run in the default DB.
- Copy the produced `run-*` id into the serialized `runId` fields for Reward Map, Run Shop, and Summary Value before testing those screens directly.
- For Run Shop, set `routeNodeId` to an active shop node from the same run. For Reward Map, use the reward/battle node state created by the same persisted run.
- Do not use the old placeholder `offline-run` for production verification; DB-backed stores expect a persisted run id.

#### Play Mode Test

- Press Play and begin an offline run from Start Run.
- Navigate Run Map and choose a reachable node; expected: map state loads and persists through the DB-backed Run Map adapter.
- Prepare and complete a Run Battle path; expected: battle preparation/completion writes through `run_events`, `run_battles`, integer ids, and snapshots.
- Open Reward Map, choose a reward, and continue; expected: reward choice/card data and before/after army snapshots persist.
- Open Run Shop, buy one offer, then leave; expected: purchase event/detail rows persist and run state returns to Run Map.
- Open Summary Value and save the pre-final army to a slot; expected: saved army slot uses DB persistence and replacement is soft-deactivated.
- Open Saved Armies and set a defence army; expected: the selected defence remains after refreshing/re-entering.
- Open Battle Result, record/reload a result; expected: data uses the default Offline Mode database, not the old separate preview DB.

### QA Verdict

- Final QA verdict: pass with Unity manual verification pending.
- QA report: `_codex/tasks/QA/2026-06-15_030_10_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: old placeholder inspector ids such as `offline-run` must be replaced with persisted `run-*` ids for manual DB-backed scene testing; DB open/migration is intentionally self-initializing and may run redundantly.
- Follow-up fixes applied: none required after QA.

### Notes

- HTML report created at `_codex/tasks/QA/2026-06-15_030_10_ImplementationReport.html`.
- Unity tests/builds were not run automatically, per local project rules.
- In-memory store classes were not deleted because existing EditMode tests still use them as explicit test doubles.
- No prefabs, scenes, Unity assets, generated files, `.asmdef`, `.asmref`, gameplay float values, or public/serialized field names were changed.

### Next Steps

- Run `OfflineModeProductionCompositionTests` in Unity Test Runner EditMode and confirm 2 passing tests.
- Perform the Play Mode flow above with a persisted Start Run `run-*` id.
- Watch Unity Console for missing SQLite provider, missing persisted run/node ids, or unassigned UI references.
