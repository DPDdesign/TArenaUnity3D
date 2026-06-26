# [TARENA] 050 PRD Battle Action API Full Migration Purge

- Status: active close scope split out; use `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`
- Type: implementation PRD stub
- Area: tactical battle, action validation, execution, AI, skills, cleanup
- Owner: Coding Agent
- Historical full PRD: `_codex/tasks/archive/050_PRD_BattleActionAPI_FullMigrationPurge_HISTORICAL.md`
- Completed-state summary: `_codex/tasks/050_COMPLETED_STATE.md`

## Current Routing

This file intentionally stays short so task 050 does not overload future coding
prompts.

For the final close pass, read:

- `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`
- `_codex/tasks/050_COMPLETED_STATE.md`

Do not read the historical full PRD by default. Open it only if the active close
brief is ambiguous, conflicts with code, or a decision must be recovered from
the original PRD text.

## Goal

Fully close PRD050 by routing every tactical runtime action through:

- `BattleActionUse`
- `BattleAction`
- `BattleActionResult`

The remaining open work is the legacy skill / `CastManager` / passive-trap-
automatic-action runtime split described in
`_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`.

## Closure Rule

Coding Agent completion notes must go to a new file under `_codex/tasks/QA/`.
Do not append another implementation-history block to this stub.

