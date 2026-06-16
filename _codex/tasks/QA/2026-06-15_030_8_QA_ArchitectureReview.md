# [TARENA] 030_8 QA Architecture Review - Summary And Saved Armies DB Integration

- Date: 2026-06-15
- Task: `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`
- Verdict: pass with Unity manual verification pending

## Findings

No blocking architecture findings remain after implementation review.

## Review Notes

- Summary Value and Saved Armies now share the same offline DB persistence path
  instead of separate in-memory state.
- `run_summaries` and `run_summary_entries` are persisted and can rebuild the
  summary screen after service/adapter recreation.
- Saving from Summary Value uses the pre-final snapshot as the saved-army
  source and does not reuse the post-final losses snapshot.
- Overwrite creates a new `saved_army_id`, deactivates the previous saved army,
  updates the slot assignment, and clears current defence when required.
- The review caught and the implementation fixed an unlock-count persistence
  bug where slot saves could have forced the account unlock count back to 8.
- The Summary Value mock controller keeps an intentional in-memory fallback for
  non-persisted sample run ids so the standalone prefab can still be opened
  safely outside the full offline run flow.

## Residual Risk

- Unity compile/import and EditMode execution were not run in this Codex pass.
- The Saved Armies screen still exposes the PRD26 seed-army bridge for
  development convenience; PRD030_8 does not rely on that path, but it remains
  adjacent legacy surface.

## Recommendation

Proceed to Unity EditMode execution and a manual offline-flow smoke check that
creates a persisted run, saves a winning summary into a slot, reloads the
summary/roster, and overwrites the current defence slot once.
