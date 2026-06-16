# [TARENA] 030_8 Coding Completion - Summary And Saved Armies DB Integration

- Date: 2026-06-15
- Task: `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`
- Status: implemented-manual-test-pending

## Scope

Integrated PRD030_8 Summary Value and Saved Armies with the shared offline
SQLite state instead of separate in-memory stores.

## Completed

- Added `OfflineSavedArmyDbRepository` to persist physical slots, immutable
  saved-army identities, roster defence state, and saved-army history.
- Added `OfflineSavedArmiesDbStore` so Saved Armies can load slot state and
  saved-army previews from the shared database.
- Added `OfflineSummaryValueDbStore` so Summary Value persists
  `run_summaries`, `run_summary_entries`, and saves pre-final candidates into
  the shared saved-army tables.
- Changed `SummaryValueService` to use stable candidate ids and to reload
  persisted summary data through the DB store when available.
- Updated the Summary Value and Saved Armies prototype controllers to use the
  DB-backed stores where the runtime context supports it, while keeping the
  standalone summary mock safe through in-memory fallback for non-persisted
  sample ids.
- Added focused EditMode DB regression coverage for summary reload, pre-final
  candidate persistence, overwrite identity replacement, and defence clearing.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineSavedArmyDbRepository.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/026_SavedArmies/OfflineSavedArmiesDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/OfflineSummaryValueDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/SummaryValueContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/SummaryValueService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/025_SummaryValue/SummaryValueScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/026_SavedArmies/SavedArmiesScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineSummarySavedArmiesDbTests.cs`

## Validation Performed

- Read-path review across Summary Value, Saved Armies, and the shared DB
  module: pass.
- Persisted-slot overwrite and current-defence clearing logic review: pass
  after fixing an unlock-count persistence bug.
- Added EditMode DB regression tests for the target acceptance flow.

## Not Run

Unity compilation, EditMode execution, and Play Mode execution were not run in
this Codex pass, in line with the project rule that the user compiles/tests in
Unity unless explicitly allowed.

## Notes

- Summary timeline rows persist through `run_summary_entries`; the row detail
  payload stores the timeline text plus the rendered army/gold stage values so
  the screen can rebuild accurately after adapter recreation.
- Saved-army overwrite now creates a new `saved_army_id`, deactivates the old
  identity, and clears `saved_army_roster_state.current_defence_saved_army_id`
  when the overwritten army had been current defence.
- The legacy PRD26 seed-army loader remains available as a development bridge,
  but PRD030_8 no longer depends on it for the summary-to-saved-army flow.
