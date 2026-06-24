# [TARENA] PRD 044: Tactical Reward Stack Identity

- Status: closed-implemented-pending-unity-validation
- Type: PRD
- Area: Run Battle, Reward Handoff, Army Snapshots
- Label: closed-implemented-pending-unity-validation
- Related:
  - `_codex/tasks/022_PRD019_RunBattle.md`
  - `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
  - `_codex/tasks/archive/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
  - `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Problem Statement

The battle-to-reward handoff reconstructs the post-battle player army before
Reward Map materialization. That reconstruction currently matches tactical
runtime units back to prepared stacks by unit id.

This is fragile when an army has two stacks of the same unit type. The bridge
can assign remaining amounts or losses to the wrong stack, and then reward
generation operates on an already-wrong post-battle snapshot.

This can produce player-visible bugs:

- reward targets the wrong stack,
- losses are attributed to the wrong stack,
- preview/apply appears correct relative to bad data,
- final saved army preserves the wrong stack amounts.

## Solution

Deepen the tactical battle result handoff so stack identity survives from the
prepared run battle snapshot to the post-battle reward snapshot.

The bridge should not identify stacks only by unit id. It should prefer a stable
runtime stack identity or formation slot identity when available, and fall back
to unit-id matching only when no better identity exists.

The result should be a post-battle `RunBattleArmySnapshot` where each stack
retains the original prepared stack id and receives the correct remaining
amount.

## User Stories

1. As a player, I want two stacks of the same unit to keep separate losses, so
   that reward cards modify the right stack.
2. As a player, I want the Reward Map preview to reflect the actual battle
   result, so that I can trust the reward screen.
3. As a developer, I want battle result reconstruction to use stable stack
   identity, so that duplicate unit types do not corrupt snapshots.
4. As a developer, I want loss calculation to compare the same stack before and
   after battle, so that DB loss rows are meaningful.
5. As a QA reviewer, I want a regression test with duplicate unit stacks, so
   that unit-id matching cannot silently return.
6. As a future online-mode developer, I want the handoff payload to preserve
   stack identity, so that backend validation can reason about exact stacks.

## Implementation Decisions

- Primary coding ownership for this PRD:
  - tactical result bridge stack matching,
  - run battle snapshot handoff tests.
- Do not edit Reward Map generator or Reward Map DB store.
- Do not edit Unity scenes, prefabs, materials, `.asmdef`, or generated files.
- Prefer existing prepared stack ids and formation slot ids over unit-id-only
  matching.
- If tactical runtime objects do not currently carry prepared stack ids, add a
  small adapter/helper that can map by formation/order with explicit fallback
  behavior.
- Keep existing `RunBattleService` next-screen routing unchanged.
- Keep `OfflineRunBattleDbStore` reward materialization trigger unchanged.
- Any fallback to unit-id matching should be documented in code comments and
  covered by tests only as a legacy fallback.

## Testing Decisions

- Add or update EditMode tests that exercise the reconstruction logic without
  requiring a Unity scene.
- If the current bridge helper is private and scene-bound, extract a small
  pure helper module for stack amount reconciliation and test that module.
- Test duplicate unit stacks with different before/after amounts.
- Test missing tactical unit fallback behavior.
- Test that existing single-stack/unit-id behavior remains unchanged.
- Prior art:
  - `RunBattleServiceTests`,
  - `OfflineRunBattleRewardDbTests`,
  - `PRD37MaterializedRunGenerationTests`.

## Out of Scope

- No tactical combat gameplay changes.
- No Reward Map generation changes.
- No Reward Map persistence changes.
- No scene or prefab edits.
- No online/backend battle result authority.

## Further Notes

This PRD can run in parallel with PRD042 because it should not edit reward
generator or Reward Map persistence files.

Suggested worker ownership:

- Owns: tactical result bridge/pure helper, run battle tests.
- Avoids: Reward Map generator, Reward Map DB store, reward schema.

## Closure - 2026-06-24

Closed after implementation and integration review.

Implemented:

- pure tactical stack reconciliation helper,
- matching priority by stack id, battle-input order, then explicit legacy
  unit-id fallback,
- shared prepared-stack ordering between battle launch and result bridge,
- duplicate same-unit stack regression coverage.

Unity compilation, EditMode tests, and Play Mode validation remain manual.
