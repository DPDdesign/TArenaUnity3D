# [TARENA] QA Architecture Review - PRD 018

- Task: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`
- Protocol: `_codex/tasks/QA/2026-06-13_018_CodingCompletion_BattleActionLifecycleFullMigration.md`
- Verdict: Needs focused follow-up

## Findings

1. High - Movement visual wait still has no local timeout or deterministic final snap.

   File: `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`

   `HexMap.DoUnitMoves()` now correctly waits after the final logical move, but it waits directly on `u.tosterView.AnimationIsPlaying` with no timeout. Because `MouseControler` lifecycle action bodies yield the movement coroutine, `BattleActionLifecycle` cannot interrupt this wait if the view flag never clears. That leaves one softlock path against PRD 018's timeout/release rule. Add a local movement visual wait timeout in `HexMap.DoUnitMoves()` and snap the view to the unit's final logical hex before continuing.

2. Low - Completion protocol has one changed-file path typo.

   File: `_codex/tasks/QA/2026-06-13_018_CodingCompletion_BattleActionLifecycleFullMigration.md`

   The protocol lists `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MostStupidAIEver.cs`, but the actual changed file is `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`.

## Non-Blocking Observations

- The implementation creates a practical lifecycle owner for live player/RPC/AI movement, move-and-attack, ranged attack, wait, defense, skill cleanup, and blocking presentation. That is consistent with ADR 004 and keeps validation in legacy adapters per ADR 005.
- End-of-round passive/deferred effects still run through the existing `TeamClass.NewTurn()` / `TosterHexUnit.CheckSpells()` model. Tracked presentation now blocks turn exposure after those effects start, but the deeper PRD wording about unit packages and queue recomputation remains the highest Play Mode validation risk.
- `CastManager.SetFalse()` still exists, but it is now a skill-mode signal/cleanup path rather than the direct turn release. This is acceptable for this migration if Play Mode confirms `EndSkills()` routes through lifecycle completion.

## Required Follow-Up

- Add timeout/snap handling to `HexMap.DoUnitMoves()`.
- Correct the protocol path typo.
- Re-run focused QA on the follow-up protocol.
