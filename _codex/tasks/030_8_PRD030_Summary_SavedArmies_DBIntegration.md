# [TARENA] PRD030_8: Summary And Saved Armies DB Integration

- Status: draft
- Type: Integration Task
- Area: Summary Value, Saved Armies, Offline DB
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related:
  - `_codex/tasks/025_PRD019_SummaryValue.md`
  - `_codex/tasks/026_PRD019_SavedArmies.md`
- Blocked by:
  - `_codex/tasks/030_4_PRD030_RemovePRD26ArenaImport.md`
  - `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
  - `_codex/tasks/030_7_PRD030_RunShop_DBIntegration.md`

## Goal

Persist run summaries and saved-army roster state through the Offline Mode
database.

## What To Build

- `run_summaries` and `run_summary_entries`.
- Summary loads start, pre-final, and post-final snapshots.
- Won final creates saved-army candidate from pre-final snapshot.
- Failed run cannot create saved army.
- `saved_army_slots` stores 8 physical slots.
- `saved_armies` stores immutable saved army identity and snapshot.
- Overwrite creates a new `saved_army_id`.
- Old saved army becomes inactive via `is_active = 0` / active flag.
- `saved_army_roster_state` stores current defence.
- Overwriting current defence clears current defence.

## Acceptance Criteria

- Summary survives adapter recreation.
- Run timeline entries persist.
- Saved army candidate comes from pre-final snapshot.
- Saving into empty slot persists saved army.
- Overwrite requires confirmation and creates a new saved army identity.
- Old saved army is inactive, not deleted.
- Current defence can be null.
- PRD26 Arena import is not part of this flow.

## Implementation - 2026-06-15

### What Changed

- `OfflineSavedArmyDbRepository`, `OfflineSavedArmiesDbStore`, and
  `OfflineSummaryValueDbStore` now persist Summary Value and Saved Armies
  through the shared offline SQLite database instead of separate in-memory
  stores.
- `SummaryValueService` now uses stable candidate ids and reloads persisted
  summary data when the roster store supports DB-backed summary persistence.
- Summary save now creates a real saved-army identity in the DB, writes slot
  assignment into `saved_army_slots`, and preserves the pre-final snapshot as
  the save source instead of the post-final result snapshot.
- Saved-army overwrite now deactivates the previous saved-army identity,
  assigns a new `saved_army_id`, and clears current defence when the replaced
  army had been selected as defence.
- The Summary Value mock controller now uses the DB-backed path only for
  persisted run ids and falls back to in-memory sample behavior for standalone
  mock ids; the Saved Armies controller now uses the DB-backed store directly.
- Added `OfflineSummarySavedArmiesDbTests.cs` to cover persisted summary reload,
  timeline entry persistence, pre-final candidate save, overwrite identity
  replacement, and current-defence clearing.
- No Inspector fields changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineSummarySavedArmiesDbTests.cs`.
- `SummaryAndSavedArmyPersistAcrossServiceRecreation` checks that
  `run_summaries` and `run_summary_entries` persist, summary rebuild works
  after service recreation, and the saved-army roster loads the pre-final army
  values from the shared DB.
- `OverwriteCreatesNewIdentityAndClearsCurrentDefence` checks that overwrite
  requires confirmation, creates a new saved-army identity, marks the previous
  identity inactive, reassigns the slot, and clears current defence.
- These tests do not require scene or prefab setup because they operate through
  the service/store layer with temporary SQLite files and fake unit catalogs.
- The tests were not run automatically in this Codex pass. Run them manually in
  Unity Test Runner: `Window > General > Test Runner`, `EditMode`, then run
  `OfflineSummarySavedArmiesDbTests`.

### Unity Test

#### Unity Setup

- No new components or serialized fields need to be added for this task.
- For manual DB-backed screen checks, first create a persisted offline run
  through the existing offline Start Run flow so a real `run-<id>` exists in
  the local database.
- If you want the Summary Value mock screen to use the DB path, set
  `SummaryValueScreenController.runId` to that persisted `run-<id>`. Leave a
  non-persisted sample id to keep the controller on its standalone in-memory
  mock fallback.
- `SavedArmiesScreenController` uses the shared DB-backed store with its
  existing serialized references; no extra wiring is required before pressing
  Play.

#### Play Mode Test

- Create or reach a winning summary for a persisted offline run, save the
  pre-final candidate into slot 1, leave the screen, and reopen the summary or
  saved-army roster. Slot 1 should still be taken and should show the saved
  army built from the pre-final snapshot rather than post-final losses.
- Set that saved army as current defence, then save a second winning summary
  into the same slot and confirm overwrite. The slot should show a new
  saved-army identity and current defence should become `none`.

### QA Verdict

- Final verdict: pass with Unity manual verification pending.
- QA report: `_codex/tasks/QA/2026-06-15_030_8_QA_ArchitectureReview.md`
- Actionable findings: none remaining after implementation review.
- Non-blocking observations: the Summary Value mock controller intentionally
  falls back to in-memory mode for non-persisted sample ids, and the PRD26
  seed-army bridge still exists as adjacent development-only surface.
- Follow-up fixes applied: yes. The review found and the implementation fixed a
  bug where slot saves could have forced the account unlock count back to 8.

### Notes

- Summary timeline rows persist through `run_summary_entries`; their stored
  detail payload keeps both the visible row text and the rendered stage values
  needed to rebuild the screen after adapter recreation.
- PRD26 Arena import is not part of the summary-to-saved-army flow delivered
  here.
- Unity compilation, EditMode execution, and Play Mode execution were not run
  in this Codex pass.

### Next Steps

- Run `OfflineSummarySavedArmiesDbTests` in Unity EditMode Test Runner.
- Perform one manual offline-flow smoke test that creates a persisted run,
  saves a winning summary into a slot, reloads the roster, and overwrites the
  current-defence slot once.
