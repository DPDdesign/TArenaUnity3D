# [TARENA] 030_9 QA Architecture Review - Account Progress And Battle Result DB Integration

- Date: 2026-06-15
- Task: `_codex/tasks/030_9_PRD030_AccountProgress_BattleResult_DBIntegration.md`
- Verdict: pass with Unity manual verification pending

## Findings

No blocking architecture findings remain after implementation review.

## Review Notes

- Battle Result now has a dedicated SQLite-backed store instead of stopping at
  `InMemoryBattleResultStore`, so result lookup can survive service/adapter
  recreation.
- Account XP and rank are written back into `offline_accounts`, while unlock
  records use stable `DBUnlockTypeId` ids and stable text targets in
  `account_unlocks`.
- Saved-army history writes are linked to `saved_army_id` and
  `async_battle_result_id`, and the implementation soft-deactivates previous
  history rows before rewriting the same async result id.
- The controller uses a dedicated local preview database file for the prototype
  Battle Result screen, which avoids mutating unrelated runtime preview state
  while still exercising persisted reload behavior.
- The review caught and the implementation fixed an attacker-history outcome
  mapping bug for defence results before finalizing the QA handoff.

## Residual Risk

- Unity compile/import and EditMode execution were not run in this Codex pass.
- Battle Result progression thresholds are currently encoded in the store while
  the preview label text remains derived in the service; the behavior is
  consistent for this task's current thresholds, but future progression changes
  should centralize those rules to avoid drift.
- The lazy `async_battle_result_details` companion table is a pragmatic local
  extension to schema v1; if a future task introduces formal DB migrations, it
  should absorb this table into the versioned schema plan.

## Recommendation

Proceed to focused EditMode coverage for persisted Battle Result reload,
account-progress/unlock writes, and saved-army-history linkage, then run Unity
EditMode plus a manual Battle Result prefab smoke check in the local preview
flow.
