# [TARENA] Follow-Up Coding Completion - PRD 018

- Task: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`
- Previous QA: `_codex/tasks/QA/2026-06-13_1630_018_QA_ArchitectureReview.md`
- Agent: Coding Agent
- Date: 2026-06-13

## Follow-Up Fixes

### `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`

- Added `UnitMoveVisualWaitTimeoutSeconds`.
- Moved the `TosterView.AnimationIsPlaying` wait into `WaitForTosterVisualMovement(...)`.
- If visual movement stays active past the timeout, the view snaps to the unit's final logical hex, clears `AnimationIsPlaying`, logs a warning, and lets lifecycle completion continue.

### `_codex/tasks/QA/2026-06-13_018_CodingCompletion_BattleActionLifecycleFullMigration.md`

- Corrected the changed-file path for `MostStupidAIEver.cs`.

## Static Verification

- Rechecked the live action-bypass search for `StartCoroutine(DoMoves...)`, `StartCoroutine(DoMoveAndAttack...)`, and `MC.StartCoroutine(...)`.
- Result: remaining matches are comments only.
- Rechecked changed-file brace counts. Counts match on changed files except `MostStupidAIEver.cs`, where the mismatch comes from pre-existing braces inside a block comment.

## Automatic Tests

- No EditMode tests added.
- Unity compile and Play Mode checks remain user-side per project rules.

## QA Request

Please verify that the follow-up resolves the movement softlock/timeout finding and the protocol path typo.
